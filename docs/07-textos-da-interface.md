# Textos da interface

Fonte única dos textos do app (RF-03). O catálogo de textos do app é gerado desta tabela; nenhum texto fica escrito direto na cena. Frases do card até 32 caracteres (RNF-05). `{n}`, `{key}` e similares são trocados em tempo de execução.

## Idioma

| Chave | pt-BR | en |
|---|---|---|
| `lang.pt` | Português | Português |
| `lang.en` | English | English |
| `setup.sit` | Sente-se de frente para o teclado. | Sit facing the keyboard. |

## Conexão e checklist

| Chave | pt-BR | en |
|---|---|---|
| `midi.searching` | Procurando teclado… | Looking for keyboard… |
| `midi.connected` | Teclado conectado. | Keyboard connected. |
| `midi.cableHint` | Use um cabo de dados, Micro-B no teclado e USB-C no headset. | Use a data cable, Micro-B on the keyboard and USB-C on the headset. |
| `midi.permission` | Permita o acesso ao teclado. | Allow access to the keyboard. |
| `midi.permissionDenied` | Sem permissão, o app não ouve o teclado. | Without permission, the app can't hear the keyboard. |
| `midi.permissionRetry` | Pedir de novo | Ask again |
| `check.title` | No teclado: | On the keyboard: |
| `check.rhythm` | Ritmo desligado. Song parado. | Rhythm off. Song stopped. |
| `check.octave` | MIDI Out Octave Shift em 0. | MIDI Out Octave Shift at 0. |
| `check.touch` | Touch Response em Normal. | Touch Response at Normal. |
| `check.velocity` | MIDI Out Velocity em Off. | MIDI Out Velocity at Off. |
| `check.local` | Local Control ligado. | Local Control on. |
| `check.done` | Pronto | Done |
| `check.octaveAgain` | O dó central não está no alcance. Confira o Octave Shift. | Middle C is out of range. Check Octave Shift. |

## Prova de nota e calibração

| Chave | pt-BR | en |
|---|---|---|
| `probe.press` | Toque qualquer tecla. | Press any key. |
| `cal.left` | Aponte o indicador para a tecla branca da esquerda e toque. | Point at the leftmost white key and press it. |
| `cal.right` | Aponte o indicador para a tecla branca da direita e toque. | Point at the rightmost white key and press it. |
| `cal.rejected` | Não bateu. Vamos de novo. | That didn't match. Let's try again. |
| `cal.nudge` | Ajuste se precisar. | Nudge if you need to. |
| `cal.ok` | Pronto | Done |
| `cal.verify` | Toque o dó central. | Press middle C. |

## Lição

| Chave | pt-BR | en |
|---|---|---|
| `lesson.label` | Lição {n} | Lesson {n} |
| `song.label` | Música | Song |
| `lesson.pose` | Ponha a mão assim. | Place your hand like this. |
| `lesson.hear` | Ouvir | Hear |
| `lesson.hand` | Mão | Hand |
| `lesson.watch` | Olhe e ouça. | Watch and listen. |
| `finger.1` | Polegar | Thumb |
| `finger.2` | Indicador | Index |
| `finger.3` | Médio | Middle |
| `finger.4` | Anelar | Ring |
| `finger.5` | Mínimo | Pinky |
| `lesson.memory` | Agora de memória. | Now from memory. |
| `lesson.backToGuide` | Mais uma vez com guia. | Once more with the guide. |
| `rhythm.early` | Adiantado | Early |
| `rhythm.late` | Atrasado | Late |
| `shortcut.hint` | ou {key} | or {key} |
| `note.60` | Dó | C |
| `note.62` | Ré | D |
| `note.64` | Mi | E |
| `note.65` | Fá | F |
| `note.67` | Sol | G |
| `vel.soft` | leve | soft |
| `vel.medium` | médio | medium |
| `vel.loud` | forte | loud |

`{key}` usa o nome da nota da tecla de atalho seguido da oitava, em notação científica nos dois idiomas (ex.: “Dó2” / “C2”), porque as teclas de atalho ficam fora da posição.

## Resultado e entre lições

| Chave | pt-BR | en |
|---|---|---|
| `result.3` | Você lembrou. | You remembered. |
| `result.2` | Lição seguinte aberta. | Next lesson unlocked. |
| `result.1` | Falta tocar de memória. | Still needs a memory pass. |
| `result.rhythm` | No tempo: {n} de {m} | On time: {n} of {m} |
| `between.next` | Próxima | Next |
| `between.again` | De novo | Again |
| `between.progress` | Progresso | Progress |
| `between.songOpen` | A música do nível 1 abriu. | The level 1 song is open. |

## Revisão

| Chave | pt-BR | en |
|---|---|---|
| `review.title` | Frase da última vez. | Phrase from last time. |
| `review.play` | Toque de memória. | Play it from memory. |
| `review.hear` | Ouvir | Hear |
| `review.guide` | Com guia | With guide |
| `review.done` | Lembrou. | Remembered. |

## Interrupções

| Chave | pt-BR | en |
|---|---|---|
| `pause.cable` | Teclado desconectado. A lição pausou. | Keyboard disconnected. Lesson paused. |
| `pause.burst` | Pare o ritmo ou a música do teclado. | Stop the keyboard rhythm or song. |
| `pause.focus` | Lição pausada. | Lesson paused. |
| `pause.continue` | Continuar | Continue |
| `pause.leave` | Sair da aula | Leave lesson |
| `warn.fixedVelocity` | Velocity fixa. No teclado, MIDI Out Velocity em Off. | Velocity is fixed. On the keyboard, set MIDI Out Velocity to Off. |
| `warn.close` | Fechar | Close |
| `warn.corruptProgress` | O progresso não pôde ser lido. Começando do zero. | Progress couldn't be read. Starting over. |
| `warn.battery` | Bateria baixa para uma sessão inteira. | Battery is low for a full session. |

## Progresso e configurações

| Chave | pt-BR | en |
|---|---|---|
| `progress.stars` | Estrelas: {n}/36 | Stars: {n}/36 |
| `progress.songStars` | Música: {n}/3 | Song: {n}/3 |
| `progress.streak` | Dias seguidos: {n} | Day streak: {n} |
| `progress.best` | Maior sequência: {n} | Best streak: {n} |
| `progress.mastered` | De memória, sem erro: {n} lições | From memory, no mistakes: {n} lessons |
| `progress.locked` | Fechada | Locked |
| `progress.rhythm` | {n} de {m} no tempo | {n} of {m} on time |
| `settings.language` | Idioma | Language |
| `settings.hand` | Mão | Hand |
| `settings.recalibrate` | Calibrar de novo | Calibrate again |
| `settings.checklist` | Checklist do teclado | Keyboard checklist |

## Matéria de cada lição (etiqueta curta no painel entre lições)

| Chave | pt-BR | en |
|---|---|---|
| `lesson.01.novelty` | Dó, polegar | C, thumb |
| `lesson.02.novelty` | Ré, indicador | D, index |
| `lesson.03.novelty` | Mi, médio | E, middle |
| `lesson.04.novelty` | Descer | Going down |
| `lesson.05.novelty` | Ida e volta | Up and back |
| `lesson.06.novelty` | Fá, anelar | F, ring |
| `lesson.07.novelty` | Subir até o fá | Up to F |
| `lesson.08.novelty` | Sol, mínimo | G, pinky |
| `lesson.09.novelty` | Cinco dedos subindo | Five fingers up |
| `lesson.10.novelty` | Cinco dedos descendo | Five fingers down |
| `lesson.11.novelty` | Salto | Skips |
| `lesson.12.novelty` | Revisão | Review |
