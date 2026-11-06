using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace VrPiano.Dominio.Testes
{
    // T-BUILD-01 (04 §4): domain/ entra na Unity como pacote local e não pode depender dela.
    public class PacoteUnityTestes
    {
        static readonly string Pasta = Path.Combine(Path.GetDirectoryName(Cenario.PastaDoConteudo.TrimEnd(Path.DirectorySeparatorChar))!, "domain");

        [Test]
        public void Assembly_nao_referencia_a_engine()
        {
            using var asmdef = JsonDocument.Parse(File.ReadAllText(Path.Combine(Pasta, "VrPiano.Dominio.asmdef")));
            Assert.That(asmdef.RootElement.GetProperty("noEngineReferences").GetBoolean(), Is.True);
            Assert.That(asmdef.RootElement.GetProperty("references").GetArrayLength(), Is.Zero);
        }

        [Test]
        public void Fontes_nao_usam_Unity_nem_Android()
        {
            var proibidos = new[] { "UnityEngine", "UnityEditor", "Android", "OpenXR" };
            foreach (var fonte in Directory.GetFiles(Pasta, "*.cs", SearchOption.AllDirectories))
            {
                var texto = File.ReadAllText(fonte);
                Assert.That(proibidos.Where(texto.Contains), Is.Empty, fonte);
            }
        }

        // A Unity compilaria os .cs gerados em obj/ e veria a DLL de bin/ como um segundo VrPiano.Dominio.
        [Test]
        public void Saida_do_build_fica_fora_do_pacote()
        {
            Assert.That(Directory.Exists(Path.Combine(Pasta, "bin")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(Pasta, "obj")), Is.False);
        }
    }
}
