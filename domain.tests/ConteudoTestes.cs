using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    public class ConteudoTestes
    {
        // As tabelas da decisão de conteúdo (RF-68): nota, dedo e tempos, escritas à mão, sem ler os arquivos.
        const int Do = 60, Re = 62, Mi = 64, Fa = 65, Sol = 67, La = 69;

        static readonly (int Nota, int Dedo, double Tempos)[] BrilhaA =
            { (Do, 1, 1), (Do, 1, 1), (Sol, 4, 1), (Sol, 4, 1), (La, 5, 1), (La, 5, 1), (Sol, 4, 2) };
        static readonly (int Nota, int Dedo, double Tempos)[] BrilhaB =
            { (Fa, 3, 1), (Fa, 3, 1), (Mi, 2, 1), (Mi, 2, 1), (Re, 1, 1), (Re, 1, 1), (Do, 1, 2) };
        static readonly (int Nota, int Dedo, double Tempos)[] BrilhaC =
            { (Sol, 4, 1), (Sol, 4, 1), (Fa, 3, 1), (Fa, 3, 1), (Mi, 2, 1), (Mi, 2, 1), (Re, 1, 2) };

        static readonly (int Nota, int Dedo, double Tempos)[] OdeA =
            { (Mi, 3, 1), (Mi, 3, 1), (Fa, 4, 1), (Sol, 5, 1), (Sol, 5, 1), (Fa, 4, 1), (Mi, 3, 1), (Re, 2, 1) };
        static readonly (int Nota, int Dedo, double Tempos)[] OdeB =
            { (Do, 1, 1), (Do, 1, 1), (Re, 2, 1), (Mi, 3, 1), (Mi, 3, 1.5), (Re, 2, 0.5), (Re, 2, 2) };
        static readonly (int Nota, int Dedo, double Tempos)[] OdeC =
            { (Do, 1, 1), (Do, 1, 1), (Re, 2, 1), (Mi, 3, 1), (Re, 2, 1.5), (Do, 1, 0.5), (Do, 1, 2) };
        static readonly (int Nota, int Dedo, double Tempos)[] OdeD =
            { (Re, 2, 1), (Re, 2, 1), (Mi, 3, 1), (Do, 1, 1), (Re, 2, 1), (Mi, 3, 0.5), (Fa, 4, 0.5), (Mi, 3, 1), (Do, 1, 1) };
        static readonly (int Nota, int Dedo, double Tempos)[] OdeE =
            { (Re, 2, 1), (Mi, 3, 0.5), (Fa, 4, 0.5), (Mi, 3, 1), (Re, 2, 1), (Do, 1, 1), (Re, 2, 1), (Sol, 5, 2) };

        static void Conferir(string id, int compassos, int notas, (int Nota, int Dedo, double Tempos)[][] trechos, int?[] repeticoes)
        {
            var musica = Cenario.Musicas[id];
            Assert.That(musica.Bpm, Is.EqualTo(60));
            Assert.That(musica.TimeSignature, Is.EqualTo("4/4"));
            Assert.That(musica.Practice, Is.EqualTo("by_chunk"));
            Assert.That(musica.Chunks.Select(c => c.RepeatOf), Is.EqualTo(repeticoes));
            Assert.That(musica.Chunks.SelectMany(c => c.Notes).Count(), Is.EqualTo(notas));
            Assert.That(musica.Chunks.Last().MeasureRange[1], Is.EqualTo(compassos));

            long t = 0;
            for (var c = 0; c < trechos.Length; c++)
            {
                var trecho = musica.Chunks[c];
                Assert.That(trecho.StartTimeMs, Is.EqualTo(c * 8000), $"{id} trecho {c + 1}");
                Assert.That(trecho.EndTimeMs, Is.EqualTo((c + 1) * 8000), $"{id} trecho {c + 1}");
                Assert.That(trecho.MeasureRange, Is.EqualTo(new[] { 2 * c + 1, 2 * c + 2 }), $"{id} trecho {c + 1}");
                Assert.That(trecho.Notes, Has.Count.EqualTo(trechos[c].Length), $"{id} trecho {c + 1}");
                for (var n = 0; n < trechos[c].Length; n++)
                {
                    var onde = $"{id} trecho {c + 1} nota {n + 1}";
                    var (nota, dedo, tempos) = trechos[c][n];
                    Assert.That(trecho.Notes[n].Pitch, Is.EqualTo(nota), onde);
                    Assert.That(trecho.Notes[n].Finger, Is.EqualTo(dedo), onde);
                    Assert.That(trecho.Notes[n].StartTimeMs, Is.EqualTo(t), onde);
                    Assert.That(trecho.Notes[n].DurationMs, Is.EqualTo((long)(tempos * 1000)), onde);
                    t += (long)(tempos * 1000);
                }
            }
        }

        [Test]
        public void RF68_BrilhaBateNotaADedoATempoComATabela() =>
            Conferir("BRILHA", 12, 42,
                new[] { BrilhaA, BrilhaB, BrilhaC, BrilhaC, BrilhaA, BrilhaB },
                new int?[] { null, null, null, 3, 1, 2 });

        [Test]
        public void RF68_OdeBateNotaADedoATempoComATabela() =>
            Conferir("ODE", 16, 62,
                new[] { OdeA, OdeB, OdeA, OdeC, OdeD, OdeE, OdeA, OdeC },
                new int?[] { null, null, 1, null, null, null, 1, 4 });

        [Test]
        public void TDom30_CatalogoEmbutidoPassaNasDuasCamadas()
        {
            Assert.That(ValidadorDeMusica.Catalogo(Cenario.Catalogo, Cenario.Musicas), Is.Empty);
            Assert.That(Cenario.Catalogo.CatalogVersion, Is.EqualTo(2));
            Assert.That(Cenario.Catalogo.Songs, Is.EqualTo(new[] { "BRILHA", "ODE" }));
        }

        [Test]
        public void RF51_ItensSaoOsTrechosNovosEAMusicaInteira()
        {
            Assert.That(Cenario.Repertorio.Itens(), Is.EqualTo(new[]
            {
                "BRILHA#1", "BRILHA#2", "BRILHA#3", "BRILHA", "ODE#1", "ODE#2", "ODE#4", "ODE#5", "ODE#6", "ODE",
            }));
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 1)]
        [TestCase(4, 3)]
        [TestCase(5, 4)]
        [TestCase(6, 5)]
        [TestCase(7, 1)]
        [TestCase(8, 3)]
        public void RF51_NumeroDaFraseContaSoTrechosNovos(int trecho, int frase) =>
            Assert.That(Cenario.Musicas["ODE"].NumeroDaFrase(trecho), Is.EqualTo(frase));

        [TestCase("hand", "LEFT", "hand LEFT")]
        [TestCase("chord_group_id", "1", "chord_group_id")]
        [TestCase("delta_to_hit_ms", "900", "delta_to_hit_ms")]
        [TestCase("start_time_ms", "500", "start_time_ms")]
        [TestCase("pitch", "61", "nota 61")]
        public void TDom30b_ArquivoInvalidoERecusadoComMotivo(string campo, string valor, string motivo)
        {
            var musica = Cenario.Ler<Musica>("songs/BRILHA.json");
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
        public void RF53_V1RecusaPracticeWholeEBpmMapMasAceitaDedoLivre()
        {
            var musica = Cenario.Ler<Musica>("songs/BRILHA.json");
            musica.Practice = "whole";
            musica.BpmMap.Add("mudança");
            var erros = ValidadorDeMusica.V1(musica);
            Assert.That(erros, Has.Some.Contains("practice"));
            Assert.That(erros, Has.Some.Contains("bpm_map"));

            var dedoLivre = Cenario.Ler<Musica>("songs/BRILHA.json");
            foreach (var n in dedoLivre.Chunks[0].Notes.Concat(dedoLivre.Chunks[4].Notes)) n.Finger = 2;
            Assert.That(ValidadorDeMusica.V1(dedoLivre), Is.Empty);
        }

        [TestCase("nota", "notas ou dedos diferentes do trecho 1")]
        [TestCase("dedo", "notas ou dedos diferentes do trecho 1")]
        [TestCase("nota a menos", "notas ou dedos diferentes do trecho 1")]
        [TestCase("repete o futuro", "não é um trecho novo anterior")]
        [TestCase("repete repetição", "não é um trecho novo anterior")]
        public void RF52_RepeticaoTemDeSerIgualAoTrechoNovoAnterior(string caso, string motivo)
        {
            var musica = Cenario.Ler<Musica>("songs/BRILHA.json");
            var trecho = musica.Chunks[4]; // trecho 5, repeat_of 1
            switch (caso)
            {
                case "nota": trecho.Notes[2].Pitch = 65; break;
                case "dedo": trecho.Notes[2].Finger = 5; break;
                case "nota a menos": trecho.Notes.RemoveAt(6); break;
                case "repete o futuro": trecho.RepeatOf = 6; break;
                case "repete repetição": trecho.RepeatOf = 4; break;
            }
            Assert.That(ValidadorDeMusica.Geral(musica), Has.Some.Contains(motivo));
        }

        [Test]
        public void TDom30c_CatalogoRecusadoComMotivo()
        {
            var erros = ValidadorDeMusica.Catalogo(new Catalogo { CatalogVersion = 1, Songs = { "BRILHA", "BRILHA", "NADA" } }, Cenario.Musicas);
            Assert.That(erros, Has.Some.Contains("catalog_version"));
            Assert.That(erros, Has.Some.Contains("repetida"));
            Assert.That(erros, Has.Some.Contains("NADA"));
        }

        [Test]
        public void FormatoDeIdaEVolta_MantemOsCamposDoArquivo()
        {
            var original = System.IO.File.ReadAllText(System.IO.Path.Combine(Cenario.PastaDoConteudo, "songs", "ODE.json"));
            var regravado = JsonSerializer.Serialize(Cenario.Ler<Musica>("songs/ODE.json"), Cenario.Json);
            var compacto = JsonSerializer.Serialize(JsonDocument.Parse(original).RootElement);
            Assert.That(regravado, Is.EqualTo(compacto));
        }
    }
}
