# Conceito do protótipo em AR

Descrição do que se vê e do que se faz. Sem cena, sem asset e sem código. Regras e tempos exatos: [06 · Modelo do domínio](06-modelo-de-dominio.md).

O protótipo serve para validar quatro coisas no Quest, com o CT-S300 de verdade:
- o realce cai na tecla física;
- o acerto vem do MIDI em tempo útil;
- o cinza dinâmico é lido como “prepare o dedo aqui, toque quando escurecer”;
- a pessoa percebe a diferença entre as três fases: assistir a mão, tocar com as teclas guiando e tocar com as luzes apagadas de propósito.

## 1. O que está no mundo

A sala é o passthrough do Quest 3 ou 3S. Não há cenário virtual, chão falso nem piano fantasma opaco.

Há três camadas, todas ancoradas no teclado depois da calibração. Nada da lição fica grudado na cabeça: um painel preso à cabeça cobriria as teclas quando a pessoa olha para baixo.

| Camada | O que é | Quando existe |
|---|---|---|
| Realce das teclas | Um plano translúcido por tecla, a uns 2 mm da superfície, menor que a tecla para a borda real continuar visível | Depois da calibração |
| Mão branca | Só a mão direita 3D branca fosca. É a única mão que o app desenha | Pose e demonstração. Some em fade no fim da demonstração e não aparece na vez guiada nem na memória |
| Card | Retângulo escuro arredondado: etiqueta, uma frase e, quando há escolha, botões brancos | Sempre, acima do teclado. Na vez guiada, só “Ouvir”. Na memória, sem botão |

O app não desenha a mão da pessoa, nem um par, nem um esqueleto por cima do passthrough. A mão de verdade aparece apenas porque a câmera a vê.

Não existe coluna de notas descendo, grade rítmica, barra de tempo, combo, vida ou partitura.

## 2. Cores e tamanho

Pensadas para passthrough colorido, não para fundo preto.

A **cor ativa** é o âmbar: vibrante sobre o branco e o preto das teclas, e diferente do verde de acerto e do vermelho de erro.

| Papel | Aparência | Duração |
|---|---|---|
| Tecla da vez (ataque) | Cor ativa, alpha em torno de 0,55 | Do tempo de ataque até o Note On certo |
| Tecla atual (acabou de ser tocada) | Cor ativa, depois do verde | Enquanto a pessoa segura a tecla, até a próxima virar ataque |
| Próxima tecla branca (antecipação) | Cinza de `#E5E7EB` até `#374151`, intensidade de `P-29` a 100 % | Do acerto anterior até o tempo de ataque |
| Próxima tecla preta (antecipação) | Brilho de grafite discreto `#4B5563` até prata nítido `#D1D5DB` | Idem |
| Mesma tecla em seguida | Contorno na cor ativa, centro com o ciclo de cinza | Idem |
| Tecla na demonstração | Cor ativa | Enquanto o piano soa a nota |
| Dedo na demonstração | Cor ativa, o mesmo tom da tecla | Enquanto o dedo está descido |
| Nota certa | Verde, alpha alto | `P-23` |
| Nota errada, na tecla tocada | Vermelho | `P-24` |
| Nota esperada depois de erro de memória | Âmbar | `P-25`, depois apaga e a tentativa recomeça |
| Dica de memória | Cinza claro, uma piscada | `P-34`, a cada `P-33` sem nota |
| “Adiantado” / “Atrasado” | Texto pequeno branco com contorno escuro, acima da tecla tocada | `P-32` |
| Tecla em repouso | Sem realce | — |
| Nome da nota | Texto branco com contorno escuro, altura de uns 2 cm, logo acima da tecla da vez | Só na vez guiada |
| Velocity | Três marcas pequenas no canto do card, uma acesa: leve, médio, forte | Atualiza no Note On, nunca fica vermelha; some se a velocity estiver fixa |

Na vez guiada há no máximo duas teclas com realce: a atual em cor ativa e a próxima em cinza. Na demonstração, a tecla acesa acompanha a nota que o piano está soando. Na memória, zero teclas acesas até o erro ou a dica, e acerto não acende verde (só a última nota da tentativa).

O cinza substitui a distância das barras que caem: quanto mais escuro, mais perto da hora de tocar. O adapter interpola a cada quadro (06 §3.1), sem degraus.

## 3. Card

Estilo aprovado, referência visual em `docs/visao/capa-licao1.jpg`. Fica acima do teclado, preso à calibração.

De cima para baixo:

1. Etiqueta pequena e clara: “Lição 1” (na música, a lição do trecho atual).
2. Uma frase grande, branca. Na pose: “Ponha a mão assim.”
3. Botões brancos, cantos arredondados, texto escuro, largura do card. Na pose: “Ouvir” e o controle “Mão”.

Uma frase por vez. Sem porcentagem, sem cronômetro, sem placar durante a lição. Na vez guiada o card tem a etiqueta, o nome do dedo e o botão “Ouvir”. Na memória, só a etiqueta e a frase. A pontuação de ritmo só aparece no resultado, numa linha (“No tempo: 3 de 4”).

Botões altos o suficiente para pinça folgada. Cada botão mostra, numa linha menor, a tecla que faz a mesma ação se a mão não rastrear (“ou Dó2”). Essas teclas são as três brancas mais graves do teclado (`P-27`), longe da posição dó–sol, então nunca se confundem com uma nota cobrada.

## 4. Mão branca

Branca fosca, mão direita, asset original, com anatomia de mão real e o visual das mãos do sistema do Quest (some em degradê no pulso). Sem pele, sem vidro, sem segunda mão. Detalhes do asset: [05 §7](05-arquitetura-tecnica.md#7-mão-branca-asset).

- **Pose.** Parada na posição de cinco dedos. Card: “Ponha a mão assim.” Com a pose aceita (RF-31), começa a demonstração (item inédito) ou a vez guiada.
- **Demonstração.** A mão toca a sequência no andamento: o dedo desce na cor ativa junto com o som do piano e com a tecla acesa. É automática na primeira vez no item; depois, só com “Ouvir”.
- **Fim da demonstração.** A mão some em fade (`P-35`) e deixa o teclado limpo.
- **Vez guiada e memória.** Sem mão. Quem guia são as teclas.
- **Mão desligada.** A malha branca some e a pose é pulada. A demonstração fica só com o som e a tecla acesa.

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

### Fase 1: demonstração

Card “Olhe e ouça.” Automática na primeira vez no item; depois, só com “Ouvir”. O app manda as notas ao CT-S300 no andamento do item. A mão branca desce o dedo, a tecla acende na cor ativa e o som sai do móvel. Notas que chegam pelo cabo nesse período não contam. No fim, a mão some em fade e começa a vez guiada.

### Fase 2: vez guiada

Card com o nome do dedo e “Ouvir”. A primeira tecla já começa na cor ativa, com o nome da nota acima.

Acerto: verde breve. No mesmo instante, a próxima tecla aparece em cinza bem claro e escurece até o tempo de ataque, quando vira cor ativa. Se a próxima é a mesma tecla, fica o contorno na cor ativa e o centro faz o cinza.

Atraso: a tecla fica na cor ativa e espera. Acerto adiantado ou atrasado mostra a palavra acima da tecla por `P-32` e segue.

Erro: vermelho na tecla cujo Note On chegou. O alvo e o cinza ficam como estavam.

Fim da sequência: recomeça. Na primeira volta sem nota errada, vai para a memória.

### Fase 3: memória

Card “Agora de memória.” Pausa de `P-22`. Cor, cinza e nome somem juntos.

Travou por `P-33`: a tecla certa pisca em cinza claro uma vez.

Erro: âmbar na nota que deveria ter sido tocada e vermelho breve na tocada. Depois tudo apaga e a tentativa recomeça do zero. Sem texto “errado”. Três erros seguidos: “Mais uma vez com guia.”, com “Ouvir”, e volta à vez guiada.

Concluiu: verde breve na última tecla e o card de estrelas.

### Estrelas

Três marcas no card (`docs/visao/poc/10-estrelas.svg`), não objetos espalhados pela sala. Elas acendem conforme o RF-40, com a frase do resultado e a linha de ritmo.

## 7. Revisão do dia seguinte

Mesma cena da memória, com a mesma dica de `P-33`. Card “Frase da última vez.” Não há demonstração automática, porque ouvir primeiro desfaz a recordação.

Se errar, aí sim aparecem “Ouvir” e “Com guia”, cada um uma vez.

## 8. Progresso e configurações

Painel maior, à direita do teclado, ainda ancorado, para não cobrir as teclas. Os números do RF-45, a lista das 12 lições e da música, idioma, “Mão”, calibrar e checklist. Item fechado é texto opaco, sem botão de entrar.

## 9. O que o protótipo mínimo precisa ter para ser conclusivo

Recorte jogável, menor que as 12 lições, suficiente para decidir se a experiência ensina:

1. Idioma, checklist e prova de uma tecla.
2. Calibração de dois pontos, ajuste por pinça e verificação do dó central.
3. Lição 1 completa: pose, demonstração automática, vez guiada com cinza dinâmico e ritmo, memória com dica, estrelas, desbloqueio. Uma segunda execução, para ver que a demonstração não toca sozinha.
4. Lição 5 inteira, para ver uma sequência com ida e volta.
5. Cabo desconectado no meio da vez guiada e no meio da memória.
6. Troca de idioma refletida no nome da nota e do dedo.
7. Revisão no dia seguinte (com relógio do aparelho adiantado, em build de desenvolvimento).

Fora desse recorte, as outras lições são dados do catálogo, não cena nova.

## 10. Critérios visuais de aceite do protótipo

- Olhando o dó central, a pessoa vê a tecla real através do realce e lê o nome sem aproximar o card da cara.
- Na vez guiada, um observador ao lado sabe qual tecla é a esperada só pela cor, e qual vem depois pelo cinza.
- O escurecimento do cinza é contínuo: não se vê degrau a 72 fps.
- Numa tecla preta, o cinza de antecipação aparece tão bem quanto numa branca.
- Na memória, o teclado fica igual ao passthrough puro, fora o card escuro, só com a frase, sem botão.
- Nenhum quadro do protótipo mostra nota em queda, pentagrama, barra de tempo ou cronômetro.
- Com as mãos em posição de tocar, escondendo umas às outras, a lição 1 ainda chega ao fim.

## 11. Imagens de referência

A referência visual vigente é a prova de conceito em `docs/visao/poc/` (um quadro por passo, mostrado no README) e a capa `docs/visao/capa-licao1.jpg`. Nos quadros, os balões roxos são explicação e não fazem parte do app.

A mão branca é sempre o mesmo modelo 3D, na capa e em todos os quadros: o mesmo esqueleto, as mesmas proporções e o mesmo material, vistos do mesmo ângulo que a pessoa vê o teclado. Quadro novo com mão usa esse modelo, nunca um desenho 2D. Muda só a pose (dedo pressionado) e a cor do dedo da vez na demonstração.

A capa é a foto de passthrough com a camada do app desenhada por cima, como num print do headset: card da pose com “Ouvir”, “Mão” e as teclas de atalho, e a mão branca em 3D, em escala real, vista de trás como a pessoa a vê, com o polegar no dó central e um dedo por tecla até o sol.

As fotos antigas (`quest-*.jpg`) ficam só como referência de clima e estão substituídas:

| Arquivo | Problema |
|---|---|
| `quest-posicao.jpg` | Mão branca grande demais, com o polegar perto do lá/si, não no dó central; falta o controle “Mão”. Corrigida na capa. |
| `quest-guiado.jpg` | Realce numa tecla preta rotulada “Dó 1”, frase antiga, sem mão branca. |
| `quest-erro.jpg` | Frase antiga (“Toque a tecla acesa.”), sem mão branca. |
| `quest-memoria.jpg` | Falta a etiqueta “Lição N”. |
| `quest-estrelas.jpg` | Sem os botões do resultado. |
