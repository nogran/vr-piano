# Conceito do protótipo em AR

Descrição do que se vê e do que se faz. Sem cena, sem asset e sem código. Regras e tempos exatos: [06 · Modelo do domínio](06-modelo-de-dominio.md).

O protótipo serve para validar três coisas no Quest, com o CT-S300 de verdade: o realce cai na tecla física, o acerto vem do MIDI em tempo útil, e a pessoa percebe a diferença entre “tocar junto da mão branca” e “luzes apagadas de propósito”.

## 1. O que está no mundo

A sala é o passthrough do Quest 3 ou 3S. Não há cenário virtual, chão falso nem piano fantasma opaco.

Há três camadas, todas ancoradas no teclado depois da calibração. Nada da lição fica grudado na cabeça: um painel preso à cabeça cobriria as teclas quando a pessoa olha para baixo.

| Camada | O que é | Quando existe |
|---|---|---|
| Realce das teclas | Um plano translúcido por tecla, a uns 2 mm da superfície, menor que a tecla para a borda real continuar visível | Depois da calibração |
| Mão branca | Só a mão direita branca fosca. É a única mão que o app desenha | Pose, demonstração e loop. Some na memória. Volta por 1 s no erro de memória |
| Card | Retângulo escuro arredondado: etiqueta, uma frase e, quando há escolha, botões brancos | Sempre, acima do teclado. Sem botão enquanto a pessoa está tocando |

O app não desenha a mão da pessoa, nem um par, nem um esqueleto por cima do passthrough. A mão de verdade aparece apenas porque a câmera a vê.

Não existe coluna de notas descendo, grade rítmica, barra de tempo, combo, vida ou partitura.

## 2. Cores e tamanho

Pensadas para passthrough colorido, não para fundo preto.

| Papel | Aparência | Duração |
|---|---|---|
| Tecla da vez (loop) | Âmbar, alpha em torno de 0,55 | Enquanto for o alvo |
| Dedo da vez na mão branca | Âmbar, o mesmo tom da tecla | Enquanto for o alvo |
| Nota certa (loop) | Verde, alpha alto | `P-23` |
| Nota errada, na tecla tocada | Vermelho | `P-24` |
| Nota esperada depois de erro de memória | Âmbar | `P-25`, depois apaga e a tentativa recomeça |
| Tecla em repouso | Sem realce | — |
| Nome da nota | Texto branco com contorno escuro, altura de uns 2 cm, logo acima da tecla da vez | Só no loop |
| Velocity | Três marcas pequenas no canto do card, uma acesa: leve, médio, forte | Atualiza no Note On, nunca fica vermelha; some se a velocity estiver fixa |

Uma tecla acesa por vez no loop. Na demonstração, a acesa acompanha a nota que o piano está soando. Na memória, zero teclas acesas até o erro, e acerto não acende verde (só a última nota da tentativa).

## 3. Card

Estilo aprovado, referência visual em `docs/visao/capa-licao1.jpg`. Fica acima do teclado, preso à calibração.

De cima para baixo:

1. Etiqueta pequena e clara: “Lição 1” (na música, a lição do trecho atual).
2. Uma frase grande, branca. Na pose: “Ponha a mão assim.”
3. Botões brancos, cantos arredondados, texto escuro, largura do card. Na pose: “Ouvir” e o controle “Mão”.

Uma frase por vez. Sem porcentagem, sem cronômetro, sem placar. Durante loop e memória o card fica só com a etiqueta e a frase.

Botões altos o suficiente para pinça folgada. Cada botão mostra, numa linha menor, a tecla que faz a mesma ação se a mão não rastrear (“ou Dó2”). Essas teclas são as três brancas mais graves do teclado (`P-27`), longe da posição dó–sol, então nunca se confundem com uma nota cobrada.

## 4. Mão branca

Branca fosca, mão direita, asset original. Sem pele, sem vidro, sem segunda mão. Detalhes do asset: [05 §7](05-arquitetura-tecnica.md#7-mão-branca-asset).

- **Pose.** Parada na posição de cinco dedos. Card: “Ponha a mão assim.” Pose aceita (RF-31): começa o loop.
- **Demonstração.** Ao apertar “Ouvir”, a mão desce o dedo de cada nota junto com o som do piano.
- **Loop.** O dedo da vez fica âmbar e a tecla da vez acende. A mão espera a nota da pessoa. No fim da sequência, recomeça.
- **Memória.** A mão some. Num erro, reaparece por 1 s com o dedo certo.
- **Mão desligada.** Some a malha branca, a pose é pulada. Ficam a cor da tecla e o nome do dedo no card.

## 5. Fluxo espacial da calibração

Antes dos dois pontos, o modelo de 61 teclas não é desenhado em cima do piano, para não mentir a posição. O esquemático da prova de cabo fica dentro do painel.

Nos dois toques de calibração:

1. A pessoa estende o indicador (pose que o Quest rastreia melhor do que a mão em formato de tocar).
2. O app usa a posição do indicador na janela `P-18` antes do Note On.
3. O Note On diz qual nota é aquela ponta de teclado.
4. Esquerda e direita definem origem, eixo e escala. O teclado é tratado como plano e nivelado; o ajuste fino corrige o resto.

Ajuste fino: quatro alças nos cantos do retângulo do teclado. Pinça arrasta. O modelo acompanha. Não há rotação numérica.

Verificação do dó central: só o realce da nota 60 aceso. Se o Note On 60 cai nessa tecla do modelo, a âncora é aceita.

## 6. Fluxo da lição, no espaço

### Pose

Card “Ponha a mão assim.”, mão branca parada, nenhuma tecla acesa. A pessoa encaixa a mão sobre a branca.

### Demonstração (só com “Ouvir”)

Card “Olhe e ouça.” O app manda as notas ao CT-S300, uma por segundo. A mão branca desce o dedo, a tecla acende, o som sai do móvel. Notas que chegam pelo cabo nesse período não contam. No fim, começa o loop.

### Loop

Card com o nome do dedo. Tecla da vez em âmbar, nome da nota acima, dedo âmbar na mão branca.

Acerto: verde breve, passo seguinte. Se o próximo passo é a mesma tecla, o realce apaga no verde e volta ao âmbar, para ficar óbvio que é outra repetição.

Erro: vermelho na tecla cujo Note On chegou, alvo intacto.

Fim da sequência: recomeça. Na primeira volta sem erro, vai para a memória.

### Memória

Card “Agora de memória.” Pausa de `P-22`. Realce, nome e mão branca somem juntos.

Erro: âmbar na nota que deveria ter sido tocada, vermelho breve na tocada, mão branca por um instante com o dedo certo. Depois tudo apaga e a tentativa recomeça do zero. Sem texto “errado”. Três erros seguidos: “Mais uma vez com guia.” e volta ao loop.

Concluiu: verde breve na última tecla e o card de estrelas.

### Estrelas

Três marcas no card (`docs/visao/poc/10-estrelas.svg`), não objetos espalhados pela sala. Acesas conforme RF-40, com a frase do resultado.

## 7. Revisão do dia seguinte

Mesma cena da memória. Card “Frase da última vez.” Sem demonstração automática, porque ouvir primeiro desfaz a recordação.

Se errar, aí sim aparecem “Ouvir” e “Com guia”, cada um uma vez.

## 8. Progresso e configurações

Painel maior, à direita do teclado, ainda ancorado, para não cobrir as teclas. Os números do RF-45, a lista das 12 lições e da música, idioma, “Mão”, calibrar e checklist. Item fechado é texto opaco, sem botão de entrar.

## 9. O que o protótipo mínimo precisa ter para ser conclusivo

Recorte jogável, menor que as 12 lições, suficiente para decidir se a experiência ensina:

1. Idioma, checklist e prova de uma tecla.
2. Calibração de dois pontos, ajuste por pinça e verificação do dó central.
3. Lição 1 completa: pose, ouvir, loop, memória, estrelas, desbloqueio.
4. Lição 5 inteira, para ver uma sequência com ida e volta.
5. Cabo desconectado no meio do loop e no meio da memória.
6. Troca de idioma refletida no nome da nota e do dedo.
7. Revisão no dia seguinte (com relógio do aparelho adiantado, em build de desenvolvimento).

Fora desse recorte, as outras lições são dados do catálogo, não cena nova.

## 10. Critérios visuais de aceite do protótipo

- Olhando o dó central, a pessoa vê a tecla real através do realce e lê o nome sem aproximar o card da cara.
- No loop, um observador ao lado sabe qual tecla é a esperada só pela cor.
- Na memória, o teclado fica igual ao passthrough puro, fora o card escuro, só com a frase, sem botão.
- Nenhum quadro do protótipo mostra nota em queda, pentagrama ou cronômetro.
- Com as mãos em posição de tocar, escondendo umas às outras, a lição 1 ainda chega ao fim.

## 11. Imagens de referência

A referência visual vigente é a prova de conceito em `docs/visao/poc/` (um quadro por passo, mostrado no README) e a capa `docs/visao/capa-licao1.jpg`. Nos quadros, os balões roxos são explicação e não fazem parte do app.

A capa é a foto de passthrough com a camada do app desenhada por cima, como num print do headset: card da pose com “Ouvir”, “Mão” e as teclas de atalho, e a mão branca em escala real com o polegar no dó central.

As fotos antigas (`quest-*.jpg`) ficam só como referência de clima e estão substituídas:

| Arquivo | Problema |
|---|---|
| `quest-posicao.jpg` | Mão branca grande demais, com o polegar perto do lá/si, não no dó central; falta o controle “Mão”. Corrigida na capa. |
| `quest-guiado.jpg` | Realce numa tecla preta rotulada “Dó 1”, frase antiga, sem mão branca. |
| `quest-erro.jpg` | Frase antiga (“Toque a tecla acesa.”), sem mão branca. |
| `quest-memoria.jpg` | Falta a etiqueta “Lição N”. |
| `quest-estrelas.jpg` | Sem os botões do resultado. |
