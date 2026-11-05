using System.Collections.Generic;

namespace VrPiano.Dominio
{
    public enum Fase { Pose, Demonstracao, Guiada, Transicao, Memoria, Juntar, RetornoDeErro, Ofertas, Resultado, Pausado, Encerrada }
    public enum MotivoDaPausa { Cabo, Rajada, Foco }
    public enum ClasseDeRitmo { NoTempo, Adiantado, Atrasado }
    public enum FaixaDeVelocity { Leve, Medio, Forte }
    public enum Oferta { Ouvir, ComGuia }
    public enum TipoDeEtiqueta { Frase, MusicaInteira, Revisao }

    // O que o card mostra em cima (RF-65). O domínio diz o quê; o texto vem da tabela do 07.
    public sealed class EtiquetaDoCard
    {
        public EtiquetaDoCard(TipoDeEtiqueta tipo, string tituloKey, int? frase) { Tipo = tipo; TituloKey = tituloKey; Frase = frase; }
        public TipoDeEtiqueta Tipo { get; }
        public string TituloKey { get; }
        public int? Frase { get; }
    }

    public sealed class Ritmo
    {
        public Ritmo(int onTime, int judged) { OnTime = onTime; Judged = judged; }
        public Ritmo() { }

        // Nomes do arquivo de progresso (06 §6).
        public int OnTime { get; set; }
        public int Judged { get; set; }

        public double Fracao() => Judged == 0 ? 0 : (double)OnTime / Judged;
    }

    // Saídas do domínio (06 §4). Tipo existe para o adapter web serializar sem conhecer as classes.
    public abstract class Evento
    {
        public string Tipo => GetType().Name;
    }

    public sealed class FaseMudou : Evento
    {
        public FaseMudou(Fase fase, string item) { Fase = fase; Item = item; }
        public Fase Fase { get; }
        public string Item { get; }
    }

    public sealed class AlvoMudou : Evento
    {
        public AlvoMudou(Passo p) { Nota = p.Nota; Dedo = p.Dedo; Indice = p.Indice; Trecho = p.Trecho; Frase = p.Frase; }
        public int Nota { get; }
        public int Dedo { get; }
        public int Indice { get; }
        public int Trecho { get; }
        public int Frase { get; }
    }

    public sealed class ProximaMudou : Evento
    {
        public ProximaMudou(int nota, long tAcerto, long tAtaque, bool mesmaTecla) { Nota = nota; TAcerto = tAcerto; TAtaque = tAtaque; MesmaTecla = mesmaTecla; }
        public int Nota { get; }
        public long TAcerto { get; }
        public long TAtaque { get; }
        public bool MesmaTecla { get; }
    }

    public sealed class AtaqueChegou : Evento
    {
        public AtaqueChegou(int nota) { Nota = nota; }
        public int Nota { get; }
    }

    public sealed class NotaCerta : Evento
    {
        public NotaCerta(int nota) { Nota = nota; }
        public int Nota { get; }
    }

    public sealed class NotaErrada : Evento
    {
        public NotaErrada(int tocada, int esperada) { Tocada = tocada; Esperada = esperada; }
        public int Tocada { get; }
        public int Esperada { get; }
    }

    public sealed class RitmoAvaliado : Evento
    {
        public RitmoAvaliado(int nota, long d, ClasseDeRitmo classe) { Nota = nota; D = d; Classe = classe; }
        public int Nota { get; }
        public long D { get; }
        public ClasseDeRitmo Classe { get; }
    }

    public sealed class VoltaConcluida : Evento
    {
        public VoltaConcluida(bool limpa, Ritmo ritmo) { Limpa = limpa; Ritmo = ritmo; }
        public bool Limpa { get; }
        public Ritmo Ritmo { get; }
    }

    public sealed class DicaDeMemoria : Evento
    {
        public DicaDeMemoria(int nota) { Nota = nota; }
        public int Nota { get; }
    }

    public sealed class TentativaFalhou : Evento
    {
        public TentativaFalhou(int tocada, int esperada, int falhasSeguidas) { Tocada = tocada; Esperada = esperada; FalhasSeguidas = falhasSeguidas; }
        public int Tocada { get; }
        public int Esperada { get; }
        public int FalhasSeguidas { get; }
    }

    public sealed class VoltouAoGuiado : Evento { }

    public sealed class JuntarFalhou : Evento
    {
        public JuntarFalhou(int tocada, int esperada, int falhasDaFrase) { Tocada = tocada; Esperada = esperada; FalhasDaFrase = falhasDaFrase; }
        public int Tocada { get; }
        public int Esperada { get; }
        public int FalhasDaFrase { get; }
    }

    public sealed class FraseJuntada : Evento
    {
        public FraseJuntada(int frase) { Frase = frase; }
        public int Frase { get; }
    }

    public sealed class CadeiaConcluida : Evento
    {
        public CadeiaConcluida(int ateOTrecho) { AteOTrecho = ateOTrecho; }
        public int AteOTrecho { get; }
    }

    public sealed class MemoriaConcluida : Evento
    {
        public MemoriaConcluida(int nota, bool primeiraTentativa, bool usouDica) { Nota = nota; PrimeiraTentativa = primeiraTentativa; UsouDica = usouDica; }
        public int Nota { get; }
        public bool PrimeiraTentativa { get; }
        public bool UsouDica { get; }
    }

    public sealed class DemoNota : Evento
    {
        public DemoNota(int nota, int dedo, bool ligada) { Nota = nota; Dedo = dedo; Ligada = ligada; }
        public int Nota { get; }
        public int Dedo { get; }
        public bool Ligada { get; }
    }

    public sealed class DemoVista : Evento
    {
        public DemoVista(string item) { Item = item; }
        public string Item { get; }
    }

    public sealed class MaoSome : Evento { }

    // Em = instante agendado; o adapter pode usar para mandar o MIDI com carimbo de tempo.
    public sealed class EnviarNoteOn : Evento
    {
        public EnviarNoteOn(int nota, int velocity, int canal, long em) { Nota = nota; Velocity = velocity; Canal = canal; Em = em; }
        public int Nota { get; }
        public int Velocity { get; }
        public int Canal { get; }
        public long Em { get; }
    }

    public sealed class EnviarNoteOff : Evento
    {
        public EnviarNoteOff(int nota, int canal, long em) { Nota = nota; Canal = canal; Em = em; }
        public int Nota { get; }
        public int Canal { get; }
        public long Em { get; }
    }

    public sealed class EstrelasMudaram : Evento
    {
        public EstrelasMudaram(string item, int de, int para) { Item = item; De = de; Para = para; }
        public string Item { get; }
        public int De { get; }
        public int Para { get; }
    }

    public sealed class ItemDesbloqueado : Evento
    {
        public ItemDesbloqueado(string item) { Item = item; }
        public string Item { get; }
    }

    // A primeira frase de uma música abriu (RF-60).
    public sealed class MusicaDesbloqueada : Evento
    {
        public MusicaDesbloqueada(string musica, string tituloKey) { Musica = musica; TituloKey = tituloKey; }
        public string Musica { get; }
        public string TituloKey { get; }
    }

    public sealed class MusicaDecorada : Evento
    {
        public MusicaDecorada(string musica) { Musica = musica; }
        public string Musica { get; }
    }

    public sealed class DiaSeguidoMudou : Evento
    {
        public DiaSeguidoMudou(int atual, int maior) { Atual = atual; Maior = maior; }
        public int Atual { get; }
        public int Maior { get; }
    }

    public sealed class RevisaoConcluida : Evento
    {
        public RevisaoConcluida(string item) { Item = item; }
        public string Item { get; }
    }

    public sealed class OfertasMudaram : Evento
    {
        public OfertasMudaram(IReadOnlyList<Oferta> ofertas) { Ofertas = ofertas; }
        public IReadOnlyList<Oferta> Ofertas { get; }
    }

    public sealed class LicaoPausada : Evento
    {
        public LicaoPausada(MotivoDaPausa motivo) { Motivo = motivo; }
        public MotivoDaPausa Motivo { get; }
    }

    public sealed class LicaoRetomada : Evento { }

    public sealed class FaixaMudou : Evento
    {
        public FaixaMudou(FaixaDeVelocity faixa) { Faixa = faixa; }
        public FaixaDeVelocity Faixa { get; }
    }

    public sealed class VelocityFixa : Evento
    {
        public VelocityFixa(bool ligada) { Ligada = ligada; }
        public bool Ligada { get; }
    }

    public sealed class GravarProgresso : Evento
    {
        public GravarProgresso(Progresso progresso) { Progresso = progresso; }
        public Progresso Progresso { get; }
    }
}
