# Conceito do protótipo em AR

Descrição do que se vê e do que se faz. Sem cena, sem asset e sem código.

O protótipo serve para validar três coisas no Quest, com o CT-S300 de verdade: o realce cai na tecla física, o acerto vem do MIDI em tempo útil, e a pessoa percebe a diferença entre “tecla acesa” e “luzes apagadas de propósito”.

## 1. O que está no mundo

A sala é o passthrough do Quest 3 ou 3S. Não há cenário virtual, chão falso nem piano fantasma opaco.

Há três camadas, todas ancoradas no teclado depois da calibração. Nada da lição fica grudado na cabeça. Painel head-locked cobre as teclas quando a pessoa olha para baixo.

| Camada | O que é | Quando existe |
|---|---|---|
| Realce das teclas | Um plano translúcido por tecla, a uns 2 mm da superfície, menor que a tecla para a borda real continuar visível | Depois da calibração |
| Mão-professora | Só a mão direita branca fosca do tutorial. É a única mão que o app desenha | Posição e simulação do Ouvir. Some na memória. Volta um segundo no erro |
| Menu | Card escuro arredondado: etiqueta da lição, uma frase, botão branco | Quando a lição pede uma ação. Sem botão enquanto a pessoa está tocando |

O app desenha uma mão só: a branca fosca da simulação. Não desenha a mão da pessoa, nem um par, nem um esqueleto por cima do passthrough. A mão de verdade aparece apenas porque a câmera a vê. A branca fica nas teclas da mão direita e mostra onde pôr a sua. Clicar em Ouvir faz essa mesma mão tocar o trecho.

Não existe coluna de notas descendo, grade rítmica, barra de tempo, combo, vida ou partitura.

## 2. Cores e tamanho

Pensadas para passthrough colorido, não para fundo preto.

| Papel | Aparência | Duração |
|---|---|---|
| Tecla alvo | Âmbar, alpha em torno de 0,55 | Enquanto for o alvo |
| Nota certa | Verde, alpha alto | Cerca de 150 ms |
| Nota errada, na tecla que foi tocada | Vermelho | Cerca de 200 ms |
| Nota esperada depois de um erro de memória | Âmbar | Cerca de 1 s, depois apaga e o pedaço recomeça |
| Tecla em repouso | Sem realce | — |
| Nome e dedo | Texto branco com contorno escuro, altura de uns 2 cm, logo acima da tecla alvo | Só no guiado |
| Velocity | Três marcas pequenas no canto do painel, uma acesa: leve, médio, forte | Atualiza no Note On, nunca fica vermelha |

Uma tecla acesa por vez no guiado. No ouvir, a acesa acompanha a nota que o piano está soando. Na memória, zero teclas acesas até o erro.

## 3. Menu

Estilo aprovado, referência visual do card em `docs/visao/quest-posicao.jpg`. O card fica acima do teclado, preso à calibração, sem grudar na cabeça.

O card é um retângulo escuro de cantos arredondados. Dentro, de cima para baixo:

1. Etiqueta pequena e clara, no canto: “Lição 1” / “Lesson 1”.
2. Uma frase grande, branca. No primeiro passo: “Ponha a mão assim.” / “Place your hand like this.”
3. Um botão branco, cantos arredondados, texto escuro, largura do card. No primeiro passo: “Ouvir” / “Hear”.

Uma frase por vez. Um botão principal por vez. Sem porcentagem, sem cronômetro, sem placar. Enquanto a pessoa está tocando (guiado e memória), o mesmo card fica só com a etiqueta e a frase, sem botão. A faixa leve / médio / forte é um ponto pequeno no card, nunca um segundo menu.

Botões altos o suficiente para pinça folgada. O botão em foco mostra, numa linha menor, a tecla que faz a mesma ação se a mão não rastrear. Exemplo: “ou Ré para ouvir de novo” / “or D to hear again”. Essa tecla de atalho não pode ser uma nota da frase que está sendo cobrada naquele momento; por isso os atalhos de menu ficam congelados durante guiado e memória. No ouvir, a pessoa ainda não está sendo pontuada, e os atalhos podem existir.

## 4. Mão-professora

Regra do sistema: na lição o app desenha uma mão só, a de simulação. Ela é branca fosca, mão direita, no estilo do tutorial de mãos do Quest. Sem pele, sem vidro, sem segunda mão.

A mão da pessoa não é desenhada. O passthrough pode mostrá-la porque a câmera a vê. O app não coloca outra mão ao lado, por cima ou espelhada. A branca fica nas teclas da mão direita e mostra onde pôr a sua. O MIDI não olha para a mão.

Pose, do primeiro dia: pulso do lado de quem toca, nós altos, dedos curvos e próximos, um dedo por tecla branca. Polegar de lado no dó central, 2 no ré, 3 no mi, 4 no fá, 5 no sol. Polegar à esquerda da mão, mínimo à direita.

- **Posição.** A mão branca fica parada nessas teclas. O card leva a etiqueta da lição, “Ponha a mão assim.” / “Place your hand like this.” e o botão branco “Ouvir” / “Hear”. A pose não é pontuada.
- **Ouvir.** O clique anima essa mesma mão no trecho: só o dedo da nota atual desce na tecla que está soando. Na lição 1 o polegar desce quatro vezes no dó. Na lição 2 o polegar desce no dó e o indicador desce no ré.
- **Guiado.** A mão branca fica no dedo da tecla acesa. Não toca o trecho sozinha.
- **Memória.** A mão branca some junto com as luzes. No erro, volta um segundo no dedo certo e some outra vez.

## 5. Fluxo espacial da calibração

Antes dos dois pontos, o modelo de 61 teclas não é desenhado em cima do piano, para não mentir a posição. O esquemático da prova de cabo fica dentro do painel.

Nos dois toques de calibração:

1. A pessoa estende o indicador (pose que o Quest rastreia melhor do que a mão em formato de tocar).
2. O app amostra a posição do indicador nos 200 ms anteriores ao Note On.
3. O Note On diz qual nota é aquela ponta de teclado.
4. Esquerda e direita definem origem, eixo e escala. Inclinação para frente e altura partem de um teclado plano; o ajuste fino corrige o resto.

Ajuste fino: quatro alças nos cantos do retângulo do teclado. Pinça arrasta. O modelo acompanha. Não há modo de rotação numérica.

Verificação do dó central: só o realce da nota 60 aceso. Se o Note On 60 cai nessa tecla do modelo, a âncora é aceita.

## 6. Fluxo da lição, no espaço

### Ouvir

Começa no clique de Ouvir. O app manda a nota ao CT-S300 e a mão branca simula o trecho no mesmo tempo: o dedo da nota desce, a tecla acende, o som sai do móvel. Intervalo folgado entre notas, perto de um segundo. Ouvir de novo repete do início.

O card: “Olhe e ouça.” / “Watch and listen.” Botões “Ouvir de novo” e “Tocar”.

Enquanto essa saída MIDI está ativa, Note On que voltar pelo cabo não conta como toque da pessoa.

### Guiado

“Toque a tecla acesa.” / “Play the lit key.”

Alvo âmbar, nome, número do dedo e a mão branca fosca. A pessoa olha a tecla e essa mão.

Acerto: verde breve, alvo seguinte. Se a próxima nota for a mesma tecla, o realce apaga no verde e volta ao âmbar, para ficar óbvio que é outra repetição.

Erro: vermelho na tecla cujo Note On chegou, alvo intacto.

### Memória

Painel: “Agora de memória.” / “Now from memory.” Pausa de um segundo. Realce, nome, dedo e mão branca somem juntos. Os pontos de pedaço continuam no painel, pequenos.

Erro: âmbar de um segundo na nota que deveria ter sido tocada, mais vermelho breve na nota que foi tocada, se foi uma nota diferente. Depois tudo apaga e a sequência do pedaço recomeça do zero. Sem texto “errado”.

Concluiu: verde breve na última tecla e o painel de estrelas.

### Estrelas

Três marcas no painel, não objetos espalhados pela sala. Acesas conforme RF-40. Uma linha de resultado:

- 3: “Você lembrou.” / “You remembered.”
- 2: “Lição seguinte aberta.” / “Next lesson unlocked.”
- 1: “Falta tocar sem luz.” / “Still needs a memory pass.” O botão “Próxima” não aparece.

## 7. Revisão do dia seguinte

Mesma cena da memória. Título: “Frase de ontem.” / “Yesterday’s phrase.” Sem ouvir automático, porque ouvir primeiro desfaz a recordação.

Se errar, aí sim os botões “Ouvir” e “Com guia” aparecem, cada um uma vez.

## 8. Progresso e configurações

Painel maior, à direita do teclado, ainda ancorado, para não cobrir as teclas. Quatro números, lista das 12 lições, idioma, calibrar, checklist. A lição fechada é texto opaco, sem botão de entrar.

## 9. O que o protótipo mínimo precisa ter para ser conclusivo

Recorte jogável, menor que as 12 lições, suficiente para decidir se a experiência ensina:

1. Idioma, checklist e prova de uma tecla.
2. Calibração de dois pontos, ajuste por pinça e verificação do dó central.
3. Lição 1 completa: ouvir, guiado, memória, estrelas, desbloqueio.
4. Um pedaço da lição 5, para ver a emenda sem construir as doze.
5. Cabo desconectado no meio do guiado.
6. Troca de idioma refletida no nome da nota que flutua sobre a tecla.

Fora desse recorte, as outras lições são dados do catálogo, não cena nova.

## 10. Critérios visuais de aceite do protótipo

- Olhando o dó central, a pessoa vê a tecla real através do realce e lê o nome sem aproximar o painel da cara.
- No guiado, um observador ao lado sabe qual tecla é a esperada só pela cor.
- Na memória, o teclado visual fica igual ao passthrough puro, fora o mesmo card escuro, só com a frase, sem botão.
- Nenhum quadro do protótipo mostra nota em queda, pentagrama ou cronômetro.
- Com as mãos em posição de tocar, escondendo umas às outras, a lição 1 ainda chega ao fim.
