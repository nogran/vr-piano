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
            RegrasDeProgresso.Itens(catalogo).ToDictionary(id => id, id => Ler<Musica>($"content/songs/{id}.json"));

        static Progresso progresso = new();
        static Dia hoje;
        static SessaoDeLicao? sessao;

        public static void Main() { }

        [JSExport]
        public static string ValidarConteudo() => JsonSerializer.Serialize(ValidadorDeMusica.Catalogo(catalogo, musicas), Js);

        // Abre o progresso de um participante no dia dado (AAAA-MM-DD). Devolve o painel e a revisão pedida, se houver.
        [JSExport]
        public static string Abrir(string progressoJson, string dia)
        {
            progresso = string.IsNullOrWhiteSpace(progressoJson) ? new Progresso() : JsonSerializer.Deserialize<Progresso>(progressoJson, Js) ?? new Progresso();
            hoje = RegrasDeProgresso.AoAbrir(progresso, Dia.Parse(dia));
            sessao = null;
            return JsonSerializer.Serialize(new
            {
                hoje = hoje.ToString(),
                revisao = RegrasDeProgresso.ItemDaRevisao(progresso, catalogo, hoje),
                painel = MontarPainel(),
                progresso,
            }, Js);
        }

        [JSExport]
        public static string Painel() => JsonSerializer.Serialize(MontarPainel(), Js);

        [JSExport]
        public static string Iniciar(string item, bool revisao, double t)
        {
            sessao = new SessaoDeLicao(item, musicas[item], progresso, catalogo, hoje, revisao: revisao, musicaCompleta: musicas[catalogo.Song]);
            sessao.Iniciar(Ms(t));
            return Estado(incluirPassos: true);
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
            return Estado(incluirPassos: false);
        }

        static string Estado(bool incluirPassos)
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
                alvo = s.Fase == Fase.Guiada || s.Fase == Fase.Memoria ? new { nota = s.Alvo.Nota, dedo = s.Alvo.Dedo, licao = s.Alvo.LicaoDoTrecho } : null,
                tAcerto = s.TAcerto,
                tAtaque = s.TAtaque,
                emAtaque = s.EmAtaque,
                ofertas = s.Ofertas,
                juntando = s.Juntando,
                frase = s.Juntando ? s.Alvo.LicaoDoTrecho : (int?)null,
                frases = s.FrasesDaCadeia,
                motivo = s.MotivoDaPausa,
                estrelas = s.EstrelasDaExecucao,
                ritmo = s.RitmoDaExecucao,
                passos = incluirPassos ? s.Passos.Select(p => new { nota = p.Nota, dedo = p.Dedo, licao = p.LicaoDoTrecho }) : null,
            }, Js);
        }

        static object MontarPainel() => new
        {
            itens = RegrasDeProgresso.Itens(catalogo).Select(id => new
            {
                id,
                titulo = musicas[id].TitleKey,
                aberto = RegrasDeProgresso.Aberto(progresso, catalogo, id, preparoOk: true),
                estrelas = RegrasDeProgresso.Estrelas(progresso, id),
                ritmo = progresso.Rhythm.TryGetValue(id, out var r) ? r : null,
                notas = Passo.DaMusica(musicas[id]).Select(p => p.Nota),
            }),
            proximo = RegrasDeProgresso.ProximoItem(progresso, catalogo, preparoOk: true),
            sequencia = progresso.Streak,
            cadeia = progresso.Chain,
        };

        static long Ms(double t) => (long)Math.Round(t);

        static T Ler<T>(string nome)
        {
            using var stream = typeof(Motor).Assembly.GetManifestResourceStream(nome)
                ?? throw new FileNotFoundException(nome);
            return JsonSerializer.Deserialize<T>(stream, Arquivo)!;
        }
    }
}
