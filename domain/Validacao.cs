using System.Collections.Generic;
using System.Linq;

namespace VrPiano.Dominio
{
    // Validações do formato (06 §5, T-DOM-30): a camada geral vale para qualquer música; a v1 só aceita o currículo de cinco dedos.
    public static class ValidadorDeMusica
    {
        static readonly Dictionary<int, int> DedoDaNota = new Dictionary<int, int> { [60] = 1, [62] = 2, [64] = 3, [65] = 4, [67] = 5 };

        public static List<string> Geral(Musica m)
        {
            var erros = new List<string>();
            if (m.Bpm < 40 || m.Bpm > 120) erros.Add($"bpm {m.Bpm} fora de 40–120");
            if (m.Chunks.Count == 0) erros.Add("sem trechos");

            long? anterior = null;
            for (int c = 0; c < m.Chunks.Count; c++)
            {
                var trecho = m.Chunks[c];
                if (trecho.ChunkIndex != c + 1) erros.Add($"chunk_index {trecho.ChunkIndex} deveria ser {c + 1}");
                if (trecho.Notes.Count == 0) erros.Add($"trecho {c + 1} sem notas");
                foreach (var n in trecho.Notes)
                {
                    var onde = $"trecho {c + 1}, nota {n.Order}";
                    if (n.LookaheadStartMs != anterior) erros.Add($"{onde}: lookahead_start_ms deveria ser {Texto(anterior)}");
                    long? delta = anterior == null ? (long?)null : n.StartTimeMs - anterior.Value;
                    if (n.DeltaToHitMs != delta) erros.Add($"{onde}: delta_to_hit_ms deveria ser {Texto(delta)}");
                    if (anterior != null && n.StartTimeMs <= anterior.Value) erros.Add($"{onde}: start_time_ms não cresce");
                    if (n.DurationMs <= 0) erros.Add($"{onde}: duration_ms deve ser maior que 0");
                    if (n.Velocity != null && (n.Velocity < 1 || n.Velocity > 127)) erros.Add($"{onde}: velocity fora de 1–127");
                    if (n.Finger < 1 || n.Finger > 5) erros.Add($"{onde}: finger fora de 1–5");
                    if (n.Pitch < 21 || n.Pitch > 108) erros.Add($"{onde}: pitch fora de 21–108");
                    if (n.StartTimeMs < trecho.StartTimeMs || n.StartTimeMs + n.DurationMs > trecho.EndTimeMs)
                        erros.Add($"{onde}: fora do intervalo do trecho");
                    anterior = n.StartTimeMs;
                }
            }
            return erros;
        }

        public static List<string> V1(Musica m)
        {
            var erros = Geral(m);
            if (m.BpmMap.Count > 0) erros.Add("v1: bpm_map deve ser vazio");
            if (m.Practice != "whole") erros.Add("v1: practice deve ser whole");
            foreach (var trecho in m.Chunks)
            {
                if (trecho.ActiveHands != "RH") erros.Add($"v1: trecho {trecho.ChunkIndex} com active_hands {trecho.ActiveHands}");
                foreach (var n in trecho.Notes)
                {
                    var onde = $"v1: trecho {trecho.ChunkIndex}, nota {n.Order}";
                    if (n.Hand != "RIGHT") erros.Add($"{onde}: hand {n.Hand}");
                    if (n.ChordGroupId != null) erros.Add($"{onde}: chord_group_id preenchido");
                    if (!DedoDaNota.TryGetValue(n.Pitch, out var dedo)) erros.Add($"{onde}: nota {n.Pitch} fora de 60, 62, 64, 65, 67");
                    else if (n.Finger != dedo) erros.Add($"{onde}: dedo {n.Finger} não combina com a nota {n.Pitch}");
                }
            }
            return erros;
        }

        public static List<string> Catalogo(Catalogo catalogo, IDictionary<string, Musica> musicas)
        {
            var erros = new List<string>();
            if (catalogo.Lessons.Count != 12) erros.Add($"o catálogo tem {catalogo.Lessons.Count} lições, não 12");
            if (catalogo.Lessons.Distinct().Count() != catalogo.Lessons.Count) erros.Add("lição repetida no catálogo");
            foreach (var id in catalogo.Lessons.Append(catalogo.Song))
            {
                if (!musicas.TryGetValue(id, out var m)) { erros.Add($"{id}: arquivo ausente"); continue; }
                erros.AddRange(V1(m).Select(e => $"{id}: {e}"));
            }
            if (erros.Count > 0 || !musicas.TryGetValue(catalogo.Song, out var musica)) return erros;

            if (musica.Chunks.Count != catalogo.Lessons.Count) erros.Add($"{catalogo.Song}: {musica.Chunks.Count} trechos, não {catalogo.Lessons.Count}");
            for (int i = 0; i < musica.Chunks.Count && i < catalogo.Lessons.Count; i++)
            {
                var notas = musica.Chunks[i].Notes.Select(n => n.Pitch);
                var daLicao = musicas[catalogo.Lessons[i]].Chunks.SelectMany(c => c.Notes).Select(n => n.Pitch);
                if (!notas.SequenceEqual(daLicao)) erros.Add($"{catalogo.Song}: trecho {i + 1} diferente de {catalogo.Lessons[i]}");
            }
            return erros;
        }

        static string Texto(long? v) => v == null ? "null" : v.Value.ToString();
    }
}
