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
- **Posição de cinco dedos** (Alfred / Faber): polegar no dó central até o mínimo no sol. A mão branca mostra essa pose. O Quest reconhece quando a mão direita da pessoa está nessa postura sobre as teclas de casa. Aí o card muda e a mão branca marca o próximo dedo. A tecla tocada continua vindo do MIDI.
- **Loop e repetição**: a mão branca repete a sequência da aula sem parar. A pessoa toca junto, decora a ordem dos dedos e repete até sair limpo.
- **Cada aula é uma sequência.** O nível 1 junta as doze sequências numa música só.
- **Mão opcional**: dá para esconder a mão branca e ficar só com a cor na tecla.

Dinâmica fica de fora da aprovação. O CT-S300 não tem aftertouch. O chart MIDI transmite Note On com velocity 1–127 e Note Off sempre em 64. O manual não diz o que `MIDI Out Velocity = Off` faz com esse número; a leitura usada no checklist vem de quem mediu o aparelho, não de uma frase da Casio. Mostrar leve / médio / forte habitua o ouvido sem transformar a lição num teste de força. Se a velocity vier fixa, a faixa some e a lição continua.

Tempo também fica de fora da aprovação. A demonstração é lenta e separada, para a frase ser inteligível. Na hora de tocar, a pessoa pode ir no tempo dela. Cobrar janela de milissegundos é o que produz a sensação de tocar no piloto automático.

### Currículo das 12 lições

Posição: dedo 1 no dó central (nota MIDI 60), 2 no ré, 3 no mi, 4 no fá, 5 no sol. Só teclas brancas. Nome exibido em pt-BR: Dó Ré Mi Fá Sol. Em inglês: C D E F G. O domínio guarda nota MIDI e dedo; o texto é localização.

| Lição | Matéria nova | Sequência |
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
| 12 | Revisão | A frase da 11 e a subida da 9 |
| Música | Nível 1 | As doze sequências em ordem, como uma peça |

Cada aula ensina uma sequência. A mão branca fica em loop nessa sequência. A próxima abre quando a pessoa toca a sequência inteira uma vez, acompanhando o loop, sem nota errada. A música do nível 1 abre quando as doze estão feitas.

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
| RF-31 | Com a pose aceita, a mão branca entra em loop na sequência da aula. A cada nota, o dedo correspondente acende na mão e a tecla acende junto. O piano soa o trecho no mesmo tempo. O loop recomeça sozinho. A pessoa toca junto. Uma passagem MIDI limpa da sequência completa a aula. |
| RF-32 | Durante o loop, a mão branca marca só o próximo dedo. Os outros ficam na posição de casa. Note On na nota da vez avança o passo. Outra nota não avança, pinta a tecla tocada de vermelho e o loop segue no mesmo passo. |
| RF-32a | No começo da aula a mão branca fica parada na posição de cinco dedos e o card diz “Ponha a mão assim.” O tracking da mão direita, sobre as teclas dó–sol calibradas, com a palma para baixo e os dedos abertos, estável por cerca de meio segundo, conta como pose correta. Aí o card troca para o nome do próximo dedo e esse dedo acende na mão branca. Se a mão sai da pose, o card volta para “Ponha a mão assim.” e o loop pausa. A pose não dá estrela. A nota continua sendo o MIDI. |
| RF-32b | A única mão desenhada é a de simulação: branca fosca, mão direita, no estilo do tutorial de mãos do Quest. O app não desenha a mão da pessoa. |
| RF-32c | Existe um controle “Mão” / “Hand”, ligado por padrão. Desligado, a mão branca some na hora e a aula segue só com a cor da tecla. Com a mão desligada, a checagem de pose não bloqueia o loop. A escolha persiste. |
| RF-32d | O fim do nível 1 é uma música: as doze sequências tocadas em ordem, no mesmo loop. Ela abre quando as doze aulas estão completas. |
| RF-33 | Qualquer velocity 1–127 satisfaz a nota. A faixa leve / médio / forte atualiza um indicador pequeno e não altera acerto. |
| RF-34 | Notas sobrepostas são aceitas. A sequência comparada é a dos Note On, não a limpeza da soltura. |
| RF-35 | Errar a nota não reinicia a aula inteira. A tecla errada fica vermelha, a tecla da vez continua acesa, o dedo marcado na mão branca não muda, e o loop espera essa nota. |
| RF-36 | A aula é uma sequência. Completar a sequência uma vez sem nota errada abre a próxima. A música do nível 1 é a junção das doze, na ordem. |
| RF-37 | O card mostra uma frase por vez. Antes da pose: “Ponha a mão assim.” Com a pose certa: o nome do próximo dedo (“Polegar”, “Indicador”, “Médio”, “Anelar”, “Mínimo” / “Thumb”, “Index”, “Middle”, “Ring”, “Pinky”). |
| RF-37a | O menu é o card aprovado: retângulo escuro de cantos arredondados, etiqueta “Lição N”, uma frase branca. Na pose há o botão “Ouvir”, que também liga o loop se a pessoa não quiser esperar o reconhecimento. Durante o loop o card não tem botão. O controle “Mão” fica nesse card. |
| RF-38 | Perder o tracking da mão pausa o loop e devolve o card para “Ponha a mão assim.” Não conta como nota errada. A cor da tecla continua. |
| RF-39 | Os botões do card aceitam pinça. Se o tracking falhar no menu, a tecla indicada no card faz a mesma ação. |

### Progresso

| ID | Requisito |
|---|---|
| RF-40 | Estrelas por aula, de 0 a 3, só sobem. 2 = a sequência saiu limpa depois de algum erro. 3 = a primeira volta do loop saiu limpa. Completar a sequência limpa abre a próxima. |
| RF-41 | A aula N+1 abre quando a N foi completada limpa. A aula 1 abre após calibração e checklist. A música abre quando as doze estão completas. |
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
- Nome da nota, cor na tecla, mão branca em loop, pose da mão direita sobre dó–sol, estrelas, idioma, progresso no aparelho.
- Mão branca para mostrar a pose e o próximo dedo. Pinça no card e o dedo na calibração usam o tracking. A nota não.

Não dá, e o app não promete:

- Saber qual dedo desceu na tecla. O erro da ponta do dedo no Quest 3 fica em torno de 1,7 cm. A pose que o app aceita é a mão direita sobre o bloco dó–sol, palma para baixo, dedos abertos. Quem acertou a nota é o Note On.
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
