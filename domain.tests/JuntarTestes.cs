using System.Linq;
using NUnit.Framework;
using static VrPiano.Dominio.Testes.Cenario;

namespace VrPiano.Dominio.Testes
{
    // Juntar: depois da memória da lição N, as frases das lições 1 a N de memória (06 §3).
    public class JuntarTestes
    {
        static long AteJuntar(SessaoDeLicao s)
        {
            var t = AteAMemoria(s, 1000);
            t = TocarTudo(s, t);
            s.Tick(t + 1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            s.Coletar();
            return t + 1000;
        }

        static long Tocar(SessaoDeLicao s, long t, int quantas)
        {
            for (var i = 0; i < quantas; i++, t += 500) s.NoteOn(s.Alvo.Nota, 70, t);
            return t;
        }

        [Test]
        public void T_DOM_39_memoria_da_licao_leva_a_juntar_com_as_frases_anteriores()
        {
            var s = NaGuiada("L03", juntar: true);
            AteJuntar(s);
            var esperado = new[] { "L01", "L02", "L03" }.SelectMany(l => Passo.DaMusica(Musicas[l]).Select(p => p.Nota));
            Assert.That(s.Cadeia!.Select(p => p.Nota), Is.EqualTo(esperado));
            Assert.That(s.EstrelasDaExecucao, Is.EqualTo(3));
        }

        [Test]
        public void T_DOM_39b_licao_1_nao_tem_o_que_juntar()
        {
            var s = NaGuiada("L01", juntar: true);
            var t = AteAMemoria(s, 1000);
            TocarTudo(s, t);
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
        }

        [Test]
        public void T_DOM_39c_a_licao_12_junta_a_musica_inteira()
        {
            var s = Sessao("L12", juntar: true);
            Assert.That(s.Cadeia!.Select(p => p.Nota), Is.EqualTo(Passo.DaMusica(Musicas["SONG1"]).Select(p => p.Nota)));
        }

        [Test]
        public void T_DOM_40_erro_volta_ao_comeco_da_frase_e_nao_tira_estrela()
        {
            var progresso = new Progresso();
            var s = NaGuiada("L03", progresso, juntar: true);
            var t = AteJuntar(s);
            t = Tocar(s, t, 5);                         // frase 1 inteira e a primeira nota da frase 2
            Assert.That(s.PassoAtual, Is.EqualTo(5));
            s.NoteOn(s.Alvo.Nota + 1, 70, t);
            Assert.That(s.Coletar().De<JuntarFalhou>().Single().FalhasDaFrase, Is.EqualTo(1));
            Assert.That(s.Fase, Is.EqualTo(Fase.RetornoDeErro));
            s.Tick(t + 1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            Assert.That(s.PassoAtual, Is.EqualTo(4));
            Assert.That(progresso.Stars["L03"], Is.EqualTo(3));
        }

        [Test]
        public void T_DOM_41_tres_falhas_na_frase_fazem_cada_tecla_piscar_ate_a_frase_seguinte()
        {
            var s = NaGuiada("L03", juntar: true);
            var t = AteJuntar(s);
            for (var i = 0; i < 3; i++, t += 2000)
            {
                s.NoteOn(s.Alvo.Nota + 1, 70, t);
                s.Tick(t + 1000);
            }
            Assert.That(s.Coletar().De<DicaDeMemoria>().Select(d => d.Nota), Is.EqualTo(new[] { 60 }));
            t = Tocar(s, t, 3);
            Assert.That(s.Coletar().De<DicaDeMemoria>().Count, Is.EqualTo(3));
            s.NoteOn(s.Alvo.Nota, 70, t);               // última nota da frase 1
            var eventos = s.Coletar();
            Assert.That(eventos.De<FraseJuntada>().Single().Frase, Is.EqualTo(1));
            Assert.That(eventos.De<DicaDeMemoria>(), Is.Empty);
        }

        [Test]
        public void T_DOM_42_concluir_grava_ate_onde_a_pessoa_toca_de_cor_e_so_sobe()
        {
            var progresso = new Progresso { Chain = 5 };
            var s = NaGuiada("L03", progresso, juntar: true);
            var t = AteJuntar(s);
            Tocar(s, t, s.Cadeia!.Count);
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
            Assert.That(s.Coletar().De<CadeiaConcluida>().Single().AteALicao, Is.EqualTo(3));
            Assert.That(progresso.Chain, Is.EqualTo(5));

            var novo = new Progresso();
            s = NaGuiada("L02", novo, juntar: true);
            t = AteJuntar(s);
            Tocar(s, t, s.Cadeia!.Count);
            Assert.That(novo.Chain, Is.EqualTo(2));
        }

        [Test]
        public void T_DOM_43_pausar_volta_ao_comeco_da_frase_sem_somar_falha()
        {
            var s = NaGuiada("L03", juntar: true);
            var t = AteJuntar(s);
            t = Tocar(s, t, 5);
            s.FocoPerdido(t);
            s.Continuar(t + 100);
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            Assert.That(s.PassoAtual, Is.EqualTo(4));
            Assert.That(s.Coletar().De<JuntarFalhou>(), Is.Empty);
        }

        [Test]
        public void T_DOM_44_sair_de_juntar_guarda_as_estrelas_e_nao_grava_a_cadeia()
        {
            var progresso = new Progresso();
            var s = NaGuiada("L03", progresso, juntar: true);
            var t = AteJuntar(s);
            s.Sair(t);
            Assert.That(s.Fase, Is.EqualTo(Fase.Encerrada));
            Assert.That(progresso.Stars["L03"], Is.EqualTo(3));
            Assert.That(progresso.Chain, Is.EqualTo(0));
        }

        [Test]
        public void T_DOM_45_revisao_nao_junta()
        {
            var progresso = new Progresso { LastMemory = new UltimaMemoria { Item = "L03", Day = "2026-10-30" } };
            var s = Sessao("L03", progresso, revisao: true, juntar: true);
            s.Iniciar(0);
            TocarTudo(s, 1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
        }
    }
}
