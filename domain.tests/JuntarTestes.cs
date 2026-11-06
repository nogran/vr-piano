using System.Linq;
using NUnit.Framework;
using static VrPiano.Dominio.Testes.Cenario;

namespace VrPiano.Dominio.Testes
{
    // Juntar: depois da memória do item SONG#k, os trechos 1 a k de memória (06 §3).
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

        [Test]
        public void T_DOM_39_memoria_da_frase_leva_a_juntar_com_as_frases_anteriores()
        {
            var s = NaGuiada("BRILHA#3");
            AteJuntar(s);
            var esperado = Passo.DaMusica(Musicas["BRILHA"]).Take(21).Select(p => p.Nota);
            Assert.That(s.Cadeia!.Select(p => p.Nota), Is.EqualTo(esperado));
            Assert.That(s.EstrelasDaExecucao, Is.EqualTo(3));
        }

        [Test]
        public void T_DOM_39b_frase_1_nao_tem_o_que_juntar()
        {
            var s = NaGuiada("BRILHA#1");
            var t = AteAMemoria(s, 1000);
            TocarTudo(s, t);
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
        }

        [Test]
        public void T_DOM_39c_juntar_passa_pelas_repeticoes_do_caminho()
        {
            var s = Sessao("ODE#4");
            Assert.That(s.Passos.Select(p => p.Trecho).Distinct(), Is.EqualTo(new[] { 4 }));
            Assert.That(s.Cadeia!.Select(p => p.Trecho).Distinct(), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(s.Cadeia!, Has.Count.EqualTo(8 + 7 + 8 + 7));
            Assert.That(s.FrasesDaCadeia, Is.EqualTo(4));
        }

        [Test]
        public void T_DOM_39d_ultima_frase_da_ode_nao_inclui_os_trechos_depois_dela()
        {
            var s = Sessao("ODE#6");
            Assert.That(s.Cadeia!.Select(p => p.Trecho).Distinct(), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void T_DOM_39e_as_fronteiras_do_juntar_sao_os_trechos_mesmo_na_repeticao()
        {
            var s = NaGuiada("ODE#4");
            var t = AteJuntar(s);
            t = Tocar(s, t, 8 + 7 + 8);
            Assert.That(s.Coletar().De<FraseJuntada>().Select(f => f.Frase), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(s.PassoAtual, Is.EqualTo(23));
            Tocar(s, t, 2);
            s.NoteOn(s.Alvo.Nota + 1, 70, t + 1000);
            s.Tick(t + 2000);
            Assert.That(s.PassoAtual, Is.EqualTo(23));
        }

        // Da Guiada até o fim da memória, com a cadeia ainda inédita: a próxima fase é a Transição do Juntar.
        static (SessaoDeLicao, long) AntesDaCadeia(string item, Progresso progresso)
        {
            progresso.DemoSeen.Add(item);
            var s = Sessao(item, progresso);
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            var t = AteAMemoria(s, 1000);
            t = TocarTudo(s, t);
            s.Coletar();
            return (s, t + 1000);
        }

        [Test]
        public void T_DOM_39f_a_mao_toca_a_cadeia_antes_do_primeiro_juntar()
        {
            var progresso = JaOuviu();
            var (s, t) = AntesDaCadeia("BRILHA#2", progresso);
            s.Tick(t);
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));

            t += 120_000;
            s.Tick(t);
            var eventos = s.Coletar();
            var ligadas = eventos.De<DemoNota>().Where(d => d.Ligada).Select(d => d.Nota);
            Assert.That(ligadas, Is.EqualTo(s.Cadeia!.Select(p => p.Nota)));
            Assert.That(eventos.De<MaoSome>(), Has.Count.EqualTo(1));
            Assert.That(progresso.DemoSeen, Does.Contain(SessaoDeLicao.ChaveDaCadeia("BRILHA#2")));

            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
            s.Tick(t + 1000);
            Assert.That((s.Fase, s.PassoAtual), Is.EqualTo((Fase.Juntar, 0)));
        }

        [Test]
        public void T_DOM_39g_nota_durante_a_mao_nao_conta_e_a_demonstracao_nao_repete()
        {
            var progresso = JaOuviu();
            var (s, t) = AntesDaCadeia("BRILHA#2", progresso);
            s.Tick(t);
            s.NoteOn(s.Alvo.Nota + 1, 70, t + 10);
            Assert.That(s.Coletar().De<JuntarFalhou>(), Is.Empty);

            var (repetida, t2) = AntesDaCadeia("BRILHA#2", new Progresso { DemoSeen = { "BRILHA", SessaoDeLicao.ChaveDaCadeia("BRILHA#2") } });
            repetida.Tick(t2);
            Assert.That(repetida.Fase, Is.EqualTo(Fase.Juntar));
        }

        [Test]
        public void T_DOM_39h_pausa_durante_a_mao_recomeca_a_cadeia()
        {
            var progresso = JaOuviu();
            var (s, t) = AntesDaCadeia("BRILHA#2", progresso);
            s.Tick(t);
            s.FocoPerdido(t + 100);
            s.Continuar(t + 200);
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
            Assert.That(progresso.DemoSeen, Does.Not.Contain(SessaoDeLicao.ChaveDaCadeia("BRILHA#2")));
            s.Tick(t + 1200);
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));
        }

        [Test]
        public void T_DOM_40_erro_volta_ao_comeco_da_frase_e_nao_tira_estrela()
        {
            var progresso = JaOuviu();
            var s = NaGuiada("BRILHA#3", progresso);
            var t = AteJuntar(s);
            t = Tocar(s, t, 8);                         // frase 1 inteira e a primeira nota da frase 2
            Assert.That(s.PassoAtual, Is.EqualTo(8));
            s.NoteOn(s.Alvo.Nota + 1, 70, t);
            Assert.That(s.Coletar().De<JuntarFalhou>().Single().FalhasDaFrase, Is.EqualTo(1));
            Assert.That(s.Fase, Is.EqualTo(Fase.RetornoDeErro));
            s.Tick(t + 1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            Assert.That(s.PassoAtual, Is.EqualTo(7));
            Assert.That(progresso.Stars["BRILHA#3"], Is.EqualTo(3));
        }

        [Test]
        public void T_DOM_41_tres_falhas_na_frase_fazem_cada_tecla_piscar_ate_a_frase_seguinte()
        {
            var s = NaGuiada("BRILHA#3");
            var t = AteJuntar(s);
            for (var i = 0; i < 3; i++, t += 2000)
            {
                s.NoteOn(s.Alvo.Nota + 1, 70, t);
                s.Tick(t + 1000);
            }
            Assert.That(s.Coletar().De<DicaDeMemoria>().Select(d => d.Nota), Is.EqualTo(new[] { 60 }));
            t = Tocar(s, t, 6);
            Assert.That(s.Coletar().De<DicaDeMemoria>().Count, Is.EqualTo(6));
            s.NoteOn(s.Alvo.Nota, 70, t);               // última nota da frase 1
            var eventos = s.Coletar();
            Assert.That(eventos.De<FraseJuntada>().Single().Frase, Is.EqualTo(1));
            Assert.That(eventos.De<DicaDeMemoria>(), Is.Empty);
        }

        [Test]
        public void T_DOM_42_concluir_grava_ate_onde_a_pessoa_toca_de_cor_e_so_sobe()
        {
            var progresso = JaOuviu();
            progresso.Chain["BRILHA"] = 5;
            var s = NaGuiada("BRILHA#3", progresso);
            var t = AteJuntar(s);
            Tocar(s, t, s.Cadeia!.Count);
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
            Assert.That(s.Coletar().De<CadeiaConcluida>().Single().AteOTrecho, Is.EqualTo(3));
            Assert.That(progresso.Chain["BRILHA"], Is.EqualTo(5));

            var novo = JaOuviu();
            s = NaGuiada("BRILHA#2", novo);
            t = AteJuntar(s);
            Tocar(s, t, s.Cadeia!.Count);
            Assert.That(novo.Chain["BRILHA"], Is.EqualTo(2));
            Assert.That(RegrasDeProgresso.Cadeia(novo, "ODE"), Is.EqualTo(0));
        }

        [Test]
        public void T_DOM_42b_memoria_da_frase_1_ja_e_a_cadeia_ate_o_trecho_1()
        {
            var progresso = JaOuviu();
            var s = NaGuiada("BRILHA#1", progresso);
            TocarTudo(s, AteAMemoria(s, 1000));
            Assert.That(progresso.Chain["BRILHA"], Is.EqualTo(1));
        }

        [Test]
        public void T_DOM_43_pausar_volta_ao_comeco_da_frase_sem_somar_falha()
        {
            var s = NaGuiada("BRILHA#3");
            var t = AteJuntar(s);
            t = Tocar(s, t, 8);
            s.FocoPerdido(t);
            s.Continuar(t + 100);
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            Assert.That(s.PassoAtual, Is.EqualTo(7));
            Assert.That(s.Coletar().De<JuntarFalhou>(), Is.Empty);
        }

        [Test]
        public void T_DOM_44_sair_de_juntar_guarda_as_estrelas_e_nao_grava_a_cadeia()
        {
            var progresso = JaOuviu();
            var s = NaGuiada("BRILHA#3", progresso);
            var t = AteJuntar(s);
            s.Sair(t);
            Assert.That(s.Fase, Is.EqualTo(Fase.Encerrada));
            Assert.That(progresso.Stars["BRILHA#3"], Is.EqualTo(3));
            Assert.That(RegrasDeProgresso.Cadeia(progresso, "BRILHA"), Is.EqualTo(0));
        }

        [Test]
        public void T_DOM_45_revisao_termina_no_fim_da_cadeia_sem_outro_juntar()
        {
            var progresso = JaOuviu();
            progresso.Chain["BRILHA"] = 3;
            progresso.LastMemory = new UltimaMemoria { Item = "BRILHA#3", Day = "2026-10-30" };
            var s = Sessao("BRILHA", progresso, revisao: true);
            s.Iniciar(0);
            Tocar(s, 1000, s.Cadeia!.Count);
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
        }
    }

    // Música inteira (RF-57, RF-59): de memória, sem vez guiada, com as regras do Juntar.
    public class MusicaInteiraTestes
    {
        [Test]
        public void TDom50_SaiDaPoseDiretoParaOJuntarSemGuiada()
        {
            var s = Sessao("BRILHA");
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
            var ev = s.Coletar();
            Assert.That(ev.De<AlvoMudou>(), Is.Empty);
            Assert.That(ev.De<EnviarNoteOn>(), Is.Empty);
            s.Tick(999);
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
            s.Tick(1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            Assert.That(s.Cadeia!, Has.Count.EqualTo(42));
            Assert.That(s.Cadeia!.Select(p => p.Frase).Distinct(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void TDom51_SemErroESemDicaValeTresEGravaTudo()
        {
            var progresso = JaOuviu();
            var s = Sessao("BRILHA", progresso);
            var t = AteAMusicaInteira(s);
            Tocar(s, t, 42);
            var ev = s.Coletar();
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
            Assert.That(s.EstrelasDaExecucao, Is.EqualTo(3));
            Assert.That(progresso.Stars["BRILHA"], Is.EqualTo(3));
            Assert.That(progresso.Chain["BRILHA"], Is.EqualTo(6));
            Assert.That(progresso.LastMemory!.Item, Is.EqualTo("BRILHA"));
            Assert.That(ev.De<DiaSeguidoMudou>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void TDom52_ErroVoltaAoComecoDoTrechoEValeDuas()
        {
            var progresso = JaOuviu();
            var s = Sessao("BRILHA", progresso);
            var t = AteAMusicaInteira(s);
            t = Tocar(s, t, 16);                        // dois trechos e 2 notas do terceiro
            s.NoteOn(s.Alvo.Nota + 1, 70, t);
            Assert.That(s.Coletar().De<JuntarFalhou>(), Has.Count.EqualTo(1));
            s.Tick(t + 1000);
            Assert.That(s.PassoAtual, Is.EqualTo(14));
            Tocar(s, t + 1000, 42 - 14);
            Assert.That(s.EstrelasDaExecucao, Is.EqualTo(2));
            Assert.That(progresso.Stars["BRILHA"], Is.EqualTo(2));
        }

        [Test]
        public void TDom53_DicaDeSilencioValeDuas()
        {
            var s = Sessao("BRILHA");
            var t = AteAMusicaInteira(s);
            s.Tick(t + 3000);
            Assert.That(s.Coletar().De<DicaDeMemoria>().Single().Nota, Is.EqualTo(60));
            Tocar(s, t + 3100, 42);
            Assert.That(s.EstrelasDaExecucao, Is.EqualTo(2));
        }

        [Test]
        public void TDom54_TresFalhasNoTrechoFazemCadaTeclaPiscar()
        {
            var s = Sessao("BRILHA");
            var t = AteAMusicaInteira(s);
            for (var i = 0; i < 3; i++, t += 2000)
            {
                s.NoteOn(s.Alvo.Nota + 1, 70, t);
                s.Tick(t + 1000);
            }
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            Assert.That(s.Coletar().De<DicaDeMemoria>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void TDom55_SairNoMeioNaoGravaEstrelas()
        {
            var progresso = JaOuviu();
            var s = Sessao("BRILHA", progresso);
            var t = AteAMusicaInteira(s);
            s.Sair(Tocar(s, t, 10));
            Assert.That(RegrasDeProgresso.Estrelas(progresso, "BRILHA"), Is.EqualTo(0));
            Assert.That(RegrasDeProgresso.Cadeia(progresso, "BRILHA"), Is.EqualTo(0));
        }

        [Test]
        public void TDom56_OuvirNaPoseTocaAMusicaEDepoisComeca()
        {
            var s = Sessao("BRILHA");
            s.Iniciar(0);
            s.Ouvir(0);
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));
            s.Tick(48000);
            Assert.That(s.Coletar().De<EnviarNoteOn>(), Has.Count.EqualTo(42));
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
        }
    }
}
