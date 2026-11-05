using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    public class ProgressoTestes
    {
        static readonly Dia D = Cenario.Hoje;
        static readonly Repertorio R = Cenario.Repertorio;

        static void Dar(Progresso p, int estrelas, params string[] itens)
        {
            foreach (var i in itens) p.Stars[i] = estrelas;
        }

        static readonly string[] FrasesDaBrilha = { "BRILHA#1", "BRILHA#2", "BRILHA#3" };
        static readonly string[] FrasesDaOde = { "ODE#1", "ODE#2", "ODE#4", "ODE#5", "ODE#6" };

        [Test]
        public void TDom17_DesbloqueioDasFrases()
        {
            var p = new Progresso();
            Assert.That(RegrasDeProgresso.Aberto(p, R, "BRILHA#1", preparoOk: false), Is.False);
            Assert.That(RegrasDeProgresso.Aberto(p, R, "BRILHA#1", preparoOk: true), Is.True);

            p.Stars["BRILHA#1"] = 1;
            Assert.That(RegrasDeProgresso.Aberto(p, R, "BRILHA#2", true), Is.False);
            p.Stars["BRILHA#1"] = 2;
            Assert.That(RegrasDeProgresso.Aberto(p, R, "BRILHA#2", true), Is.True);
        }

        [Test]
        public void TDom17b_FraseDaOdeDependeDoTrechoNovoAnterior()
        {
            var p = new Progresso();
            Dar(p, 2, "BRILHA", "ODE#1");
            Assert.That(RegrasDeProgresso.Aberto(p, R, "ODE#2", true), Is.True);
            Assert.That(RegrasDeProgresso.Aberto(p, R, "ODE#4", true), Is.False);
            p.Stars["ODE#2"] = 2;
            Assert.That(RegrasDeProgresso.Aberto(p, R, "ODE#4", true), Is.True);
            Assert.That(RegrasDeProgresso.Aberto(p, R, "ODE#3", true), Is.False, "trecho repetido não é item");
        }

        [Test]
        public void TDom17c_MusicaInteiraAbreComTodasAsFrasesEmDuas()
        {
            var p = new Progresso();
            Dar(p, 2, FrasesDaBrilha);
            p.Stars["BRILHA#2"] = 1;
            Assert.That(RegrasDeProgresso.Aberto(p, R, "BRILHA", true), Is.False);
            p.Stars["BRILHA#2"] = 2;
            Assert.That(RegrasDeProgresso.Aberto(p, R, "BRILHA", true), Is.True);
        }

        [Test]
        public void TDom17d_ProximaMusicaAbreComDuasEstrelasNoItemInteiro()
        {
            var p = new Progresso();
            Dar(p, 3, FrasesDaBrilha);
            Assert.That(RegrasDeProgresso.Aberto(p, R, "ODE#1", true), Is.False);
            p.Stars["BRILHA"] = 2;
            Assert.That(RegrasDeProgresso.Aberto(p, R, "ODE#1", true), Is.True);
            Assert.That(RegrasDeProgresso.Aberto(p, R, "ODE", true), Is.False);
            Dar(p, 2, FrasesDaOde);
            Assert.That(RegrasDeProgresso.Aberto(p, R, "ODE", true), Is.True);
        }

        [Test]
        public void TDom17e_ConcluirAMusicaAbreAProxima()
        {
            var p = Cenario.JaOuviu();
            Dar(p, 2, FrasesDaBrilha);
            var s = Cenario.Sessao("BRILHA", p);
            var t = Cenario.AteAMusicaInteira(s);
            Cenario.Tocar(s, t, 42);
            var ev = s.Coletar();
            Assert.That(ev.De<ItemDesbloqueado>().Single().Item, Is.EqualTo("ODE#1"));
            var aberta = ev.De<MusicaDesbloqueada>().Single();
            Assert.That((aberta.Musica, aberta.TituloKey), Is.EqualTo(("ODE", "song.ODE.title")));
            Assert.That(RegrasDeProgresso.ProximoItem(p, R, true), Is.EqualTo("ODE#1"));
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
            var p = new Progresso { Stars = { ["BRILHA#1"] = 3 }, Streak = { Current = 5, Best = 5, LastCountedDay = (D - 3).ToString() } };
            RegrasDeProgresso.AoAbrir(p, D);
            Assert.That((p.Streak.Current, p.Streak.Best, p.Stars["BRILHA#1"]), Is.EqualTo((0, 5, 3)));
            RegrasDeProgresso.ContarDia(p, D);
            Assert.That(p.Streak.Current, Is.EqualTo(1));
        }

        [Test]
        public void TDom24_RelogioParaTrasNaoVoltaODia()
        {
            var p = new Progresso
            {
                LastSeenDay = D.ToString(),
                LastMemory = new UltimaMemoria { Item = "BRILHA#3", Day = D.ToString() },
                Streak = { Current = 2, Best = 2, LastCountedDay = D.ToString() },
            };
            var hoje = RegrasDeProgresso.AoAbrir(p, D - 1);
            Assert.That(hoje, Is.EqualTo(D));
            Assert.That(p.LastSeenDay, Is.EqualTo(D.ToString()));
            Assert.That(p.Streak.Current, Is.EqualTo(2));
            Assert.That(RegrasDeProgresso.MusicaDaRevisao(p, R, hoje), Is.Null);
        }

        [Test]
        public void TDom25_RevisaoSoQuandoAUltimaMemoriaEDeOutroDia()
        {
            Assert.That(RegrasDeProgresso.MusicaDaRevisao(new Progresso { LastMemory = new UltimaMemoria { Item = "ODE#4", Day = (D - 1).ToString() } }, R, D), Is.EqualTo("ODE"));
            Assert.That(RegrasDeProgresso.MusicaDaRevisao(new Progresso { LastMemory = new UltimaMemoria { Item = "ODE#4", Day = D.ToString() } }, R, D), Is.Null);
            Assert.That(RegrasDeProgresso.MusicaDaRevisao(new Progresso(), R, D), Is.Null);
        }

        [Test]
        public void TDom26_RevisaoVaiAteOChainOuInteira()
        {
            var musica = Cenario.Musicas["BRILHA"];
            var p = new Progresso { Chain = { ["BRILHA"] = 2 } };
            Assert.That(RegrasDeProgresso.TrechosDaRevisao(p, musica), Is.EqualTo(2));
            Assert.That(Cenario.Sessao("BRILHA", p, revisao: true).Cadeia!, Has.Count.EqualTo(14));

            p.Stars["BRILHA"] = 2;
            Assert.That(RegrasDeProgresso.TrechosDaRevisao(p, musica), Is.EqualTo(6));
            Assert.That(Cenario.Sessao("BRILHA", p, revisao: true).Cadeia!, Has.Count.EqualTo(42));

            Assert.That(RegrasDeProgresso.TrechosDaRevisao(new Progresso(), musica), Is.EqualTo(1), "sem chain, pelo menos o primeiro trecho");
        }

        static SessaoDeLicao RevisaoAteOTrecho2(Progresso? p = null)
        {
            p ??= Cenario.JaOuviu();
            p.Chain["BRILHA"] = 2;
            p.LastMemory = new UltimaMemoria { Item = "BRILHA#2", Day = (D - 1).ToString() };
            var s = Cenario.Sessao("BRILHA", p, revisao: true);
            s.Iniciar(0);
            return s;
        }

        [Test]
        public void TDom27_OfertasDaRevisaoSaoDoTrechoDoErro()
        {
            var s = RevisaoAteOTrecho2();
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            var t = Cenario.Tocar(s, 100, 8);           // trecho 1 e a primeira nota do trecho 2
            s.Coletar();

            s.NoteOn(s.Alvo.Nota + 1, 70, t);
            s.Tick(t + 1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Ofertas));
            Assert.That(s.Ofertas, Is.EqualTo(new[] { Oferta.Ouvir, Oferta.ComGuia }));

            s.Coletar();
            s.Ouvir(t + 1100);
            s.Tick(t + 20000);
            var demo = s.Coletar().De<EnviarNoteOn>().Select(e => e.Nota);
            Assert.That(demo, Is.EqualTo(new[] { 65, 65, 64, 64, 62, 62, 60 }), "Ouvir toca só o trecho do erro");
            Assert.That(s.Fase, Is.EqualTo(Fase.Ofertas));
            Assert.That(s.Ofertas, Is.EqualTo(new[] { Oferta.ComGuia }));

            t += 21000;
            s.NoteOn(64, 70, t);                        // a primeira do trecho 2 é 65: falha na hora
            Assert.That(s.Fase, Is.EqualTo(Fase.RetornoDeErro));
            s.Tick(t + 1000);
            s.ComGuia(t + 1100);
            Assert.That(s.Fase, Is.EqualTo(Fase.Guiada));
            Assert.That(s.Alvo.Nota, Is.EqualTo(65));
            foreach (var nota in new[] { 65, 65, 64, 64, 62, 62, 60 }) s.NoteOn(nota, 70, t + 1200);
            Assert.That(s.Fase, Is.EqualTo(Fase.Ofertas));
            Assert.That(s.Ofertas, Is.Empty);

            s.NoteOn(65, 70, t + 2000);                 // conta como a primeira nota do trecho
            Assert.That((s.Fase, s.PassoAtual), Is.EqualTo((Fase.Juntar, 8)));
        }

        [Test]
        public void TDom27b_RevisaoNaoVoltaAoGuiadoSozinhaNemTemPular()
        {
            var s = RevisaoAteOTrecho2();
            long t = 100;
            for (var i = 0; i < 4; i++, t += 2000)
            {
                s.NoteOn(s.Passos[0].Nota + 1, 70, t);
                s.Tick(t + 1000);
                Assert.That(s.Fase, Is.EqualTo(Fase.Ofertas));
            }
            Assert.That(s.Coletar().De<VoltouAoGuiado>(), Is.Empty);
        }

        [Test]
        public void TDom27c_DicaValeNaRevisao()
        {
            var s = RevisaoAteOTrecho2();
            s.Coletar();
            s.Tick(3000);
            Assert.That(s.Coletar().De<DicaDeMemoria>().Single().Nota, Is.EqualTo(60));
        }

        [Test]
        public void TDom28_RevisaoConcluidaContaODiaSemMudarEstrelas()
        {
            var p = new Progresso
            {
                Stars = { ["BRILHA#2"] = 2 },
                Streak = { Current = 1, Best = 1, LastCountedDay = (D - 1).ToString() },
            };
            var s = RevisaoAteOTrecho2(p);
            var t = Cenario.Tocar(s, 100, 3);
            s.NoteOn(61, 70, t);
            s.Tick(t + 1000);
            s.NoteOn(60, 70, t + 1100);
            Cenario.Tocar(s, t + 1200, 13);
            var ev = s.Coletar();
            Assert.That(ev.De<RevisaoConcluida>().Single().Item, Is.EqualTo("BRILHA"));
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
            Assert.That(p.Streak.Current, Is.EqualTo(2));
            Assert.That(p.Stars["BRILHA#2"], Is.EqualTo(2));
            Assert.That(p.LastMemory!.Day, Is.EqualTo(D.ToString()));
            Assert.That(p.Memorized, Is.Empty);
            Assert.That(RegrasDeProgresso.MusicaDaRevisao(p, R, D), Is.Null);
        }

        static Progresso MusicaConcluida(string dia)
        {
            var p = Cenario.JaOuviu();
            p.Stars["BRILHA"] = 2;
            p.Chain["BRILHA"] = 6;
            p.LastMemory = new UltimaMemoria { Item = "BRILHA", Day = dia };
            return p;
        }

        [Test]
        public void RF61_DecoraQuandoSaiInteiraNumaRevisaoNoutroDia()
        {
            var p = MusicaConcluida((D - 1).ToString());
            var s = Cenario.Sessao("BRILHA", p, revisao: true);
            s.Iniciar(0);
            Cenario.Tocar(s, 100, 42);
            Assert.That(s.Coletar().De<MusicaDecorada>().Single().Musica, Is.EqualTo("BRILHA"));
            Assert.That(s.Decorou, Is.True);
            Assert.That(p.Memorized, Is.EqualTo(new[] { "BRILHA" }));

            var outraVez = Cenario.Sessao("BRILHA", p, revisao: true, hoje: D + 1);
            outraVez.Iniciar(0);
            Cenario.Tocar(outraVez, 100, 42);
            Assert.That(outraVez.Coletar().De<MusicaDecorada>(), Is.Empty);
            Assert.That(p.Memorized, Has.Count.EqualTo(1));
        }

        [Test]
        public void RF61_NoMesmoDiaDaConclusaoNaoDecora()
        {
            var p = MusicaConcluida(D.ToString());
            var s = Cenario.Sessao("BRILHA", p, revisao: true);
            s.Iniciar(0);
            Cenario.Tocar(s, 100, 42);
            Assert.That(p.Memorized, Is.Empty);
        }

        [Test]
        public void RF61_ConcluirAMusicaInteiraForaDaRevisaoNaoDecora()
        {
            var p = Cenario.JaOuviu();
            var s = Cenario.Sessao("BRILHA", p);
            Cenario.Tocar(s, Cenario.AteAMusicaInteira(s), 42);
            Assert.That(p.Memorized, Is.Empty);
        }

        [Test]
        public void RF61_RevisaoDeParteDaMusicaNaoDecora()
        {
            var p = Cenario.JaOuviu();
            var s = RevisaoAteOTrecho2(p);
            Cenario.Tocar(s, 100, 14);
            Assert.That(s.Fase, Is.EqualTo(Fase.Resultado));
            Assert.That(p.Memorized, Is.Empty);
        }

        [Test]
        public void RF63_MigracaoV1MantemDiasEConfiguracoesEDescartaAsLicoes()
        {
            const string v1 = @"{
                ""schemaVersion"": 1, ""language"": ""en"", ""handGuide"": false, ""checklistConfirmed"": true,
                ""calibration"": { ""anchorUuid"": ""abc"", ""lowNote"": 36 },
                ""stars"": { ""L01"": 3, ""SONG1"": 2 }, ""demoSeen"": [""L01""],
                ""rhythm"": { ""L01"": { ""onTime"": 3, ""judged"": 3 } },
                ""streak"": { ""current"": 2, ""best"": 4, ""lastCountedDay"": ""2026-10-30"" },
                ""lastMemory"": { ""item"": ""L02"", ""day"": ""2026-10-30"" }, ""chain"": 2, ""lastSeenDay"": ""2026-10-30""
            }";
            var js = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var p = RegrasDeProgresso.MigrarDaV1(JsonSerializer.Deserialize<ProgressoV1>(v1, js)!);

            Assert.That(p.SchemaVersion, Is.EqualTo(2));
            Assert.That((p.Streak.Current, p.Streak.Best, p.Streak.LastCountedDay), Is.EqualTo((2, 4, "2026-10-30")));
            Assert.That(p.LastSeenDay, Is.EqualTo("2026-10-30"));
            Assert.That((p.Language, p.HandGuide, p.ChecklistConfirmed), Is.EqualTo(("en", false, true)));
            Assert.That(JsonSerializer.Serialize(p.Calibration), Does.Contain("abc"));
            Assert.That(p.Stars, Is.Empty);
            Assert.That(p.DemoSeen, Is.Empty);
            Assert.That(p.Rhythm, Is.Empty);
            Assert.That(p.Chain, Is.Empty);
            Assert.That(p.Memorized, Is.Empty);
            Assert.That(p.LastMemory, Is.Null);
            Assert.That(RegrasDeProgresso.MusicaDaRevisao(p, R, D), Is.Null);
        }

        [Test]
        public void RF63_ProgressoV2TemChainPorMusicaEIdaEVolta()
        {
            var js = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var p = new Progresso { Chain = { ["BRILHA"] = 3 }, Memorized = { "BRILHA" } };
            var lido = JsonSerializer.Deserialize<Progresso>(JsonSerializer.Serialize(p, js), js)!;
            Assert.That(lido.SchemaVersion, Is.EqualTo(2));
            Assert.That(lido.Chain["BRILHA"], Is.EqualTo(3));
            Assert.That(lido.Memorized, Is.EqualTo(new[] { "BRILHA" }));
        }

        [Test]
        public void RF66_PainelListaMusicasEFrases()
        {
            var p = Cenario.JaOuviu();
            Dar(p, 3, FrasesDaBrilha);
            p.Stars["BRILHA"] = 2;
            p.Rhythm["BRILHA#1"] = new Ritmo(5, 6);
            p.Memorized.Add("BRILHA");
            p.Chain["BRILHA"] = 6;
            p.DemoSeen.Remove("ODE");
            var painel = RegrasDeProgresso.Painel(p, R, true);

            Assert.That(painel.Musicas.Select(m => m.Id), Is.EqualTo(new[] { "BRILHA", "ODE" }));
            var brilha = painel.Musicas[0];
            Assert.That((brilha.TituloKey, brilha.Aberta, brilha.Decorada, brilha.PodeOuvir, brilha.Cadeia), Is.EqualTo(("song.BRILHA.title", true, true, true, 6)));
            Assert.That(brilha.Itens.Select(i => i.Frase), Is.EqualTo(new int?[] { 1, 2, 3, null }));
            Assert.That(brilha.Itens[0].Ritmo!.OnTime, Is.EqualTo(5));
            var ode = painel.Musicas[1];
            Assert.That((ode.Aberta, ode.Decorada, ode.PodeOuvir), Is.EqualTo((true, false, false)));
            Assert.That(ode.Itens.Select(i => i.Id), Is.EqualTo(new[] { "ODE#1", "ODE#2", "ODE#4", "ODE#5", "ODE#6", "ODE" }));
            Assert.That(ode.Itens.Select(i => i.Frase), Is.EqualTo(new int?[] { 1, 2, 3, 4, 5, null }));
            Assert.That(ode.Itens.Select(i => i.Aberto), Is.EqualTo(new[] { true, false, false, false, false, false }));
            Assert.That((painel.Estrelas, painel.FrasesSemErro, painel.Decoradas, painel.Proximo), Is.EqualTo((11, 3, 1, "ODE#1")));
        }

        [Test]
        public void ItemInicial_SoNaPrimeiraAbertura()
        {
            Assert.That(RegrasDeProgresso.ItemInicial(new Progresso(), R), Is.EqualTo("BRILHA#1"));
            Assert.That(RegrasDeProgresso.ItemInicial(new Progresso { DemoSeen = { "BRILHA" } }, R), Is.Null);
            Assert.That(RegrasDeProgresso.ItemInicial(new Progresso { Stars = { ["BRILHA#1"] = 1 } }, R), Is.Null);
        }

        [Test]
        public void ConcluirFrase_GravaUltimaMemoriaEDesbloqueiaAProxima()
        {
            var p = Cenario.JaOuviu();
            var s = Cenario.NaGuiada("BRILHA#1", p);
            var t = Cenario.AteAMemoria(s, 0);
            Cenario.TocarOQueFalta(s, t);
            var ev = s.Coletar();
            Assert.That(ev.De<ItemDesbloqueado>().Single().Item, Is.EqualTo("BRILHA#2"));
            Assert.That(ev.De<MusicaDesbloqueada>(), Is.Empty);
            Assert.That(ev.De<DiaSeguidoMudou>().Single().Atual, Is.EqualTo(1));
            Assert.That((p.LastMemory!.Item, p.LastMemory.Day), Is.EqualTo(("BRILHA#1", D.ToString())));
            Assert.That(RegrasDeProgresso.ProximoItem(p, R, true), Is.EqualTo("BRILHA#2"));
        }
    }
}
