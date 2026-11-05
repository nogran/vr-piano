using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VrPiano.Dominio
{
    // Arquivo de progresso (06 §6). Só os campos que o domínio lê ou grava; idioma e calibração são do adapter.
    public sealed class Progresso
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, int> Stars { get; set; } = new Dictionary<string, int>();
        public List<string> DemoSeen { get; set; } = new List<string>();
        public Dictionary<string, Ritmo> Rhythm { get; set; } = new Dictionary<string, Ritmo>();
        public Sequencia Streak { get; set; } = new Sequencia();
        public UltimaMemoria? LastMemory { get; set; }
        public int Chain { get; set; }
        public string? LastSeenDay { get; set; }
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

        // preparoOk = checklist confirmado e calibração válida no VR; no app sem VR não há o que preparar.
        public static bool Aberto(Progresso p, Catalogo catalogo, string item, bool preparoOk)
        {
            if (item == catalogo.Song) return catalogo.Lessons.All(l => Estrelas(p, l) >= 2);
            var i = catalogo.Lessons.IndexOf(item);
            if (i < 0) return false;
            if (i == 0) return preparoOk;
            return Estrelas(p, catalogo.Lessons[i - 1]) >= 2;
        }

        public static IEnumerable<string> Itens(Catalogo catalogo) => catalogo.Lessons.Append(catalogo.Song);

        // "Entre lições" abre por padrão a lição aberta mais adiante.
        public static string ProximoItem(Progresso p, Catalogo catalogo, bool preparoOk) =>
            Itens(catalogo).LastOrDefault(i => Aberto(p, catalogo, i, preparoOk)) ?? catalogo.Lessons[0];

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

        // Item cuja revisão é pedida hoje, ou null. A revisão da música usa a última lição.
        public static string? ItemDaRevisao(Progresso p, Catalogo catalogo, Dia hoje)
        {
            if (p.LastMemory == null || !(Dia.Parse(p.LastMemory.Day) < hoje)) return null;
            return p.LastMemory.Item == catalogo.Song ? catalogo.Lessons[catalogo.Lessons.Count - 1] : p.LastMemory.Item;
        }

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
