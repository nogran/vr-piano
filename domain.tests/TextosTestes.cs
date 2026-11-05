using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    // Tabela de textos (04 §4): content/textos.json é gerado do docs/07 e é a única origem dos textos da tela.
    public class TextosTestes
    {
        static readonly string Raiz = Path.GetDirectoryName(Cenario.PastaDoConteudo.TrimEnd(Path.DirectorySeparatorChar))!;
        static readonly Dictionary<string, Dictionary<string, string>> Textos =
            JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(Path.Combine(Cenario.PastaDoConteudo, "textos.json")))!;

        [Test]
        public void TTxt01_TodaChaveTemPtBrEEnNaoVazios()
        {
            Assert.That(Textos.Keys, Is.EquivalentTo(new[] { "pt-BR", "en" }));
            Assert.That(Textos["en"].Keys, Is.EquivalentTo(Textos["pt-BR"].Keys));
            Assert.That(Textos.Values.SelectMany(t => t.Values), Has.All.Not.Empty);
        }

        [Test]
        public void TTxt01b_TodaChaveUsadaNaTelaExiste()
        {
            var tela = File.ReadAllText(Path.Combine(Raiz, "web", "wwwroot", "main.js"));
            var usadas = Regex.Matches(tela, @"'((?:lesson|song|review|result|between|progress|pause|warn|midi|rhythm|shortcut|web|card|phrase|whole|vel)\.[\w.]+)'")
                .Select(m => m.Groups[1].Value).Distinct().ToList();
            Assert.That(usadas, Is.Not.Empty);
            Assert.That(usadas, Is.SubsetOf(Textos["pt-BR"].Keys));
        }

        [Test]
        public void TTxt01c_ChavesMontadasPeloConteudoExistem()
        {
            var passos = Cenario.Musicas.Values.SelectMany(m => Passo.DaMusica(m)).ToList();
            var chaves = passos.Select(p => $"note.{p.Nota}")
                .Concat(passos.Select(p => $"finger.{p.Dedo}"))
                .Concat(Cenario.Musicas.Values.Select(m => m.TitleKey)).Distinct();
            Assert.That(chaves, Is.SubsetOf(Textos["pt-BR"].Keys));
        }

        // A etiqueta é o texto mais longo que depende do conteúdo: título da música e número da frase juntos.
        [Test]
        public void RF65_EtiquetaCabeEm32CaracteresNosDoisIdiomas()
        {
            foreach (var idioma in Textos.Values)
                foreach (var musica in Cenario.Musicas.Values)
                {
                    var titulo = idioma[musica.TitleKey];
                    var frases = musica.TrechosNovos().Count();
                    Assert.That(idioma["card.whole"].Replace("{title}", titulo).Length, Is.LessThanOrEqualTo(32), titulo);
                    Assert.That(idioma["card.phrase"].Replace("{title}", titulo).Replace("{n}", frases.ToString()).Length, Is.LessThanOrEqualTo(32), titulo);
                }
            Assert.That(Textos["pt-BR"]["card.phrase"].Replace("{title}", "Brilha, Brilha").Replace("{n}", "2"), Is.EqualTo("Brilha, Brilha · Frase 2"));
            Assert.That(Textos["pt-BR"]["card.whole"].Replace("{title}", "Brilha, Brilha"), Is.EqualTo("Brilha, Brilha · Música inteira"));
        }

        [Test]
        public void RF66_PainelEResultadoNaoFalamMaisDeLicoes()
        {
            foreach (var chave in new[] { "progress.stars", "progress.mastered", "result.2" })
            {
                Assert.That(Textos["pt-BR"][chave], Does.Not.Contain("/36").And.Not.Contain("ição").And.Not.Contain("ições"), chave);
            }
        }

        [Test]
        public void TTxt03_ArquivoGeradoEstaIgualAoDocs07()
        {
            var linhas = File.ReadLines(Path.Combine(Raiz, "docs", "07-textos-da-interface.md"))
                .Select(l => Regex.Match(l, @"^\| `([^`]+)` \| (.+?) \| (.+?) \|\s*$")).Where(m => m.Success).ToList();
            Assert.That(Textos["pt-BR"], Is.EquivalentTo(linhas.ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value)));
            Assert.That(Textos["en"], Is.EquivalentTo(linhas.ToDictionary(m => m.Groups[1].Value, m => m.Groups[3].Value)));
        }
    }
}
