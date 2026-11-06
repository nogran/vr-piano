# Textos da interface

Fonte única dos textos do app (RF-03). O arquivo `content/textos.json` é gerado destas tabelas com `dotnet run tools/gerar-textos.cs`, e o app e a versão web/ leem só ele; nenhum texto fica escrito direto na cena ou no `main.js`. Depois de mudar uma tabela, rode o gerador (T-TXT-03 acusa se esquecer). Frases do card até 32 caracteres (RNF-05). “Frase” com número conta só trechos novos (RF-51); “Parte” conta os trechos do Juntar, repetições incluídas (RF-56). `{n}`, `{key}` e similares são trocados em tempo de execução.

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
| `card.phrase` | {title} · Frase {n} | {title} · Phrase {n} |
| `card.whole` | {title} · Música inteira | {title} · Whole song |
| `phrase.label` | Frase {n} | Phrase {n} |
| `whole.label` | Música inteira | Whole song |
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
| `lesson.join.intro` | Agora junte tudo. | Now put it all together. |
| `lesson.join.count` | As {n} partes, de memória. | All {n} parts, from memory. |
| `lesson.join` | Junte tudo de memória. | Put it together from memory. |
| `lesson.join.phrase` | Parte {i} de {n} | Part {i} of {n} |
| `lesson.join.retry` | Do começo da parte. | From the start of the part. |
| `result.join` | De cor: {n} partes seguidas. | By heart: {n} parts in a row. |
| `lesson.backToGuide` | Mais uma vez com guia. | Once more with the guide. |
| `rhythm.early` | Adiantado | Early |
| `rhythm.late` | Atrasado | Late |
| `shortcut.hint` | ou {key} | or {key} |
| `note.60` | Dó | C |
| `note.62` | Ré | D |
| `note.64` | Mi | E |
| `note.65` | Fá | F |
| `note.67` | Sol | G |
| `note.69` | Lá | A |
| `note.71` | Si | B |
| `vel.soft` | leve | soft |
| `vel.medium` | médio | medium |
| `vel.loud` | forte | loud |

`{key}` usa o nome da nota da tecla de atalho seguido da oitava, em notação científica nos dois idiomas (ex.: “Dó2” / “C2”), porque as teclas de atalho ficam fora da posição.

## Resultado e painel

| Chave | pt-BR | en |
|---|---|---|
| `result.3` | Você lembrou. | You remembered. |
| `result.2` | Próxima etapa aberta. | Next step unlocked. |
| `result.1` | Falta tocar de memória. | Still needs a memory pass. |
| `result.rhythm` | No tempo: {n} de {m} | On time: {n} of {m} |
| `between.next` | Próxima | Next |
| `between.again` | De novo | Again |
| `between.progress` | Progresso | Progress |
| `between.songOpen` | {title} abriu. | {title} is open. |

## Revisão

| Chave | pt-BR | en |
|---|---|---|
| `review.label` | Revisão | Review |
| `review.title` | Música da última vez. | Song from last time. |
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
| `progress.stars` | Estrelas: {n} | Stars: {n} |
| `progress.streak` | Dias seguidos: {n} | Day streak: {n} |
| `progress.best` | Maior sequência: {n} | Best streak: {n} |
| `progress.mastered` | De memória, sem erro: {n} frases | From memory, no mistakes: {n} phrases |
| `progress.chain` | De cor, seguidas: {n} partes | By heart, in a row: {n} parts |
| `progress.memorized` | Decoradas: {n} | Memorized: {n} |
| `progress.memorized.badge` | Decorada | Memorized |
| `progress.locked` | Fechada | Locked |
| `progress.rhythm` | {n} de {m} no tempo | {n} of {m} on time |
| `settings.language` | Idioma | Language |
| `settings.hand` | Mão | Hand |
| `settings.recalibrate` | Calibrar de novo | Calibrate again |
| `settings.checklist` | Checklist do teclado | Keyboard checklist |

## Músicas

| Chave | pt-BR | en |
|---|---|---|
| `song.BRILHA.title` | Brilha, Brilha | Twinkle, Twinkle |
| `song.ODE.title` | Ode à Alegria | Ode to Joy |
| `song.hear` | Ouvir a música | Hear the song |

## Versão web/

Textos que só existem na versão web/.

| Chave | pt-BR | en |
|---|---|---|
| `web.pose.hint` | Polegar no dó central, um dedo em cada tecla. | Thumb on middle C, one finger on each key. |
| `web.start` | Começar | Start |
| `web.day` | Dia: {n} | Day: {n} |
| `web.velocity` | Força: {band} | Force: {band} |
| `web.midi.connected` | Teclado conectado: {name} | Keyboard connected: {name} |
| `web.midi.none` | Sem teclado MIDI: use o computador. | No MIDI keyboard: use the computer. |
| `web.midi.unsupported` | Sem Web MIDI. Use Chrome ou Edge. | No Web MIDI. Use Chrome or Edge. |
| `web.fac.day` | Dia do app: {day} | App day: {day} |
| `web.fac.dayShifted` | Dia do app: {day} ({n} a mais que o real) | App day: {day} ({n} ahead of the real one) |
| `web.fac.confirmErase` | Apagar o progresso de {name}? Execuções e eventos continuam guardados. | Erase {name}'s progress? Runs and events stay saved. |
