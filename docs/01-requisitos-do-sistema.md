# Requisitos do sistema — vr-piano

Planejamento. Sem implementação neste documento. Números ajustáveis (tempos, limites, tolerâncias) moram em [06 · Modelo do domínio §7](06-modelo-de-dominio.md#7-parâmetros) com ID `P-nn`; este documento cita o ID, não repete o valor.

## 1. Decisões fechadas

| Tema | Decisão |
|---|---|
| Headset | Meta Quest 3 e Quest 3S. Quest 2 fora. |
| Usuário | Adulto iniciante, sozinho. Sem conta, sem login, sem nuvem. |
| Corte da v1 | Mão direita, posição de cinco dedos a partir do dó central (MIDI 60), melodias curtas. |
| Como se aprende | Chamada e resposta, em três fases por trecho: assistir a mão 3D tocar → tocar guiado (tecla da vez colorida, próxima em cinza dinâmico) → tocar de memória, com as luzes apagadas. A lição conta quando a sequência sai sem guia. |
| Demonstração | Híbrida. Num trecho inédito, a mão 3D toca sozinha uma vez. Depois disso, só quando a pessoa aperta “Ouvir”. |
| Tempo | Tempo elástico. O cinza da próxima tecla escurece no andamento real da música. Se a pessoa atrasa, a tecla fica colorida e espera por ela. Adiantar ou atrasar não trava nada, mas entra na pontuação de ritmo. |
| Som do app | O app só envia notas ao piano na demonstração. Na vez guiada e na memória, o piano soa só o que a pessoa toca (`Local Control` ligado). |
| Tracking | A pose libera o início da lição. Depois disso, perder o tracking não pausa nem erra; a lição segue pelo MIDI. |
| O que não entra | Esteira de notas caindo, combo, vidas, partitura, mão esquerda, acordes. |
| Conteúdo | 12 lições autorais, cerca de 5 minutos cada, e a música do nível 1, escritas no formato de música que o importador futuro também vai gerar. Importar MIDI ou MusicXML fica para depois da v1 ([08](08-roadmap-importacao.md)). |
| Gamificação | 0–3 estrelas por lição, dias seguidos, quadro de progresso no aparelho. |
| Idioma | Português (Brasil) e inglês, escolha explícita na primeira tela e nas configurações. |
| Distribuição da v1 | Sideload de desenvolvimento. Sem Meta Store, sem compra, sem anúncio. |
| Hardware de teste | Quest 3 ou 3S e Casio CT-S300 disponíveis para os testes de aceite. |

Premissas adotadas porque não foram contestadas:

- Velocity aparece em três faixas (leve, médio, forte) e nunca reprova a lição.
- “Ranking” com uma pessoa só no aparelho é um quadro de progresso pessoal. Não há outros jogadores.
- Não há modelo de negócio na v1. O requisito de negócio é o problema e o critério de sucesso do aprendizado.

## 2. Problema e sucesso

Apps de piano em realidade mista usam notas caindo. A pessoa reage ao que vê, como um robô: depende da tela, não ganha noção espacial do teclado e, ao parar, não lembra a frase.

Este sistema ensina por assimilação e repetição ativa (chamada e resposta), com o foco direto nas teclas reais do CT-S300 e o Quest em passthrough. A pessoa vê e ouve a frase, repete com uma pista que antecipa o próximo dedo e o tempo, e depois toca de memória. O objetivo é tocar e repetir de memória frases curtas de mão direita.

Sucesso da v1, medido no próprio aparelho, sem analytics de rede:

- Na primeira sessão, a pessoa termina a lição 1 de memória sem ajuda de outra pessoa.
- Na abertura seguinte, em outro dia, a revisão pede a frase anterior antes de matéria nova, e a pessoa consegue concluí-la.

## 3. Abordagem pedagógica

Base usada, reduzida ao mínimo que cabe num teclado de 61 teclas e numa sessão curta:

- **Observar antes de tocar** (método Suzuki, a parte que serve aqui): a mão 3D toca a sequência no andamento, o próprio piano soa e a tecla acende na cor ativa no instante de cada nota. A pessoa só assiste a postura, o dedilhado e a passagem entre as notas. Num trecho inédito isso é automático, para a pessoa ansiosa não pular e tentar adivinhar. Depois, “Ouvir” repete quando ela quiser. No fim, a mão some em fade e o teclado fica limpo para a vez dela.
- **Uma novidade por lição** (carga cognitiva): uma nota nova ou um gesto novo (subir, descer, saltar).
- **Posição de cinco dedos** (Alfred / Faber): polegar no dó central até o mínimo no sol. A mão branca mostra essa pose. O Quest reconhece quando a mão direita da pessoa está nessa postura sobre as teclas de casa e libera a lição. A tecla tocada sempre vem do MIDI.
- **Vez guiada com antecipação**: a tecla da vez fica na cor ativa e a próxima já aparece em cinza claro, que escurece até a hora de tocar. A própria tecla mostra onde vai o próximo dedo e quanto falta para tocar, sem barra caindo. Ao fim da sequência, recomeça. A pessoa repete até uma volta sair sem nota errada.
- **Recordação, não reestudo** (retrieval practice): depois da volta limpa, as luzes apagam. Tocar a sequência sem guia é o que prova que a pessoa decorou e abre a próxima lição. Se ela trava por `P-33`, a tecla certa pisca em cinza uma vez, para ela não se frustrar.
- **Revisão noutro dia**: ao abrir o app noutro dia local, a primeira tarefa é a última frase concluída de memória.
- **Mão opcional**: dá para esconder a mão branca e ficar só com a cor na tecla.

Dinâmica fica de fora da aprovação. O CT-S300 não tem aftertouch. O chart MIDI transmite Note On com velocity 1–127 e Note Off sempre em 64. O manual não diz o que `MIDI Out Velocity = Off` faz com esse número; a leitura usada no checklist vem de quem mediu o aparelho, não de uma frase da Casio, e é confirmada em T-MIDI-03. Mostrar leve / médio / forte habitua o ouvido sem transformar a lição num teste de força. Se a velocity vier fixa, a faixa some e a lição continua.

Tempo é ensinado, mas não reprova. O cinza dinâmico mostra a cadência real (andamento lento, `P-10`). Cada nota da vez guiada é avaliada como no tempo, adiantada ou atrasada (`P-30`, `P-31`). Atrasar não atropela a pessoa: a tecla espera, colorida. A pontuação de ritmo é guardada à parte e não muda estrela nem desbloqueio.

### Currículo

Posição: dedo 1 no dó central (nota MIDI 60), 2 no ré (62), 3 no mi (64), 4 no fá (65), 5 no sol (67). Só teclas brancas. Nome exibido em pt-BR: Dó Ré Mi Fá Sol. Em inglês: C D E F G. O domínio guarda nota MIDI e dedo; o texto é localização. Cada lição é **uma** sequência; o formato e o catálogo estão no [06 §5](06-modelo-de-dominio.md#5-formato-de-música-e-catálogo).

| Lição | Matéria nova | Sequência | Notas |
|---|---|---|---|
| 1 | Dó, dedo 1 | Dó, quatro vezes | 4 |
| 2 | Ré, dedo 2 | Dó–Ré | 2 |
| 3 | Mi, dedo 3 | Dó–Ré–Mi | 3 |
| 4 | Descer | Mi–Ré–Dó | 3 |
| 5 | Ida e volta | Dó–Ré–Mi–Ré–Dó | 5 |
| 6 | Fá, dedo 4 | Mi–Fá | 2 |
| 7 | Subir até o fá | Dó–Ré–Mi–Fá | 4 |
| 8 | Sol, dedo 5 | Fá–Sol | 2 |
| 9 | Cinco dedos subindo | Dó–Ré–Mi–Fá–Sol | 5 |
| 10 | Cinco dedos descendo | Sol–Fá–Mi–Ré–Dó | 5 |
| 11 | Salto dentro da posição | Dó–Mi–Sol–Mi–Dó | 5 |
| 12 | Revisão | A frase da 11 seguida da subida da 9 | 10 |
| Música | Nível 1 | As doze sequências em ordem, como uma peça | 50 |

## 4. Requisitos funcionais

### Idioma

| ID | Requisito |
|---|---|
| RF-01 | A primeira tela oferece pt-BR e English, sem pré-seleção silenciosa. |
| RF-02 | A escolha persiste no aparelho e pode ser trocada nas configurações, fora de uma lição. |
| RF-03 | Notas, botões, fases e faixas de velocity usam a língua escolhida. A lógica da lição não muda com a língua. Todo texto vem da [tabela de textos](07-textos-da-interface.md). |

### Teclado e MIDI

| ID | Requisito |
|---|---|
| RF-10 | O app detecta o CT-S300 como dispositivo USB-MIDI na porta USB-C do Quest e mostra conectado ou desconectado. Se o sistema pedir permissão de USB, o app explica numa linha e, se negada, oferece pedir de novo. |
| RF-11 | Cada Note On entregue ao domínio contém nota MIDI, velocity 1–127 e o instante de recebimento. Note On com velocity 0 é tratado como Note Off. A velocity do Note Off é ignorada (o CT-S300 envia 64). Mensagens que não são nota (clock, active sensing, control change, program change) são descartadas antes do domínio. |
| RF-12 | Antes da primeira lição, um checklist pede: ritmo desligado, song player parado, `MIDI Out Octave Shift = 0`, `Touch Response = Normal`, `MIDI Out Velocity = Off`, `Local Control` ligado. O usuário confirma os cinco itens. |
| RF-13 | Se as últimas `P-03` Note On da sessão tiverem a mesma velocity, um aviso não bloqueante sugere conferir `MIDI Out Velocity = Off` e o indicador de faixa se esconde. Ele volta quando chega uma velocity diferente. |
| RF-14 | A demonstração envia ao teclado Note On e Note Off, uma nota por vez, nos instantes, durações e velocities do formato de música (`P-09`, `P-11` quando a nota não traz velocity, canal `P-12`). É o único momento em que o app envia notas. Nada é pontuado. |
| RF-15 | Se o cabo cair durante a lição, a lição pausa. Na vez guiada, retoma no mesmo passo, com a tecla já colorida. Na memória, a tentativa recomeça do início sem contar como falha. Durante a demonstração, a demonstração é cancelada e a lição volta à pose. Estrelas gravadas não mudam. |
| RF-16 | Rajada (`P-04`/`P-05` ou `P-06`) pausa a lição e avisa para desligar ritmo ou song. As notas da rajada não contam como erro. A lição retoma depois de `P-07` de silêncio, com a mesma regra do RF-15. |

### Calibração

| ID | Requisito |
|---|---|
| RF-20 | O app só mostra guia em cima das teclas depois de uma calibração de dois pontos: tecla branca mais à esquerda e tecla branca mais à direita, cada uma confirmada por Note On. |
| RF-21 | A identidade da tecla é a nota MIDI. A posição no espaço é a da ponta do indicador direito, mediana das amostras na janela `P-18` antes do Note On. |
| RF-22 | Com os dois pontos, o app escala, posiciona e gira o modelo de 61 teclas. Se as duas notas não forem teclas brancas separadas por 60 semitons, ou se a distância medida não bater com 35 larguras de tecla branca (`P-19`, `P-20`), os toques são recusados e pedidos de novo. Depois, o usuário pode arrastar quatro alças nos cantos com pinça até o realce coincidir com as teclas reais. |
| RF-23 | A nota da tecla da esquerda define o deslocamento de oitava efetivo. A calibração fica salva (âncora espacial e notas) para a próxima abertura. No início, “Toque o dó central”: se a nota 60 cair sob o realce do dó central, a calibração completa é pulada. Se a âncora não for encontrada, a calibração completa é pedida. |
| RF-24 | Não há lição jogável sem calibração válida. Se a nota 60 não estiver no alcance calibrado, a lição não começa e o checklist de octave shift reaparece. |

### Lição

Máquina de estados completa em [06 §3](06-modelo-de-dominio.md#3-máquina-de-estados-da-lição).

| ID | Requisito |
|---|---|
| RF-30 | Cada lição e a música são arquivos no formato de música (06 §5): metadados, trechos e eventos de nota com nota MIDI, mão, dedo (1–5), instante, duração e velocity. O app só lê esse formato. A v1 recusa arquivos fora das restrições da v1 (mão direita, sem acorde, notas 60–67). |
| RF-31 | **Pose.** No começo de cada lição a mão branca fica parada na posição de cinco dedos e o card diz “Ponha a mão assim.”, com o botão “Ouvir” e o controle “Mão”. A mão direita rastreada que cumpre `P-13`–`P-17` sobre as teclas dó–sol calibradas conta como pose correta e leva à demonstração (se o item for inédito) ou à vez guiada. Antes disso, se a mão sai da pose, o reconhecimento recomeça. A pose não dá estrela. Note On nesta fase não conta. |
| RF-31a | **Demonstração.** Na primeira vez que a pessoa entra num item (item inédito), a demonstração começa sozinha depois da pose. Quando ela termina uma vez, o item deixa de ser inédito, e a demonstração passa a acontecer só com “Ouvir”, no card da pose e no da vez guiada. A demonstração envia a sequência ao piano (RF-14). A mão branca desce o dedo de cada nota junto com o som, e a tecla acende na cor ativa no instante da nota. No fim, a mão some em fade (`P-35`) e a vez guiada começa, com ou sem pose reconhecida. Note On recebido durante a demonstração e até `P-08` depois dela não conta. |
| RF-32 | **Vez guiada.** Não há mão branca. A tecla da vez fica na cor ativa e o card mostra o nome do dedo. Assim que a pessoa acerta, a próxima tecla aparece em cinza dinâmico. O cinza escurece de `P-29` até o tom cheio no tempo de ataque (acerto anterior + duração do passo no andamento), quando a tecla vira cor ativa. Se a pessoa não toca, a tecla espera colorida, sem limite (tempo elástico). Note On na nota da vez: verde por `P-23` e avança, e a tecla tocada segue na cor ativa enquanto estiver pressionada. Outra nota: vermelho por `P-24` na tecla tocada, e o passo e o tempo de ataque não mudam. O primeiro passo de cada volta já começa colorido. Ao fim da sequência, a vez guiada recomeça do primeiro passo. Regras completas no [06 §3.1](06-modelo-de-dominio.md#31-cinza-dinâmico-e-ritmo). |
| RF-32a | **Ritmo.** Cada acerto que tinha tempo de ataque é avaliado. Mais de `P-30` antes do tempo de ataque é “Adiantado”, mais de `P-31` depois é “Atrasado”, e entre os dois é no tempo. Adiantado e atrasado avançam normalmente e mostram a palavra acima da tecla por `P-32`; no tempo não mostra nada. A pontuação de ritmo da volta é o número de notas no tempo sobre o de notas avaliadas. Ela não muda estrela nem desbloqueio. |
| RF-32e | **Mesma tecla em seguida.** Quando a próxima nota é a tecla que acabou de ser tocada, a tecla fica com contorno na cor ativa e só o centro faz o ciclo de cinza. Assim a pessoa entende que é para tocar de novo, e não para segurar. |
| RF-32b | A única mão desenhada é a de simulação: mão 3D direita com anatomia real, branca fosca, asset original no estilo das mãos do sistema do Quest. Ela aparece na pose e na demonstração. O app não desenha a mão da pessoa. |
| RF-32c | Existe um controle “Mão” / “Hand”, ligado por padrão. Desligado, a mão branca some na hora e a pose é pulada. A demonstração continua, só com som e tecla acesa. A escolha persiste. |
| RF-32d | A música do nível 1 é a junção das doze sequências, na ordem, com o mesmo ciclo pose → demonstração → vez guiada → memória. A etiqueta do card mostra de que lição é o trecho atual. Ela abre quando as doze lições têm 2 ou mais estrelas. |
| RF-33 | Qualquer velocity 1–127 satisfaz a nota. A faixa leve / médio / forte (`P-01`, `P-02`) atualiza um indicador pequeno e não altera acerto. |
| RF-34 | Notas sobrepostas são aceitas. A sequência comparada é a dos Note On, não a limpeza da soltura. |
| RF-35 | **Passagem limpa.** Uma volta da vez guiada é limpa quando vai do primeiro ao último passo sem nenhum Note On errado. O ritmo não conta para isso. A primeira volta limpa leva à memória: o card diz “Agora de memória.”, espera `P-22`, e cor, cinza e nome somem juntos. Note On nessa espera não conta. |
| RF-36 | **Memória.** Nenhuma tecla acesa, sem mão branca, card só com a frase e sem botão. Note On certo avança sem cor. Note On errado encerra a tentativa: âmbar por `P-25` na nota esperada, vermelho por `P-24` na tocada, e a tentativa recomeça do primeiro passo. Depois de `P-21` tentativas falhas seguidas, a lição volta à vez guiada com “Mais uma vez com guia.”, sem demonstração automática e com “Ouvir” disponível. Completar a sequência conclui a memória: verde por `P-23` na última tecla e o resultado. |
| RF-36a | **Dica de memória.** Se a pessoa fica `P-33` sem tocar na memória (desde o começo da tentativa ou desde o último acerto), a tecla esperada pisca em cinza claro por `P-34`. A piscada se repete a cada `P-33` sem nota. Usar a dica não é erro, mas a execução não ganha 3 estrelas (RF-40). |
| RF-37 | O card mostra uma frase por vez: “Ponha a mão assim.”, “Olhe e ouça.”, o nome do dedo da vez (“Polegar”, “Indicador”, “Médio”, “Anelar”, “Mínimo” / “Thumb”, “Index”, “Middle”, “Ring”, “Pinky”), “Agora de memória.” ou o resultado. O resultado mostra as estrelas e a pontuação de ritmo da volta limpa (“No tempo: 3 de 4”). |
| RF-37a | O menu é o card aprovado (`docs/visao/capa-licao1.jpg`): retângulo escuro de cantos arredondados, etiqueta “Lição N”, uma frase branca e, quando há escolha, botões brancos de texto escuro. Na vez guiada o card tem só “Ouvir”. Na memória o card não tem botão. |
| RF-38 | Depois da pose, perder o tracking ou sair da pose não pausa, não erra e não esconde a cor da tecla. |
| RF-39 | Os botões aceitam pinça. Cada botão tem uma tecla equivalente, mostrada numa linha menor: o 1º botão é a tecla branca mais grave do teclado, o 2º a tecla branca seguinte, o 3º a próxima (`P-27`). Essas teclas ficam fora de 60–67 e nunca colidem com uma nota cobrada. |
| RF-39a | Se o app perde o foco (botão Meta, headset tirado), a lição pausa. Ao voltar, o card oferece “Continuar” e “Sair da aula”. Continuar segue a regra de retomada do RF-15. Sair leva ao painel entre lições, sem mudar estrelas. |

### Progresso

| ID | Requisito |
|---|---|
| RF-40 | Estrelas por lição, de 0 a 3, só sobem. 1 = fez uma volta limpa na vez guiada, sem concluir a memória. 2 = concluiu a memória depois de pelo menos uma tentativa falha, de voltar à vez guiada ou de usar a dica. 3 = concluiu a memória na primeira tentativa daquela execução, sem dica. O ritmo não entra nas estrelas. |
| RF-41 | A lição N+1 abre quando a N tem 2 ou mais estrelas. A lição 1 abre após checklist e calibração. A música abre quando as doze têm 2 ou mais. |
| RF-42 | Repetir uma lição pode subir a estrela. Não desce. |
| RF-43 | Dias seguidos: um dia local do aparelho conta se houve pelo menos uma memória concluída (lição, música ou revisão). Concluir no dia D com último dia contado D−1 soma 1; com último dia contado D não muda; em qualquer outro caso a sequência vira 1. Ao abrir, se o último dia contado é anterior a D−1, a sequência atual mostra 0. Relógio que volta para trás não altera a contagem nem dispara revisão. |
| RF-44 | Se a última memória concluída foi noutro dia local, a abertura (depois da verificação do dó central) pede essa sequência de memória antes de qualquer outra coisa. Se foi a música, a revisão usa a sequência da lição 12. Errar mostra o retorno de erro do RF-36 e oferece “Ouvir” e “Com guia” (uma volta da vez guiada), cada um uma vez por revisão; tocar a primeira nota inicia a próxima tentativa. Concluir conta para o dia seguido e não altera estrelas. A revisão não tem botão de pular. |
| RF-45 | O quadro de progresso mostra estrelas das lições sobre 36, estrelas da música sobre 3, maior sequência de dias, sequência atual e quantas lições estão em 3 estrelas. Lista as 12 lições e a música com estrelas, melhor ritmo (“3 de 4 no tempo”) e estado (aberta ou fechada). Sem nomes de outras pessoas, sem servidor. |
| RF-46 | Progresso (estrelas, ritmo, itens já demonstrados), idioma, controle “Mão”, checklist confirmado e calibração persistem no aparelho e sobrevivem a fechar o app, inclusive à força no meio de uma gravação. |

## 5. Requisitos não funcionais

| ID | Requisito |
|---|---|
| RNF-01 | Do Note On recebido até o realce da tecla correspondente: abaixo de `P-28` (mediana de 20 amostras) no Quest 3 e no 3S. O caminho não espera o hand tracking. |
| RNF-02 | A cena mantém 72 fps no Quest 3 e no 3S com passthrough ligado e as duas mãos no campo de visão. |
| RNF-03 | Funciona em modo sentado, teclado na mesa, sala com luz suficiente para o passthrough colorido. |
| RNF-04 | Nenhum recurso da v1 exige rede. O manifesto não pede permissão de internet. |
| RNF-05 | Texto de lição cabe numa linha curta, com contorno escuro, legível sobre a sala real a cerca de 50 cm. Nenhuma frase do card passa de 32 caracteres em nenhum dos idiomas. |
| RNF-06 | Com o teclado parado, a calibração permanece utilizável por uma sessão de 15 minutos. Se o realce sair da tecla, o usuário recalibra sem perder estrelas. |
| RNF-07 | Depois da pose (ou com “Mão” desligada), uma lição é completável só com MIDI, mesmo com hand tracking indisponível. |
| RNF-08 | Falha de USB, nota inesperada ou tracking perdido produz um estado visível (pausa ou aviso). Não corrompe estrelas já gravadas. |
| RNF-09 | A gravação do progresso é atômica: em qualquer instante de queda, o arquivo lido na próxima abertura é a versão anterior completa ou a nova completa. |
| RNF-10 | Abertura a frio até a primeira tela útil em menos de 10 s no Quest 3. |
| RNF-11 | O domínio compila e roda os testes sem Unity, sem Android e sem headset. |

## 6. Requisitos técnicos e arquitetura

Arquitetura hexagonal. O domínio não referencia Unity, OpenXR, Android USB nem o formato de byte MIDI. Detalhes de stack, ponte MIDI, manifest, persistência e geometria do teclado: [05 · Arquitetura técnica](05-arquitetura-tecnica.md). Estados, eventos, catálogo e parâmetros: [06 · Modelo do domínio](06-modelo-de-dominio.md).

### Host previsto

Unity com Meta XR SDK (passthrough, hand tracking, âncora espacial) e um adapter Android fino para USB host MIDI. O Quest é o host USB. Cabo Micro-B do CT-S300 para USB-C do headset, cabo de dados. O teclado usa alimentação própria. A porta USB-C do headset fica ocupada durante a sessão, então o headset roda na bateria.

Essa escolha de host é decisão de planejamento para não reabrir na implementação. O domínio continua compilável sem o runtime do Quest, para os testes de lição e pontuação rodarem em máquina de desenvolvimento.

### Portas

| Porta | Contrato | Adapter da v1 | Fora da v1 |
|---|---|---|---|
| `MidiInPort` | Note On/Off com instante, conexão | USB-MIDI Android, filtro CT-S300 | Bluetooth MIDI, outro teclado, MIDI de rede |
| `MidiOutPort` | Tocar e soltar uma nota no instrumento | Mesmo cabo, saída para o CT-S300 | Sintetizador do headset |
| `KeyboardPosePort` | Modelo de teclas ancorado no mundo, salvar e recuperar pose | Calibração de dois pontos + âncora do Quest | Detecção automática do teclado por câmera |
| `HandInputPort` | Pinça, arraste, ponta do indicador para calibrar, sinal “pose de cinco dedos” | Hand tracking do Quest | Controle físico, detecção de dedo na tecla |
| `LessonCatalogPort` | As 12 lições e a música, no formato de música | Catálogo embutido | Importador de MIDI e MusicXML ([08](08-roadmap-importacao.md)), loja de músicas |
| `ProgressStorePort` | Estrelas, dias, idioma, “Mão”, checklist, calibração, última memória | Arquivo local no aparelho | Conta, nuvem, leaderboard |
| `ClockPort` | Instante monotônico e o dia local | Relógio do aparelho | — |

O domínio emite fatos (lista em [06 §4](06-modelo-de-dominio.md#4-eventos)). Os adapters desenham realce, card, mão branca e som de interface. O som musical da demonstração sai pelo `MidiOutPort`, no piano real, para o timbre que a pessoa ouve ser o mesmo que ela vai tocar.

### Regras que ficam no domínio

- Máquina de estados da lição e da revisão.
- Comparar o Note On com o passo da vez.
- Calcular tempo de ataque, estado da tecla (antecipação, ataque) e avaliação de ritmo.
- Decidir quando a demonstração é automática e quando aparece a dica de memória.
- Ignorar Note On durante a demonstração e até `P-08` depois dela (proteção contra eco USB, confirmada ou descartada em T-MIDI-07).
- Detectar rajada e velocity fixa.
- Calcular estrelas, desbloqueio, dia seguido e quando pedir revisão.
- Mapear velocity para três faixas sem participar do acerto.
- Não interpretar pose de mão como nota. A pose chega como sinal booleano do `HandInputPort`; as tolerâncias são aplicadas no adapter.

### Mapa do teclado

O modelo tem 61 teclas porque o instrumento alvo tem 61 (com shift 0, MIDI 36–96). A nota MIDI de cada tecla física vem da calibração (nota da tecla da esquerda), não de uma tabela chumbada. As lições pedem as notas 60–67.

### Dados locais

Um único arquivo de progresso, esquema em [06 §6](06-modelo-de-dominio.md#6-arquivo-de-progresso). Sem telemetria.

## 7. Requisitos de negócio

| ID | Requisito |
|---|---|
| RN-01 | O produto da v1 é um protótipo sideload para o próprio uso e teste. Não há preço, anúncio, conta nem ranking entre pessoas. |
| RN-02 | O valor é retenção da frase, não duração de sessão nem número de notas por minuto. |
| RN-03 | Critério para seguir além da v1: um adulto iniciante completa a lição 1 de memória na primeira sessão e, noutro dia, completa a revisão sem outra pessoa guiando. |
| RN-04 | Conteúdo, marca, mão branca e áudio de interface são originais. Nenhum asset da Meta ou da Casio é copiado. Demonstração musical é MIDI enviado ao CT-S300, sem gravar obra de terceiros. |

## 8. Fora de escopo

- Notas caindo, pista rítmica, metrônomo audível, ritmo como critério de estrela ou de desbloqueio.
- Pauta, clave, leitura musical.
- Mão esquerda, as duas mãos, acordes, pedal, dinâmica como critério de passe.
- Reconhecer qual dedo tocou.
- Outros teclados e outros protocolos, além da porta já desenhada.
- Importar música (planejado depois da v1, no [08](08-roadmap-importacao.md)), gravar performance, compartilhar, modo multijogador.
- Aftertouch. O instrumento não envia.
- Loja da Meta, compra, conta Meta como identidade do app.
- Quest 2 e controles como interação principal.

## 9. O que o hardware já decide

Isto não espera um teste seu. O desenho cabe no que os dois aparelhos já publicam.

Dá para implementar:

- Nota certa ou errada. O CT-S300 é USB-MIDI class-compliant (micro-B, sem driver, aparece como CASIO USB-MIDI) e o Quest 3 abre teclado dessa classe direto no USB-C. O mesmo caminho já funciona com outro Casio (Privia) no Quest 3, com um pedido de permissão. O cabo tem que ser de dados, micro-B no teclado e USB-C no headset.
- Demonstração no alto-falante do próprio teclado. O CT-S300 recebe Note On e toca. O app manda a sequência; o domínio ignora, durante a demonstração, as notas que chegam.
- Nome da nota, cor e cinza dinâmico na tecla, ritmo medido pelo instante do Note On, mão branca na demonstração, pose grossa da mão direita sobre dó–sol, estrelas, idioma, progresso no aparelho.
- Pinça no card e o indicador na calibração usam o tracking. A nota não.

Não dá, e o app não promete:

- Saber qual dedo desceu na tecla. O erro da ponta do dedo no Quest 3 fica em torno de 1,7 cm. A pose aceita é grossa: mão direita sobre o bloco dó–sol, palma para baixo, polegar e mínimo perto das teclas certas. Quem acertou a nota é o Note On.
- Aftertouch. O chart de transmissão marca aftertouch como ausente.
- Dinâmica de soltura. O Note Off sai sempre com velocity 64.
- Carregar o headset e ler MIDI ao mesmo tempo. Há uma porta USB-C.
- Grudar o realce na tecla por câmera, sem calibração. Nem o sensor de profundidade do Quest 3 entrega registro por tecla. Dois toques e o ajuste por pinça são o método.
- Lição de força. O teclado tem touch response de duas curvas mais Off, ação leve, não de martelo. A velocity do Note On existe (1–127). `MIDI Out Velocity` em 1–127 trava esse byte num número; Off é a posição sem número fixo. A faixa leve/médio/forte lê o byte e se esconde se ele vier constante. A lição não depende disso.
- MIDI enquanto o song player do teclado está tocando. O manual diz que a troca para. Ritmo ligado ainda por cima emite notas que não são do dedo. Os dois casos pausam a lição.

## 10. Fontes pedagógicas

- Suzuki, Shinichi. *Nurtured by Love* e o núcleo do método: ouvir e memorizar um trecho pequeno antes de ler símbolo.
- Faber e Faber, *Piano Adventures* nível 1, e Alfred *Basic Piano Library* nível 1: dó central, números de dedo, posição de cinco dedos, uma mão.
- Prática aditiva de frase (chunking e chaining), padrão de ensino instrumental: dominar um trecho curto, emendar o próximo, só então tocar o todo.
- Chamada e resposta (*call and response*), prática comum no ensino de música por imitação: o professor toca, o aluno repete logo em seguida.
- Roediger, H. L. e Karpicke, J. D. “Test-Enhanced Learning”, *Psychological Science*, 2006: recordar o trecho retém mais do que ouvir de novo.
- Sweller, J. Cognitive load theory: uma informação nova por vez, guia removido quando o passo já cabe na memória de trabalho.
