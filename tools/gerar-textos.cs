// Gera content/textos.json a partir das tabelas do docs/07. Rode na raiz do repositório: dotnet run tools/gerar-textos.cs
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

var linha = new Regex(@"^\| `([^`]+)` \| (.+?) \| (.+?) \|\s*$");
var textos = new List<(string Chave, string Pt, string En)>();
foreach (var texto in File.ReadLines("docs/07-textos-da-interface.md"))
{
    var m = linha.Match(texto);
    if (!m.Success) continue;
    if (textos.Any(t => t.Chave == m.Groups[1].Value)) throw new InvalidDataException($"chave repetida: {m.Groups[1].Value}");
    textos.Add((m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value));
}

using var arquivo = File.Create("content/textos.json");
using var json = new Utf8JsonWriter(arquivo, new JsonWriterOptions
{
    Indented = true,
    NewLine = "\n",
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
});
json.WriteStartObject();
json.WriteStartObject("pt-BR");
foreach (var t in textos) json.WriteString(t.Chave, t.Pt);
json.WriteEndObject();
json.WriteStartObject("en");
foreach (var t in textos) json.WriteString(t.Chave, t.En);
json.WriteEndObject();
json.WriteEndObject();
Console.WriteLine($"{textos.Count} chaves em content/textos.json");
