using System;
using System.Collections.Generic;
using System.Linq;

namespace VrPiano.Dominio
{
    // Máquina de estados de uma execução de frase, música inteira ou revisão (06 §3).
    // Entradas são métodos com o instante t em ms de um relógio monotônico; saídas se acumulam e saem em Coletar().
    // Ainda não implementado: detecção de rajada (P-04 a P-07) e a confirmação da falha de memória depois de P-05.
    public sealed class SessaoDeLicao
    {
        readonly Parametros p;
        readonly Progresso progresso;
        readonly Repertorio repertorio;
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

        // Juntar: os trechos 1 a k, de memória. Serve ao Juntar da frase, à música inteira e à revisão.
        readonly IReadOnlyList<Passo>? cadeia;
        int inicioDaFrase;
        int falhasDaFrase;
        IReadOnlyList<Passo>? guiaDoTrecho;   // revisão: a volta com guia percorre só o trecho do erro

        // Execução
        Ritmo? ritmoDaExecucao;

        // Demonstração
        readonly List<(long em, bool ligar, Passo passo)> agenda = new List<(long, bool, Passo)>();
        int proximoDaAgenda;
        int? notaSoando;
        Fase destinoDaDemo;
        bool ouvindoMusica;
        readonly bool soOuvir;

        // Pausa
        Fase faseAntesDaPausa;
        MotivoDaPausa? motivo;

        // Velocity
        int? ultimaVelocity;
        int velocityIguais;
        bool velocityFixa;

        readonly List<Oferta> ofertas = new List<Oferta>();
        bool ouvirUsado, guiaUsado;

        // item: "BRILHA#2" (frase), "BRILHA" (música inteira) ou, com revisao ou soOuvir, a música.
        public SessaoDeLicao(string item, Repertorio repertorio, Progresso progresso, Dia hoje,
            Parametros? parametros = null, bool revisao = false, bool preparoOk = true, bool soOuvir = false)
        {
            Item = item;
            this.progresso = progresso;
            this.repertorio = repertorio;
            this.hoje = hoje;
            this.preparoOk = preparoOk;
            this.soOuvir = soOuvir;
            p = parametros ?? new Parametros();
            Revisao = revisao;
            Fase = Fase.Pose;

            var (musica, trecho) = repertorio.Analisar(item);
            Musica = musica;
            passosDaMusica = Passo.DaMusica(musica);
            if (revisao)
            {
                var ate = RegrasDeProgresso.TrechosDaRevisao(progresso, musica);
                cadeia = passosDaMusica.Where(x => x.Trecho <= ate).ToList();
                Passos = cadeia;
            }
            else if (trecho == null)
            {
                MusicaInteira = true;
                cadeia = passosDaMusica;
                Passos = passosDaMusica;
            }
            else
            {
                numeroDaFrase = musica.NumeroDaFrase(trecho.Value);
                Passos = passosDaMusica.Where(x => x.Trecho == trecho.Value).ToList();
                if (numeroDaFrase > 1) cadeia = passosDaMusica.Where(x => x.Trecho <= trecho.Value).ToList();
            }
        }

        readonly IReadOnlyList<Passo> passosDaMusica;
        readonly int numeroDaFrase;

        public string Item { get; }
        public Musica Musica { get; }
        public bool Revisao { get; }
        public bool MusicaInteira { get; }
        public IReadOnlyList<Passo> Passos { get; }
        public Fase Fase { get; private set; }
        public int PassoAtual { get; private set; }
        public bool Juntando { get; private set; }
        public IReadOnlyList<Passo>? Cadeia => cadeia;
        public int? FrasesDaCadeia => cadeia?[cadeia.Count - 1].Trecho;
        IReadOnlyList<Passo> Sequencia => guiaDoTrecho ?? (Juntando ? cadeia! : Passos);
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
        public bool Decorou { get; private set; }

        // Tecla branca do polegar da mão 3D (RF-64). Na demonstração, ela anda quando a nota solta, antes da próxima.
        // Na vez guiada, fica onde o dedo da vez alcança a tecla da vez.
        int ancoraDaDemo = Mao.AncoraDaPose;
        public int AncoraDaMao => Fase == Fase.Guiada ? Alvo.Ancora : ancoraDaDemo;

        // Nomes das notas nas teclas brancas (RF-75). Somem em tudo que faz parte de tocar de memória, inclusive na
        // pausa no meio dela, para a pausa não virar um jeito de espiar.
        public bool NomesNasTeclas
        {
            get
            {
                var fase = Fase == Fase.Pausado ? faseAntesDaPausa : Fase;
                return fase != Fase.Transicao && fase != Fase.Memoria && fase != Fase.Juntar
                    && fase != Fase.RetornoDeErro && fase != Fase.Ofertas;
            }
        }

        public EtiquetaDoCard Etiqueta =>
            Revisao ? new EtiquetaDoCard(TipoDeEtiqueta.Revisao, Musica.TitleKey, null)
            : MusicaInteira || ouvindoMusica ? new EtiquetaDoCard(TipoDeEtiqueta.MusicaInteira, Musica.TitleKey, null)
            : new EtiquetaDoCard(TipoDeEtiqueta.Frase, Musica.TitleKey, numeroDaFrase);

        public List<Evento> Coletar()
        {
            var eventos = new List<Evento>(saida);
            saida.Clear();
            return eventos;
        }

        // Começo da execução: frase e música inteira esperam a pose; revisão vai direto à memória.
        // Na primeira vez numa música, ela toca inteira antes da frase 1 (RF-54).
        public void Iniciar(long t)
        {
            if (soOuvir) OuvirMusica(t, Fase.Encerrada);
            else if (Revisao)
            {
                Juntando = true;
                EntrarJuntar(t);
            }
            else if (numeroDaFrase == 1 && !progresso.DemoSeen.Contains(Musica.SongId)) OuvirMusica(t, Fase.Pose);
            else EntrarPose();
        }

        void EntrarPose()
        {
            ancoraDaDemo = Mao.AncoraDaPose;
            Mudar(Fase.Pose);
        }

        void OuvirMusica(long t, Fase destino)
        {
            ouvindoMusica = true;
            EntrarDemonstracao(t, destino, passosDaMusica);
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
            if (Fase == Fase.Pose || (Fase == Fase.Guiada && !Revisao)) EntrarDemonstracao(t, Fase.Guiada, Passos);
            else if (Fase == Fase.Ofertas && ofertas.Contains(Oferta.Ouvir))
            {
                ouvirUsado = true;
                EntrarDemonstracao(t, Fase.Ofertas, PassosDoTrechoAtual());
            }
        }

        public void ComGuia(long t)
        {
            if (Fase != Fase.Ofertas || !ofertas.Contains(Oferta.ComGuia)) return;
            guiaUsado = true;
            apenasUmaVolta = true;
            guiaDoTrecho = PassosDoTrechoAtual();
            EntrarGuiada(t);
        }

        IReadOnlyList<Passo> PassosDoTrechoAtual()
        {
            var trecho = cadeia![inicioDaFrase].Trecho;
            return cadeia.Where(x => x.Trecho == trecho).ToList();
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
                    // Em Ofertas, qualquer nota recomeça o trecho e já conta como a primeira dele.
                    EntrarJuntar(t);
                    NoteOnJuntar(nota, t);
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
                        UsouDica = true;
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
            if (MusicaInteira) EntrarCadeia(t);
            else if (progresso.DemoSeen.Contains(Item)) EntrarGuiada(t);
            else EntrarDemonstracao(t, Fase.Guiada, Passos);
        }

        void EntrarDemonstracao(long t, Fase destino, IReadOnlyList<Passo> passos)
        {
            destinoDaDemo = destino;
            agenda.Clear();
            proximoDaAgenda = 0;
            notaSoando = null;
            var inicio = passos[0].InicioMs;
            foreach (var passo in passos)
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
                    ancoraDaDemo = passo.Ancora;
                }
                else
                {
                    saida.Add(new EnviarNoteOff(passo.Nota, p.CanalDeSaida, em));
                    if (notaSoando == passo.Nota) notaSoando = null;
                    if (proximoDaAgenda < agenda.Count) ancoraDaDemo = agenda[proximoDaAgenda].passo.Ancora;
                }
                saida.Add(new DemoNota(passo.Nota, passo.Dedo, ligar));
            }
            if (proximoDaAgenda < agenda.Count) return;

            var ouvido = ouvindoMusica ? Musica.SongId : Item;
            if (!Revisao && !progresso.DemoSeen.Contains(ouvido))
            {
                progresso.DemoSeen.Add(ouvido);
                saida.Add(new DemoVista(ouvido));
                saida.Add(new GravarProgresso(progresso));
            }
            ouvindoMusica = false;
            ignorarAte = t + p.CaudaDoEco;
            if (destinoDaDemo == Fase.Pose)
            {
                EntrarPose();
                return;
            }
            saida.Add(new MaoSome());
            if (destinoDaDemo == Fase.Encerrada) Mudar(Fase.Encerrada);
            else if (destinoDaDemo == Fase.Ofertas) EntrarOfertas();
            else if (MusicaInteira) EntrarCadeia(t);
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
            if (PassoAtual < Sequencia.Count)
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
                guiaDoTrecho = null;
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

        // Da memória da frase ou da pose da música inteira: espera P-22 e começa a tocar a cadeia de memória.
        void EntrarCadeia(long t)
        {
            Juntando = true;
            PassoAtual = 0;
            fimDaEspera = t + p.PausaAntesDaMemoria;
            Mudar(Fase.Transicao);
        }

        void EntrarJuntar(long t)
        {
            PassoAtual = inicioDaFrase;
            ultimoAtivo = t;
            Mudar(Fase.Juntar);
            AjudarNaFrase();
        }

        // Errar volta ao começo do trecho, não ao começo de tudo: a sequência chega a 62 notas.
        void NoteOnJuntar(int nota, long t)
        {
            var alvo = Alvo;
            if (nota != alvo.Nota)
            {
                falhasDaFrase++;
                TeveFalha = true;
                saida.Add(new JuntarFalhou(nota, alvo.Nota, falhasDaFrase));
                fimDaEspera = t + p.AmbarDepoisDoErro;
                Mudar(Fase.RetornoDeErro);
                return;
            }
            ultimoAtivo = t;
            PassoAtual++;
            if (PassoAtual == cadeia!.Count)
            {
                ConcluirCadeia(nota);
                return;
            }
            if (Alvo.Trecho != alvo.Trecho)
            {
                inicioDaFrase = PassoAtual;
                falhasDaFrase = 0;
                saida.Add(new FraseJuntada(alvo.Trecho));
            }
            AjudarNaFrase();
        }

        // Depois de P-21 falhas na mesma frase, cada tecla dela pisca como dica, para ninguém ficar preso.
        void AjudarNaFrase()
        {
            if (falhasDaFrase < p.FalhasAteGuiada) return;
            UsouDica = true;
            saida.Add(new DicaDeMemoria(Alvo.Nota));
        }

        void ConcluirCadeia(int ultimaNota)
        {
            if (Revisao) ConcluirRevisao(ultimaNota);
            else if (MusicaInteira) ConcluirMusicaInteira(ultimaNota);
            else
            {
                var ate = FrasesDaCadeia!.Value;
                GuardarCadeia(ate);
                saida.Add(new CadeiaConcluida(ate));
            }
            saida.Add(new GravarProgresso(progresso));
            Mudar(Fase.Resultado);
        }

        void ConcluirRevisao(int ultimaNota)
        {
            saida.Add(new MemoriaConcluida(ultimaNota, !TeveFalha, UsouDica));
            saida.Add(new RevisaoConcluida(Item));
            // Só a música já concluída noutro dia decora (RF-61).
            var inteira = cadeia!.Count == passosDaMusica.Count && RegrasDeProgresso.Estrelas(progresso, Musica.SongId) >= 2;
            var outroDia = progresso.LastMemory != null && Dia.Parse(progresso.LastMemory.Day) < hoje;
            if (inteira && outroDia && !progresso.Memorized.Contains(Musica.SongId))
            {
                progresso.Memorized.Add(Musica.SongId);
                Decorou = true;
                saida.Add(new MusicaDecorada(Musica.SongId));
            }
            ContarDia();
            if (progresso.LastMemory != null) progresso.LastMemory.Day = hoje.ToString();
        }

        // Vale 3 sem erro e sem dica, e 2 nos outros casos (RF-59). O que abre sai de MudarEstrelas.
        void ConcluirMusicaInteira(int ultimaNota)
        {
            saida.Add(new MemoriaConcluida(ultimaNota, !TeveFalha, UsouDica));
            EstrelasDaExecucao = TeveFalha || UsouDica ? 2 : 3;
            if (EstrelasDaExecucao.Value > RegrasDeProgresso.Estrelas(progresso, Item)) MudarEstrelas(EstrelasDaExecucao.Value);
            GuardarCadeia(Musica.Chunks.Count);
            ContarDia();
            progresso.LastMemory = new UltimaMemoria { Item = Item, Day = hoje.ToString() };
        }

        void GuardarCadeia(int ate) =>
            progresso.Chain[Musica.SongId] = Math.Max(RegrasDeProgresso.Cadeia(progresso, Musica.SongId), ate);

        void DepoisDoErro(long t)
        {
            if (Revisao) EntrarOfertas();
            else if (Juntando) EntrarJuntar(t);
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
            EstrelasDaExecucao = TeveFalha || UsouDica ? 2 : 3;
            if (EstrelasDaExecucao.Value > RegrasDeProgresso.Estrelas(progresso, Item)) MudarEstrelas(EstrelasDaExecucao.Value);
            if (ritmoDaExecucao != null) RegrasDeProgresso.GuardarRitmo(progresso, Item, ritmoDaExecucao);
            ContarDia();
            progresso.LastMemory = new UltimaMemoria { Item = Item, Day = hoje.ToString() };
            // A frase 1 não tem Juntar: tocá-la de memória já é a cadeia até o trecho 1.
            if (cadeia == null) GuardarCadeia(Passos[0].Trecho);
            saida.Add(new GravarProgresso(progresso));
            if (cadeia == null) Mudar(Fase.Resultado);
            else EntrarCadeia(t);
        }

        void MudarEstrelas(int para)
        {
            var abertosAntes = repertorio.Itens().Where(i => RegrasDeProgresso.Aberto(progresso, repertorio, i, preparoOk)).ToList();
            var de = RegrasDeProgresso.Estrelas(progresso, Item);
            progresso.Stars[Item] = para;
            saida.Add(new EstrelasMudaram(Item, de, para));
            foreach (var i in repertorio.Itens())
            {
                if (abertosAntes.Contains(i) || !RegrasDeProgresso.Aberto(progresso, repertorio, i, preparoOk)) continue;
                saida.Add(new ItemDesbloqueado(i));
                var aberta = repertorio.Analisar(i).Musica;
                if (i == repertorio.ItensDaMusica(aberta).First()) saida.Add(new MusicaDesbloqueada(aberta.SongId, aberta.TitleKey));
            }
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
                    else if (ouvindoMusica) OuvirMusica(t, destinoDaDemo);
                    else EntrarPose();
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
