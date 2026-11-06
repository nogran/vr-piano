# Jornada do usuário

Adulto iniciante, sozinho, Quest 3 ou 3S, CT-S300 na mesa, cabo Micro-B para USB-C já ao alcance. Sem conta. Os textos exatos, nos dois idiomas, estão em [07 · Textos da interface](07-textos-da-interface.md); aqui aparecem em pt-BR.

## Visão única

```text
Idioma → cabo e checklist → qualquer tecla → calibrar → ouvir a música → frase 1
        → pose → assistir a mão 3D → vez guiada até sair limpa → memória → estrelas
        → juntar as frases → música inteira de cor
        → (outro dia) música da última vez → próxima frase ou “Decorada”
        → quadro de progresso, quando quiser
```

A pessoa olha para o piano o tempo todo. O menu é um card escuro acima do teclado: etiqueta da música e da frase, uma frase e, quando ela precisa escolher, botões brancos.

## 1. Primeira abertura

O passthrough mostra a sala. Um painel pequeno, central, dois botões grandes: “Português” e “English”.

Não há terceiro botão e não há idioma pré-marcado. A escolha grava na hora (RF-01, RF-02).

Em seguida, uma linha: “Sente-se de frente para o teclado.” Sem tutorial de gesto. A próxima tela já é o cabo.

## 2. Conectar o CT-S300

Painel com o estado em uma frase: “Procurando teclado…”. Se o Quest pedir permissão de USB, uma linha explica e o pedido do sistema aparece. Negado: “Sem permissão, o app não ouve o teclado.” e o botão “Pedir de novo” (RF-10).

Quando o USB-MIDI enumera: “Teclado conectado.”

Checklist, cada item é uma linha que a pessoa marca com pinça. Se a mão não estiver rastreada, a tecla branca mais grave do teclado marca o item em foco e a seguinte passa ao próximo item (RF-39):

1. Ritmo desligado. Song parado.
2. MIDI Out Octave Shift em 0.
3. Touch Response em Normal.
4. MIDI Out Velocity em Off.
5. Local Control ligado.

O botão “Pronto” só acende com os cinco marcados e o cabo conectado (RF-12).

Se o cabo não aparece em `P-26`, uma linha extra: “Use um cabo de dados, Micro-B no teclado e USB-C no headset.” Sem QR code, sem site.

## 3. Prova de que a nota chega

“Toque qualquer tecla.”

A tecla que chegar no MIDI acende num teclado esquemático dentro do painel, com o nome da nota. Isso prova o cabo antes de investir na calibração. A velocity aparece discreta: “leve”, “médio” ou “forte”.

## 4. Calibrar

Duas ordens, uma por vez.

1. “Aponte o indicador para a tecla branca da esquerda e toque.”
2. “Aponte o indicador para a tecla branca da direita e toque.”

Se os dois toques não baterem com um teclado de 61 teclas: “Não bateu. Vamos de novo.” e volta ao primeiro ponto (RF-22).

O realce passa a cobrir o piano real. “Ajuste se precisar.” Pinça nas quatro alças dos cantos.

Verificação: “Toque o dó central.” O realce do dó central tem que estar na tecla que enviou a nota 60. Acertou: entra na Brilha, Brilha. Errou: volta ao ponto 1 da calibração, sem texto de culpa.

Nas aberturas seguintes, só a verificação do dó central. Se coincidir, a calibração longa não aparece (RF-23).

## 5. Brilha, Brilha, frase 1 — primeira vez

Não há tela inicial antes desta frase.

### Ouvir a música

É a primeira vez dela na música, então ela ouve a música inteira antes de tudo (RF-54). A etiqueta diz “Brilha, Brilha · Música inteira” e o card diz “Olhe e ouça.”. A mão branca toca as 42 notas, e o CT-S300 soa cada uma. Nas próximas vezes isso não se repete: o botão “Ouvir a música” fica no painel.

### Pose

A etiqueta passa a “Brilha, Brilha · Frase 1”. A mão branca fica parada: polegar no dó, os outros quatro sobre ré, mi, fá e sol. O card diz “Ponha a mão assim.” e tem o botão “Ouvir” e o controle “Mão”.

Ela põe a mão direita em cima da mão branca. Quando o Quest vê a mão estável nessa posição, a lição segue (RF-31).

### Fase 1: assistir

É a primeira vez dela na frase 1, então a demonstração começa sozinha (RF-31a). O card diz “Olhe e ouça.”. A mão branca toca dó, dó, sol, sol, lá, lá, sol no andamento, o CT-S300 soa cada nota e a tecla acende na cor ativa no instante da nota. Antes do primeiro sol, a mão desliza uma tecla para a direita: o anelar fica no sol e o mínimo no lá (RF-64). No fim, a mão some num fade suave e o teclado fica limpo. Fora da demonstração, o app nunca toca o piano.

Nas próximas vezes nesta frase, a demonstração não toca sozinha. O botão “Ouvir” fica no card para quando ela quiser rever.

### Fase 2: vez guiada

O dó fica na cor ativa e o card diz “Polegar”. Ela toca: verde breve. Como o próximo é o mesmo dó, a tecla fica com o contorno colorido e o centro começa em cinza bem claro, que escurece no ritmo da música. Quando o cinza fica cheio, a tecla vira cor ativa: é a hora de tocar (RF-32, RF-32e).

Se ela toca com o cinza ainda claro, aparece “Adiantado” acima da tecla. Se demora com a tecla já colorida, a tecla espera por ela e, quando ela toca, aparece “Atrasado”. Nos dois casos ela avança (RF-32a). Depois da última nota, a volta recomeça do primeiro.

Se ela toca outra tecla: vermelho breve nessa tecla, o dó continua como estava e o card continua “Polegar”. A volta deixa de ser limpa, mas ela segue tocando.

Se ela tira a mão ou o Quest perde a mão, nada muda: a lição segue pelo MIDI (RF-38).

### Fase 3: memória

Na primeira volta sem nota errada, o card diz “Agora de memória.”. Um segundo depois, cor, cinza e nome somem juntos. Ela toca a frase sem ajuda.

Se trava por três segundos, o dó pisca em cinza claro uma vez, como dica (RF-36a).

Se erra: o dó acende em âmbar por um segundo, a tecla tocada fica vermelha, e tudo apaga. Ela recomeça da primeira nota. Depois de três erros seguidos, o card diz “Mais uma vez com guia.”, com o botão “Ouvir”, e a vez guiada volta (RF-36).

### Estrelas

Três marcas no card, com a pontuação de ritmo embaixo (“No tempo: 3 de 3”). “Você lembrou.” se saiu na primeira tentativa e sem dica (3); “Próxima etapa aberta.” se precisou de mais de uma tentativa ou da dica (2).

“Mão” desligada esconde a mão branca e pula a pose. A demonstração continua, só com o som e a tecla acesa.

## 6. Depois de cada item

Card curto, ainda acima do piano:

- Estrelas do item que acabou.
- “Próxima”, se há um item novo aberto.
- “De novo”.
- “Progresso”.

Cada botão mostra, numa linha menor, a tecla de atalho (“ou Dó2”).

Não há mapa de fases espalhado no espaço. O próximo item é uma ação. A lista completa mora no progresso, em texto, para não virar um tabuleiro.

## 7. Frases seguintes e Juntar

A partir da frase 2, o ciclo é o mesmo, e depois da memória vem o Juntar (RF-56). O card diz “Agora junte tudo.” e “As 2 partes, de memória.”. Ela toca os trechos desde o começo, sem luz, com “Parte 1 de 2” no card. Errou: volta ao começo daquela parte, não ao começo de tudo.

Trecho repetido não é frase nova. Na Ode, o trecho 3 repete o 1, então ODE#4 é a Frase 3, e o Juntar dela passa pelo trecho repetido: são quatro partes.

## 8. Música inteira

Com todas as frases em 2 ou mais estrelas, abre o item “Música inteira”. A pessoa faz a pose e toca a música de memória, sem vez guiada, com as mesmas regras do Juntar. Sem erro e sem dica vale 3 estrelas; nos outros casos, 2. Com 2 ou mais, a próxima música abre, e o card avisa “Ode à Alegria abriu.”.

## 9. Outro dia — lembrar

Depois da verificação do dó central, antes de qualquer item, o card diz “Música da última vez.” e “Toque de memória.” Sem luz e sem mão branca. A revisão vai até onde ela já toca de cor (`chain`), ou a música inteira se ela já concluiu a música (RF-62).

- Acertou: “Lembrou.” O dia seguido soma. Vai para o painel.
- Saiu a música inteira, já concluída noutro dia: o card também diz “Decorada”, e a marca fica no painel (RF-61).
- Travou por três segundos: a tecla certa pisca em cinza, como na memória da frase.
- Errou: o retorno de erro da memória (âmbar e vermelho por um segundo). Depois aparecem “Ouvir” e “Com guia”, para aquele trecho, cada um uma vez. Tocar a primeira nota recomeça o trecho. Não há botão de pular, e as estrelas não mudam.

Se ela abrir de novo no mesmo dia, a revisão não se repete.

## 10. Quadro de progresso

Aberto pelo botão “Progresso”. Painel ao lado do teclado:

- Estrelas: 14
- Dias seguidos: 3
- Maior sequência: 3
- De memória, sem erro: 4 frases
- Decoradas: 1

Abaixo, cada música com o título, “Decorada” se for o caso, “Ouvir a música” depois da primeira audição e a lista de frases e da música inteira, com 0–3 estrelas, o melhor ritmo (“3 de 4 no tempo”) e a próxima aberta. Item fechado é texto opaco, sem botão. Dá para repetir qualquer item aberto.

Sem lista de jogadores, sem nome, sem compartilhar.

## 11. Configurações

No mesmo painel de progresso, embaixo:

- Idioma: Português / English. Trocar aqui reconstrói os textos e não mexe nas estrelas (RF-03).
- “Mão”: ligada mostra a mão branca na pose, na demonstração e na vez guiada. Desligada deixa só o som e a cor na tecla (RF-32c).
- “Calibrar de novo”.
- “Checklist do teclado”.

## 12. Interrupções

| O que acontece | O que ela vê | O que é gravado |
|---|---|---|
| Cabo sai | “Teclado desconectado. A lição pausou.” | Estrelas anteriores intactas. Nada vira erro. |
| Cabo volta | Na vez guiada, o mesmo passo, já colorido. Na memória, a tentativa recomeça sem contar falha. Na demonstração, volta à pose. | — |
| Oito notas com a mesma velocity | “Velocity fixa. No teclado, MIDI Out Velocity em Off.” Dá para fechar o aviso. | Nada |
| Ritmo ou song soltam várias notas | “Pare o ritmo ou a música do teclado.” Lição pausada; retoma após 3 s de silêncio. | Nada vira erro |
| Botão Meta ou headset tirado | Ao voltar, “Lição pausada.” com “Continuar” e “Sair da aula”. | Estrelas já concedidas permanecem |
| Mão some no meio da vez guiada ou da memória | Nada. A lição segue pelo MIDI. | — |
| Mão some num menu | A linha do botão em foco diz qual tecla faz a mesma ação. | — |

## 13. O que ela é capaz de fazer ao fim da v1

Tocar, de memória, a Brilha, Brilha e a Ode à Alegria inteiras, com a mão direita, sem luz e sem esteira, num andamento lento e constante. Não lê pauta. Não usa a mão esquerda. O ritmo é medido na vez guiada, mas não reprova.

## 14. Critério de jornada aceita

Um adulto que nunca usou o app:

1. Escolhe o idioma, liga o cabo, marca o checklist e calibra sem outra pessoa lendo um manual externo.
2. Entende, sem explicação externa, que primeiro assiste a mão, depois toca a tecla colorida e prepara o dedo na cinza, e por fim toca sem luz.
3. Sai da frase 1 sabendo repetir dó, dó, sol, sol, lá, lá, sol com o headset no colo.
4. No dia seguinte, reconhece a tela “Música da última vez.” como revisão, não como frase nova.
