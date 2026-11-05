namespace VrPiano.Dominio
{
    // Parâmetros do 06 §7 que o domínio usa. Os de pose e calibração ficam nos adapters.
    public sealed class Parametros
    {
        public int TetoLeve = 49;                // P-01
        public int TetoMedio = 89;               // P-02
        public int NoteOnIguaisParaFixa = 8;     // P-03
        public long CaudaDoEco = 300;            // P-08
        public double FracaoSoando = 0.7;        // P-09
        public int VelocityDaDemo = 80;          // P-11
        public int CanalDeSaida = 1;             // P-12
        public int FalhasAteGuiada = 3;          // P-21
        public long PausaAntesDaMemoria = 1000;  // P-22
        public long AmbarDepoisDoErro = 1000;    // P-25
        public double CinzaInicial = 0.15;       // P-29
        public long ToleranciaAdiantado = 150;   // P-30
        public long ToleranciaAtrasado = 250;    // P-31
        public long SilencioAteDica = 3000;      // P-33

        // Intensidade do cinza dinâmico (06 §3.1, T-DOM-32).
        public double IntensidadeDoCinza(double progresso)
        {
            if (progresso < 0) progresso = 0;
            if (progresso > 1) progresso = 1;
            return CinzaInicial + (1 - CinzaInicial) * progresso;
        }

        public FaixaDeVelocity Faixa(int velocity) =>
            velocity <= TetoLeve ? FaixaDeVelocity.Leve : velocity <= TetoMedio ? FaixaDeVelocity.Medio : FaixaDeVelocity.Forte;
    }
}
