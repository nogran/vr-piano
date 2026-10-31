# Testes

Casos da v1. Cada caso aponta o requisito ([01](01-requisitos-do-sistema.md)) ou a regra ([06](06-modelo-de-dominio.md)) que ele trava. A matriz da §10 prova que nenhum requisito ficou sem teste.

## 0. Ambiente e ferramentas

| Grupo | Onde roda | Como |
|---|---|---|
| T-DOM, T-ADP, T-PER, T-TXT, T-BUILD-01 | Máquina de desenvolvimento, sem headset | `dotnet test` (domínio) e Unity Test Framework (adapters). Rodam a cada commit. |
| T-MIDI, T-CAL, T-POSE, T-ACC, T-PERF, T-BUILD-02/03 | Quest 3 e Quest 3S + CT-S300 | Build de desenvolvimento com log (05 §9). |
| T-UX | Headset, com participante | Adulto que não viu o desenho das telas. A tarefa é lida em voz alta uma vez, sem apontar tecla. |
| T-REV | Revisão humana | Checklist antes de fechar a v1. |

**Cenário de domínio.** Os testes T-DOM são escritos como: estado inicial do progresso + lista de entradas com instante (`t=0 PoseMudou(true)`, `t=500 NoteOn(60, 64)`, `t=… Tick`) → lista esperada de eventos e comandos. O `ClockPort` é falso. Uma sessão gravada no headset (05 §9) pode virar um cenário novo sem reescrita.

**Dublês.** `MidiIn` e `MidiOut` falsos (o de saída registra os comandos), `ClockPort` falso com dia local ajustável, `ProgressStore` em memória com falha injetável, catálogo real.

**Setup do teclado** nos testes de headset, salvo quando o caso muda de propósito: ritmo off, song parado, octave shift 0, Touch Response Normal, MIDI Out Velocity Off, Local Control on, alimentação própria, cabo de dados Micro-B para USB-C.

**Revisão no dia seguinte em headset.** Build de desenvolvimento tem um ajuste escondido que soma um dia ao `ClockPort`. Os casos que dependem de outro dia usam esse ajuste ou o relógio do sistema.

## 1. Domínio (T-DOM)

### Lição

| ID | Dado | Quando | Então | Req. |
|---|---|---|---|---|
| T-DOM-01 | L01 inédita em Pose, Mão ligada | `PoseMudou(true)` | `FaseMudou(Demonstração)`. Nenhum `AlvoMudou` antes do fim da demonstração | RF-31, RF-31a |
| T-DOM-01b | L01 em `demoSeen`, em Pose | `PoseMudou(true)` | `FaseMudou(Guiada)`, `AlvoMudou(60, 1, 0)`. Nenhum comando de envio | RF-31, RF-31a |
| T-DOM-02 | L01 em Pose; (a) inédita, (b) já vista | `MãoAlternada(false)` | (a) Demonstração, com `DemoNota` e envio, sem esperar pose; (b) Guiada no passo 0 | RF-32c |
| T-DOM-03 | L01 em Pose | `NoteOn(60)` | Nenhum evento de acerto ou erro. Fase continua Pose | RF-31 |
| T-DOM-04 | L03 em Pose, conectado, 60 BPM, durações 1, 1, 2 | `OuvirAcionado` em t=0 | Comandos: On 60 em 0, Off 60 em 700, On 62 em 1000, Off 62 em 1700, On 64 em 2000, Off 64 em 3400 (velocity `P-11`, canal `P-12`); `DemoNota` para cada um; depois do último Off: `DemoVista(L03)`, `MãoSome`, Guiada no passo 0 | RF-14, RF-31a, P-09, P-10 |
| T-DOM-05 | Demonstração da L01 terminou em t=T | `NoteOn(60)` em T+299 e em T+301 | O primeiro é ignorado; o segundo é `NotaCerta`. Note On durante a demonstração também é ignorado | RF-31a, P-08 |
| T-DOM-06 | L01 na Guiada | Quatro `NoteOn(60)` | Quatro `NotaCerta`, `VoltaConcluída(limpa)`, `FaseMudou(Transição)`, estrelas 0→1 | RF-32, RF-35 |
| T-DOM-07 | Guiada | 8 `NoteOn` com velocity 100; depois um com 70 | `VelocityFixa(true)` no 8º, `VelocityFixa(false)` no 9º. Nenhum efeito em acerto ou fase | RF-13, P-03 |
| T-DOM-08 | L01 na Guiada, passo 2 | 3 `NoteOn` (60, 64, 67) em 40 ms | `LiçãoPausada(Rajada)`. Passo volta a 2. Nenhuma falha contada | RF-16, P-04, P-05 |
| T-DOM-08b | Guiada | 13 `NoteOn` em 1000 ms; noutro cenário, alternância 60/62 a cada 100 ms por 5 s | O primeiro pausa por rajada. O segundo (10 por segundo) não pausa | RF-16, P-06 |
| T-DOM-08c | Pausado(Rajada) | Silêncio de 2999 ms com um Note On no meio; depois 3000 ms sem nada | Só retoma depois de 3000 ms contínuos de silêncio | RF-16, P-07 |
| T-DOM-09 | L02 na Guiada, alvo 60 | `NoteOn(64)`; depois 60, 62 | `NotaErrada(64, 60)`, passo continua 0; a volta termina não limpa e a Guiada recomeça sem ir para a memória | RF-32, RF-35 |
| T-DOM-10 | Guiada, alvo 60 | `NoteOn(60)` com velocity 1, 49, 50, 89, 90, 127 | Todos são acerto. Faixas: Leve, Leve, Médio, Médio, Forte, Forte | RF-33, P-01, P-02 |
| T-DOM-11 | L02 na Guiada | `NoteOn(60)`, `NoteOn(62)` sem `NoteOff(60)` entre eles | Os dois são acerto | RF-34 |
| T-DOM-12 | Transição | `NoteOn(60)` 500 ms depois de entrar; `Tick` em 1000 ms | O Note On é ignorado. Memória começa em 1000 ms | RF-35, P-22 |
| T-DOM-13 | L03 na Memória, passo 1 | `NoteOn(65)`; `NoteOn(60)` 300 ms depois; `Tick` em 1000 ms | `TentativaFalhou(65, 62)`; o Note On durante o retorno de erro é ignorado; Memória volta ao passo 0 | RF-36, P-25 |
| T-DOM-14 | Memória | Três tentativas falhas seguidas | `VoltouAoGuiado` depois da 3ª, sem `DemoNota` nem envio. Uma volta limpa leva de novo à memória; concluir dá 2 estrelas | RF-36, P-21 |
| T-DOM-15 | Execuções da L01 | (a) memória limpa na 1ª tentativa; (b) uma falha e depois conclusão; (c) sair na memória depois da volta limpa; (d) sair na Guiada sem volta limpa; (e) memória na 1ª tentativa, mas com uma dica | (a) 3, (b) 2, (c) 1, (d) 0, (e) 2 | RF-40, RF-36a |
| T-DOM-16 | L01 com 3 estrelas; L02 com 2 | Execução da L01 que vale 2; execução da L02 que vale 3 | L01 continua 3. L02 sobe para 3 | RF-42 |
| T-DOM-17 | Progresso variado | Calcular abertos | L01 fechada sem checklist ou sem calibração. L02 fechada com L01 em 1, aberta com L01 em 2. SONG1 fechada com 11 lições ≥ 2 e uma em 1; aberta com as 12 ≥ 2 | RF-41, RF-32d |
| T-DOM-18 | (a) Guiada no passo 2; (b) Memória no passo 3 com uma falha anterior; (c) Demonstração | `MidiDesconectado`, depois `MidiConectado` | (a) volta no passo 2; (b) volta no passo 0, `falhasSeguidas` igual, a execução ainda vale no máximo 2; (c) volta para Pose, o item continua fora de `demoSeen` e nenhum comando de envio sai depois da desconexão. Estrelas salvas iguais | RF-15, RNF-08 |
| T-DOM-18b | Memória limpa até o passo 3, sem falha | `MidiDesconectado`, `MidiConectado`, conclui sem erro | 3 estrelas: a queda não conta como falha | RF-15, RF-40 |
| T-DOM-19 | Guiada | `FocoPerdido`, `Continuar`; noutro cenário, `SairDaAula` | Retoma no passo; sair encerra sem mudar estrelas | RF-39a |
| T-DOM-20 | Guiada | `PoseMudou(false)` e nenhum dado de mão por 60 s, depois Note On certos | Nenhuma pausa, a lição termina | RF-38, RNF-07 |
| T-DOM-21 | SONG1 na Guiada | Percorrer os 50 passos | `AlvoMudou` traz a lição do trecho certo em cada passo (passos 0–3 → L01, 4–5 → L02 … 40–49 → L12). O tempo de ataque depois da última nota de cada trecho é de 2 tempos. Conclusão grava estrelas da SONG1 | RF-32d |

### Cinza dinâmico, ritmo, dica e demonstração automática

| ID | Dado | Quando | Então | Req. |
|---|---|---|---|---|
| T-DOM-31 | L05 na Guiada, 60 BPM, passo 0 | `NoteOn(60)` em 0; `Tick` em 999 e 1000; `Tick` em 5000; `NoteOn(62)` em 5200 | O passo 0 entra direto como Ataque. `PróximaMudou(62, 0, 1000, false)`; `AtaqueChegou(62)` em 1000, não antes; nada muda até 5200 (tempo elástico); `NotaCerta(62)`, `RitmoAvaliado(Atrasado)`, `PróximaMudou(64, 5200, 6200, false)` | RF-32, RF-32a, P-10 |
| T-DOM-32 | Função de intensidade do cinza | progresso 0; 0,5; 1 | 0,15; 0,575; 1,0. Progresso fora de 0–1 é limitado | RF-32, P-29 |
| T-DOM-33 | Guiada, `tAtaque` = 1000 | `NoteOn` certo em 849, 850, 1250 e 1251 (quatro cenários) | Adiantado, NoTempo, NoTempo, Atrasado. Os quatro avançam o passo. A volta limpa continua limpa com Adiantado e Atrasado. `VoltaConcluída` traz `{noTempo, avaliadas}`, sem contar o passo 0 | RF-32a, RF-35, P-30, P-31 |
| T-DOM-34 | Memória da L03 começou em 0 | `Tick` em 2999, 3000, 6000; `NoteOn(60)` em 6500; `Tick` em 9499 e 9500; conclui sem erro | Nada em 2999. `DicaDeMemória(60)` em 3000 e em 6000. Depois do acerto, a próxima dica é `DicaDeMemória(62)` em 9500. Resultado: 2 estrelas | RF-36a, RF-40, P-33, P-34 |
| T-DOM-35 | L02 inédita | Pose ok; demonstração até o fim; sair; nova execução com pose ok; depois `OuvirAcionado` na Guiada | 1ª execução: Demonstração, `DemoVista(L02)`. 2ª: Guiada direto. Ouvir na Guiada: Demonstração e volta à Guiada no passo 0, sem `VoltaConcluída` | RF-31a |
| T-DOM-36 | L01 na Guiada | `NoteOn(60)` | `PróximaMudou(60, t, t+1000, mesmaTecla = true)` | RF-32e |
| T-DOM-37 | Guiada no passo 2, em Antecipação | `MidiDesconectado`, `MidiConectado`, `NoteOn` certo 3 s depois | Retoma com o passo 2 em Ataque. O acerto não gera `RitmoAvaliado` | RF-15, RF-32a |
| T-DOM-38 | L01 com ritmo salvo 2 de 3 | Execução com volta limpa 3 de 3; outra com 1 de 3 | Ritmo salvo vira 3 de 3 e não cai. Estrelas iguais nos dois casos | RF-32a, RF-45 |

### Dia seguido e revisão

| ID | Dado | Quando | Então | Req. |
|---|---|---|---|---|
| T-DOM-22 | `lastCountedDay` = D−1, current 3, best 3 | Memória concluída em D; outra em D | current 4, best 4; a segunda não muda nada | RF-43 |
| T-DOM-23 | `lastCountedDay` = D−3, current 5 | Abrir em D | current 0, best mantido, estrelas intactas. Memória concluída em D → current 1 | RF-43 |
| T-DOM-24 | `lastSeenDay` = D | Abrir com o relógio em D−1 | Nenhuma revisão, nenhuma mudança de sequência; `lastSeenDay` continua D | RF-43 |
| T-DOM-25 | `lastMemory` = (L03, D−1); noutro, (L03, D); noutro, nulo | Abrir em D | Só o primeiro pede revisão, com o item L03 | RF-44 |
| T-DOM-26 | `lastMemory` = (SONG1, D−1) | Abrir em D | A revisão usa a sequência da L12 | RF-44 |
| T-DOM-27 | Revisão da L02 | Erra; `OuvirAcionado`; erra; `ComGuiaAcionado`; erra | Depois do 1º erro, ofertas Ouvir e Com guia; depois do Ouvir, só Com guia; depois do Com guia, nenhuma. Em Ofertas, `NoteOn(60)` inicia a tentativa como acerto; `NoteOn(64)` inicia e falha | RF-44 |
| T-DOM-28 | Revisão da L02, estrelas da L02 = 2 | Conclui com erro antes | `RevisãoConcluída`; dia seguido conta; L02 continua 2; `lastMemory.day` = D; abrir de novo em D não pede revisão | RF-43, RF-44 |
| T-DOM-30 | Catálogo embutido | Validar | As regras do 06 §5 passam; L01–L12 batem com a tabela, com última nota de 2 tempos; SONG1 tem 50 passos | RF-30 |

## 2. Adapters sem headset (T-ADP)

| ID | Caso | Aceite | Req. |
|---|---|---|---|
| T-ADP-01 | Parser MIDI com bytes gravados: Note On v0, running status, clock, active sensing, CC, program change, Note On em canal 2 | Note On v0 vira Note Off; running status resolvido; o resto descartado; canal não importa | RF-11 |
| T-ADP-02 | Matemática da calibração com pontos sintéticos: (a) 36 e 96 a 822,5 mm; (b) 36 e 95; (c) 36 e 96 a 600 mm; (d) 48 e 108 | (a) aceito, passo 23,5 mm; (b) recusado (95 não é branca); (c) recusado por `P-19`; (d) aceito, dó central é a 8ª tecla branca a partir da esquerda (com 36, seria a 15ª) | RF-22, RF-23 |
| T-ADP-03 | Mapa do teclado: `lowNote` 36; `lowNote` 72 | 61 teclas, 36 brancas, pretas nas posições do 05 §6. Com 72, a nota 60 está fora do alcance e a lição é recusada | RF-24 |
| T-ADP-04 | Detector de pose com quadros de mão gravados: pose correta; palma a 50°; polegar duas teclas ao lado; palma a 20 cm; mão esquerda; pose correta por 400 ms | Só a primeira vira `PoseMudou(true)`, depois de 500 ms | RF-31, P-13–P-17 |
| T-ADP-05 | Teclas de atalho com `lowNote` 36 e 48 | Botões mapeados em 36/38/40 e 48/50/52; o rótulo mostra “Dó2/Ré2/Mi2” e “Dó3/Ré3/Mi3” | RF-39, P-27 |

## 3. Persistência (T-PER)

| ID | Caso | Aceite | Req. |
|---|---|---|---|
| T-PER-01 | Gravar e ler um progresso com todos os campos preenchidos, inclusive `demoSeen` e `rhythm` | Leitura idêntica | RF-46 |
| T-PER-02 | Falha injetada em cada etapa da gravação (meio do `.tmp`, depois do `.tmp`, depois do `.bak`, antes do rename) | Toda leitura seguinte dá a versão anterior completa ou a nova completa | RNF-09, RNF-08 |
| T-PER-03 | Principal truncado, `.bak` bom | Lê o `.bak` | RNF-08 |
| T-PER-04 | Principal e `.bak` ilegíveis | Arquivo vira `progress.corrupt-<data>.json`, app começa do zero, aviso uma vez | RNF-08 |
| T-PER-05 | `schemaVersion` 99 | Não sobrescreve; o arquivo continua no disco | RNF-08 |
| T-PER-06 | `stars` sem a L05 | L05 lida como 0; nenhum desbloqueio gravado no arquivo | RF-41 |

## 4. Textos e build (T-TXT, T-BUILD)

| ID | Caso | Aceite | Req. |
|---|---|---|---|
| T-TXT-01 | Tabela de textos | Toda chave tem pt-BR e en, não vazios; toda chave usada no app existe | RF-03 |
| T-TXT-02 | Frases do card | Nenhuma passa de 32 caracteres | RNF-05 |
| T-BUILD-01 | Assembly do domínio | Não referencia `UnityEngine`, Android nem OpenXR; `dotnet test` passa numa máquina sem Unity | RNF-11 |
| T-BUILD-02 | Manifest final do APK (`aapt dump`) | Sem `INTERNET`; com MIDI, USB host, hand tracking, passthrough e âncora | RNF-04 |
| T-BUILD-03 | `adb install` no Quest 3 e no 3S | Instala e abre na tela de idioma | RN-01 |

## 5. MIDI no Quest (T-MIDI)

Fazer T-MIDI-00 e 01 antes de qualquer trabalho visual fino.

| ID | Caso | Procedimento | Aceite | Req. |
|---|---|---|---|---|
| T-MIDI-00 | Caminho da ponte | App mínimo lista os aparelhos do `MidiManager` com o CT-S300 ligado | Registrar no 05 §12 se o CT-S300 aparece (nome, portas). Se não aparece, o caminho reserva USB host vira o principal | 05 §3 |
| T-MIDI-01 | Enumeração | CT-S300 ligado, cabo no USB-C do Quest, app em “Procurando teclado…” | Passa a conectado sem PC no meio. Nome do aparelho no log | RF-10 |
| T-MIDI-02 | As 61 teclas | Tocar cada tecla uma vez, brancas e pretas | 61 Note On distintos (36–96), um Note Off para cada, sem nota fantasma | RF-11 |
| T-MIDI-03 | Velocity viva | `MIDI Out Velocity = Off`, Touch Response Normal; a mesma tecla, 8 toques fracos e 8 fortes | Os valores mudam. Registrar a distribuição para conferir `P-01`/`P-02`. Se não mudarem, a faixa de velocity sai do protótipo | RF-33 |
| T-MIDI-04 | Velocity travada | `MIDI Out Velocity = 100`, 8 toques de forças diferentes | Aviso de velocity fixa; as notas continuam contando | RF-13 |
| T-MIDI-05 | Note Off | Soltar devagar e rápido | O domínio não lê a velocity 64 do Note Off como dinâmica | RF-11 |
| T-MIDI-06 | Octave shift | Shift +1, calibrar de novo, tocar a tecla física que agora envia 60 | O realce do dó central fica nessa tecla. A lição 1 segue tocável | RF-23 |
| T-MIDI-07 | Eco | “Ouvir” na L09, ninguém encosta no teclado, gravar o MIDI de entrada | Ou não volta Note On, ou o que volta cai na demonstração ou na cauda `P-08`. Nenhum acerto ou erro é marcado. Se pontuar, falha | RF-31a |
| T-MIDI-08 | Demonstração audível | Demonstração da L01 e da L03 | O CT-S300 soa as notas no andamento: 1 s entre ataques a 60 BPM, e a última nota da L03 mais longa (± 50 ms no log). A mão branca desce o dedo junto de cada som e some em fade no fim | RF-14, RF-31a |
| T-MIDI-09 | Latência | Note On de uma tecla fora da lição, com log do instante de recepção e do quadro em que o realce muda, 20 amostras | Mediana abaixo de `P-28` no Quest 3 e no Quest 3S | RNF-01 |
| T-MIDI-10 | Queda de cabo | Tirar o Micro-B (a) na vez guiada, (b) na memória | Pausa visível. Religar: (a) mesmo passo, (b) tentativa do início. Estrelas anteriores iguais | RF-15 |
| T-MIDI-11 | Ritmo e song | Ligar um ritmo no CT-S300 durante a vez guiada; depois o song player | Registrar no 05 §12 se essas notas chegam pelo USB. Se chegam: pausa com o aviso, sem somar erro | RF-16 |
| T-MIDI-12 | Trilo | Alternar duas teclas o mais rápido possível por 5 s na vez guiada | Nenhuma nota perdida no log. Nenhuma pausa por rajada. A regra de acerto continua valendo para a nota da vez | RF-16, P-06 |
| T-MIDI-14 | Instante do ritmo | Vez guiada da L09 com metrônomo externo a 60 BPM, tocando junto do clique; log do `timestampNanos` e do quadro do dreno | Pelo menos 3 de 4 notas avaliadas como no tempo. O `d` do log usa o `timestampNanos`, e a diferença para o instante do quadro nunca passa de um quadro | RF-32a |
| T-MIDI-13 | Permissão USB | Se o sistema pedir permissão: negar, depois “Pedir de novo” e permitir | Mensagem do RF-10; depois de permitir, conectado | RF-10 |

## 6. Calibração (T-CAL)

| ID | Caso | Aceite | Req. |
|---|---|---|---|
| T-CAL-01 | Medir o CT-S300 com régua | Medidas gravadas no 05 §6 e `P-20` atualizado | 05 §6 |
| T-CAL-02 | 10 calibrações seguidas, sem ajuste por pinça | Em pelo menos 8, o centro do realce das teclas 60–67 fica a menos de ¼ de tecla branca do centro real | RF-20, RF-21 |
| T-CAL-03 | Tocar duas teclas que não são as pontas | Recusa com “Não bateu. Vamos de novo.” | RF-22 |
| T-CAL-04 | Ajuste pelas alças | Depois do ajuste, as 61 teclas ficam a menos de ¼ de tecla do centro real | RF-22 |
| T-CAL-05 | Fechar e abrir o app; depois mover o teclado 10 cm e abrir | Primeiro: só a verificação do dó central. Segundo: a verificação falha e a calibração completa é pedida | RF-23 |
| T-CAL-06 | Calibrar, tocar 15 minutos, olhar o realce do dó | O realce ainda está na tecla, ou a pessoa recalibra sozinha e as estrelas seguem lá | RNF-06 |
| T-CAL-07 | Apagar as âncoras salvas do app e abrir | Calibração completa pedida, sem erro | RF-23 |

## 7. Pose (T-POSE)

Três pessoas com mãos de tamanhos diferentes, luz normal de sala.

| ID | Caso | Aceite | Req. |
|---|---|---|---|
| T-POSE-01 | Pôr a mão direita sobre a mão branca, 10 vezes por pessoa | Reconhecida em até 2 s em pelo menos 9 de 10 | RF-31 |
| T-POSE-02 | Poses erradas: palma para cima, mão duas teclas ao lado, mão a 20 cm de altura, punho fechado | Nenhuma reconhecida em 10 tentativas cada | RF-31 |
| T-POSE-03 | Mão esquerda na pose; luz fraca | Esquerda não é aceita. Com luz fraca, sem travar: “Ouvir” e “Mão” continuam funcionando | RF-31, RF-31a |
| T-POSE-04 | Tirar a mão do campo de visão no meio da vez guiada | A vez guiada continua e termina pelo MIDI | RF-38 |

## 8. Aceite ponta a ponta (T-ACC)

| ID | Dado | Ação | Aceite | Req. |
|---|---|---|---|---|
| T-ACC-01 | App recém-instalado | Abrir | Só “Português” e “English”, nenhum marcado | RF-01 |
| T-ACC-02 | Idioma ainda não escolhido | Matar o app na primeira tela e abrir de novo | A escolha é pedida outra vez | RF-02 |
| T-ACC-03 | Sem cabo | Esperar na tela de conexão | A dica do cabo de dados aparece depois de `P-26` | RF-10 |
| T-ACC-04 | Checklist com 4 itens | Tentar “Pronto” | Não acende; a lição não começa | RF-12 |
| T-ACC-05 | Sem calibração válida | Tentar chegar à lição 1 por qualquer caminho | A calibração é o próximo passo | RF-20, RF-24 |
| T-ACC-06 | L01 inédita na pose | Pôr a mão na pose | “Olhe e ouça.”; a mão toca os quatro dós com o piano soando e a tecla na cor ativa; no fim, a mão some em fade de cerca de `P-35`; o card troca para “Polegar” e o dó fica na cor ativa | RF-31, RF-31a, RF-32, RF-37, P-35 |
| T-ACC-07 | L01 já vista, na pose, mão fora | (a) Pôr a mão na pose; (b) noutra execução, “Ouvir” sem pose | (a) Vai direto à vez guiada, sem demonstração; (b) demonstração e, no fim, a vez guiada começa sem pose | RF-31a |
| T-ACC-08 | L02 na vez guiada, alvo dó | Tocar ré (62) | Vermelho no ré, dó continua na cor ativa, passo não avança | RF-32 |
| T-ACC-09 | L01 | Volta limpa e memória sem erro | 3 estrelas, “Você lembrou.”, L02 abre | RF-40, RF-41 |
| T-ACC-10 | L01 | Volta limpa, erro na memória, depois memória certa | 2 estrelas, L02 abre | RF-36, RF-40 |
| T-ACC-11 | L01 com 1 estrela | Tentar abrir a L02 | Fechada | RF-41 |
| T-ACC-12 | L01 com 3 estrelas | Repetir e errar a memória antes de concluir | Continua 3 | RF-42 |
| T-ACC-13 | Memória da L03 | Errar três vezes seguidas | “Mais uma vez com guia.” com “Ouvir”; a vez guiada volta, sem demonstração automática | RF-36 |
| T-ACC-14 | Memória concluída ontem | Abrir hoje | Depois do dó central, “Frase da última vez.”, sem demonstração automática | RF-44 |
| T-ACC-15 | Revisão | Errar, usar “Ouvir”, concluir | Dia seguido soma; estrelas da lição revisada iguais | RF-43, RF-44 |
| T-ACC-16 | Memória concluída hoje | Abrir de novo hoje | Sem revisão | RF-44 |
| T-ACC-17 | Um dia inteiro sem memória concluída | Abrir no dia seguinte ao vazio | Dias seguidos em 0, estrelas intactas | RF-43 |
| T-ACC-18 | Pose, demonstração, vez guiada, memória, resultado, progresso | Inspecionar a cena | Só a mão branca é desenhada, e só na pose e na demonstração; card conforme RF-37a; nenhuma nota caindo, barra de tempo, cronômetro, combo ou pauta | RF-32b, RF-37a, §8 do 01 |
| T-ACC-19 | Progresso com várias lições e a música | Abrir o quadro | n/36, música n/3, dias atual e maior, lições em 3 estrelas, lista com abertas e fechadas e melhor ritmo de cada uma. Sem outro jogador | RF-45 |
| T-ACC-20 | Tracking de mão desligado nas configurações do Quest, já calibrado | Completar checklist, “Ouvir”, “Próxima” e “Continuar” só com as teclas de atalho | Todas as ações funcionam; os rótulos mostram as teclas certas | RF-39 |
| T-ACC-21 | “Mão” desligada | Fechar e abrir o app, entrar numa lição | Continua desligada; a pose é pulada | RF-32c |
| T-ACC-22 | Progresso de desenvolvimento com as 12 lições em 2 | Abrir a música e concluí-la | Abre, a etiqueta acompanha a lição do trecho, estrelas aparecem como música n/3 | RF-32d |
| T-ACC-23 | pt-BR com estrelas | Trocar para inglês nas configurações e abrir a L02 | Nome sobre a tecla é C ou D; dedo “Thumb” ou “Index”; estrelas iguais | RF-03 |
| T-ACC-24 | Vez guiada | Botão Meta e voltar; depois “Sair da aula” | Pausa com “Continuar” e “Sair da aula”; sair não muda estrelas | RF-39a |
| T-ACC-25 | Resultado com 3 estrelas aparecendo | Matar o app pelo sistema logo depois | Ao abrir, as 3 estrelas estão lá | RF-46 |
| T-ACC-26 | L05 na vez guiada | Tocar o dó; observar o ré; numa volta, tocar o ré logo de cara; noutra, esperar 3 s e tocar | O ré aparece cinza claro, escurece em cerca de 1 s sem degrau e vira cor ativa. Tocar cedo mostra “Adiantado”. Esperar deixa o ré colorido, parado, e tocar mostra “Atrasado”. O resultado mostra “No tempo: n de m” | RF-32, RF-32a, RF-37 |
| T-ACC-27 | Memória da L03 | Ficar parado 3 s, depois concluir sem erro | O dó pisca em cinza claro uma vez; conclusão com 2 estrelas | RF-36a, RF-40 |
| T-ACC-28 | L01 na vez guiada, numa tecla preta (build de desenvolvimento com uma sequência em 61–63) | Tocar a primeira nota | L01: contorno na cor ativa e centro em cinza no mesmo dó. Tecla preta: o cinza vai de grafite a prata e se lê tão bem quanto na branca | RF-32e, 03 §2 |

## 9. Experiência com participante (T-UX)

| ID | Tarefa | Aceite | Req. |
|---|---|---|---|
| T-UX-01 | “Escolha a língua, ligue o teclado e deixe o app pronto para a primeira lição.” Sentado, teclado na mesa | Conclui sem o condutor mostrar onde tocar | Jornada §14, RNF-03 |
| T-UX-02 | “Faça a lição 1 até o fim.” | Descreve que primeiro assistiu a mão tocar, depois tocou com as teclas guiando e por fim tocou sem luz. Não tenta tocar durante a demonstração. Termina com 2 ou 3 estrelas | RF-31–RF-37a |
| T-UX-03 | Headset no colo, logo após T-UX-02: “Toca o que a lição pediu.” | Quatro dós com o polegar, sem o app | §2 do 01, RN-03 |
| T-UX-04 | No dia seguinte: “Abre e faz o que o app pedir primeiro.” | Entra na revisão e a reconhece como revisão, não como lição nova | RF-44, RN-03 |
| T-UX-05 | Lição 1 com luva fina ou mão parcialmente fechada, tracking instável, depois de calibrado | A lição termina. Nenhum erro é atribuído à mão | RF-38, RNF-07 |
| T-UX-07 | Trocar o idioma para inglês no progresso e abrir a lição 2 | Encontra a configuração sozinho; nome e dedo em inglês | RF-03 |
| T-UX-08 | Errar de propósito na vez guiada e na memória | Em menos de um segundo aponta a tecla que o app quer, sem ler frase de erro | RF-32, RF-36 |
| T-UX-10 | Desconectar o cabo e perguntar o que houve | Diz que pausou e que o teclado saiu. Não acha que perdeu as estrelas | RF-15 |
| T-UX-11 | Desligar “Mão” na pose e perguntar o que mudou | A mão some e a pose é pulada; na demonstração, continua sabendo a tecla pela cor e pelo som | RF-32c |
| T-UX-12 | Ler o card e o nome da nota a 50 cm, com a sala iluminada normalmente | Lê sem se aproximar | RNF-05 |
| T-UX-13 | Lição 5 sem explicar o cinza. Depois: “O que a tecla cinza queria dizer?” | Responde que é a próxima tecla e que, quando escurece, é a hora de tocar. Prepara o dedo antes de a tecla ficar colorida em pelo menos metade das notas da segunda volta | RF-32, P-29 |
| T-UX-14 | Lição 9 na vez guiada. Depois: “Você estava no tempo?” | Cita o aviso “Adiantado” ou “Atrasado” ou diz que não viu nenhum. Não acha que errou a nota por causa dele | RF-32a, P-32 |
| T-UX-15 | Memória da lição 5, pedir para esperar sem tocar | Percebe a piscada como dica, não como erro, e continua | RF-36a, P-34 |

T-UX-03 é o teste que os apps de esteira costumam falhar. Se a pessoa não toca os quatro dós com o headset no colo, a lição não está pronta, mesmo com estrela no card.

## 10. Desempenho (T-PERF)

| ID | Caso | Aceite | Req. |
|---|---|---|---|
| T-PERF-01 | Lição 1 no Quest 3, passthrough ligado, duas mãos sobre as teclas | 72 fps estáveis no contador do sistema, sem queda prolongada quando o realce troca de tecla | RNF-02 |
| T-PERF-02 | O mesmo no Quest 3S | Igual | RNF-02 |
| T-PERF-03 | Modo avião, cabo MIDI local | Lição 1 completa | RNF-04 |
| T-PERF-04 | Sessão de 30 min, bateria cheia | Registrar o consumo no 05 §10 | 05 §10 |
| T-PERF-05 | Abertura a frio no Quest 3 | Primeira tela útil em menos de 10 s | RNF-10 |

## 11. Revisão (T-REV)

| ID | Caso | Aceite | Req. |
|---|---|---|---|
| T-REV-01 | Assets, sons e textos | Todos originais; nenhum asset da Meta ou da Casio; nenhuma gravação de obra de terceiros | RN-04 |
| T-REV-02 | Requisitos sem teste | A matriz abaixo não tem linha vazia | — |

## 12. Matriz de rastreabilidade

| Requisito | Testes |
|---|---|
| RF-01 | T-ACC-01 |
| RF-02 | T-ACC-02, T-PER-01 |
| RF-03 | T-TXT-01, T-ACC-23, T-UX-07 |
| RF-10 | T-MIDI-01, T-MIDI-13, T-ACC-03 |
| RF-11 | T-ADP-01, T-MIDI-02, T-MIDI-05 |
| RF-12 | T-ACC-04 |
| RF-13 | T-DOM-07, T-MIDI-04 |
| RF-14 | T-DOM-04, T-MIDI-08 |
| RF-15 | T-DOM-18, T-DOM-18b, T-DOM-37, T-MIDI-10, T-UX-10 |
| RF-16 | T-DOM-08, T-DOM-08b, T-DOM-08c, T-MIDI-11, T-MIDI-12 |
| RF-20 | T-ACC-05, T-CAL-02 |
| RF-21 | T-CAL-02 |
| RF-22 | T-ADP-02, T-CAL-03, T-CAL-04 |
| RF-23 | T-ADP-02, T-CAL-05, T-CAL-07, T-MIDI-06 |
| RF-24 | T-ADP-03, T-ACC-05 |
| RF-30 | T-DOM-30 |
| RF-31 | T-DOM-01, T-DOM-01b, T-DOM-03, T-ADP-04, T-POSE-01, T-POSE-02, T-POSE-03, T-ACC-06 |
| RF-31a | T-DOM-01, T-DOM-01b, T-DOM-04, T-DOM-05, T-DOM-35, T-MIDI-07, T-MIDI-08, T-ACC-06, T-ACC-07 |
| RF-32 | T-DOM-06, T-DOM-09, T-DOM-31, T-DOM-32, T-ACC-06, T-ACC-08, T-ACC-26, T-UX-08, T-UX-13 |
| RF-32a | T-DOM-31, T-DOM-33, T-DOM-37, T-DOM-38, T-MIDI-14, T-ACC-26, T-UX-14 |
| RF-32b | T-ACC-18 |
| RF-32c | T-DOM-02, T-ACC-21, T-UX-11 |
| RF-32d | T-DOM-17, T-DOM-21, T-ACC-22 |
| RF-32e | T-DOM-36, T-ACC-28 |
| RF-33 | T-DOM-10, T-MIDI-03 |
| RF-34 | T-DOM-11 |
| RF-35 | T-DOM-06, T-DOM-09, T-DOM-12 |
| RF-36 | T-DOM-13, T-DOM-14, T-ACC-10, T-ACC-13, T-UX-08 |
| RF-36a | T-DOM-15, T-DOM-34, T-ACC-27, T-UX-15 |
| RF-37 | T-ACC-06, T-ACC-26, T-UX-02 |
| RF-37a | T-ACC-18, T-UX-02 |
| RF-38 | T-DOM-20, T-POSE-04, T-UX-05 |
| RF-39 | T-ADP-05, T-ACC-20 |
| RF-39a | T-DOM-19, T-ACC-24 |
| RF-40 | T-DOM-15, T-DOM-18b, T-DOM-34, T-ACC-09, T-ACC-10, T-ACC-27 |
| RF-41 | T-DOM-17, T-PER-06, T-ACC-09, T-ACC-11 |
| RF-42 | T-DOM-16, T-ACC-12 |
| RF-43 | T-DOM-22, T-DOM-23, T-DOM-24, T-DOM-28, T-ACC-15, T-ACC-17 |
| RF-44 | T-DOM-25, T-DOM-26, T-DOM-27, T-DOM-28, T-ACC-14, T-ACC-15, T-ACC-16, T-UX-04 |
| RF-45 | T-DOM-38, T-ACC-19 |
| RF-46 | T-PER-01, T-PER-02, T-ACC-25 |
| RNF-01 | T-MIDI-09 |
| RNF-02 | T-PERF-01, T-PERF-02 |
| RNF-03 | T-UX-01 |
| RNF-04 | T-BUILD-02, T-PERF-03 |
| RNF-05 | T-TXT-02, T-UX-12 |
| RNF-06 | T-CAL-06 |
| RNF-07 | T-DOM-20, T-UX-05 |
| RNF-08 | T-DOM-18, T-PER-02, T-PER-03, T-PER-04, T-PER-05 |
| RNF-09 | T-PER-02 |
| RNF-10 | T-PERF-05 |
| RNF-11 | T-BUILD-01 |
| RN-01 | T-BUILD-03 |
| RN-02 | Sem teste: orienta decisões, não comportamento |
| RN-03 | T-UX-03, T-UX-04 |
| RN-04 | T-REV-01 |

## 13. Ordem de execução

1. T-DOM, T-ADP, T-PER, T-TXT e T-BUILD-01 na máquina de desenvolvimento. Escritos antes da cena; ficam vermelhos até o domínio existir.
2. T-MIDI-00 e T-MIDI-01. Se o CT-S300 não conectar por nenhum dos dois caminhos, parar: não há o que calibrar.
3. T-MIDI-02, 03, 05, 07, 08, 13 e T-CAL-01. Atualizar o 05 §12 e os parâmetros com o que for medido.
4. Protótipo mínimo do 03 §9.
5. T-CAL-02 a 05, T-POSE-01 a 04, T-MIDI-09 a 12 e T-ACC.
6. T-UX-02, 03, 05, 08 e 13 com participante. Se T-UX-13 falhar, ajustar `P-29` e as cores do cinza antes de seguir.
7. O resto, e só então gravar as 12 lições como conteúdo fechado.

## 14. Pronto para a v1

- Todos os T-DOM, T-ADP, T-PER, T-TXT e T-BUILD passando.
- Todos os T-MIDI, T-CAL, T-POSE, T-ACC e T-PERF passando no Quest 3 e, onde o caso pede, no 3S.
- T-UX-02, 03, 04 e 13 aprovados com pelo menos três participantes.
- T-REV-01 e T-REV-02 feitos.

## 15. Fora destes testes

Compra, conta, ranking online, Quest 2, Bluetooth, mão esquerda, leitura de pauta, dinâmica ou ritmo como passe, metrônomo audível e nota caindo. Não criar caso de teste para isso na v1.
