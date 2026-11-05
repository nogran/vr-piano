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
        public string Practice { get; set; } = "whole";
        public List<Trecho> Chunks { get; set; } = new List<Trecho>();
    }

    public sealed class Trecho
    {
        public int ChunkIndex { get; set; }
        public long StartTimeMs { get; set; }
        public long EndTimeMs { get; set; }
        public int[] MeasureRange { get; set; } = new int[0];
        public string ActiveHands { get; set; } = "RH";
        public int LabelLesson { get; set; }
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

    // Um passo da execução: uma nota, já com a lição do trecho de onde veio.
    public sealed class Passo
    {
        public Passo(int indice, NotaDaMusica nota, int licaoDoTrecho)
        {
            Indice = indice;
            Nota = nota.Pitch;
            Dedo = nota.Finger;
            InicioMs = nota.StartTimeMs;
            DuracaoMs = nota.DurationMs;
            DeltaMs = nota.DeltaToHitMs;
            Velocity = nota.Velocity;
            LicaoDoTrecho = licaoDoTrecho;
        }

        public int Indice { get; }
        public int Nota { get; }
        public int Dedo { get; }
        public long InicioMs { get; }
        public long DuracaoMs { get; }
        public long? DeltaMs { get; }
        public int? Velocity { get; }
        public int LicaoDoTrecho { get; }

        // practice "whole": a sequência é a concatenação dos trechos, em ordem.
        public static IReadOnlyList<Passo> DaMusica(Musica musica)
        {
            return musica.Chunks
                .SelectMany(c => c.Notes.Select(n => (nota: n, licao: c.LabelLesson)))
                .Select((x, i) => new Passo(i, x.nota, x.licao))
                .ToList();
        }
    }

    public sealed class Catalogo
    {
        public int CatalogVersion { get; set; }
        public List<string> Lessons { get; set; } = new List<string>();
        public string Song { get; set; } = "";
    }
}
