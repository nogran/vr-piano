using System.Linq;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    public class ProgressoTestes
    {
        static readonly Dia D = Cenario.Hoje;

        [Test]
        public void TDom17_Desbloqueio()
        {
            var c = Cenario.Catalogo;
            var p = new Progresso();
            Assert.That(RegrasDeProgresso.Aberto(p, c, "L01", preparoOk: false), Is.False);
            Assert.That(RegrasDeProgresso.Aberto(p, c, "L01", preparoOk: true), Is.True);

            p.Stars["L01"] = 1;
            Assert.That(RegrasDeProgresso.Aberto(p, c, "L02", true), Is.False);
            p.Stars["L01"] = 2;
            Assert.That(RegrasDeProgresso.Aberto(p, c, "L02", true), Is.True);

            foreach (var l in c.Lessons) p.Stars[l] = 2;
            p.Stars["L07"] = 1;
            Assert.That(RegrasDeProgresso.Aberto(p, c, "SONG1", true), Is.False);
            p.Stars["L07"] = 2;
            Assert.That(RegrasDeProgresso.Aberto(p, c, "SONG1", true), Is.True);
        }

        [Test]
        public void TDom22_DiaSeguidoContaUmaVezPorDia()
        {
            var p = new Progresso { Streak = { Current = 3, Best = 3, LastCountedDay = (D - 1).ToString() } };
            Assert.That(RegrasDeProgresso.ContarDia(p, D), Is.True);
            Assert.That((p.Streak.Current, p.Streak.Best), Is.EqualTo((4, 4)));
            Assert.That(RegrasDeProgresso.ContarDia(p, D), Is.False);
            Assert.That(p.Streak.Current, Is.EqualTo(4));
        }

        [Test]
        public void TDom23_SequenciaQuebradaApareceComoZero()
        {
            var p = new Progresso { Stars = { ["L01"] = 3 }, Streak = { Current = 5, Best = 5, LastCountedDay = (D - 3).ToString() } };
            RegrasDeProgresso.AoAbrir(p, D);
            Assert.That((p.Streak.Current, p.Streak.Best, p.Stars["L01"]), Is.EqualTo((0, 5, 3)));
            RegrasDeProgresso.ContarDia(p, D);
            Assert.That(p.Streak.Current, Is.EqualTo(1));
        }

        [Test]
        public void TDom24_RelogioParaTrasNaoVoltaODia()
        {
            var p = new Progresso
            {
                LastSeenDay = D.ToString(),
                LastMemory = new UltimaMemoria { Item = "L03", Day = D.ToString() },
                Streak = { Current = 2, Best = 2, LastCountedDay = D.ToString() },
            };
            var hoje = RegrasDeProgresso.AoAbrir(p, D - 1);
            Assert.That(hoje, Is.EqualTo(D));
            Assert.That(p.LastSeenDay, Is.EqualTo(D.ToString()));
            Assert.That(p.Streak.Current, Is.EqualTo(2));
            Assert.That(RegrasDeProgresso.ItemDaRevisao(p, Cenario.Catalogo, hoje), Is.Null);
        }

        [Test]
        public void TDom25_RevisaoSoQuandoAUltimaMemoriaEDeOutroDia()
        {
            var c = Cenario.Catalogo;
            Assert.That(RegrasDeProgresso.ItemDaRevisao(new Progresso { LastMemory = new UltimaMemoria { Item = "L03", Day = (D - 1).ToString() } }, c, D), Is.EqualTo("L03"));
            Assert.That(RegrasDeProgresso.ItemDaRevisao(new Progresso { LastMemory = new UltimaMemoria { Item = "L03", Day = D.ToString() } }, c, D), Is.Null);
            Assert.That(RegrasDeProgresso.ItemDaRevisao(new Progresso(), c, D), Is.Null);
        }

        [Test]
        public void TDom26_RevisaoDaMusicaUsaAUltimaLicao()
        {
            var p = new Progresso { LastMemory = new UltimaMemoria { Item = "SONG1", Day = (D - 1).ToString() } };
            Assert.That(RegrasDeProgresso.ItemDaRevisao(p, Cenario.Catalogo, D), Is.EqualTo("L12"));
        }

        [Test]
        public void TDom27_OfertasDaRevisao()
        {
            var s = Cenario.Sessao("L02", revisao: true);
            s.Iniciar(0);
            Assert.That(s.Fase, Is.EqualTo(Fase.Memoria));

            s.NoteOn(64, 70, 100);
            s.Tick(1100);
            Assert.That(s.Ofertas, Is.EqualTo(new[] { Oferta.Ouvir, Oferta.ComGuia }));

            s.Ouvir(1200);
            s.Tick(5000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Ofertas));
            Assert.That(s.Ofertas, Is.EqualTo(new[] { Oferta.ComGuia }));

            s.NoteOn(64, 70, 6000);
            Assert.That(s.Fase, Is.EqualTo(Fase.RetornoDeErro));
            s.Tick(7000);
            s.ComGuia(7100);
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            s.NoteOn(60, 70, 7200);
            s.NoteOn(64, 70, 7300);
            s.NoteOn(62, 70, 7400);
            Assert.That(s.Fase, Is.EqualTo(Fase.Ofertas));
            Assert.That(s.Ofertas, Is.Empty);

            s.NoteOn(60, 70, 8000);
            Assert.That((s.Fase, s.PassoAtual), Is.EqualTo((Fase.Memoria, 1)));
        }

        [Test]
        public void TDom28_RevisaoConcluidaContaODiaSemMudarEstrelas()
        {
            var p = new Progresso
            {
                Stars = { ["L02"] = 2 },
                LastMemory = new UltimaMemoria { Item = "L02", Day = (D - 1).ToString() },
                Streak = { Current = 1, Best = 1, LastCountedDay = (D - 1).ToString() },
            };
            var item = RegrasDeProgresso.ItemDaRevisao(p, Cenario.Catalogo, D)!;
            var s = Cenario.Sessao(item, p, revisao: true);
            s.Iniciar(0);
            s.NoteOn(62, 70, 100);
            s.Tick(1100);
            s.NoteOn(60, 70, 1200);
            s.NoteOn(62, 70, 1300);
            var ev = s.Coletar();
            Assert.That(ev.De<RevisaoConcluida>().Single().Item, Is.EqualTo("L02"));
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
            Assert.That(p.Streak.Current, Is.EqualTo(2));
            Assert.That(p.Stars["L02"], Is.EqualTo(2));
            Assert.That(p.LastMemory!.Day, Is.EqualTo(D.ToString()));
            Assert.That(RegrasDeProgresso.ItemDaRevisao(p, Cenario.Catalogo, D), Is.Null);
        }

        [Test]
        public void ConcluirLicao_GravaUltimaMemoriaEDesbloqueiaAProxima()
        {
            var p = new Progresso();
            var s = Cenario.NaGuiada("L01", p);
            var t = Cenario.AteAMemoria(s, 0);
            for (int i = 0; i < 4; i++) s.NoteOn(60, 70, t + i * 500);
            var ev = s.Coletar();
            Assert.That(ev.De<ItemDesbloqueado>().Single().Item, Is.EqualTo("L02"));
            Assert.That(ev.De<DiaSeguidoMudou>().Single().Atual, Is.EqualTo(1));
            Assert.That((p.LastMemory!.Item, p.LastMemory.Day), Is.EqualTo(("L01", D.ToString())));
            Assert.That(RegrasDeProgresso.ProximoItem(p, Cenario.Catalogo, true), Is.EqualTo("L02"));
        }
    }
}
