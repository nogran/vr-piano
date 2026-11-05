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
            Catalogo.Lessons.Append(Catalogo.Song).ToDictionary(id => id, id => Ler<Musica>(Path.Combine("songs", id + ".json")));

        public static readonly Dia Hoje = Dia.Parse("2026-10-31");

        public static T Ler<T>(string relativo) =>
            JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(PastaDoConteudo, relativo)), Json)!;

        public static SessaoDeLicao Sessao(string item, Progresso? progresso = null, bool revisao = false, Dia? hoje = null, bool juntar = false) =>
            new SessaoDeLicao(item, Musicas[item], progresso ?? new Progresso(), Catalogo, hoje ?? Hoje, revisao: revisao,
                musicaCompleta: juntar ? Musicas[Catalogo.Song] : null);

        // Sessão já na Guiada, passo 0, com a demonstração vista.
        public static SessaoDeLicao NaGuiada(string item, Progresso? progresso = null, bool juntar = false)
        {
            progresso ??= new Progresso();
            if (!progresso.DemoSeen.Contains(item)) progresso.DemoSeen.Add(item);
            var s = Sessao(item, progresso, juntar: juntar);
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

        public static List<T> De<T>(this IEnumerable<Evento> eventos) => eventos.OfType<T>().ToList();

        static string AcharConteudo()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "content", "catalog.json"))) dir = dir.Parent;
            return Path.Combine(dir!.FullName, "content");
        }
    }
}
