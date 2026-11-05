using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    public class ConteudoTestes
    {
        static readonly Dictionary<string, int[]> Tabela = new()
        {
            ["L01"] = new[] { 60, 60, 60, 60 }, ["L02"] = new[] { 60, 62 }, ["L03"] = new[] { 60, 62, 64 },
            ["L04"] = new[] { 64, 62, 60 }, ["L05"] = new[] { 60, 62, 64, 62, 60 }, ["L06"] = new[] { 64, 65 },
            ["L07"] = new[] { 60, 62, 64, 65 }, ["L08"] = new[] { 65, 67 }, ["L09"] = new[] { 60, 62, 64, 65, 67 },
            ["L10"] = new[] { 67, 65, 64, 62, 60 }, ["L11"] = new[] { 60, 64, 67, 64, 60 },
            ["L12"] = new[] { 60, 64, 67, 64, 60, 60, 62, 64, 65, 67 },
        };

        [Test]
        public void TDom30_CatalogoEmbutidoPassaNasDuasCamadas()
        {
            Assert.That(ValidadorDeMusica.Catalogo(Cenario.Catalogo, Cenario.Musicas), Is.Empty);
            foreach (var (id, notas) in Tabela)
            {
                var passos = Passo.DaMusica(Cenario.Musicas[id]);
                Assert.That(passos.Select(p => p.Nota), Is.EqualTo(notas), id);
                Assert.That(passos.Skip(1).Select(p => p.DeltaMs), Is.All.EqualTo(1000), id);
                Assert.That(passos.Last().DuracaoMs, Is.EqualTo(2000), id);
            }
            var musica = Cenario.Musicas["SONG1"];
            Assert.That(musica.Chunks, Has.Count.EqualTo(12));
            Assert.That(Passo.DaMusica(musica), Has.Count.EqualTo(50));
        }

        [TestCase("hand", "LEFT", "hand LEFT")]
        [TestCase("chord_group_id", "1", "chord_group_id")]
        [TestCase("delta_to_hit_ms", "900", "delta_to_hit_ms")]
        [TestCase("start_time_ms", "500", "start_time_ms")]
        [TestCase("pitch", "61", "nota 61")]
        public void TDom30b_ArquivoInvalidoERecusadoComMotivo(string campo, string valor, string motivo)
        {
            var musica = Cenario.Ler<Musica>("songs/L03.json");
            var nota = musica.Chunks[0].Notes[2];
            switch (campo)
            {
                case "hand": nota.Hand = valor; break;
                case "chord_group_id": nota.ChordGroupId = valor; break;
                case "delta_to_hit_ms": nota.DeltaToHitMs = long.Parse(valor); break;
                case "start_time_ms": nota.StartTimeMs = long.Parse(valor); break;
                case "pitch": nota.Pitch = int.Parse(valor); break;
            }
            Assert.That(ValidadorDeMusica.V1(musica), Has.Some.Contains(motivo));
        }

        [Test]
        public void FormatoDeIdaEVolta_MantemOsCamposDoArquivo()
        {
            var original = System.IO.File.ReadAllText(System.IO.Path.Combine(Cenario.PastaDoConteudo, "songs", "L01.json"));
            var regravado = JsonSerializer.Serialize(Cenario.Ler<Musica>("songs/L01.json"), Cenario.Json);
            var compacto = JsonSerializer.Serialize(JsonDocument.Parse(original).RootElement);
            Assert.That(regravado, Is.EqualTo(compacto));
        }
    }
}
