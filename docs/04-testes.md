# Testes

Casos da v1. Os de domínio (lição, estrela, desbloqueio, dia seguido, filtro de eco) rodam sem headset. Os de MIDI, passthrough e jornada rodam com Quest 3 ou 3S e CT-S300, cabo de dados Micro-B para USB-C, teclado na alimentação dele.

Setup comum do teclado nos testes de lição, salvo quando o caso muda de propósito: ritmo off, song parado, octave shift 0, Touch Response Normal, MIDI Out Velocity Off, Local Control on.

## 1. Aceite

| ID | Dado | Ação | Aceite | Requisito |
|---|---|---|---|---|
| T-ACC-01 | App recém-instalado, sem arquivo de progresso | Abrir | Aparecem só “Português” e “English”, nenhum dos dois marcado | RF-01 |
| T-ACC-02 | Idioma ainda não escolhido | Matar o app na primeira tela e abrir de novo | A escolha é pedida outra vez | RF-02 |
| T-ACC-03 | Idioma pt-BR gravado, cabo conectado, checklist feito, calibração válida | Abrir no mesmo dia, depois de já ter jogado | Não pede idioma. Pede só a verificação do dó central se a âncora existir | RF-02, RF-23 |
| T-ACC-04 | Checklist incompleto | Tentar “Pronto” | A lição não começa | RF-12 |
| T-ACC-05 | Sem calibração válida | Disparar o fluxo da lição 1 por qualquer atalho | A lição não começa; a calibração é o próximo passo | RF-24 |
| T-ACC-06 | Lição 1, modo guiado, alvo dó | Note On 60, velocity 20; repetir até as quatro | Avança as quatro. Faixa mostra leve. Termina o guiado com direito a 1 estrela se parar aqui | RF-32, RF-33, RF-40 |
| T-ACC-07 | Lição 1, guiado, alvo dó | Note On 62 | Realce vermelho em mi, dó continua alvo, passo não avança | RF-32 |
| T-ACC-08 | Lição 1, memória | Quatro Note On 60, sem outra nota, na primeira tentativa | 3 estrelas. Lição 2 abre | RF-35, RF-40, RF-41 |
| T-ACC-09 | Lição 1, memória | Uma nota errada, depois quatro dó corretos | 2 estrelas. Lição 2 abre. A estrela não fica em 1 | RF-35, RF-40, RF-41 |
| T-ACC-10 | Lição 1 com 1 estrela apenas | Abrir a lição 2 | Não abre | RF-41 |
| T-ACC-11 | Lição 1 com 3 estrelas | Repetir a memória e errar | Continua 3 estrelas | RF-42 |
| T-ACC-12 | Lição 5 | Concluir os dois pedaços e errar a frase acumulada | Só a tentativa acumulada reinicia. Os pedaços anteriores seguem concluídos nesta sessão | RF-36 |
| T-ACC-13 | Última memória concluída no dia local anterior | Abrir o app, calibração ok | A primeira tarefa é a frase anterior, sem ouvir automático | RF-44 |
| T-ACC-14 | Revisão do dia falha e depois é concluída | Observar o contador | O dia conta. A estrela da lição antiga não desce | RF-43, RF-44 |
| T-ACC-15 | Memória concluída hoje | Abrir de novo hoje | A revisão não reaparece | RF-44 |
| T-ACC-16 | Um dia local inteiro sem memória concluída | Abrir no dia seguinte ao vazio | Dias seguidos voltam a zero. Estrelas permanecem | RF-43 |
| T-ACC-17 | Qualquer fase pontuada | Inspecionar a cena | Não há nota caindo, cronômetro, combo, pauta nem avaliação de timing | seção 8 dos requisitos |
| T-ACC-18 | Progresso com várias lições | Abrir o quadro | Mostra n/36, dias atual e maior, contagem de lições em 3 estrelas. Sem outro jogador | RF-45 |

## 2. Domínio, sem headset

Estes casos travam a regra antes de existir cena.

| ID | Caso | Aceite |
|---|---|---|
| T-DOM-01 | Sequência esperada 60, 62, 64. Chega 60, 62, 64 | Pedaço completo, zero reinício |
| T-DOM-02 | Esperado 60, 60. Chega um único 60 | Não completa |
| T-DOM-03 | Esperado 60. Chega 60 com velocity 1 e outro ensaio com 127 | Os dois contam como acerto. Faixas diferentes |
| T-DOM-04 | Note On certo com a nota anterior ainda sem Note Off | Conta. Sobreposição não é erro |
| T-DOM-05 | Durante uma janela de saída marcada pelo `MidiOutPort`, chega Note On igual ao que foi enviado | Não entra na sequência do usuário |
| T-DOM-06 | Fora de janela de saída, Note On 60 | Entra na sequência |
| T-DOM-07 | Oito Note On seguidos com velocity idêntica | Sinal de velocity fixa ligado. A lição não é reprovada por isso |
| T-DOM-08 | Três ou mais Note On com espaçamento curto numa lição de uma nota só | Sinal de rajada. Pedaço não incrementa erro |
| T-DOM-09 | Memória final: errou e depois acertou na mesma execução. Noutra execução, a primeira passagem sai limpa | A execução com erro fica em 2. A execução seguinte, limpa na primeira entrada, sobe para 3. Quem já tem 3 não desce | RF-40, RF-42 |
| T-DOM-10 | Troca de idioma | Notas MIDI do catálogo iguais. Só o rótulo muda | RF-03 |
| T-DOM-11 | Nota 60 ausente do alcance informado pela calibração | A lição não inicia | mapa do teclado |
| T-DOM-12 | Cabo cai com 2 estrelas já gravadas e pedaço corrente pela metade | Ao reconectar, estrelas seguem 2. O pedaço corrente não ganhou um erro extra | RF-15, RNF-08 |

T-DOM-09 em detalhe, para não ambiguidade: 3 estrelas significam que, na execução em que a memória final foi vencida, a primeira entrada nesse modo já saiu limpa. Se a pessoa errou e depois acertou na mesma execução, o máximo dessa execução é 2. Noutra execução, entrada limpa sobe 2 para 3.

## 3. Integração MIDI no Quest

Fazer T-MIDI-01 antes de qualquer trabalho visual fino.

| ID | Caso | Procedimento | Aceite |
|---|---|---|---|
| T-MIDI-01 | Enumeração | CT-S300 ligado na alimentação dele, cabo de dados no USB-C do Quest, app em “procurando” | O estado passa a conectado sem PC no meio. Nome do dispositivo visível no log de desenvolvimento |
| T-MIDI-02 | As 61 teclas | Tocar cada tecla uma vez, piano e sustenido | 61 Note On distintos, Note Off para cada um, sem nota fantasma |
| T-MIDI-03 | Velocity viva | `MIDI Out Velocity = Off`, Touch Response Normal. A mesma tecla, toque fraco e toque forte, 8 pares | Os valores mudam entre os pares. Se não mudarem, o risco da seção 9 dos requisitos se confirma e a faixa de velocity sai do protótipo |
| T-MIDI-04 | Velocity travada | `MIDI Out Velocity = 100`, tocar 8 vezes com forças diferentes | O app mostra o aviso de velocity fixa. As notas continuam contando para a lição |
| T-MIDI-05 | Note Off | Soltar devagar e rápido | O domínio não lê a velocity 64 do Note Off como dinâmica |
| T-MIDI-06 | Octave shift | Shift +1, calibrar de novo, tocar a tecla física que agora envia 60 | O realce do dó central fica nessa tecla. A lição 1 segue tocável |
| T-MIDI-07 | Eco | Modo ouvir, ninguém encosta no teclado, gravar o MIDI de entrada | Ou não volta Note On, ou o que volta cai na janela filtrada e a pontuação permanece zerada. Se voltar e pontuar, o teste falha |
| T-MIDI-08 | Demonstração audível | Clicar em Ouvir na lição 1 | O CT-S300 soa quatro dó, espaçados. A mão branca desce o polegar junto de cada som, nas teclas da mão direita |
| T-MIDI-09 | Latência | Note On de uma tecla fora da lição, com log de tempo na recepção e no quadro em que o realce muda | Diferença abaixo de 80 ms no Quest 3, em pelo menos 20 amostras medianas | RNF-01 |
| T-MIDI-10 | Queda de cabo | Tirar o Micro-B no guiado | Pausa visível. Religar retoma o mesmo passo. Estrelas anteriores iguais |
| T-MIDI-11 | Song ou ritmo | Ligar um ritmo no CT-S300 durante a lição 1 | Pausa com o aviso de ritmo ou song, sem somar erro |
| T-MIDI-12 | Trilo | Alternar duas teclas o mais rápido possível por 5 segundos no modo guiado | Nenhuma nota da alternância se perde no log. A regra de acerto continua valendo para a nota alvo |

T-MIDI-06 existe porque alguém vai deixar o shift fora de zero. A calibração nova é o conserto; o app não precisa adivinhar o shift antigo.

## 4. Experiência no headset

Participante: adulto que não viu o desenho das telas. Tarefa lida em voz alta uma vez, sem apontar tecla.

| ID | Tarefa | Aceite | Requisito |
|---|---|---|---|
| T-UX-01 | “Escolha a língua, ligue o teclado e deixe o app pronto para a primeira lição.” Checklist impresso só com os nomes das funções Casio, igual ao painel | Conclui sem o condutor mostrar onde tocar | jornada §13 |
| T-UX-02 | “Faça a lição 1 até o fim.” | Descreve que primeiro copiou a mão, depois as luzes e a mão sumiram e ela teve que lembrar. Termina com 2 ou 3 estrelas | jornada §13, RF-37, RF-32a |
| T-UX-02b | Na pose da lição 1, olhar a mão e o card | A única mão desenhada é a branca, direita, nas teclas da mão direita. O card tem a etiqueta da lição, a frase e o botão branco Ouvir. Na memória a mão branca sai e o card fica sem botão | RF-31, RF-32a, RF-32b, RF-35, RF-37a |
| T-UX-03 | Headset no colo, logo após T-UX-02: “Toca o que a lição pediu.” | Quatro dó na posição, sem o app | sucesso da seção 2 dos requisitos |
| T-UX-04 | No dia seguinte: “Abre e faz o que o app pedir primeiro.” | Entra na revisão, não pede para pular para a lição 2 | RF-44, jornada §8 |
| T-UX-05 | Lição 1 com uma luva fina ou mão parcialmente fechada, tracking instável, depois de calibrado | A lição termina. Nenhum erro é atribuído à mão | RF-38, RNF-07 |
| T-UX-06 | Calibrar, tocar 15 minutos, olhar o realce do dó | O realce ainda está na tecla, ou a pessoa acha sozinha o caminho de recalibrar e as estrelas seguem lá | RNF-06 |
| T-UX-07 | Trocar o idioma para inglês no progresso e abrir a lição 2 | O nome sobre a tecla é C, D ou E, conforme o alvo. Estrelas iguais às de antes da troca | RF-03 |
| T-UX-08 | Errar de propósito no guiado e na memória | Em menos de um segundo aponta a tecla que o app quer, sem ler uma frase de erro | protótipo §10 |
| T-UX-09 | Inspecionar ouvir, guiado, memória, progresso | Zero ocorrência de nota caindo, pentagrama ou cronômetro | T-ACC-17 |
| T-UX-10 | Desconectar o cabo e perguntar o que houve | A pessoa diz que pausou e que o teclado saiu. Não acha que perdeu as estrelas | RF-15 |
| T-UX-11 | Com tracking de mão desligado nas configurações do Quest, depois da calibração feita com tracking ligado | Botão do modo ouvir ainda avança pela tecla indicada no painel | RF-39 |

T-UX-03 é o teste que os apps de esteira costumam falhar. Se a pessoa não toca os quatro dó com o headset no colo, a lição não está pronta, mesmo com estrela na tela.

## 5. Desempenho

| ID | Caso | Aceite |
|---|---|---|
| T-PERF-01 | Lição 1 no Quest 3, passthrough ligado, duas mãos sobre as teclas | 72 fps estáveis no contador do sistema, sem queda prolongada quando o realce troca de tecla | RNF-02 |
| T-PERF-02 | O mesmo no Quest 3S | Igual | RNF-02 |
| T-PERF-03 | Avião do Quest, cabo MIDI local | Lição 1 completa | RNF-04 |

## 6. Ordem de execução

1. T-DOM-01 a T-DOM-12 na máquina de desenvolvimento.
2. T-MIDI-01. Se falhar, parar. Não há o que calibrar.
3. T-MIDI-02, 03, 05, 07, 08.
4. Protótipo mínimo da seção 9 do conceito AR.
5. T-MIDI-09, 10, 11 e T-UX-02, 03, 05, 08, 09.
6. O resto antes de gravar as 12 lições como conteúdo fechado.

## 7. Fora destes testes

Compra, conta, ranking online, Quest 2, Bluetooth, mão esquerda, leitura de pauta, dinâmica como passe e nota caindo. Não criar caso de teste para isso na v1.
