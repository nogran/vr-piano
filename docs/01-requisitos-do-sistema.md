# Requisitos do sistema — vr-piano

Planejamento. Sem implementação neste documento.

## 1. Decisões fechadas

| Tema | Decisão |
|---|---|
| Headset | Meta Quest 3 e Quest 3S. Quest 2 fora. |
| Usuário | Adulto iniciante, sozinho. Sem conta, sem login, sem nuvem. |
| Corte da v1 | Mão direita, posição de cinco dedos a partir do dó central (MIDI 60), melodias curtas. |
| Como se aprende | Ouvir → tocar com uma tecla acesa → tocar de memória. A lição conta quando o trecho sai sem guia. |
| O que não entra | Esteira de notas caindo, janela de tempo, combo, vidas, partitura, mão esquerda, acordes. |
| Conteúdo | 12 lições autorais, cerca de 5 minutos cada. Sem importar MIDI ou MusicXML. |
| Gamificação | 1–3 estrelas, dias seguidos, quadro de progresso no aparelho. |
| Idioma | Português (Brasil) e inglês, escolha explícita na primeira tela e nas configurações. |
| Distribuição da v1 | Sideload de desenvolvimento. Sem Meta Store, sem compra, sem anúncio. |
| Hardware de teste | Quest 3 ou 3S e Casio CT-S300 disponíveis para os testes de aceite. |

Premissas adotadas porque não foram contestadas:

- Velocity aparece em três faixas (leve, médio, forte) e nunca reprova a lição.
- “Ranking” com uma pessoa só no aparelho é um quadro de progresso pessoal. Não há outros jogadores.
- Não há modelo de negócio na v1. O requisito de negócio é o problema e o critério de sucesso do aprendizado.

## 2. Problema e sucesso

Apps de piano em mista realidade treinam o reflexo de seguir uma nota que cai. A pessoa toca enquanto o guia está na frente e, ao parar, não lembra a frase. Este sistema ensina um adulto a tocar e repetir de memória frases curtas de mão direita no CT-S300, com o Quest em passthrough.

Sucesso da v1, medido no próprio aparelho, sem analytics de rede:

- Na primeira sessão, a pessoa termina a lição 1 de memória sem ajuda de outra pessoa.
- Na abertura seguinte, em outro dia, a revisão pede a frase anterior antes de matéria nova, e a pessoa consegue concluí-la.

## 3. Abordagem pedagógica

Base usada, reduzida ao mínimo que cabe num teclado de 61 teclas e numa sessão curta:

- **Ouvir antes de tocar** (método Suzuki, a parte que serve aqui): a frase soa no próprio piano, a tecla acende junto, a pessoa só assiste.
- **Uma novidade por lição** (carga cognitiva): uma nota nova ou um gesto novo (subir, descer, saltar).
- **Posição fixa de cinco dedos** (Alfred / Faber, primeiro volume): polegar no dó central, até o mínimo no sol. Desde a lição 1 só a mão branca fosca aparece desenhada, nas teclas da mão direita, e mostra qual dedo desce. A mão da pessoa não é desenhada. O app não compara pose. A tecla vem do MIDI.
- **Pedaços pequenos e emenda** (prática aditiva): 2 a 4 notas, memória imediata, só então o próximo pedaço, depois o trecho inteiro.
- **Recordação, não reestudo** (retrieval practice): o modo que desbloqueia a próxima lição é tocar sem luz. Se errar, a tecla certa acende um instante e o pedaço recomeça.
- **Revisão no dia seguinte**: ao abrir o app noutro dia, a primeira tarefa é a frase da última lição, de memória, antes de qualquer nota nova.
- **Guia que some**: a mão branca fosca, o nome da nota e o número do dedo existem ao ouvir e no modo guiado, nas teclas da mão direita. Na memória somem juntos. Se a pessoa erra, a mão branca volta um instante no dedo certo e some de novo.

Dinâmica fica de fora da aprovação. O CT-S300 não tem aftertouch. O chart MIDI transmite Note On com velocity 1–127 e Note Off sempre em 64. O manual não diz o que `MIDI Out Velocity = Off` faz com esse número; a leitura usada no checklist vem de quem mediu o aparelho, não de uma frase da Casio. Mostrar leve / médio / forte habitua o ouvido sem transformar a lição num teste de força. Se a velocity vier fixa, a faixa some e a lição continua.

Tempo também fica de fora da aprovação. A demonstração é lenta e separada, para a frase ser inteligível. Na hora de tocar, a pessoa pode ir no tempo dela. Cobrar janela de milissegundos é o que produz a sensação de tocar no piloto automático.

### Currículo das 12 lições

Posição: dedo 1 no dó central (nota MIDI 60), 2 no ré, 3 no mi, 4 no fá, 5 no sol. Só teclas brancas. Nome exibido em pt-BR: Dó Ré Mi Fá Sol. Em inglês: C D E F G. O domínio guarda nota MIDI e dedo; o texto é localização.

| Lição | Matéria nova | Trecho de memória que desbloqueia a próxima |
|---|---|---|
| 1 | Dó, dedo 1 | Dó, quatro vezes |
| 2 | Ré, dedo 2 | Dó–Ré |
| 3 | Mi, dedo 3 | Dó–Ré–Mi |
| 4 | Descer | Mi–Ré–Dó |
| 5 | Emendar dois pedaços | Dó–Ré–Mi–Ré–Dó |
| 6 | Fá, dedo 4 | Mi–Fá |
| 7 | Subir até o fá | Dó–Ré–Mi–Fá |
| 8 | Sol, dedo 5 | Fá–Sol |
| 9 | Cinco dedos subindo | Dó–Ré–Mi–Fá–Sol |
| 10 | Cinco dedos descendo | Sol–Fá–Mi–Ré–Dó |
| 11 | Salto dentro da posição | Dó–Mi–Sol–Mi–Dó |
| 12 | Revisão | A frase da 11 e a subida da 9, de memória, ouvir só se errar |

Cada lição nova segue o ciclo: ouvir o pedaço, tocar com guia, tocar de memória, repetir com o pedaço acumulado. A lição 12 começa direto na recordação.

## 4. Requisitos funcionais

### Idioma

| ID | Requisito |
|---|---|
| RF-01 | A primeira tela oferece pt-BR e English, sem pré-seleção silenciosa. |
| RF-02 | A escolha persiste no aparelho e pode ser trocada nas configurações, fora de uma lição. |
| RF-03 | Notas, botões, fases e faixas de velocity usam a língua escolhida. A lógica da lição não muda com a língua. |

### Teclado e MIDI

| ID | Requisito |
|---|---|
| RF-10 | O app detecta o CT-S300 como dispositivo USB-MIDI na porta USB-C do Quest e mostra conectado ou desconectado. |
| RF-11 | Cada Note On entregue ao domínio contém nota MIDI, velocity 1–127 e o instante de recebimento. Note Off encerra a nota. A velocity do Note Off é ignorada (o CT-S300 envia 64). |
| RF-12 | Antes da primeira lição, um checklist pede: ritmo desligado, song player parado, `MIDI Out Octave Shift = 0`, `Touch Response = Normal`, `MIDI Out Velocity = Off`, `Local Control` ligado. O usuário confirma. |
| RF-13 | Se as últimas 8 Note On da sessão tiverem a mesma velocity, um aviso não bloqueante sugere conferir `MIDI Out Velocity = Off`. |
| RF-14 | No modo ouvir, o app envia Note On e Note Off ao teclado, devagar, uma nota por vez, para a frase soar no CT-S300. Essas notas não entram na pontuação. |
| RF-15 | Se o cabo cair no meio da lição, a lição pausa, o progresso do pedaço atual não conta como erro, e retoma quando o MIDI volta. |
| RF-16 | Rajada incompatível com a lição (várias Note On quase juntas numa lição de nota única) pausa e avisa para desligar ritmo ou song. |

### Calibração

| ID | Requisito |
|---|---|
| RF-20 | O app só mostra guia em cima das teclas depois de uma calibração de dois pontos: tecla branca mais à esquerda e tecla branca mais à direita, cada uma confirmada por Note On. |
| RF-21 | A identidade da tecla é a nota MIDI. A posição no espaço é a do indicador apontando a tecla, amostrada antes do instante em que o dedo oclui a tecla. |
| RF-22 | Com os dois pontos o app escala, posiciona e gira o modelo de 61 teclas. O usuário pode arrastar os cantos com pinça até o realce coincidir com as teclas reais. |
| RF-23 | A calibração fica salva para a sessão e para a próxima abertura. No início, “toque o dó central”: se a nota 60 cair sob o realce do dó central, a calibração completa é pulada. |
| RF-24 | Não há lição jogável sem calibração válida. |

### Lição

| ID | Requisito |
|---|---|
| RF-30 | Uma lição é uma sequência de pedaços. Cada pedaço tem uma lista ordenada de notas esperadas (nota MIDI + dedo sugerido). |
| RF-31 | O botão Ouvir dispara a simulação do trecho. O piano toca o pedaço, a tecla acende e a mão branca fosca, no lugar da mão direita, desce o dedo de cada nota em sincronia, mostrando como tocar. Os outros dedos ficam na posição de cinco dedos. Ouvir de novo repete a mesma simulação. Nada é pontuado. |
| RF-32 | Modo guiado: exatamente uma tecla alvo acesa, com nome, número do dedo e a mão branca fosca com esse dedo na tecla, no lugar da mão direita. O app espera sem limite de tempo. Note On na nota alvo avança. Qualquer outra Note On não avança, marca a tecla premida e mantém o alvo. |
| RF-32a | Antes do primeiro ouvir de cada lição, a mão branca fosca fica parada na posição de cinco dedos, nas teclas onde a mão real direita entra. O card mostra o título da lição, “Ponha a mão assim.” e o botão Ouvir. A pose não é pontuada. Clicar em Ouvir tira a pose parada e começa a simulação do trecho. |
| RF-32b | A única mão desenhada na lição é a de simulação: branca fosca, mão direita, no estilo do tutorial de mãos do Quest, nas teclas onde a mão direita deve ficar. O app não desenha a mão da pessoa. |
| RF-33 | Qualquer velocity 1–127 satisfaz a nota. A faixa leve / médio / forte atualiza um indicador pequeno e não altera acerto. |
| RF-34 | Notas sobrepostas são aceitas. A sequência comparada é a dos Note On, não a limpeza da soltura. |
| RF-35 | Modo memória: nenhuma tecla começa acesa e a mão branca fica ausente. A pessoa toca a sequência do pedaço. Erro acende a nota esperada, a mão branca reaparece no dedo certo dessa tecla por cerca de um segundo, e o pedaço recomeça. A lição inteira não reinicia. |
| RF-36 | Ao concluir um pedaço de memória, o próximo pedaço começa no modo ouvir. O último passo da lição é o trecho acumulado, de memória. |
| RF-37 | Durante a lição há uma instrução curta por vez: ouvir, tocar a tecla acesa, ou tocar de memória. |
| RF-37a | O menu da lição é o card aprovado: retângulo escuro de cantos arredondados, etiqueta “Lição N”, uma frase branca e, quando há uma ação, um botão branco de texto escuro. No passo da pose a frase é “Ponha a mão assim.” e o botão é “Ouvir”. Durante o toque o card não tem botão. |
| RF-38 | Perder o hand tracking durante a lição não pausa, não erra e não esconde o guia das teclas. |
| RF-39 | A fase ouvir e os menus aceitam pinça. Se o tracking de mão falhar no menu, um equivalente por tecla (nota combinada, mostrada no painel) avança a mesma ação. |

### Progresso

| ID | Requisito |
|---|---|
| RF-40 | Estrelas por lição, de 0 a 3, só sobem. 1 = terminou o guiado e não a memória final. 2 = terminou a memória final com um ou mais reinícios de pedaço. 3 = memória final correta na primeira passagem, sem nota errada. |
| RF-41 | A lição N+1 só abre quando a lição N tem pelo menos 2 estrelas. A lição 1 abre após calibração e checklist. |
| RF-42 | Repetir uma lição pode subir a estrela. Não desce. |
| RF-43 | Dias seguidos: um dia local do aparelho conta se houve pelo menos uma memória concluída (lição ou revisão). A contagem zera quando um dia local passa sem isso. Uma falha no meio do dia não zera. |
| RF-44 | Se a última memória concluída foi noutro dia local, a abertura pede essa frase de memória antes da próxima lição nova. Errar oferece ouvir uma vez, guiar uma vez e tentar de novo. Concluir a revisão conta para o dia seguido. |
| RF-45 | O quadro de progresso mostra estrelas totais sobre 36, maior sequência de dias, sequência atual e quantas lições estão em 3 estrelas. Sem nomes de outras pessoas, sem servidor. |
| RF-46 | Progresso, idioma, checklist confirmado e calibração persistem no aparelho e sobrevivem a fechar o app. |

## 5. Requisitos não funcionais

| ID | Requisito |
|---|---|
| RNF-01 | Do Note On recebido até o realce da tecla correspondente: alvo abaixo de 80 ms no Quest 3, medido no teste de MIDI. O caminho não espera o hand tracking. |
| RNF-02 | A cena mantém 72 fps no Quest 3 e no 3S com passthrough ligado e as duas mãos no campo de visão. |
| RNF-03 | Funciona em modo sentado, teclado na mesa, sala com luz suficiente para o passthrough colorido. |
| RNF-04 | Nenhum recurso da v1 exige rede. O manifesto não pede permissão de internet. |
| RNF-05 | Texto de lição cabe numa linha curta, com contorno escuro, legível sobre a sala real a cerca de 50 cm. |
| RNF-06 | Com o teclado parado, a calibração permanece utilizável por uma sessão de 15 minutos. Se o realce sair da tecla, o usuário recalibra sem perder estrelas. |
| RNF-07 | Uma lição é completável só com MIDI, mesmo com hand tracking indisponível depois da calibração. |
| RNF-08 | Falha de USB, nota inesperada ou tracking perdido produz um estado visível (pausa ou aviso). Não corrompe estrelas já gravadas. |

## 6. Requisitos técnicos e arquitetura

Arquitetura hexagonal. O domínio não referencia Unity, OpenXR, Android USB nem o formato de byte MIDI.

### Host previsto

Unity com Meta XR SDK (passthrough, hand tracking, âncora espacial) e um adapter Android fino para USB host MIDI. O Quest é o host USB. Cabo Micro-B do CT-S300 para USB-C do headset, cabo de dados. O teclado usa alimentação própria. A porta USB-C do headset fica ocupada durante a sessão.

Essa escolha de host é decisão de planejamento para não reabrir na implementação. O domínio deve continuar compilável sem o runtime do Quest, para os testes de lição e pontuação rodarem em máquina de desenvolvimento.

### Portas

| Porta | Contrato | Adapter da v1 | Fora da v1 |
|---|---|---|---|
| `MidiInPort` | Notas, velocity, conexão, rajada | USB-MIDI Android, filtro CT-S300 | Bluetooth MIDI, outro teclado, MIDI de rede |
| `MidiOutPort` | Tocar e soltar uma nota no instrumento | Mesmo cabo, saída para o CT-S300 | Sintetizador do headset |
| `KeyboardPosePort` | Modelo de teclas ancorado no mundo, salvar e recuperar pose | Calibração de dois pontos + âncora do Quest | Detecção automática do teclado por câmera |
| `HandInputPort` | Pinça, arraste, pose do indicador para calibrar | Hand tracking do Quest | Controle físico, detecção de dedo na tecla |
| `LessonCatalogPort` | As 12 lições e os textos por idioma | Catálogo embutido | Loja de músicas, arquivo do usuário |
| `ProgressStorePort` | Estrelas, dias, idioma, flag do checklist, data da última memória | Arquivo local no aparelho | Conta, nuvem, leaderboard |
| `ClockPort` | “Agora” e o dia local | Relógio do aparelho | — |

O domínio emite fatos (`NoteHit`, `ChunkCompleted`, `LessonStarsChanged`). Os adapters desenham realce, painel e som de interface. Som musical da frase sai pelo `MidiOutPort`, no piano real, para o timbre que a pessoa vai usar ao tocar ser o mesmo que ela acabou de ouvir.

### Regras que ficam no domínio

- Comparar a sequência de Note On com o pedaço.
- Ignorar Note On enquanto o `MidiOutPort` está tocando a demonstração, num intervalo casado com o que foi enviado (proteção contra eco USB, confirmada ou descartada no teste T-MIDI-07).
- Calcular estrelas, desbloqueio e dia seguido.
- Mapear velocity para três faixas sem participar do acerto.
- Não interpretar pose de mão como nota.

### Mapa do teclado

O modelo tem 61 teclas porque o instrumento alvo tem 61. A nota MIDI de cada tecla física vem da calibração e do octave shift efetivo, não de uma tabela chumbada de fábrica. As lições pedem as notas 60–67. Se a nota 60 não existir no alcance observado, a lição não começa e o checklist de octave shift reaparece.

### Dados locais

Um único arquivo de progresso: idioma, estrelas por lição, sequência atual, maior sequência, dia da última memória concluída, pose de calibração, checklist já confirmado. Sem telemetria.

## 7. Requisitos de negócio

| ID | Requisito |
|---|---|
| RN-01 | O produto da v1 é um protótipo sideload para o próprio uso e teste. Não há preço, anúncio, conta nem ranking entre pessoas. |
| RN-02 | O valor é retenção da frase, não duração de sessão nem número de notas por minuto. |
| RN-03 | Critério para seguir além da v1: um adulto iniciante completa a lição 1 de memória na primeira sessão e, noutro dia, completa a revisão sem outra pessoa guiando. |
| RN-04 | Conteúdo, marca e áudio de interface são originais. Demonstração musical é MIDI enviado ao CT-S300, sem gravar obra de terceiros. |

## 8. Fora de escopo

- Notas caindo, pista rítmica, metrônomo obrigatório, avaliação de timing.
- Pauta, clave, leitura musical.
- Mão esquerda, as duas mãos, acordes, pedal, dinâmica como critério de passe.
- Reconhecer qual dedo tocou.
- Outros teclados e outros protocolos, além da porta já desenhada.
- Importar música, gravar performance, compartilhar, modo multijogador.
- Aftertouch. O instrumento não envia.
- Loja da Meta, compra, conta Meta como identidade do app.
- Quest 2 e controles como interação principal.

## 9. O que o hardware já decide

Isto não espera um teste seu. O desenho cabe no que os dois aparelhos já publicam.

Dá para implementar:

- Nota certa ou errada. O CT-S300 é USB-MIDI class-compliant (micro-B, sem driver, aparece como CASIO USB-MIDI) e o Quest 3 abre teclado dessa classe direto no USB-C. O mesmo caminho já funciona com outro Casio (Privia) no Quest 3, com um pedido de permissão. O cabo tem que ser de dados, micro-B no teclado e USB-C no headset.
- Modo ouvir no alto-falante do próprio teclado. O CT-S300 recebe Note On e toca. O app manda a frase; o domínio ignora, por um instante, as notas que ele mesmo acabou de enviar.
- Nome da nota, uma tecla acesa, memória com luz apagada, estrelas, idioma, progresso no aparelho.
- Mão só para pinça no painel e para apontar na calibração.

Não dá, e o app não promete:

- Saber qual dedo desceu. O erro da ponta do dedo no Quest 3 fica em torno de 1,7 cm, com latência que passa de 200 ms. A tecla branca tem cerca de 2,2 cm. Quem acertou é o Note On.
- Aftertouch. O chart de transmissão marca aftertouch como ausente.
- Dinâmica de soltura. O Note Off sai sempre com velocity 64.
- Carregar o headset e ler MIDI ao mesmo tempo. Há uma porta USB-C.
- Grudar o realce na tecla por câmera, sem calibração. Nem o sensor de profundidade do Quest 3 entrega registro por tecla. Dois toques e o ajuste por pinça são o método.
- Lição de tempo ou de força. O teclado tem touch response de duas curvas mais Off, ação leve, não de martelo. A velocity do Note On existe (1–127). `MIDI Out Velocity` em 1–127 trava esse byte num número; Off é a posição sem número fixo, e o chart continua transmitindo velocity. A faixa leve/médio/forte lê o byte e se esconde se ele vier constante. A lição não depende disso.
- MIDI enquanto o song player do teclado está tocando. O manual diz que a troca para. Ritmo ligado ainda por cima emite notas que não são do dedo. Os dois casos pausam a lição.

## 10. Fontes pedagógicas

- Suzuki, Shinichi. *Nurtured by Love* e o núcleo do método: ouvir e memorizar um trecho pequeno antes de ler símbolo.
- Faber e Faber, *Piano Adventures* nível 1, e Alfred *Basic Piano Library* nível 1: dó central, números de dedo, posição de cinco dedos, uma mão.
- Prática aditiva de frase (chunking e chaining), padrão de ensino instrumental: dominar dois compassos, emendar os próximos, só então tocar o todo.
- Roediger, H. L. e Karpicke, J. D. “Test-Enhanced Learning”, *Psychological Science*, 2006: recordar o trecho retém mais do que ouvir de novo.
- Sweller, J. Cognitive load theory: uma informação nova por vez, guia removido quando o passo já cabe na memória de trabalho.
