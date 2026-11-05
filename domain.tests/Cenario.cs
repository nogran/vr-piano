using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    // Catálogo real de content/ e atalhos para montar uma sessão (04 §1, "Cenário de domínio").
    static class Cenario
    {
        public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        public static readonly string PastaDoConteudo = AcharConteudo();
        public static readonly Catalogo Catalogo = Ler<Catalogo>("catalog.json");
        public static readonly Dictionary<string, Musica> Musicas =
            Catalogo.Songs.ToDictionary(id => id, id => Ler<Musica>(Path.Combine("songs", id + ".json")));
        public static readonly Repertorio Repertorio = new Repertorio(Catalogo, Musicas);

        public static readonly Dia Hoje = Dia.Parse("2026-10-31");

        public static T Ler<T>(string relativo) =>
            JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(PastaDoConteudo, relativo)), Json)!;

        // Progresso de quem já ouviu as duas músicas: a sessão começa na pose, sem a audição do RF-54.
        public static Progresso JaOuviu() => new Progresso { DemoSeen = { "BRILHA", "ODE" } };

        public static SessaoDeLicao Sessao(string item, Progresso? progresso = null, bool revisao = false, Dia? hoje = null, bool preparoOk = true) =>
            new SessaoDeLicao(item, Repertorio, progresso ?? JaOuviu(), hoje ?? Hoje, revisao: revisao, preparoOk: preparoOk);

        // Sessão já na Guiada, passo 0, com a demonstração vista.
        public static SessaoDeLicao NaGuiada(string item, Progresso? progresso = null)
        {
            progresso ??= JaOuviu();
            foreach (var visto in new[] { item, Repertorio.Analisar(item).Musica.SongId })
                if (!progresso.DemoSeen.Contains(visto)) progresso.DemoSeen.Add(visto);
            var s = Sessao(item, progresso);
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            s.Coletar();
            return s;
        }

        // Toca a sequência inteira certa, uma nota por segundo a partir de t.
        public static long TocarTudo(SessaoDeLicao s, long t)
        {
            foreach (var passo in s.Passos)
            {
                s.Tick(t);
                s.NoteOn(passo.Nota, 70, t);
                t += 1000;
            }
            return t;
        }

        // Da Guiada até a Memória: uma volta limpa e a pausa P-22.
        public static long AteAMemoria(SessaoDeLicao s, long t)
        {
            t = TocarTudo(s, t);
            s.Tick(t + 1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Memoria));
            return t + 1000;
        }

        // Toca as próximas notas certas, sem esperar: serve à memória, ao Juntar, à música inteira e à revisão.
        public static long Tocar(SessaoDeLicao s, long t, int quantas)
        {
            for (var i = 0; i < quantas; i++, t += 500) s.NoteOn(s.Alvo.Nota, 70, t);
            return t;
        }

        public static long TocarOQueFalta(SessaoDeLicao s, long t) => Tocar(s, t, s.Passos.Count - s.PassoAtual);

        // Música inteira saindo da pose: até a fase Juntar.
        public static long AteAMusicaInteira(SessaoDeLicao s)
        {
            s.Iniciar(0);
            s.PoseMudou(true, 0);
            s.Tick(1000);
            Assert.That(s.Fase, Is.EqualTo(Fase.Juntar));
            s.Coletar();
            return 1000;
        }

        public static List<T> De<T>(this IEnumerable<Evento> eventos) => eventos.OfType<T>().ToList();

        static string AcharConteudo()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "content", "catalog.json"))) dir = dir.Parent;
            return Path.Combine(dir!.FullName, "content");
        }
    }
}
