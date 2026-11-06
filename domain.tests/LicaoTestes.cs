using System.Linq;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    public class LicaoTestes
    {
        [Test]
        public void TDom01_LicaoInedita_PoseOk_ComecaPelaDemonstracao()
        {
            var s = Cenario.Sessao("BRILHA#1");
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            var ev = s.Coletar();
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));
            Assert.That(ev.De<AlvoMudou>(), Is.Empty);
        }

        [Test]
        public void TDom01b_LicaoVista_PoseOk_VaiDiretoParaGuiada()
        {
            var progresso = new Progresso { DemoSeen = { "BRILHA", "BRILHA#1" } };
            var s = Cenario.Sessao("BRILHA#1", progresso);
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
            var inedita = Cenario.Sessao("BRILHA#1");
            inedita.Iniciar(0);
            inedita.MaoAlternada(false, 0);
            Assert.That(inedita.Fase, Is.EqualTo(Fase.Demonstracao));
            Assert.That(inedita.Coletar().De<EnviarNoteOn>(), Has.Count.EqualTo(1));

            var vista = Cenario.Sessao("BRILHA#1", new Progresso { DemoSeen = { "BRILHA", "BRILHA#1" } });
            vista.Iniciar(0);
            vista.MaoAlternada(false, 0);
            Assert.That(vista.Fase, Is.EqualTo(Fase.Guiada));
        }

        [Test]
        public void TDom03_NoteOnNaPose_EIgnorado()
        {
            var s = Cenario.Sessao("BRILHA#1");
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
            var s = Cenario.Sessao("BRILHA#1");
            s.Iniciar(0);
            s.Ouvir(0);
            for (long t = 0; t <= 7400; t += 100) s.Tick(t);
            var ev = s.Coletar();

            var comandos = ev.Where(e => e is EnviarNoteOn || e is EnviarNoteOff)
                .Select(e => e is EnviarNoteOn on ? $"On {on.Nota} {on.Em} v{on.Velocity} c{on.Canal}" : $"Off {((EnviarNoteOff)e).Nota} {((EnviarNoteOff)e).Em}");
            Assert.That(comandos, Is.EqualTo(new[]
            {
                "On 60 0 v80 c1", "Off 60 700", "On 60 1000 v80 c1", "Off 60 1700", "On 67 2000 v80 c1", "Off 67 2700",
                "On 67 3000 v80 c1", "Off 67 3700", "On 69 4000 v80 c1", "Off 69 4700", "On 69 5000 v80 c1", "Off 69 5700",
                "On 67 6000 v80 c1", "Off 67 7400",
            }));
            Assert.That(ev.De<DemoNota>(), Has.Count.EqualTo(14));
            Assert.That(ev.De<DemoVista>().Single().Item, Is.EqualTo("BRILHA#1"));
            Assert.That(ev.De<MaoSome>(), Has.Count.EqualTo(1));
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            Assert.That(s.PassoAtual, Is.EqualTo(0));
        }

        [Test]
        public void TDom05_DepoisDaDemonstracao_IgnoraOEcoPorP08()
        {
            var s = Cenario.Sessao("BRILHA#1");
            s.Iniciar(0);
            s.Ouvir(0);
            s.NoteOn(60, 70, 500);
            var fim = 6000 + 1400;
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
            var progresso = Cenario.JaOuviu();
            var s = Cenario.NaGuiada("BRILHA#1", progresso);
            Cenario.TocarTudo(s, 0);
            var ev = s.Coletar();
            Assert.That(ev.De<NotaCerta>(), Has.Count.EqualTo(7));
            Assert.That(ev.De<VoltaConcluida>().Single().Limpa, Is.True);
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
            var estrelas = ev.De<EstrelasMudaram>().Single();
            Assert.That((estrelas.De, estrelas.Para), Is.EqualTo((0, 1)));
            Assert.That(progresso.Stars["BRILHA#1"], Is.EqualTo(1));
        }

        [Test]
        public void TDom07_VelocityFixa_LigaNoOitavoIgualEDesligaNoDiferente()
        {
            var s = Cenario.NaGuiada("BRILHA#1");
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
            var s = Cenario.NaGuiada("BRILHA#1");
            s.NoteOn(64, 70, 0);
            var erro = s.Coletar().De<NotaErrada>().Single();
            Assert.That((erro.Tocada, erro.Esperada), Is.EqualTo((64, 60)));
            Assert.That(s.PassoAtual, Is.EqualTo(0));

            Cenario.TocarTudo(s, 500);
            var ev = s.Coletar();
            Assert.That(ev.De<VoltaConcluida>().Single().Limpa, Is.False);
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            Assert.That(s.PassoAtual, Is.EqualTo(0));
        }

        [Test]
        public void TDom10_FaixasDeVelocity()
        {
            var s = Cenario.NaGuiada("BRILHA#1");
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
            var s = Cenario.NaGuiada("BRILHA#1");
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
            var s = Cenario.NaGuiada("BRILHA#1");
            var t = Cenario.AteAMemoria(s, 0);
            s.NoteOn(60, 70, t);
            s.Coletar();
            s.NoteOn(65, 70, t + 100);
            var falha = s.Coletar().De<TentativaFalhou>().Single();
            Assert.That((falha.Tocada, falha.Esperada), Is.EqualTo((65, 60)));
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
            var s = Cenario.NaGuiada("BRILHA#1");
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
            Cenario.TocarOQueFalta(s, t);
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
            var progresso = Cenario.JaOuviu();
            var s = Cenario.NaGuiada("BRILHA#1", progresso);
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
                    Cenario.TocarOQueFalta(s, t);
                }
            }
            Assert.That(RegrasDeProgresso.Estrelas(progresso, "BRILHA#1"), Is.EqualTo(esperado));
        }

        [Test]
        public void TDom16_EstrelasSalvasNuncaCaem()
        {
            var progresso = new Progresso { Stars = { ["BRILHA#1"] = 3, ["ODE#1"] = 2 } };
            var frase1 = Cenario.NaGuiada("BRILHA#1", progresso);
            var t = Cenario.AteAMemoria(frase1, 0);
            frase1.NoteOn(62, 70, t);
            frase1.Tick(t + 1000);
            Cenario.TocarOQueFalta(frase1, t + 1000);
            Assert.That(frase1.EstrelasDaExecucao, Is.EqualTo(2));
            Assert.That(progresso.Stars["BRILHA#1"], Is.EqualTo(3));

            var ode = Cenario.NaGuiada("ODE#1", progresso);
            t = Cenario.AteAMemoria(ode, 0);
            Cenario.TocarOQueFalta(ode, t);
            Assert.That(progresso.Stars["ODE#1"], Is.EqualTo(3));
        }

        [Test]
        public void TDom18a_CaboNaGuiada_VoltaNoMesmoPasso()
        {
            var s = Cenario.NaGuiada("BRILHA#1");
            s.NoteOn(60, 70, 0);
            s.NoteOn(60, 70, 1000);
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
            var progresso = Cenario.JaOuviu();
            var s = Cenario.NaGuiada("BRILHA#1", progresso);
            var t = Cenario.AteAMemoria(s, 0);
            Cenario.Tocar(s, t, 3);
            s.MidiDesconectado(t + 1200);
            s.MidiConectado(t + 2000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Memoria));
            Assert.That(s.PassoAtual, Is.EqualTo(0));
            Assert.That(s.FalhasSeguidas, Is.EqualTo(0));
            Cenario.TocarOQueFalta(s, t + 2100);
            Assert.That(progresso.Stars["BRILHA#1"], Is.EqualTo(3));
        }

        [Test]
        public void TDom18c_CaboNaDemonstracao_VoltaParaPoseSemMarcarVista()
        {
            var progresso = Cenario.JaOuviu();
            var s = Cenario.Sessao("BRILHA#1", progresso);
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            s.Tick(1000);
            s.MidiDesconectado(1200);
            s.Coletar();
            s.Tick(9000);
            Assert.That(s.Coletar().OfType<EnviarNoteOn>(), Is.Empty);
            s.MidiConectado(10000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Pose));
            Assert.That(progresso.DemoSeen, Does.Not.Contain("BRILHA#1"));
        }

        [Test]
        public void TDom19_FocoEContinuar_RetomaNoPasso_SairNaoMudaEstrelas()
        {
            var progresso = Cenario.JaOuviu();
            var s = Cenario.NaGuiada("BRILHA#1", progresso);
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
        public void TDom21_AlvoTrazTrechoEFraseEAtaqueSegueOsTempos()
        {
            var s = Cenario.Sessao("ODE#4", new Progresso { DemoSeen = { "ODE", "ODE#4" } });
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            var alvo = s.Coletar().De<AlvoMudou>().Single();
            Assert.That((alvo.Nota, alvo.Trecho, alvo.Frase), Is.EqualTo((60, 4, 3)));

            long t = 0;
            var deltas = new System.Collections.Generic.List<long?>();
            foreach (var passo in s.Passos)
            {
                deltas.Add(s.TAtaque - s.TAcerto);
                s.NoteOn(passo.Nota, 70, t);
                t += 1000;
            }
            Assert.That(deltas, Is.EqualTo(new long?[] { null, 1000, 1000, 1000, 1000, 1500, 500 }));
            Assert.That(s.Fase, Is.EqualTo(Fase.Transicao));
        }

        [Test]
        public void TDom21b_EntreTrechosAMusicaRespiraNaUltimaNota()
        {
            var passos = Passo.DaMusica(Cenario.Musicas["BRILHA"]);
            Assert.That(passos[6].Trecho, Is.EqualTo(1));
            Assert.That(passos[7].Trecho, Is.EqualTo(2));
            Assert.That(passos[7].DeltaMs, Is.EqualTo(2000));
            Assert.That(passos.Skip(1).Sum(p => p.DeltaMs), Is.EqualTo(46000));
        }

        [Test]
        public void TDom31_CinzaEAtaqueElastico()
        {
            var s = Cenario.NaGuiada("ODE#6");
            Assert.That(s.EmAtaque, Is.True);
            s.NoteOn(62, 70, 0);
            var proxima = s.Coletar().De<ProximaMudou>().Single();
            Assert.That((proxima.Nota, proxima.TAcerto, proxima.TAtaque, proxima.MesmaTecla), Is.EqualTo((64, 0L, 1000L, false)));
            s.Tick(999);
            Assert.That(s.Coletar().De<AtaqueChegou>(), Is.Empty);
            s.Tick(1000);
            Assert.That(s.Coletar().De<AtaqueChegou>().Single().Nota, Is.EqualTo(64));
            s.Tick(5000);
            Assert.That(s.Coletar(), Is.Empty);
            s.NoteOn(64, 70, 5200);
            var ev = s.Coletar();
            Assert.That(ev.De<RitmoAvaliado>().Single().Classe, Is.EqualTo(ClasseDeRitmo.Atrasado));
            var seguinte = ev.De<ProximaMudou>().Single();
            Assert.That((seguinte.Nota, seguinte.TAcerto, seguinte.TAtaque), Is.EqualTo((65, 5200L, 5700L)));
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
            var s = Cenario.NaGuiada("BRILHA#1");
            s.NoteOn(60, 70, 0);
            s.NoteOn(60, 70, t);
            Assert.That(s.Coletar().De<RitmoAvaliado>().Single().Classe, Is.EqualTo(esperada));
            foreach (var passo in s.Passos.Skip(2)) s.NoteOn(passo.Nota, 70, t += 1000);
            var volta = s.Coletar().De<VoltaConcluida>().Single();
            Assert.That(volta.Limpa, Is.True);
            Assert.That(volta.Ritmo.Judged, Is.EqualTo(6));
        }

        [Test]
        public void TDom34_DicaDeMemoria()
        {
            var s = Cenario.NaGuiada("BRILHA#1");
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
            Assert.That(s.Coletar().De<DicaDeMemoria>().Single().Nota, Is.EqualTo(60));
            Cenario.TocarOQueFalta(s, t0 + 9600);
            Assert.That(s.EstrelasDaExecucao, Is.EqualTo(2));
        }

        [Test]
        public void TDom35_DemonstracaoSoNaPrimeiraVez_OuvirVoltaAoPasso0()
        {
            var progresso = Cenario.JaOuviu();
            var primeira = Cenario.Sessao("BRILHA#2", progresso);
            primeira.Iniciar(0);
            primeira.PoseMudou(true, 0);
            primeira.Tick(9000);
            Assert.That(primeira.Coletar().De<DemoVista>().Single().Item, Is.EqualTo("BRILHA#2"));
            primeira.Sair(9000);

            var segunda = Cenario.Sessao("BRILHA#2", progresso);
            segunda.Iniciar(0);
            segunda.PoseMudou(true, 0);
            Assert.That(segunda.Fase, Is.EqualTo(Fase.Guiada));
            segunda.NoteOn(65, 70, 100);
            segunda.Ouvir(500);
            Assert.That(segunda.Fase, Is.EqualTo(Fase.Demonstracao));
            segunda.Tick(9000);
            Assert.That((segunda.Fase, segunda.PassoAtual), Is.EqualTo((Fase.Guiada, 0)));
            Assert.That(segunda.Coletar().De<VoltaConcluida>(), Is.Empty);
        }

        [Test]
        public void TDom36_MesmaTeclaEmSeguida()
        {
            var s = Cenario.NaGuiada("BRILHA#1");
            s.NoteOn(60, 70, 300);
            var proxima = s.Coletar().De<ProximaMudou>().Single();
            Assert.That((proxima.Nota, proxima.TAcerto, proxima.TAtaque, proxima.MesmaTecla), Is.EqualTo((60, 300L, 1300L, true)));
        }

        [Test]
        public void TDom37_RetomadaEmAtaqueSemAvaliarRitmo()
        {
            var s = Cenario.NaGuiada("BRILHA#1");
            s.NoteOn(60, 70, 0);
            s.NoteOn(60, 70, 1000);
            s.MidiDesconectado(1500);
            s.MidiConectado(1600);
            Assert.That((s.PassoAtual, s.EmAtaque), Is.EqualTo((2, true)));
            s.Coletar();
            s.NoteOn(67, 70, 4600);
            var ev = s.Coletar();
            Assert.That(ev.De<NotaCerta>(), Has.Count.EqualTo(1));
            Assert.That(ev.De<RitmoAvaliado>(), Is.Empty);
        }

        [Test]
        public void TDom38_RitmoSalvoGuardaOMelhor()
        {
            var progresso = new Progresso { Rhythm = { ["BRILHA#1"] = new Ritmo(2, 3) } };
            void Executar(long[] instantes)
            {
                var s = Cenario.NaGuiada("BRILHA#1", progresso);
                for (int i = 0; i < instantes.Length; i++) s.NoteOn(s.Passos[i].Nota, 70, instantes[i]);
                var t0 = instantes.Last() + 1000;
                s.Tick(t0);
                Cenario.TocarOQueFalta(s, t0);
            }
            Executar(new long[] { 0, 1000, 2000, 3000, 4000, 5000, 6000 });
            Assert.That((progresso.Rhythm["BRILHA#1"].OnTime, progresso.Rhythm["BRILHA#1"].Judged), Is.EqualTo((6, 6)));
            Executar(new long[] { 0, 1000, 2500, 4000, 5500, 7000, 8500 });
            Assert.That((progresso.Rhythm["BRILHA#1"].OnTime, progresso.Rhythm["BRILHA#1"].Judged), Is.EqualTo((6, 6)));
            Assert.That(progresso.Stars["BRILHA#1"], Is.EqualTo(3));
        }

        [Test]
        public void RF54_PrimeiraVezOuveAMusicaInteiraAntesDaFrase1()
        {
            var progresso = new Progresso();
            var s = Cenario.Sessao("BRILHA#1", progresso);
            s.Iniciar(0);
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));
            Assert.That(s.Etiqueta.Tipo, Is.EqualTo(TipoDeEtiqueta.MusicaInteira));
            s.Tick(47000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));
            s.Tick(47400);
            var ev = s.Coletar();
            Assert.That(ev.De<EnviarNoteOn>(), Has.Count.EqualTo(42));
            Assert.That(ev.De<DemoVista>().Single().Item, Is.EqualTo("BRILHA"));
            Assert.That(s.Fase, Is.EqualTo(Fase.Pose));
            Assert.That(s.AncoraDaMao, Is.EqualTo(Mao.AncoraDaPose));
            Assert.That(s.Etiqueta.Tipo, Is.EqualTo(TipoDeEtiqueta.Frase));

            s.PoseMudou(true, 48000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao), "a frase inédita ainda tem a demonstração dela");
            s.Tick(57000);
            Assert.That(s.Coletar().De<EnviarNoteOn>(), Has.Count.EqualTo(7));
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            Assert.That(progresso.DemoSeen, Is.EqualTo(new[] { "BRILHA", "BRILHA#1" }));

            var depois = Cenario.Sessao("BRILHA#1", progresso);
            depois.Iniciar(0);
            Assert.That(depois.Fase, Is.EqualTo(Fase.Pose));
        }

        [Test]
        public void RF54_SoAFrase1OuveAMusicaEAPausaRecomecaAAudicao()
        {
            var novaFrase = Cenario.Sessao("BRILHA#2", new Progresso());
            novaFrase.Iniciar(0);
            Assert.That(novaFrase.Fase, Is.EqualTo(Fase.Pose));

            var progresso = new Progresso();
            var s = Cenario.Sessao("BRILHA#1", progresso);
            s.Iniciar(0);
            s.Tick(9000);
            s.FocoPerdido(9100);
            s.Coletar();
            s.Continuar(10000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));
            Assert.That(s.Coletar().De<EnviarNoteOn>().Single().Nota, Is.EqualTo(60));
            Assert.That(progresso.DemoSeen, Is.Empty);
        }

        [Test]
        public void RF54_OuvirAMusicaNoPainelNaoMudaNadaEEncerra()
        {
            var progresso = new Progresso { DemoSeen = { "BRILHA" }, Stars = { ["BRILHA#1"] = 2 } };
            var s = new SessaoDeLicao("BRILHA", Cenario.Repertorio, progresso, Cenario.Hoje, soOuvir: true);
            s.Iniciar(0);
            Assert.That(s.Fase, Is.EqualTo(Fase.Demonstracao));
            s.Tick(47400);
            Assert.That(s.Coletar().De<EnviarNoteOn>(), Has.Count.EqualTo(42));
            Assert.That(s.Fase, Is.EqualTo(Fase.Encerrada));
            Assert.That(progresso.Stars, Has.Count.EqualTo(1));
            Assert.That(progresso.LastMemory, Is.Null);
        }

        [TestCase("BRILHA#1", TipoDeEtiqueta.Frase, 1)]
        [TestCase("BRILHA#3", TipoDeEtiqueta.Frase, 3)]
        [TestCase("ODE#4", TipoDeEtiqueta.Frase, 3)]
        [TestCase("ODE#6", TipoDeEtiqueta.Frase, 5)]
        [TestCase("ODE", TipoDeEtiqueta.MusicaInteira, null)]
        public void RF65_EtiquetaDoCard(string item, TipoDeEtiqueta tipo, int? frase)
        {
            var etiqueta = Cenario.Sessao(item).Etiqueta;
            Assert.That((etiqueta.Tipo, etiqueta.Frase), Is.EqualTo((tipo, frase)));
            Assert.That(etiqueta.TituloKey, Is.EqualTo(Cenario.Repertorio.Analisar(item).Musica.TitleKey));
        }

        [TestCase(60, 1, 0)]
        [TestCase(62, 2, 0)]
        [TestCase(64, 3, 0)]
        [TestCase(65, 4, 0)]
        [TestCase(67, 5, 0)]
        [TestCase(67, 4, 1)]
        [TestCase(69, 5, 1)]
        [TestCase(65, 3, 1)]
        [TestCase(64, 2, 1)]
        [TestCase(62, 1, 1)]
        [TestCase(72, 1, 7)]
        [TestCase(59, 1, -1)]
        public void RF64_AncoraEATeclaBrancaMenosODedo(int nota, int dedo, int ancora) =>
            Assert.That(Mao.Ancora(nota, dedo), Is.EqualTo(ancora));

        [Test]
        public void RF64_NaBrilhaSolComDedo4LevaAMaoParaOReEOPolegarNoDoTrazDeVolta()
        {
            var ancoras = Passo.DaMusica(Cenario.Musicas["BRILHA"]).Take(8).Select(p => p.Ancora);
            Assert.That(ancoras, Is.EqualTo(new[] { 0, 0, 1, 1, 1, 1, 1, 1 }));
            Assert.That(Passo.DaMusica(Cenario.Musicas["BRILHA"]).Where(p => p.Trecho == 2).Select(p => p.Ancora),
                Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1, 0 }));
            Assert.That(Passo.DaMusica(Cenario.Musicas["ODE"]).Select(p => p.Ancora), Is.All.EqualTo(0));
        }

        [Test]
        public void RF64_NaDemonstracaoAMaoAndaQuandoANotaSoltaAntesDaProxima()
        {
            var s = Cenario.Sessao("BRILHA#1");
            s.Iniciar(0);
            Assert.That(s.AncoraDaMao, Is.EqualTo(0));
            s.Ouvir(0);
            s.Tick(1000);
            Assert.That(s.AncoraDaMao, Is.EqualTo(0), "segundo dó");
            s.Tick(1699);
            Assert.That(s.AncoraDaMao, Is.EqualTo(0));
            s.Tick(1700);
            Assert.That(s.AncoraDaMao, Is.EqualTo(1), "soltou o dó: já vai para o sol com o anelar");
            s.Tick(2000);
            var nota = s.Coletar().De<DemoNota>().Last();
            Assert.That((nota.Nota, nota.Dedo, nota.Ligada), Is.EqualTo((67, 4, true)));
        }

        [Test]
        public void RF75_NomesNasTeclasAparecemAteAMemoria()
        {
            var s = Cenario.Sessao("BRILHA#1");
            s.Iniciar(0);
            Assert.That((s.Fase, s.NomesNasTeclas), Is.EqualTo((Fase.Pose, true)));
            s.PoseMudou(true, 0);
            Assert.That((s.Fase, s.NomesNasTeclas), Is.EqualTo((Fase.Demonstracao, true)));

            var guiada = Cenario.NaGuiada("BRILHA#1");
            Assert.That((guiada.Fase, guiada.NomesNasTeclas), Is.EqualTo((Fase.Guiada, true)));
            guiada.MidiDesconectado(0);
            Assert.That((guiada.Fase, guiada.NomesNasTeclas), Is.EqualTo((Fase.Pausado, true)));
        }

        [Test]
        public void RF75_NomesNasTeclasSomemNaMemoria()
        {
            var s = Cenario.NaGuiada("BRILHA#1");
            var t = Cenario.TocarTudo(s, 0);
            Assert.That((s.Fase, s.NomesNasTeclas), Is.EqualTo((Fase.Transicao, false)));

            s.Tick(t + 1000);
            Assert.That((s.Fase, s.NomesNasTeclas), Is.EqualTo((Fase.Memoria, false)));

            // Pausar no meio da memória não revela os nomes.
            s.MidiDesconectado(t + 1000);
            Assert.That((s.Fase, s.NomesNasTeclas), Is.EqualTo((Fase.Pausado, false)));
            s.MidiConectado(t + 1000);

            // O retorno do erro ainda é memória.
            s.NoteOn(s.Alvo.Nota + 2, 70, t + 1000);
            Assert.That((s.Fase, s.NomesNasTeclas), Is.EqualTo((Fase.RetornoDeErro, false)));
        }

        [Test]
        public void RF32_NaVezGuiadaAMaoFicaOndeODedoDaVezAlcancaATecla()
        {
            var s = Cenario.NaGuiada("BRILHA#1");
            Assert.That(s.AncoraDaMao, Is.EqualTo(0), "dó com o polegar");
            Cenario.Tocar(s, 0, 2);
            Assert.That((s.Alvo.Nota, s.Alvo.Dedo, s.AncoraDaMao), Is.EqualTo((67, 4, 1)), "sol com o anelar");
        }

        [Test]
        public void RF64_FraseQueComecaForaDaPoseAncoraNaPrimeiraNota()
        {
            var s = Cenario.Sessao("BRILHA#3");
            s.Iniciar(0);
            Assert.That(s.AncoraDaMao, Is.EqualTo(0), "a pose continua em dó-sol");
            s.Ouvir(0);
            Assert.That(s.AncoraDaMao, Is.EqualTo(1));
        }
    }
}
