using System;
using System.Collections.Generic;
using System.Linq;

namespace VrPiano.Dominio
{
    // Formato de música (06 §5). Os nomes seguem o JSON em snake_case; quem lê o arquivo é o adapter.
    public sealed class Musica
    {
        public string SongId { get; set; } = "";
        public string TitleKey { get; set; } = "";
        public int Bpm { get; set; }
        public List<object> BpmMap { get; set; } = new List<object>();
        public string TimeSignature { get; set; } = "4/4";
        public string SourceFormat { get; set; } = "NATIVE";
        public string? RawFileUrl { get; set; }
        public string Practice { get; set; } = "by_chunk";
        public List<Trecho> Chunks { get; set; } = new List<Trecho>();

        public IEnumerable<Trecho> TrechosNovos() => Chunks.Where(c => c.RepeatOf == null);

        // O número da frase conta só trechos novos; um trecho repetido tem o número do original (RF-51).
        public int NumeroDaFrase(int chunkIndex)
        {
            var trecho = Chunks.First(c => c.ChunkIndex == chunkIndex);
            var original = trecho.RepeatOf ?? chunkIndex;
            return TrechosNovos().TakeWhile(c => c.ChunkIndex != original).Count() + 1;
        }
    }

    public sealed class Trecho
    {
        public int ChunkIndex { get; set; }
        public long StartTimeMs { get; set; }
        public long EndTimeMs { get; set; }
        public int[] MeasureRange { get; set; } = new int[0];
        public string ActiveHands { get; set; } = "RH";
        public int? RepeatOf { get; set; }
        public List<NotaDaMusica> Notes { get; set; } = new List<NotaDaMusica>();
    }

    public sealed class NotaDaMusica
    {
        public int Order { get; set; }
        public int Pitch { get; set; }
        public string NoteName { get; set; } = "";
        public string Hand { get; set; } = "RIGHT";
        public int Finger { get; set; }
        public long? LookaheadStartMs { get; set; }
        public long StartTimeMs { get; set; }
        public long DurationMs { get; set; }
        public long? DeltaToHitMs { get; set; }
        public object? ChordGroupId { get; set; }
        public int? Velocity { get; set; }
    }

    // Um passo da execução: uma nota, com o trecho de onde veio, o número da frase e a âncora da mão.
    public sealed class Passo
    {
        public Passo(int indice, NotaDaMusica nota, int trecho, int frase)
        {
            Indice = indice;
            Nota = nota.Pitch;
            Dedo = nota.Finger;
            InicioMs = nota.StartTimeMs;
            DuracaoMs = nota.DurationMs;
            DeltaMs = nota.DeltaToHitMs;
            Velocity = nota.Velocity;
            Trecho = trecho;
            Frase = frase;
            Ancora = Mao.Ancora(nota.Pitch, nota.Finger);
        }

        public int Indice { get; }
        public int Nota { get; }
        public int Dedo { get; }
        public long InicioMs { get; }
        public long DuracaoMs { get; }
        public long? DeltaMs { get; }
        public int? Velocity { get; }
        public int Trecho { get; }
        public int Frase { get; }
        public int Ancora { get; }

        // A música inteira: a concatenação dos trechos, em ordem, repetições incluídas.
        public static IReadOnlyList<Passo> DaMusica(Musica musica)
        {
            return musica.Chunks
                .SelectMany(c => c.Notes.Select(n => (nota: n, trecho: c.ChunkIndex, frase: musica.NumeroDaFrase(c.ChunkIndex))))
                .Select((x, i) => new Passo(i, x.nota, x.trecho, x.frase))
                .ToList();
        }
    }

    // Posição da mão no teclado (RF-64). A âncora é a tecla branca do polegar, contada a partir do dó central.
    public static class Mao
    {
        public const int AncoraDaPose = 0;

        static readonly int[] BrancaDoSemitom = { 0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6 };

        // Tecla preta conta como a branca à esquerda; a v1 só tem teclas brancas.
        public static int IndiceDaTeclaBranca(int nota)
        {
            var oitava = (int)Math.Floor((nota - 60) / 12.0);
            return oitava * 7 + BrancaDoSemitom[nota - 60 - oitava * 12];
        }

        public static int Ancora(int nota, int dedo) => IndiceDaTeclaBranca(nota) - (dedo - 1);
    }

    public sealed class Catalogo
    {
        public int CatalogVersion { get; set; }
        public List<string> Songs { get; set; } = new List<string>();
    }

    // O catálogo com as músicas carregadas: sabe quais itens existem e em que ordem (RF-50, RF-51).
    public sealed class Repertorio
    {
        const char Separador = '#';
        readonly IDictionary<string, Musica> musicas;

        public Repertorio(Catalogo catalogo, IDictionary<string, Musica> musicas)
        {
            Catalogo = catalogo;
            this.musicas = musicas;
        }

        public Catalogo Catalogo { get; }
        public IEnumerable<Musica> Musicas => Catalogo.Songs.Select(Musica);
        public Musica Musica(string id) => musicas[id];

        public static string ItemDoTrecho(string musica, int chunkIndex) => $"{musica}{Separador}{chunkIndex}";

        // Cada trecho novo é um item, e a música inteira fecha a lista.
        public IEnumerable<string> ItensDaMusica(Musica musica) =>
            ItensDasFrases(musica).Append(musica.SongId);

        public IEnumerable<string> ItensDasFrases(Musica musica) =>
            musica.TrechosNovos().Select(c => ItemDoTrecho(musica.SongId, c.ChunkIndex));

        public IEnumerable<string> Itens() => Musicas.SelectMany(ItensDaMusica);

        // "ODE#4" é o trecho 4 da ODE; "ODE" é a música inteira, sem trecho.
        public (Musica Musica, int? Trecho) Analisar(string item)
        {
            var i = item.IndexOf(Separador);
            if (i < 0) return (Musica(item), null);
            return (Musica(item.Substring(0, i)), int.Parse(item.Substring(i + 1)));
        }
    }
}
