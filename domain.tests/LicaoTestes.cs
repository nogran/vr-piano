using System.Linq;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    public class LicaoTestes
    {
        [Test]
        public void TDom01_LicaoInedita_PoseOk_ComecaPelaDemonstracao()
        {
            var s = Cenario.Sessao("L01");
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            var ev = s.Coletar();
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));
            Assert.That(ev.De<AlvoMudou>(), Is.Empty);
        }

        [Test]
        public void TDom01b_LicaoVista_PoseOk_VaiDiretoParaGuiada()
        {
            var progresso = new Progresso { DemoSeen = { "L01" } };
            var s = Cenario.Sessao("L01", progresso);
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            var ev = s.Coletar();
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            var alvo = ev.De<AlvoMudou>().Single();
            Assert.That((alvo.Nota, alvo.Dedo, alvo.Indice), Is.EqualTo((60, 1, 0)));
            Assert.That(ev.De<EnviarNoteOn>(), Is.Empty);
        }

        [Test]
        public void TDom02_MaoDesligadaNaPose_SegueSemEsperarPose()
        {
            var inedita = Cenario.Sessao("L01");
            inedita.Iniciar(0);
            inedita.MaoAlternada(false, 0);
            Assert.That(inedita.Fase, Is.EqualTo(Fase.Demonstracao));
            Assert.That(inedita.Coletar().De<EnviarNoteOn>(), Has.Count.EqualTo(1));

            var vista = Cenario.Sessao("L01", new Progresso { DemoSeen = { "L01" } });
            vista.Iniciar(0);
            vista.MaoAlternada(false, 0);
            Assert.That(vista.Fase, Is.EqualTo(Fase.Guiada));
        }

        [Test]
        public void TDom03_NoteOnNaPose_EIgnorado()
        {
            var s = Cenario.Sessao("L01");
            s.Iniciar(0);
            s.Coletar();
            s.NoteOn(60, 70, 100);
            var ev = s.Coletar();
            Assert.That(s.Fase, Is.EqualTo(Fase.Pose));
            Assert.That(ev.De<NotaCerta>(), Is.Empty);
            Assert.That(ev.De<NotaErrada>(), Is.Empty);
        }

        [Test]
        public void TDom04_Demonstracao_AgendaNotasEDepoisGuiada()
        {
            var s = Cenario.Sessao("L03");
            s.Iniciar(0);
            s.Ouvir(0);
            for (long t = 0; t <= 3400; t += 100) s.Tick(t);
            var ev = s.Coletar();

            var comandos = ev.Where(e => e is EnviarNoteOn || e is EnviarNoteOff)
                .Select(e => e is EnviarNoteOn on ? $"On {on.Nota} {on.Em} v{on.Velocity} c{on.Canal}" : $"Off {((EnviarNoteOff)e).Nota} {((EnviarNoteOff)e).Em}");
            Assert.That(comandos, Is.EqualTo(new[]
            {
                "On 60 0 v80 c1", "Off 60 700", "On 62 1000 v80 c1", "Off 62 1700", "On 64 2000 v80 c1", "Off 64 3400",
            }));
            Assert.That(ev.De<DemoNota>(), Has.Count.EqualTo(6));
            Assert.That(ev.De<DemoVista>().Single().Item, Is.EqualTo("L03"));
            Assert.That(ev.De<MaoSome>(), Has.Count.EqualTo(1));
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            Assert.That(s.PassoAtual, Is.EqualTo(0));
        }

        [Test]
        public void TDom05_DepoisDaDemonstracao_IgnoraOEcoPorP08()
        {
            var s = Cenario.Sessao("L01");
            s.Iniciar(0);
            s.Ouvir(0);
            s.NoteOn(60, 70, 500);
            var fim = 3000 + 1400;
            s.Tick(fim);
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            s.Coletar();

            s.NoteOn(60, 70, fim + 299);
            Assert.That(s.Coletar().De<NotaCerta>(), Is.Empty);
            s.NoteOn(60, 70, fim + 301);
            Assert.That(s.Coletar().De<NotaCerta>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void TDom06_VoltaLimpa_VaiParaTransicaoEDaUmaEstrela()
        {
            var progresso = new Progresso();
            var s = Cenario.NaGuiada("L01", progresso);
            Cenario.TocarTudo(s, 0);
            var ev = s.Coletar();
            Assert.That(ev.De<NotaCerta>(), Has.Count.EqualTo(4));
            Assert.That(ev.De<VoltaConcluida>().Single().Limpa, Is.True);
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
            var estrelas = ev.De<EstrelasMudaram>().Single();
            Assert.That((estrelas.De, estrelas.Para), Is.EqualTo((0, 1)));
            Assert.That(progresso.Stars["L01"], Is.EqualTo(1));
        }

        [Test]
        public void TDom07_VelocityFixa_LigaNoOitavoIgualEDesligaNoDiferente()
        {
            var s = Cenario.NaGuiada("L01");
            for (int i = 0; i < 7; i++) s.NoteOn(60, 100, i * 1000);
            Assert.That(s.Coletar().De<VelocityFixa>(), Is.Empty);
            s.NoteOn(60, 100, 7000);
            Assert.That(s.Coletar().De<VelocityFixa>().Single().Ligada, Is.True);
            s.NoteOn(60, 70, 8000);
            Assert.That(s.Coletar().De<VelocityFixa>().Single().Ligada, Is.False);
        }

        [Test]
        public void TDom09_ErroNaGuiada_NaoAvancaEAVoltaRecomeca()
        {
            var s = Cenario.NaGuiada("L02");
            s.NoteOn(64, 70, 0);
            var erro = s.Coletar().De<NotaErrada>().Single();
            Assert.That((erro.Tocada, erro.Esperada), Is.EqualTo((64, 60)));
            Assert.That(s.PassoAtual, Is.EqualTo(0));

            s.NoteOn(60, 70, 500);
            s.NoteOn(62, 70, 1500);
            var ev = s.Coletar();
            Assert.That(ev.De<VoltaConcluida>().Single().Limpa, Is.False);
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            Assert.That(s.PassoAtual, Is.EqualTo(0));
        }

        [Test]
        public void TDom10_FaixasDeVelocity()
        {
            var s = Cenario.NaGuiada("L01");
            var faixas = new[] { 1, 49, 50, 89, 90, 127 }.Select(v =>
            {
                s.NoteOn(60, v, 0);
                return s.Coletar().De<FaixaMudou>().Single().Faixa;
            });
            Assert.That(faixas, Is.EqualTo(new[]
            {
                FaixaDeVelocity.Leve, FaixaDeVelocity.Leve, FaixaDeVelocity.Medio,
                FaixaDeVelocity.Medio, FaixaDeVelocity.Forte, FaixaDeVelocity.Forte,
            }));
        }

        [Test]
        public void TDom12_Transicao_IgnoraNotaEComecaMemoriaEmP22()
        {
            var s = Cenario.NaGuiada("L01");
            var t = Cenario.TocarTudo(s, 0) - 1000;
            s.Coletar();
            s.NoteOn(60, 70, t + 500);
            s.Tick(t + 999);
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
            s.Tick(t + 1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Memoria));
            Assert.That(s.PassoAtual, Is.EqualTo(0));
        }

        [Test]
        public void TDom13_ErroNaMemoria_RetornoIgnoraNotasEVoltaAoPasso0()
        {
            var s = Cenario.NaGuiada("L03");
            var t = Cenario.AteAMemoria(s, 0);
            s.NoteOn(60, 70, t);
            s.Coletar();
            s.NoteOn(65, 70, t + 100);
            var falha = s.Coletar().De<TentativaFalhou>().Single();
            Assert.That((falha.Tocada, falha.Esperada), Is.EqualTo((65, 62)));
            s.NoteOn(60, 70, t + 400);
            s.Tick(t + 1099);
            Assert.That(s.Fase, Is.EqualTo(Fase.RetornoDeErro));
            s.Tick(t + 1100);
            Assert.That(s.Fase, Is.EqualTo(Fase.Memoria));
            Assert.That(s.PassoAtual, Is.EqualTo(0));
        }

        [Test]
        public void TDom14_TresFalhas_VoltaAoGuiadoEDepoisValeDuasEstrelas()
        {
            var s = Cenario.NaGuiada("L02");
            var t = Cenario.AteAMemoria(s, 0);
            for (int i = 0; i < 3; i++)
            {
                s.NoteOn(67, 70, t);
                s.Tick(t + 1000);
                t += 1000;
            }
            var ev = s.Coletar();
            Assert.That(ev.De<VoltouAoGuiado>(), Has.Count.EqualTo(1));
            Assert.That(ev.De<DemoNota>(), Is.Empty);
            Assert.That(ev.De<EnviarNoteOn>(), Is.Empty);
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));

            t = Cenario.AteAMemoria(s, t);
            s.NoteOn(60, 70, t);
            s.NoteOn(62, 70, t + 1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
            Assert.That(s.EstrelasDaExecucao, Is.EqualTo(2));
        }

        [TestCase("a", 3)]
        [TestCase("b", 2)]
        [TestCase("c", 1)]
        [TestCase("d", 0)]
        [TestCase("e", 2)]
        public void TDom15_EstrelasDaExecucao(string caso, int esperado)
        {
            var progresso = new Progresso();
            var s = Cenario.NaGuiada("L01", progresso);
            if (caso == "d")
            {
                s.NoteOn(60, 70, 0);
                s.Sair(500);
            }
            else
            {
                var t = Cenario.AteAMemoria(s, 0);
                if (caso == "c") s.Sair(t);
                else
                {
                    if (caso == "b")
                    {
                        s.NoteOn(62, 70, t);
                        s.Tick(t + 1000);
                        t += 1000;
                    }
                    if (caso == "e")
                    {
                        s.Tick(t + 3000);
                        t += 3000;
                    }
                    for (int i = 0; i < 4; i++) s.NoteOn(60, 70, t + i * 500);
                }
            }
            Assert.That(RegrasDeProgresso.Estrelas(progresso, "L01"), Is.EqualTo(esperado));
        }

        [Test]
        public void TDom16_EstrelasSalvasNuncaCaem()
        {
            var progresso = new Progresso { Stars = { ["L01"] = 3, ["L02"] = 2 } };
            var l01 = Cenario.NaGuiada("L01", progresso);
            var t = Cenario.AteAMemoria(l01, 0);
            l01.NoteOn(62, 70, t);
            l01.Tick(t + 1000);
            for (int i = 0; i < 4; i++) l01.NoteOn(60, 70, t + 1000 + i * 500);
            Assert.That(l01.EstrelasDaExecucao, Is.EqualTo(2));
            Assert.That(progresso.Stars["L01"], Is.EqualTo(3));

            var l02 = Cenario.NaGuiada("L02", progresso);
            t = Cenario.AteAMemoria(l02, 0);
            l02.NoteOn(60, 70, t);
            l02.NoteOn(62, 70, t + 1000);
            Assert.That(progresso.Stars["L02"], Is.EqualTo(3));
        }

        [Test]
        public void TDom18a_CaboNaGuiada_VoltaNoMesmoPasso()
        {
            var s = Cenario.NaGuiada("L03");
            s.NoteOn(60, 70, 0);
            s.NoteOn(62, 70, 1000);
            s.MidiDesconectado(1200);
            Assert.That(s.Fase, Is.EqualTo(Fase.Pausado));
            s.NoteOn(64, 70, 1300);
            s.MidiConectado(5000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            Assert.That(s.PassoAtual, Is.EqualTo(2));
        }

        [Test]
        public void TDom18b_CaboNaMemoria_RecomecaSemContarFalha()
        {
            var progresso = new Progresso();
            var s = Cenario.NaGuiada("L05", progresso);
            var t = Cenario.AteAMemoria(s, 0);
            s.NoteOn(60, 70, t);
            s.NoteOn(62, 70, t + 500);
            s.NoteOn(64, 70, t + 1000);
            s.MidiDesconectado(t + 1200);
            s.MidiConectado(t + 2000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Memoria));
            Assert.That(s.PassoAtual, Is.EqualTo(0));
            Assert.That(s.FalhasSeguidas, Is.EqualTo(0));
            foreach (var nota in new[] { 60, 62, 64, 62, 60 }) s.NoteOn(nota, 70, t + 2100);
            Assert.That(progresso.Stars["L05"], Is.EqualTo(3));
        }

        [Test]
        public void TDom18c_CaboNaDemonstracao_VoltaParaPoseSemMarcarVista()
        {
            var progresso = new Progresso();
            var s = Cenario.Sessao("L03", progresso);
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            s.Tick(1000);
            s.MidiDesconectado(1200);
            s.Coletar();
            s.Tick(5000);
            Assert.That(s.Coletar().OfType<EnviarNoteOn>(), Is.Empty);
            s.MidiConectado(6000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Pose));
            Assert.That(progresso.DemoSeen, Does.Not.Contain("L03"));
        }

        [Test]
        public void TDom19_FocoEContinuar_RetomaNoPasso_SairNaoMudaEstrelas()
        {
            var progresso = new Progresso();
            var s = Cenario.NaGuiada("L03", progresso);
            s.NoteOn(60, 70, 0);
            s.FocoPerdido(500);
            Assert.That(s.Fase, Is.EqualTo(Fase.Pausado));
            s.Continuar(9000);
            Assert.That((s.Fase, s.PassoAtual), Is.EqualTo((Fase.Guiada, 1)));
            s.Sair(9500);
            Assert.That(s.Fase, Is.EqualTo(Fase.Encerrada));
            Assert.That(progresso.Stars, Is.Empty);
        }

        [Test]
        public void TDom21_Musica_AlvoTrazALicaoDoTrechoEAtaqueDeDoisTemposEntreTrechos()
        {
            var s = Cenario.NaGuiada("SONG1");
            var licoes = new System.Collections.Generic.List<int>();
            long t = 0;
            foreach (var passo in s.Passos)
            {
                licoes.Add(s.Alvo.LicaoDoTrecho);
                if (passo.Indice == 4) Assert.That(s.TAtaque - s.TAcerto, Is.EqualTo(2000));
                s.NoteOn(passo.Nota, 70, t);
                t += 1000;
            }
            Assert.That(licoes.Take(4), Is.All.EqualTo(1));
            Assert.That(licoes.Skip(4).Take(2), Is.All.EqualTo(2));
            Assert.That(licoes.Skip(40), Is.All.EqualTo(12));
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
        }

        [Test]
        public void TDom31_CinzaEAtaqueElastico()
        {
            var s = Cenario.NaGuiada("L05");
            Assert.That(s.EmAtaque, Is.True);
            s.NoteOn(60, 70, 0);
            var proxima = s.Coletar().De<ProximaMudou>().Single();
            Assert.That((proxima.Nota, proxima.TAcerto, proxima.TAtaque, proxima.MesmaTecla), Is.EqualTo((62, 0L, 1000L, false)));
            s.Tick(999);
            Assert.That(s.Coletar().De<AtaqueChegou>(), Is.Empty);
            s.Tick(1000);
            Assert.That(s.Coletar().De<AtaqueChegou>().Single().Nota, Is.EqualTo(62));
            s.Tick(5000);
            Assert.That(s.Coletar(), Is.Empty);
            s.NoteOn(62, 70, 5200);
            var ev = s.Coletar();
            Assert.That(ev.De<RitmoAvaliado>().Single().Classe, Is.EqualTo(ClasseDeRitmo.Atrasado));
            var seguinte = ev.De<ProximaMudou>().Single();
            Assert.That((seguinte.Nota, seguinte.TAcerto, seguinte.TAtaque), Is.EqualTo((64, 5200L, 6200L)));
        }

        [Test]
        public void TDom32_IntensidadeDoCinza()
        {
            var p = new Parametros();
            Assert.That(p.IntensidadeDoCinza(0), Is.EqualTo(0.15).Within(1e-9));
            Assert.That(p.IntensidadeDoCinza(0.5), Is.EqualTo(0.575).Within(1e-9));
            Assert.That(p.IntensidadeDoCinza(1), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(p.IntensidadeDoCinza(-1), Is.EqualTo(0.15).Within(1e-9));
            Assert.That(p.IntensidadeDoCinza(2), Is.EqualTo(1.0).Within(1e-9));
        }

        [TestCase(849, ClasseDeRitmo.Adiantado)]
        [TestCase(850, ClasseDeRitmo.NoTempo)]
        [TestCase(1250, ClasseDeRitmo.NoTempo)]
        [TestCase(1251, ClasseDeRitmo.Atrasado)]
        public void TDom33_ClassesDeRitmo(long t, ClasseDeRitmo esperada)
        {
            var s = Cenario.NaGuiada("L02");
            s.NoteOn(60, 70, 0);
            s.NoteOn(62, 70, t);
            var ev = s.Coletar();
            Assert.That(ev.De<RitmoAvaliado>().Single().Classe, Is.EqualTo(esperada));
            var volta = ev.De<VoltaConcluida>().Single();
            Assert.That(volta.Limpa, Is.True);
            Assert.That(volta.Ritmo.Judged, Is.EqualTo(1));
        }

        [Test]
        public void TDom34_DicaDeMemoria()
        {
            var s = Cenario.NaGuiada("L03");
            var t0 = Cenario.AteAMemoria(s, 0);
            s.Coletar();
            s.Tick(t0 + 2999);
            Assert.That(s.Coletar().De<DicaDeMemoria>(), Is.Empty);
            s.Tick(t0 + 3000);
            s.Tick(t0 + 6000);
            Assert.That(s.Coletar().De<DicaDeMemoria>().Select(d => d.Nota), Is.EqualTo(new[] { 60, 60 }));
            s.NoteOn(60, 70, t0 + 6500);
            s.Tick(t0 + 9499);
            Assert.That(s.Coletar().De<DicaDeMemoria>(), Is.Empty);
            s.Tick(t0 + 9500);
            Assert.That(s.Coletar().De<DicaDeMemoria>().Single().Nota, Is.EqualTo(62));
            s.NoteOn(62, 70, t0 + 9600);
            s.NoteOn(64, 70, t0 + 9700);
            Assert.That(s.EstrelasDaExecucao, Is.EqualTo(2));
        }

        [Test]
        public void TDom35_DemonstracaoSoNaPrimeiraVez_OuvirVoltaAoPasso0()
        {
            var progresso = new Progresso();
            var primeira = Cenario.Sessao("L02", progresso);
            primeira.Iniciar(0);
            primeira.PoseMudou(true, 0);
            primeira.Tick(5000);
            Assert.That(primeira.Coletar().De<DemoVista>().Single().Item, Is.EqualTo("L02"));
            primeira.Sair(5000);

            var segunda = Cenario.Sessao("L02", progresso);
            segunda.Iniciar(0);
            segunda.PoseMudou(true, 0);
            Assert.That(segunda.Fase, Is.EqualTo(Fase.Guiada));
            segunda.NoteOn(60, 70, 100);
            segunda.Ouvir(500);
            Assert.That(segunda.Fase, Is.EqualTo(Fase.Demonstracao));
            segunda.Tick(5000);
            Assert.That((segunda.Fase, segunda.PassoAtual), Is.EqualTo((Fase.Guiada, 0)));
            Assert.That(segunda.Coletar().De<VoltaConcluida>(), Is.Empty);
        }

        [Test]
        public void TDom36_MesmaTeclaEmSeguida()
        {
            var s = Cenario.NaGuiada("L01");
            s.NoteOn(60, 70, 300);
            var proxima = s.Coletar().De<ProximaMudou>().Single();
            Assert.That((proxima.Nota, proxima.TAcerto, proxima.TAtaque, proxima.MesmaTecla), Is.EqualTo((60, 300L, 1300L, true)));
        }

        [Test]
        public void TDom37_RetomadaEmAtaqueSemAvaliarRitmo()
        {
            var s = Cenario.NaGuiada("L03");
            s.NoteOn(60, 70, 0);
            s.NoteOn(62, 70, 1000);
            s.MidiDesconectado(1500);
            s.MidiConectado(1600);
            Assert.That((s.PassoAtual, s.EmAtaque), Is.EqualTo((2, true)));
            s.Coletar();
            s.NoteOn(64, 70, 4600);
            var ev = s.Coletar();
            Assert.That(ev.De<NotaCerta>(), Has.Count.EqualTo(1));
            Assert.That(ev.De<RitmoAvaliado>(), Is.Empty);
        }

        [Test]
        public void TDom38_RitmoSalvoGuardaOMelhor()
        {
            var progresso = new Progresso { Rhythm = { ["L01"] = new Ritmo(2, 3) } };
            void Executar(long[] instantes)
            {
                var s = Cenario.NaGuiada("L01", progresso);
                foreach (var t in instantes) s.NoteOn(60, 70, t);
                var t0 = instantes.Last() + 1000;
                s.Tick(t0);
                for (int i = 0; i < 4; i++) s.NoteOn(60, 70, t0 + i * 500);
            }
            Executar(new long[] { 0, 1000, 2000, 3000 });
            Assert.That((progresso.Rhythm["L01"].OnTime, progresso.Rhythm["L01"].Judged), Is.EqualTo((3, 3)));
            Executar(new long[] { 0, 1000, 2500, 4000 });
            Assert.That((progresso.Rhythm["L01"].OnTime, progresso.Rhythm["L01"].Judged), Is.EqualTo((3, 3)));
            Assert.That(progresso.Stars["L01"], Is.EqualTo(3));
        }
    }
}
