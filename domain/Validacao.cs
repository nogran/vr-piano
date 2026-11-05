using System.Collections.Generic;
using System.Linq;

namespace VrPiano.Dominio
{
    // Validações do formato (06 §5, T-DOM-30): a camada geral vale para qualquer música; a v1 só aceita o corte de uma mão.
    public static class ValidadorDeMusica
    {
        static readonly HashSet<int> NotasDaV1 = new HashSet<int> { 60, 62, 64, 65, 67, 69 };

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
                if (trecho.RepeatOf != null) ValidarRepeticao(m, trecho, erros);
            }
            return erros;
        }

        // Trecho repetido traz as notas completas; elas têm de ser as mesmas do trecho novo que ele repete (RF-52).
        static void ValidarRepeticao(Musica m, Trecho trecho, List<string> erros)
        {
            var original = m.Chunks.FirstOrDefault(c => c.ChunkIndex == trecho.RepeatOf);
            if (original == null || original.ChunkIndex >= trecho.ChunkIndex || original.RepeatOf != null)
            {
                erros.Add($"trecho {trecho.ChunkIndex}: repeat_of {trecho.RepeatOf} não é um trecho novo anterior");
                return;
            }
            var iguais = trecho.Notes.Select(n => (n.Pitch, n.Finger)).SequenceEqual(original.Notes.Select(n => (n.Pitch, n.Finger)));
            if (!iguais) erros.Add($"trecho {trecho.ChunkIndex}: notas ou dedos diferentes do trecho {trecho.RepeatOf}");
        }

        public static List<string> V1(Musica m)
        {
            var erros = Geral(m);
            if (m.BpmMap.Count > 0) erros.Add("v1: bpm_map deve ser vazio");
            if (m.Practice != "by_chunk") erros.Add("v1: practice deve ser by_chunk");
            foreach (var trecho in m.Chunks)
            {
                if (trecho.ActiveHands != "RH") erros.Add($"v1: trecho {trecho.ChunkIndex} com active_hands {trecho.ActiveHands}");
                foreach (var n in trecho.Notes)
                {
                    var onde = $"v1: trecho {trecho.ChunkIndex}, nota {n.Order}";
                    if (n.Hand != "RIGHT") erros.Add($"{onde}: hand {n.Hand}");
                    if (n.ChordGroupId != null) erros.Add($"{onde}: chord_group_id preenchido");
                    if (!NotasDaV1.Contains(n.Pitch)) erros.Add($"{onde}: nota {n.Pitch} fora de 60, 62, 64, 65, 67, 69");
                }
            }
            return erros;
        }

        public static List<string> Catalogo(Catalogo catalogo, IDictionary<string, Musica> musicas)
        {
            var erros = new List<string>();
            if (catalogo.CatalogVersion != 2) erros.Add($"catalog_version {catalogo.CatalogVersion}, não 2");
            if (catalogo.Songs.Count == 0) erros.Add("catálogo sem músicas");
            if (catalogo.Songs.Distinct().Count() != catalogo.Songs.Count) erros.Add("música repetida no catálogo");
            foreach (var id in catalogo.Songs)
            {
                if (!musicas.TryGetValue(id, out var m)) { erros.Add($"{id}: arquivo ausente"); continue; }
                erros.AddRange(V1(m).Select(e => $"{id}: {e}"));
            }
            return erros;
        }

        static string Texto(long? v) => v == null ? "null" : v.Value.ToString();
    }
}
