using System;
using System.Collections.Generic;
using System.Linq;

namespace VrPiano.Dominio
{
    // Máquina de estados de uma execução de lição, música ou revisão (06 §3).
    // Entradas são métodos com o instante t em ms de um relógio monotônico; saídas se acumulam e saem em Coletar().
    // Ainda não implementado: detecção de rajada (P-04 a P-07) e a confirmação da falha de memória depois de P-05.
    public sealed class SessaoDeLicao
    {
        readonly Parametros p;
        readonly Progresso progresso;
        readonly Catalogo catalogo;
        readonly Dia hoje;
        readonly bool preparoOk;
        readonly List<Evento> saida = new List<Evento>();

        // Guiada
        bool voltaTemErro;
        int noTempo, avaliadas;
        long ignorarAte = long.MinValue;
        bool apenasUmaVolta;

        // Memória
        long ultimoAtivo;
        long fimDaEspera;

        // Juntar: as frases das lições 1 a N, de memória, depois da memória da lição N
        readonly IReadOnlyList<Passo>? cadeia;
        int inicioDaFrase;
        int falhasDaFrase;

        // Execução
        Ritmo? ritmoDaExecucao;

        // Demonstração
        readonly List<(long em, bool ligar, Passo passo)> agenda = new List<(long, bool, Passo)>();
        int proximoDaAgenda;
        int? notaSoando;
        Fase destinoDaDemo;

        // Pausa
        Fase faseAntesDaPausa;
        MotivoDaPausa? motivo;

        // Velocity
        int? ultimaVelocity;
        int velocityIguais;
        bool velocityFixa;

        readonly List<Oferta> ofertas = new List<Oferta>();
        bool ouvirUsado, guiaUsado;

        public SessaoDeLicao(string item, Musica musica, Progresso progresso, Catalogo catalogo, Dia hoje,
            Parametros? parametros = null, bool revisao = false, bool preparoOk = true, Musica? musicaCompleta = null)
        {
            Item = item;
            Passos = Passo.DaMusica(musica);
            this.progresso = progresso;
            this.catalogo = catalogo;
            this.hoje = hoje;
            this.preparoOk = preparoOk;
            p = parametros ?? new Parametros();
            Revisao = revisao;
            Fase = Fase.Pose;
            // Cada lição é um trecho da música, então juntar é tocar a música até o trecho da lição.
            var licao = catalogo.Lessons.IndexOf(item) + 1;
            if (musicaCompleta != null && !revisao && licao >= 2)
                cadeia = Passo.DaMusica(musicaCompleta).Where(x => x.LicaoDoTrecho <= licao).ToList();
        }

        public string Item { get; }
        public bool Revisao { get; }
        public IReadOnlyList<Passo> Passos { get; }
        public Fase Fase { get; private set; }
        public int PassoAtual { get; private set; }
        public bool Juntando { get; private set; }
        public IReadOnlyList<Passo>? Cadeia => cadeia;
        public int? FrasesDaCadeia => cadeia?[cadeia.Count - 1].LicaoDoTrecho;
        IReadOnlyList<Passo> Sequencia => Juntando ? cadeia! : Passos;
        public Passo Alvo => Sequencia[Math.Min(PassoAtual, Sequencia.Count - 1)];
        public long? TAcerto { get; private set; }
        public long? TAtaque { get; private set; }
        public bool EmAtaque { get; private set; }
        public int FalhasSeguidas { get; private set; }
        public bool TeveFalha { get; private set; }
        public bool UsouDica { get; private set; }
        public bool TeveVoltaLimpa => ritmoDaExecucao != null;
        public Ritmo? RitmoDaExecucao => ritmoDaExecucao;
        public int? EstrelasDaExecucao { get; private set; }
        public MotivoDaPausa? MotivoDaPausa => motivo;
        public IReadOnlyList<Oferta> Ofertas => ofertas;

        public List<Evento> Coletar()
        {
            var eventos = new List<Evento>(saida);
            saida.Clear();
            return eventos;
        }

        // Começo da execução: lição e música esperam a pose; revisão vai direto à memória.
        public void Iniciar(long t)
        {
            if (Revisao) EntrarMemoria(t);
            else Mudar(Fase.Pose);
        }

        public void PoseMudou(bool ok, long t)
        {
            if (Fase == Fase.Pose && ok) SairDaPose(t);
        }

        public void MaoAlternada(bool ligada, long t)
        {
            if (Fase == Fase.Pose && !ligada) SairDaPose(t);
        }

        public void Ouvir(long t)
        {
            if (Fase == Fase.Pose || (Fase == Fase.Guiada && !Revisao)) EntrarDemonstracao(t, Fase.Guiada);
            else if (Fase == Fase.Ofertas && ofertas.Contains(Oferta.Ouvir))
            {
                ouvirUsado = true;
                EntrarDemonstracao(t, Fase.Ofertas);
            }
        }

        public void ComGuia(long t)
        {
            if (Fase != Fase.Ofertas || !ofertas.Contains(Oferta.ComGuia)) return;
            guiaUsado = true;
            apenasUmaVolta = true;
            EntrarGuiada(t);
        }

        public void NoteOn(int nota, int velocity, long t)
        {
            Velocity(velocity);
            switch (Fase)
            {
                case Fase.Guiada: NoteOnGuiada(nota, t); break;
                case Fase.Memoria: NoteOnMemoria(nota, t); break;
                case Fase.Juntar: NoteOnJuntar(nota, t); break;
                case Fase.Ofertas:
                    // Em Ofertas, qualquer nota começa a tentativa e já conta como a primeira dela.
                    EntrarMemoria(t);
                    NoteOnMemoria(nota, t);
                    break;
            }
        }

        public void Tick(long t)
        {
            switch (Fase)
            {
                case Fase.Demonstracao: TickDemonstracao(t); break;
                case Fase.Guiada:
                    if (!EmAtaque && TAtaque != null && t >= TAtaque.Value)
                    {
                        EmAtaque = true;
                        saida.Add(new AtaqueChegou(Alvo.Nota));
                    }
                    break;
                case Fase.Transicao:
                    if (t < fimDaEspera) break;
                    if (Juntando) EntrarJuntar(t);
                    else EntrarMemoria(t);
                    break;
                case Fase.Memoria:
                    if (t - ultimoAtivo >= p.SilencioAteDica)
                    {
                        UsouDica = true;
                        ultimoAtivo = t;
                        saida.Add(new DicaDeMemoria(Alvo.Nota));
                    }
                    break;
                case Fase.Juntar:
                    if (t - ultimoAtivo >= p.SilencioAteDica)
                    {
                        ultimoAtivo = t;
                        saida.Add(new DicaDeMemoria(Alvo.Nota));
                    }
                    break;
                case Fase.RetornoDeErro:
                    if (t >= fimDaEspera) DepoisDoErro(t);
                    break;
            }
        }

        public void MidiDesconectado(long t) => Pausar(Dominio.MotivoDaPausa.Cabo, t);

        public void MidiConectado(long t)
        {
            if (Fase == Fase.Pausado && motivo == Dominio.MotivoDaPausa.Cabo) Retomar(t);
        }

        public void FocoPerdido(long t) => Pausar(Dominio.MotivoDaPausa.Foco, t);

        public void Continuar(long t)
        {
            if (Fase == Fase.Pausado && motivo == Dominio.MotivoDaPausa.Foco) Retomar(t);
        }

        // Sair no meio não muda estrelas: a de volta limpa já foi gravada na transição.
        public void Sair(long t)
        {
            if (Fase == Fase.Resultado || Fase == Fase.Encerrada) return;
            CalarDemonstracao(t);
            Mudar(Fase.Encerrada);
        }

        void SairDaPose(long t)
        {
            if (progresso.DemoSeen.Contains(Item)) EntrarGuiada(t);
            else EntrarDemonstracao(t, Fase.Guiada);
        }

        void EntrarDemonstracao(long t, Fase destino)
        {
            destinoDaDemo = destino;
            agenda.Clear();
            proximoDaAgenda = 0;
            notaSoando = null;
            var inicio = Passos[0].InicioMs;
            foreach (var passo in Passos)
            {
                var em = t + passo.InicioMs - inicio;
                agenda.Add((em, true, passo));
                agenda.Add((em + (long)Math.Round(passo.DuracaoMs * p.FracaoSoando), false, passo));
            }
            Mudar(Fase.Demonstracao);
            TickDemonstracao(t);
        }

        void TickDemonstracao(long t)
        {
            while (proximoDaAgenda < agenda.Count && agenda[proximoDaAgenda].em <= t)
            {
                var (em, ligar, passo) = agenda[proximoDaAgenda++];
                if (ligar)
                {
                    saida.Add(new EnviarNoteOn(passo.Nota, passo.Velocity ?? p.VelocityDaDemo, p.CanalDeSaida, em));
                    notaSoando = passo.Nota;
                }
                else
                {
                    saida.Add(new EnviarNoteOff(passo.Nota, p.CanalDeSaida, em));
                    if (notaSoando == passo.Nota) notaSoando = null;
                }
                saida.Add(new DemoNota(passo.Nota, passo.Dedo, ligar));
            }
            if (proximoDaAgenda < agenda.Count) return;

            if (!Revisao && !progresso.DemoSeen.Contains(Item))
            {
                progresso.DemoSeen.Add(Item);
                saida.Add(new DemoVista(Item));
                saida.Add(new GravarProgresso(progresso));
            }
            saida.Add(new MaoSome());
            ignorarAte = t + p.CaudaDoEco;
            if (destinoDaDemo == Fase.Ofertas) EntrarOfertas();
            else EntrarGuiada(t);
        }

        // Para a demonstração no meio: solta a nota que estiver soando, se ainda houver cabo.
        void CalarDemonstracao(long t)
        {
            if (Fase == Fase.Demonstracao && notaSoando != null)
                saida.Add(new EnviarNoteOff(notaSoando.Value, p.CanalDeSaida, t));
            notaSoando = null;
            agenda.Clear();
        }

        void EntrarGuiada(long t)
        {
            Mudar(Fase.Guiada);
            ReiniciarVolta();
        }

        void ReiniciarVolta()
        {
            PassoAtual = 0;
            voltaTemErro = false;
            noTempo = avaliadas = 0;
            TAcerto = null;
            TAtaque = null;
            EmAtaque = true;
            saida.Add(new AlvoMudou(Alvo));
        }

        void NoteOnGuiada(int nota, long t)
        {
            if (t < ignorarAte) return;
            var alvo = Alvo;
            if (nota != alvo.Nota)
            {
                voltaTemErro = true;
                saida.Add(new NotaErrada(nota, alvo.Nota));
                return;
            }

            saida.Add(new NotaCerta(nota));
            if (TAtaque != null)
            {
                var d = t - TAtaque.Value;
                var classe = d < -p.ToleranciaAdiantado ? ClasseDeRitmo.Adiantado
                    : d > p.ToleranciaAtrasado ? ClasseDeRitmo.Atrasado
                    : ClasseDeRitmo.NoTempo;
                avaliadas++;
                if (classe == ClasseDeRitmo.NoTempo) noTempo++;
                saida.Add(new RitmoAvaliado(nota, d, classe));
            }

            PassoAtual++;
            if (PassoAtual < Passos.Count)
            {
                var proximo = Alvo;
                TAcerto = t;
                TAtaque = t + (proximo.DeltaMs ?? 0);
                EmAtaque = false;
                saida.Add(new AlvoMudou(proximo));
                saida.Add(new ProximaMudou(proximo.Nota, t, TAtaque.Value, proximo.Nota == nota));
                return;
            }

            var ritmo = new Ritmo(noTempo, avaliadas);
            var limpa = !voltaTemErro;
            saida.Add(new VoltaConcluida(limpa, ritmo));
            if (apenasUmaVolta)
            {
                apenasUmaVolta = false;
                EntrarOfertas();
            }
            else if (limpa) EntrarTransicao(t, ritmo);
            else ReiniciarVolta();
        }

        void EntrarTransicao(long t, Ritmo ritmo)
        {
            if (ritmoDaExecucao == null)
            {
                ritmoDaExecucao = ritmo;
                if (RegrasDeProgresso.Estrelas(progresso, Item) < 1) MudarEstrelas(1);
            }
            fimDaEspera = t + p.PausaAntesDaMemoria;
            Mudar(Fase.Transicao);
        }

        void EntrarMemoria(long t)
        {
            PassoAtual = 0;
            ultimoAtivo = t;
            Mudar(Fase.Memoria);
        }

        void NoteOnMemoria(int nota, long t)
        {
            var alvo = Alvo;
            if (nota != alvo.Nota)
            {
                FalhasSeguidas++;
                TeveFalha = true;
                saida.Add(new TentativaFalhou(nota, alvo.Nota, FalhasSeguidas));
                fimDaEspera = t + p.AmbarDepoisDoErro;
                Mudar(Fase.RetornoDeErro);
                return;
            }
            ultimoAtivo = t;
            PassoAtual++;
            if (PassoAtual == Passos.Count) ConcluirMemoria(nota, t);
        }

        void EntrarJuntar(long t)
        {
            PassoAtual = inicioDaFrase;
            ultimoAtivo = t;
            Mudar(Fase.Juntar);
            AjudarNaFrase();
        }

        // Errar volta ao começo da frase, não ao começo de tudo: a sequência chega a 50 notas.
        void NoteOnJuntar(int nota, long t)
        {
            var alvo = Alvo;
            if (nota != alvo.Nota)
            {
                falhasDaFrase++;
                saida.Add(new JuntarFalhou(nota, alvo.Nota, falhasDaFrase));
                fimDaEspera = t + p.AmbarDepoisDoErro;
                Mudar(Fase.RetornoDeErro);
                return;
            }
            ultimoAtivo = t;
            PassoAtual++;
            if (PassoAtual == cadeia!.Count)
            {
                ConcluirCadeia();
                return;
            }
            if (Alvo.LicaoDoTrecho != alvo.LicaoDoTrecho)
            {
                inicioDaFrase = PassoAtual;
                falhasDaFrase = 0;
                saida.Add(new FraseJuntada(alvo.LicaoDoTrecho));
            }
            AjudarNaFrase();
        }

        // Depois de P-21 falhas na mesma frase, cada tecla dela pisca como dica, para ninguém ficar preso.
        void AjudarNaFrase()
        {
            if (falhasDaFrase >= p.FalhasAteGuiada) saida.Add(new DicaDeMemoria(Alvo.Nota));
        }

        void ConcluirCadeia()
        {
            var ate = FrasesDaCadeia!.Value;
            progresso.Chain = Math.Max(progresso.Chain, ate);
            saida.Add(new CadeiaConcluida(ate));
            saida.Add(new GravarProgresso(progresso));
            Mudar(Fase.Resultado);
        }

        void DepoisDoErro(long t)
        {
            if (Juntando) EntrarJuntar(t);
            else if (Revisao) EntrarOfertas();
            else if (FalhasSeguidas >= p.FalhasAteGuiada)
            {
                FalhasSeguidas = 0;
                saida.Add(new VoltouAoGuiado());
                EntrarGuiada(t);
            }
            else EntrarMemoria(t);
        }

        void EntrarOfertas()
        {
            ofertas.Clear();
            if (!ouvirUsado) ofertas.Add(Oferta.Ouvir);
            if (!guiaUsado) ofertas.Add(Oferta.ComGuia);
            Mudar(Fase.Ofertas);
            saida.Add(new OfertasMudaram(ofertas.ToList()));
        }

        void ConcluirMemoria(int ultimaNota, long t)
        {
            saida.Add(new MemoriaConcluida(ultimaNota, !TeveFalha, UsouDica));
            if (Revisao)
            {
                saida.Add(new RevisaoConcluida(Item));
                ContarDia();
                if (progresso.LastMemory != null) progresso.LastMemory.Day = hoje.ToString();
            }
            else
            {
                EstrelasDaExecucao = TeveFalha || UsouDica ? 2 : 3;
                if (EstrelasDaExecucao.Value > RegrasDeProgresso.Estrelas(progresso, Item)) MudarEstrelas(EstrelasDaExecucao.Value);
                if (ritmoDaExecucao != null) RegrasDeProgresso.GuardarRitmo(progresso, Item, ritmoDaExecucao);
                ContarDia();
                progresso.LastMemory = new UltimaMemoria { Item = Item, Day = hoje.ToString() };
            }
            saida.Add(new GravarProgresso(progresso));
            if (cadeia == null)
            {
                Mudar(Fase.Resultado);
                return;
            }
            Juntando = true;
            PassoAtual = 0;
            fimDaEspera = t + p.PausaAntesDaMemoria;
            Mudar(Fase.Transicao);
        }

        void MudarEstrelas(int para)
        {
            var abertosAntes = RegrasDeProgresso.Itens(catalogo).Where(i => RegrasDeProgresso.Aberto(progresso, catalogo, i, preparoOk)).ToList();
            var de = RegrasDeProgresso.Estrelas(progresso, Item);
            progresso.Stars[Item] = para;
            saida.Add(new EstrelasMudaram(Item, de, para));
            foreach (var i in RegrasDeProgresso.Itens(catalogo))
                if (!abertosAntes.Contains(i) && RegrasDeProgresso.Aberto(progresso, catalogo, i, preparoOk))
                    saida.Add(new ItemDesbloqueado(i));
            saida.Add(new GravarProgresso(progresso));
        }

        void ContarDia()
        {
            if (RegrasDeProgresso.ContarDia(progresso, hoje))
                saida.Add(new DiaSeguidoMudou(progresso.Streak.Current, progresso.Streak.Best));
        }

        void Pausar(MotivoDaPausa m, long t)
        {
            if (Fase == Fase.Resultado || Fase == Fase.Encerrada || Fase == Fase.Pausado) return;
            if (m != Dominio.MotivoDaPausa.Cabo) CalarDemonstracao(t);
            else { notaSoando = null; agenda.Clear(); }
            faseAntesDaPausa = Fase;
            motivo = m;
            Mudar(Fase.Pausado);
            saida.Add(new LicaoPausada(m));
        }

        void Retomar(long t)
        {
            motivo = null;
            saida.Add(new LicaoRetomada());
            switch (faseAntesDaPausa)
            {
                case Fase.Demonstracao:
                    if (Revisao) EntrarOfertas();
                    else Mudar(Fase.Pose);
                    break;
                case Fase.Guiada:
                    // Mesmo passo, já em Ataque; a nota da retomada não tem avaliação de ritmo.
                    TAcerto = null;
                    TAtaque = null;
                    EmAtaque = true;
                    Mudar(Fase.Guiada);
                    saida.Add(new AlvoMudou(Alvo));
                    break;
                case Fase.Memoria:
                    EntrarMemoria(t);
                    break;
                case Fase.Juntar:
                    EntrarJuntar(t);
                    break;
                case Fase.Transicao:
                    fimDaEspera = t + p.PausaAntesDaMemoria;
                    Mudar(Fase.Transicao);
                    break;
                case Fase.RetornoDeErro:
                    fimDaEspera = t + p.AmbarDepoisDoErro;
                    Mudar(Fase.RetornoDeErro);
                    break;
                case Fase.Ofertas:
                    EntrarOfertas();
                    break;
                default:
                    Mudar(faseAntesDaPausa);
                    break;
            }
        }

        void Velocity(int velocity)
        {
            saida.Add(new FaixaMudou(p.Faixa(velocity)));
            velocityIguais = velocity == ultimaVelocity ? velocityIguais + 1 : 1;
            ultimaVelocity = velocity;
            if (!velocityFixa && velocityIguais >= p.NoteOnIguaisParaFixa)
            {
                velocityFixa = true;
                saida.Add(new VelocityFixa(true));
            }
            else if (velocityFixa && velocityIguais == 1)
            {
                velocityFixa = false;
                saida.Add(new VelocityFixa(false));
            }
        }

        void Mudar(Fase fase)
        {
            Fase = fase;
            saida.Add(new FaseMudou(fase, Item));
        }
    }
}
