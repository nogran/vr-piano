using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VrPiano.Dominio
{
    // Arquivo de progresso (06 §6). Idioma, mão, checklist e calibração são do adapter; o domínio só os carrega na migração.
    public sealed class Progresso
    {
        public const int VersaoAtual = 2;

        public int SchemaVersion { get; set; } = VersaoAtual;
        public string? Language { get; set; }
        public bool HandGuide { get; set; } = true;
        public bool ChecklistConfirmed { get; set; }
        public object? Calibration { get; set; }
        public Dictionary<string, int> Stars { get; set; } = new Dictionary<string, int>();
        public List<string> DemoSeen { get; set; } = new List<string>();
        public Dictionary<string, Ritmo> Rhythm { get; set; } = new Dictionary<string, Ritmo>();
        public Sequencia Streak { get; set; } = new Sequencia();
        public UltimaMemoria? LastMemory { get; set; }
        public Dictionary<string, int> Chain { get; set; } = new Dictionary<string, int>();
        public List<string> Memorized { get; set; } = new List<string>();
        public string? LastSeenDay { get; set; }
    }

    // O que sobrevive de um arquivo v1 (RF-63). O resto era das 12 lições e é descartado.
    public sealed class ProgressoV1
    {
        public string? Language { get; set; }
        public bool HandGuide { get; set; } = true;
        public bool ChecklistConfirmed { get; set; }
        public object? Calibration { get; set; }
        public Sequencia Streak { get; set; } = new Sequencia();
        public string? LastSeenDay { get; set; }
    }

    // O quadro de progresso, pronto para desenhar (RF-66).
    public sealed class PainelDoJogo
    {
        public List<PainelDaMusica> Musicas { get; set; } = new List<PainelDaMusica>();
        public string Proximo { get; set; } = "";
        public Sequencia Sequencia { get; set; } = new Sequencia();
        public int Estrelas { get; set; }
        public int FrasesSemErro { get; set; }
        public int Decoradas { get; set; }
    }

    public sealed class PainelDaMusica
    {
        public string Id { get; set; } = "";
        public string TituloKey { get; set; } = "";
        public bool Aberta { get; set; }
        public bool Decorada { get; set; }
        public bool PodeOuvir { get; set; }
        public int Cadeia { get; set; }
        public List<PainelDoItem> Itens { get; set; } = new List<PainelDoItem>();
    }

    public sealed class PainelDoItem
    {
        public string Id { get; set; } = "";
        public int? Frase { get; set; }
        public bool Aberto { get; set; }
        public int Estrelas { get; set; }
        public Ritmo? Ritmo { get; set; }
    }

    public sealed class Sequencia
    {
        public int Current { get; set; }
        public int Best { get; set; }
        public string? LastCountedDay { get; set; }
    }

    public sealed class UltimaMemoria
    {
        public string Item { get; set; } = "";
        public string Day { get; set; } = "";
    }

    // Dia local como número, para a conta de dias seguidos. No arquivo fica AAAA-MM-DD.
    public readonly struct Dia : IEquatable<Dia>, IComparable<Dia>
    {
        static readonly DateTime Origem = new DateTime(2000, 1, 1);

        public Dia(int numero) { Numero = numero; }
        public int Numero { get; }

        public static Dia De(DateTime data) => new Dia((int)(data.Date - Origem).TotalDays);
        public static Dia Parse(string texto) => De(DateTime.ParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture));
        public static Dia? ParseOuNulo(string? texto) => texto == null ? (Dia?)null : Parse(texto);

        public override string ToString() => Origem.AddDays(Numero).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static Dia operator +(Dia d, int dias) => new Dia(d.Numero + dias);
        public static Dia operator -(Dia d, int dias) => new Dia(d.Numero - dias);
        public static bool operator <(Dia a, Dia b) => a.Numero < b.Numero;
        public static bool operator >(Dia a, Dia b) => a.Numero > b.Numero;
        public static bool operator ==(Dia a, Dia b) => a.Numero == b.Numero;
        public static bool operator !=(Dia a, Dia b) => a.Numero != b.Numero;
        public bool Equals(Dia outro) => Numero == outro.Numero;
        public override bool Equals(object? obj) => obj is Dia d && Equals(d);
        public override int GetHashCode() => Numero;
        public int CompareTo(Dia outro) => Numero.CompareTo(outro.Numero);
    }

    // Regras do 06 §8 que não dependem de uma lição em andamento.
    public static class RegrasDeProgresso
    {
        public static int Estrelas(Progresso p, string item) => p.Stars.TryGetValue(item, out var e) ? e : 0;

        public static int Cadeia(Progresso p, string musica) => p.Chain.TryGetValue(musica, out var n) ? n : 0;

        // preparoOk = checklist confirmado e calibração válida no VR; na versão web/ não há o que preparar.
        public static bool Aberto(Progresso p, Repertorio r, string item, bool preparoOk)
        {
            var (musica, trecho) = r.Analisar(item);
            var frases = r.ItensDasFrases(musica).ToList();
            if (trecho == null) return frases.All(f => Estrelas(p, f) >= 2);                 // RF-59
            var i = frases.IndexOf(item);
            if (i < 0) return false;
            if (i > 0) return Estrelas(p, frases[i - 1]) >= 2;                               // RF-58
            var ordem = r.Catalogo.Songs.IndexOf(musica.SongId);
            return ordem == 0 ? preparoOk : Estrelas(p, r.Catalogo.Songs[ordem - 1]) >= 2;   // RF-60
        }

        // O painel abre por padrão o item aberto mais adiante.
        public static string ProximoItem(Progresso p, Repertorio r, bool preparoOk) =>
            r.Itens().LastOrDefault(i => Aberto(p, r, i, preparoOk)) ?? r.Itens().First();

        // Na primeira abertura não há painel: a primeira frase começa direto.
        public static string? ItemInicial(Progresso p, Repertorio r) =>
            p.Stars.Count == 0 && p.DemoSeen.Count == 0 ? r.Itens().First() : null;

        public static PainelDoJogo Painel(Progresso p, Repertorio r, bool preparoOk) => new PainelDoJogo
        {
            Musicas = r.Musicas.Select(m => new PainelDaMusica
            {
                Id = m.SongId,
                TituloKey = m.TitleKey,
                Aberta = Aberto(p, r, r.ItensDaMusica(m).First(), preparoOk),
                Decorada = p.Memorized.Contains(m.SongId),
                PodeOuvir = p.DemoSeen.Contains(m.SongId),
                Cadeia = Cadeia(p, m.SongId),
                Itens = r.ItensDaMusica(m).Select(i => new PainelDoItem
                {
                    Id = i,
                    Frase = r.Analisar(i).Trecho is int trecho ? m.NumeroDaFrase(trecho) : (int?)null,
                    Aberto = Aberto(p, r, i, preparoOk),
                    Estrelas = Estrelas(p, i),
                    Ritmo = p.Rhythm.TryGetValue(i, out var ritmo) ? ritmo : null,
                }).ToList(),
            }).ToList(),
            Proximo = ProximoItem(p, r, preparoOk),
            Sequencia = p.Streak,
            Estrelas = r.Itens().Sum(i => Estrelas(p, i)),
            FrasesSemErro = r.Musicas.SelectMany(r.ItensDasFrases).Count(i => Estrelas(p, i) == 3),
            Decoradas = p.Memorized.Count,
        };

        // Na abertura: o dia nunca anda para trás, e uma sequência quebrada aparece como 0.
        public static Dia AoAbrir(Progresso p, Dia relogio)
        {
            var visto = Dia.ParseOuNulo(p.LastSeenDay);
            var hoje = visto != null && visto.Value > relogio ? visto.Value : relogio;
            p.LastSeenDay = hoje.ToString();
            var contado = Dia.ParseOuNulo(p.Streak.LastCountedDay);
            if (contado == null || contado.Value < hoje - 1) p.Streak.Current = 0;
            return hoje;
        }

        // Música cuja revisão é pedida hoje, ou null: a da última memória concluída noutro dia (RF-62).
        public static string? MusicaDaRevisao(Progresso p, Repertorio r, Dia hoje)
        {
            if (p.LastMemory == null || !(Dia.Parse(p.LastMemory.Day) < hoje)) return null;
            return r.Analisar(p.LastMemory.Item).Musica.SongId;
        }

        // Até qual trecho a revisão vai: a música toda, se ela já foi concluída; senão, o chain dela.
        public static int TrechosDaRevisao(Progresso p, Musica musica) =>
            Estrelas(p, musica.SongId) >= 2 ? musica.Chunks.Count : Math.Max(1, Cadeia(p, musica.SongId));

        // v1→v2 (RF-63): mantém dias e configurações; estrelas, demoSeen, rhythm, chain e lastMemory eram das lições.
        public static Progresso MigrarDaV1(ProgressoV1 v1) => new Progresso
        {
            Language = v1.Language,
            HandGuide = v1.HandGuide,
            ChecklistConfirmed = v1.ChecklistConfirmed,
            Calibration = v1.Calibration,
            Streak = v1.Streak,
            LastSeenDay = v1.LastSeenDay,
        };

        // Memória concluída no dia: devolve true se a sequência mudou.
        public static bool ContarDia(Progresso p, Dia hoje)
        {
            var s = p.Streak;
            var contado = Dia.ParseOuNulo(s.LastCountedDay);
            if (contado == hoje) return false;
            s.Current = contado == hoje - 1 ? s.Current + 1 : 1;
            s.Best = Math.Max(s.Best, s.Current);
            s.LastCountedDay = hoje.ToString();
            return true;
        }

        // Guarda a volta com maior fração no tempo; no empate, a mais recente.
        public static void GuardarRitmo(Progresso p, string item, Ritmo ritmo)
        {
            if (p.Rhythm.TryGetValue(item, out var salvo) && ritmo.Fracao() < salvo.Fracao()) return;
            p.Rhythm[item] = new Ritmo(ritmo.OnTime, ritmo.Judged);
        }
    }
}
