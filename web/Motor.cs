using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using VrPiano.Dominio;

[assembly: SupportedOSPlatform("browser")]

namespace VrPiano.Web
{
    // Ponte entre o JavaScript e o domínio. Tudo entra e sai como JSON; o tempo é o performance.now() do navegador.
    public static partial class Motor
    {
        static readonly JsonSerializerOptions Arquivo = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        static readonly JsonSerializerOptions Js = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        };

        static readonly Catalogo catalogo = Ler<Catalogo>("content/catalog.json");
        static readonly Dictionary<string, Musica> musicas =
            catalogo.Songs.ToDictionary(id => id, id => Ler<Musica>($"content/songs/{id}.json"));
        static readonly Repertorio repertorio = new Repertorio(catalogo, musicas);

        static Progresso progresso = new();
        static Dia hoje;
        static SessaoDeLicao? sessao;

        public static void Main() { }

        // Tabela de textos gerada do docs/07. O idioma é escolhido na tela.
        [JSExport]
        public static string Textos()
        {
            using var leitor = new StreamReader(Recurso("content/textos.json"));
            return leitor.ReadToEnd();
        }

        [JSExport]
        public static string ValidarConteudo() => JsonSerializer.Serialize(ValidadorDeMusica.Catalogo(catalogo, musicas), Js);

        // Abre o progresso de um participante no dia dado (AAAA-MM-DD). Devolve o painel e a revisão pedida, se houver.
        [JSExport]
        public static string Abrir(string progressoJson, string dia)
        {
            progresso = LerProgresso(progressoJson);
            hoje = RegrasDeProgresso.AoAbrir(progresso, Dia.Parse(dia));
            sessao = null;
            return JsonSerializer.Serialize(new
            {
                hoje = hoje.ToString(),
                revisao = RegrasDeProgresso.MusicaDaRevisao(progresso, repertorio, hoje),
                itemInicial = RegrasDeProgresso.ItemInicial(progresso, repertorio),
                painel = MontarPainel(),
                progresso,
            }, Js);
        }

        // Arquivo sem schemaVersion 2 é v1: passa pela migração (06 §6).
        static Progresso LerProgresso(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Progresso();
            using var documento = JsonDocument.Parse(json);
            var versao = documento.RootElement.TryGetProperty("schemaVersion", out var v) ? v.GetInt32() : 1;
            return versao >= Progresso.VersaoAtual
                ? JsonSerializer.Deserialize<Progresso>(json, Js) ?? new Progresso()
                : RegrasDeProgresso.MigrarDaV1(JsonSerializer.Deserialize<ProgressoV1>(json, Js) ?? new ProgressoV1());
        }

        [JSExport]
        public static string Painel() => JsonSerializer.Serialize(MontarPainel(), Js);

        [JSExport]
        public static string Iniciar(string item, bool revisao, double t)
        {
            sessao = new SessaoDeLicao(item, repertorio, progresso, hoje, revisao: revisao);
            sessao.Iniciar(Ms(t));
            return Estado();
        }

        // O botão "Ouvir a música" do painel: toca a música e volta, sem mudar o progresso.
        [JSExport]
        public static string OuvirMusica(string musica, double t)
        {
            sessao = new SessaoDeLicao(musica, repertorio, progresso, hoje, soOuvir: true);
            sessao.Iniciar(Ms(t));
            return Estado();
        }

        // Todos os passos da música, para a partitura desenhar a sequência da fase.
        [JSExport]
        public static string PassosDaMusica(string musica)
        {
            var m = repertorio.Musica(musica);
            return JsonSerializer.Serialize(new { bpm = m.Bpm, passos = Passo.DaMusica(m) }, Js);
        }

        // tipo: tick, noteOn, pose, ouvir, comGuia, desconectado, conectado, foco, continuar, sair.
        [JSExport]
        public static string Entrada(string tipo, int nota, int velocity, double t)
        {
            if (sessao == null) return "null";
            var ms = Ms(t);
            switch (tipo)
            {
                case "tick": sessao.Tick(ms); break;
                case "noteOn": sessao.NoteOn(nota, velocity, ms); break;
                case "pose": sessao.PoseMudou(true, ms); break;
                case "ouvir": sessao.Ouvir(ms); break;
                case "comGuia": sessao.ComGuia(ms); break;
                case "desconectado": sessao.MidiDesconectado(ms); break;
                case "conectado": sessao.MidiConectado(ms); break;
                case "foco": sessao.FocoPerdido(ms); break;
                case "continuar": sessao.Continuar(ms); break;
                case "sair": sessao.Sair(ms); break;
                default: throw new ArgumentException($"entrada desconhecida: {tipo}");
            }
            return Estado();
        }

        static string Estado()
        {
            var s = sessao!;
            var eventos = s.Coletar();
            return JsonSerializer.Serialize(new
            {
                eventos = eventos.Where(e => !(e is GravarProgresso)).Cast<object>().ToList(),
                progresso = eventos.Any(e => e is GravarProgresso) ? progresso : null,
                item = s.Item,
                revisao = s.Revisao,
                fase = s.Fase,
                passo = s.PassoAtual,
                alvo = s.Fase == Fase.Guiada || s.Fase == Fase.Memoria ? new { nota = s.Alvo.Nota, dedo = s.Alvo.Dedo } : null,
                tAcerto = s.TAcerto,
                tAtaque = s.TAtaque,
                emAtaque = s.EmAtaque,
                ofertas = s.Ofertas,
                juntando = s.Juntando,
                frase = s.Juntando ? s.Alvo.Trecho : (int?)null,
                frases = s.FrasesDaCadeia,
                etiqueta = s.Etiqueta,
                ancora = s.AncoraDaMao,
                nomesNasTeclas = s.NomesNasTeclas,
                decorou = s.Decorou,
                motivo = s.MotivoDaPausa,
                estrelas = s.EstrelasDaExecucao,
                ritmo = s.RitmoDaExecucao,
            }, Js);
        }

        static PainelDoJogo MontarPainel() => RegrasDeProgresso.Painel(progresso, repertorio, preparoOk: true);

        static long Ms(double t) => (long)Math.Round(t);

        static T Ler<T>(string nome)
        {
            using var stream = Recurso(nome);
            return JsonSerializer.Deserialize<T>(stream, Arquivo)!;
        }

        static Stream Recurso(string nome) =>
            typeof(Motor).Assembly.GetManifestResourceStream(nome) ?? throw new FileNotFoundException(nome);
    }
}
