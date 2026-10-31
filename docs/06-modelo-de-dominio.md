# Modelo do domínio

Este documento escreve a regra do app de um jeito que dá para implementar e testar sem headset. Quando um texto de outro documento for menos preciso, vale o que está aqui. Os requisitos citados estão no [01](01-requisitos-do-sistema.md).

O domínio é uma biblioteca pura. Ele recebe **entradas** (fatos do mundo, já traduzidos pelos adapters), guarda estado e devolve **eventos** (o que mudou) e **comandos de saída** (notas a enviar, progresso a gravar). O tempo só entra pelo `ClockPort` e por `Tick(t)`. Por isso cada cenário de teste é uma lista de entradas com instantes e uma lista esperada de eventos.

## 1. Glossário

| Termo | Significado |
|---|---|
| Passo | Um evento de nota do formato de música (§5): nota MIDI, dedo (1 = polegar … 5 = mínimo), instante, duração e velocity. |
| Sequência | Lista ordenada de passos de uma lição ou da música, com um andamento (BPM). |
| Trecho | Um bloco da música (`chunk`). Nas lições há um trecho; na música, doze. |
| Item | Uma lição (`L01`–`L12`) ou a música (`SONG1`). |
| Execução | Do início de um item até o resultado ou até “Sair da aula”. |
| Demonstração | A mão branca toca a sequência no andamento, com o piano soando e a tecla acesa junto. Toca sozinha na primeira vez que a pessoa entra num item (item inédito). Depois disso, só quando ela pede com “Ouvir”. |
| Vez guiada | A pessoa toca a sequência com a tecla da vez colorida e a próxima em cinza dinâmico. Repete até uma volta limpa. |
| Volta | Percorrer a sequência do passo 0 ao último na vez guiada. Volta limpa = nenhum Note On errado. O ritmo não conta para a volta ser limpa. |
| Tentativa | Percorrer a sequência na memória. Falha no primeiro Note On errado. |
| Tempo de ataque | Instante em que a próxima nota deveria soar: o acerto anterior mais o `delta_to_hit_ms` dela. |
| Cinza dinâmico | Realce da próxima tecla que escurece do acerto anterior até o tempo de ataque. |
| Tempo elástico | O cinza anda no BPM real. Se a pessoa não toca no tempo de ataque, o tempo congela naquela tecla até ela acertar. |
| Dia local | Data (AAAA-MM-DD) no fuso atual do aparelho, dada pelo `ClockPort`. |
| Memória concluída | Fim de uma tentativa sem erro, numa lição, na música ou na revisão. |

## 2. Fluxo do app

```text
Idioma ─► Conexão ─► Checklist ─► Prova de nota ─► Calibração ─► Verificação do dó central
   (só se       (até haver      (se não            (se não há            │
   não há       teclado)        confirmado)        âncora válida)        ▼
   idioma)                                                         Revisão? ──► Entre lições ◄──► Progresso / Configurações
                                                                   (RF-44)          │   ▲
                                                                                    ▼   │
                                                                                   Lição / Música
```

- Cada etapa à esquerda é pulada quando o dado já existe no arquivo de progresso.
- Teclado desconectado em qualquer etapa depois de Conexão leva à tela de conexão. A exceção é dentro de uma lição, onde a lição pausa (RF-15).
- Na primeira abertura não há “Entre lições”: depois da calibração, a lição 1 começa direto.
- “Entre lições” abre por padrão a próxima lição aberta.

## 3. Máquina de estados da lição

Vale para lição e música. Estado interno: `fase`, `passo`, `voltaTemErro`, `falhasSeguidas`, `execuçãoTeveFalha`, `execuçãoUsouDica`, `ignorarAté`, `tAcerto`, `tAtaque`, `ritmo` (contagem da volta), `pausa?`.

```text
                                     item inédito
 ┌──────┐  pose ok (ou Mão off)  ┌──────────────┐  fim + fade da mão  ┌─────────┐  volta limpa  ┌───────────┐  P-22  ┌─────────┐ concluiu ┌───────────┐
 │ Pose │ ─────────────────────► │ Demonstração │ ──────────────────► │ Guiada  │ ────────────► │ Transição │ ─────► │ Memória │ ───────► │ Resultado │
 └──────┘ ─┐                     └──────────────┘ ◄── Ouvir ───────── └─────────┘               └───────────┘        └─────────┘          └───────────┘
    │      └── pose ok, item já visto ───────────────────────────────────► ▲                                             │ erro
    │ Ouvir                                                                │ P-21 falhas seguidas                        ▼
    ▼                                                                      │                                   ┌────────────────┐  P-25
 Demonstração                                                              └────────────────────────────────── │ RetornoDeErro  │ ─────► Memória (passo 0)
                                                                                                               └────────────────┘

 Qualquer fase ── cabo caiu / rajada / foco perdido ──► Pausado(motivo, fase anterior) ── retomar ──► ver tabela
```

### Transições

| Fase | Entrada | Condição | Efeito |
|---|---|---|---|
| Pose | `PoseMudou(true)` | Mão ligada, item inédito (`demoSeen` não contém o item) | Demonstração. |
| Pose | `PoseMudou(true)` | Mão ligada, item já visto | Guiada, passo 0. Eventos `FaseMudou(Guiada)` e `AlvoMudou`. |
| Pose | `MãoAlternada(false)` | — | Igual a `PoseMudou(true)`: Demonstração se o item for inédito, senão Guiada. |
| Pose | `OuvirAcionado` | Conectado | Demonstração. |
| Pose | `NoteOn` | — | Ignorado (não é erro nem acerto). O adapter de UI trata antes os atalhos de botão (`P-27`). |
| Demonstração | entrada | — | Agenda `EnviarNoteOn` em `start_time_ms` e `EnviarNoteOff` em `start_time_ms + duration_ms × P-09`, com a velocity da nota (`P-11` se ausente) e canal `P-12`. Instantes contados a partir do primeiro passo. |
| Demonstração | `Tick` | Hora de uma nota | `EnviarNoteOn` ou `EnviarNoteOff` e `DemoNota(passo)`. |
| Demonstração | `Tick` | Última nota solta | Grava o item em `demoSeen` (`DemoVista`). Evento `MãoSome` (fade de `P-35`). Guiada, passo 0. `ignorarAté = agora + P-08`. |
| Demonstração | `NoteOn` | — | Ignorado. |
| Guiada | entrada | — | Passo 0 entra direto como **Ataque** (cor ativa), sem cinza e sem avaliação de ritmo. `tAcerto = null`. |
| Guiada | `NoteOn(n)` | `t < ignorarAté` | Ignorado. |
| Guiada | `NoteOn(n)` | `n == alvo` | `NotaCerta`. Se `tAtaque` existe, avalia o ritmo (§3.1) e emite `RitmoAvaliado`. Avança: `tAcerto = t`, `tAtaque = t + delta_to_hit_ms(próximo passo)`, `PróximaMudou(nota, tAcerto, tAtaque, mesmaTecla)`. No fim da sequência: `VoltaConcluída(limpa = !voltaTemErro, ritmo)`. Se limpa → Transição; senão passo 0, `voltaTemErro = false`. |
| Guiada | `NoteOn(n)` | `n != alvo` | `NotaErrada(n, alvo)`. `voltaTemErro = true`. O passo e o `tAtaque` não mudam. |
| Guiada | `Tick` | `t ≥ tAtaque` e estado ainda Antecipação | `AtaqueChegou(nota)`: a tecla vira cor ativa e o tempo congela até o Note On certo. |
| Guiada | `OuvirAcionado` | Conectado | Demonstração. No fim volta para Guiada, passo 0, sem contar volta. |
| Guiada | `PoseMudou(*)` | — | Ignorado (RF-38). |
| Transição | entrada | — | Na primeira volta limpa da execução, se estrelas < 1: grava 1 (`EstrelasMudaram`). Guarda o `ritmo` dessa volta como ritmo da execução. |
| Transição | `Tick` | `P-22` passou | Memória, passo 0. Note On nesse intervalo é ignorado. |
| Memória | `NoteOn(n)` | `n == alvo` | Avança sem evento visual de cor. No último passo → Resultado. |
| Memória | `Tick` | `P-33` sem Note On desde o início da tentativa ou do último acerto | `DicaDeMemória(alvo)`: a tecla esperada pisca em cinza claro por `P-34`. `execuçãoUsouDica = true`. Repete a cada `P-33` enquanto a pessoa não tocar. |
| Memória | `NoteOn(n)` | `n != alvo` | `TentativaFalhou(n, alvo)`. `falhasSeguidas++`, `execuçãoTeveFalha = true`. Vai para RetornoDeErro. A falha só é confirmada `P-05` depois; se uma rajada for detectada nesse intervalo, ela é descartada. |
| RetornoDeErro | `NoteOn` | — | Ignorado. |
| RetornoDeErro | `Tick` | `P-25` passou | Se `falhasSeguidas ≥ P-21`: Guiada, passo 0, `falhasSeguidas = 0`, `VoltouAoGuiado` (sem demonstração automática; “Ouvir” fica disponível). Senão: Memória, passo 0. |
| Resultado | entrada | — | Estrelas da execução pela regra do §8. Grava se for maior que a salva. Grava o melhor ritmo do item. Atualiza o dia seguido e `últimaMemória`. Recalcula os desbloqueios. |
| qualquer | `MidiDesconectado` | Fase ≠ Resultado | Pausado(Cabo). Na demonstração, envia o Note Off pendente se ainda houver cabo, e volta para Pose ao retomar. |
| qualquer | rajada detectada | Guiada ou Memória | Pausado(Rajada). Na Guiada, o passo volta ao que era antes da primeira nota da janela. Na Memória, a tentativa será refeita. |
| qualquer | `FocoPerdido` | Fase ≠ Resultado | Pausado(Foco). |
| Pausado(Cabo) | `MidiConectado` | — | Retoma. |
| Pausado(Rajada) | `Tick` | `P-07` sem Note On | Retoma. |
| Pausado(Foco) | `Continuar` | — | Retoma. `SairDaAula` encerra a execução sem mudar estrelas. |

Retomada:
- **Guiada:** volta no mesmo passo, já em Ataque. A nota da retomada não tem avaliação de ritmo.
- **Memória:** volta no passo 0, sem somar falha.
- **Transição e RetornoDeErro:** reiniciam o seu tempo.
- **Demonstração:** volta para Pose, e o item continua inédito.

### 3.1 Cinza dinâmico e ritmo

Na Guiada, cada tecla da sequência passa por três estados:

| Estado | Quando | Visual (03 §2) |
|---|---|---|
| Antecipação | De `tAcerto` (acerto da nota anterior) até `tAtaque` | Cinza que escurece. Intensidade = `P-29 + (1 − P-29) × progresso`, com `progresso = (t − tAcerto) / (tAtaque − tAcerto)`. |
| Ataque | De `tAtaque` até o Note On certo, sem limite (tempo elástico) | Cor ativa. |
| Acerto | Note On certo | Verde por `P-23`. Depois, a tecla tocada fica na cor ativa enquanto estiver pressionada (até o Note Off dela ou até a próxima virar Ataque), como **tecla atual**. A próxima entra em Antecipação no instante do acerto. |

O domínio emite só `PróximaMudou` (com `tAcerto` e `tAtaque`) e `AtaqueChegou`. O adapter calcula o progresso a cada quadro pelo mesmo `ClockPort`.

Avaliação do Note On certo de um passo que tinha `tAtaque` (todo passo, menos o passo 0 da volta e o passo de uma retomada):

```text
d = t − tAtaque
d < −P-30           → Adiantado
−P-30 ≤ d ≤ P-31    → NoTempo
d > P-31            → Atrasado
```

- Adiantado e Atrasado avançam do mesmo jeito que NoTempo. O app mostra “Adiantado” ou “Atrasado” acima da tecla por `P-32` e não mostra nada quando a nota sai no tempo.
- `ritmo` da volta = `{ noTempo, avaliadas }`. Ele é zerado no começo de cada volta.
- **Mesma tecla em seguida:** quando a próxima nota é a mesma tecla, a tecla fica com contorno na cor ativa e só o centro faz o ciclo de cinza. Isso mostra que é para tocar de novo, e não para segurar a nota.

### Revisão (RF-44)

```text
Memória(item) ─ erro ─► RetornoDeErro ─► Ofertas { Ouvir? , Com guia? }
      ▲                                     │ Ouvir      → Demonstração → Ofertas
      │                                     │ Com guia   → Guiada por uma volta (limpa ou não) → Ofertas
      └──────── primeiro Note On ───────────┘
Memória concluída → `RevisãoConcluída`, dia seguido, sem mudar estrelas → Entre lições
```

- Item da revisão: `últimaMemória.item`. Se for `SONG1`, a revisão usa `L12`.
- A revisão não tem fase Pose, demonstração automática, volta automática à Guiada por `P-21` nem botão de pular.
- A dica de memória (`P-33`) vale também na revisão.
- “Ouvir” e “Com guia” valem uma vez cada por revisão. Depois de usados, somem das ofertas.
- Em Ofertas, um Note On igual ao primeiro passo inicia a tentativa e conta como acerto dela. Qualquer outro Note On (que não seja atalho de botão) inicia a tentativa e a faz falhar na hora.

### Quando a revisão é pedida

Na abertura, depois da verificação do dó central: `hoje = max(ClockPort.hoje, últimoDiaVisto)`. A revisão é pedida se `últimaMemória != null` e `últimaMemória.dia < hoje`. Uma revisão concluída grava `últimaMemória.dia = hoje`, então ela não se repete no mesmo dia.

## 4. Eventos

### Entradas (adapters → domínio)

| Entrada | Dados | Origem |
|---|---|---|
| `NoteOn` | nota, velocity 1–127, t | `MidiInPort` |
| `NoteOff` | nota, t | `MidiInPort` (inclui Note On com velocity 0) |
| `MidiConectado` / `MidiDesconectado` | t | `MidiInPort` |
| `PoseMudou` | ok, t | `HandInputPort` (as tolerâncias `P-13`–`P-17` são aplicadas no adapter) |
| `OuvirAcionado`, `ComGuiaAcionado`, `Continuar`, `SairDaAula`, `Próxima`, `DeNovo` | t | Card (pinça ou tecla de atalho) |
| `MãoAlternada` | ligada | Card ou configurações |
| `FocoPerdido` / `FocoRecuperado` | t | Ciclo de vida do app |
| `Tick` | t | Uma vez por quadro |

### Saídas (domínio → adapters)

| Evento | Dados | Quem consome |
|---|---|---|
| `FaseMudou` | fase, item | Card, mão branca |
| `AlvoMudou` | nota, dedo, índice, lição do trecho (na música) | Realce em cor ativa, card (nome do dedo, etiqueta) |
| `PróximaMudou` | nota, tAcerto, tAtaque, mesmaTecla | Realce em cinza dinâmico |
| `AtaqueChegou` | nota | Realce em cor ativa |
| `NotaCerta` | nota | Realce verde |
| `NotaErrada` | tocada, esperada | Realce vermelho |
| `RitmoAvaliado` | nota, d (ms), classe: NoTempo, Adiantado, Atrasado | Aviso acima da tecla |
| `VoltaConcluída` | limpa, ritmo | Card |
| `DicaDeMemória` | nota | Piscada cinza na tecla |
| `TentativaFalhou` | tocada, esperada, falhasSeguidas | Âmbar na esperada, vermelho na tocada, por `P-25` |
| `VoltouAoGuiado` | — | Card “Mais uma vez com guia.” com “Ouvir” |
| `MemóriaConcluída` | primeiraTentativa, usouDica | Realce verde na última tecla |
| `DemoNota` | nota, dedo, ligada | Mão branca, realce em cor ativa |
| `DemoVista` | item | Progresso |
| `MãoSome` | — | Fade da mão branca por `P-35` |
| `EnviarNoteOn` / `EnviarNoteOff` | nota, velocity, canal | `MidiOutPort` |
| `EstrelasMudaram` | item, de, para | Card de resultado, progresso |
| `ItemDesbloqueado` | item | Entre lições, progresso |
| `DiaSeguidoMudou` | atual, maior | Progresso |
| `RevisãoPedida` / `RevisãoConcluída` | item | Card |
| `LiçãoPausada` | motivo: Cabo, Rajada, Foco | Card |
| `LiçãoRetomada` | — | Card |
| `FaixaDeVelocity` | Leve, Médio, Forte | Indicador do card |
| `VelocityFixa` | ligada | Aviso, esconde a faixa |
| `GravarProgresso` | instantâneo do progresso | `ProgressStorePort` |

## 5. Formato de música e catálogo

Cada lição e a música do nível 1 são arquivos no **formato de música**, o mesmo que o importador futuro de MIDI e MusicXML vai gerar ([08 §3](08-roadmap-importacao.md#3-formato-de-música-o-mesmo-da-v1)). O domínio, o realce, o cinza dinâmico e a mão 3D só leem esse formato. Assim, importar música depois é escrever um tradutor, sem mexer na lição.

O catálogo vem embutido no app e é lido pelo `LessonCatalogPort`: um índice com a ordem das lições e um arquivo por música.

```json
{
  "catalog_version": 1,
  "lessons": ["L01","L02","L03","L04","L05","L06","L07","L08","L09","L10","L11","L12"],
  "song": "SONG1"
}
```

Lição 1 (`L01.json`):

```json
{
  "song_id": "L01",
  "title_key": "lesson.01.novelty",
  "bpm": 60,
  "bpm_map": [],
  "time_signature": "4/4",
  "source_format": "NATIVE",
  "raw_file_url": null,
  "practice": "whole",
  "chunks": [
    { "chunk_index": 1, "start_time_ms": 0, "end_time_ms": 5000, "measure_range": [1, 2],
      "active_hands": "RH", "label_lesson": 1,
      "notes": [
        { "order": 1, "pitch": 60, "note_name": "C4", "hand": "RIGHT", "finger": 1,
          "lookahead_start_ms": null, "start_time_ms": 0, "duration_ms": 1000,
          "delta_to_hit_ms": null, "chord_group_id": null, "velocity": 80 },
        { "order": 2, "pitch": 60, "note_name": "C4", "hand": "RIGHT", "finger": 1,
          "lookahead_start_ms": 0, "start_time_ms": 1000, "duration_ms": 1000,
          "delta_to_hit_ms": 1000, "chord_group_id": null, "velocity": 80 },
        { "order": 3, "pitch": 60, "note_name": "C4", "hand": "RIGHT", "finger": 1,
          "lookahead_start_ms": 1000, "start_time_ms": 2000, "duration_ms": 1000,
          "delta_to_hit_ms": 1000, "chord_group_id": null, "velocity": 80 },
        { "order": 4, "pitch": 60, "note_name": "C4", "hand": "RIGHT", "finger": 1,
          "lookahead_start_ms": 2000, "start_time_ms": 3000, "duration_ms": 2000,
          "delta_to_hit_ms": 1000, "chord_group_id": null, "velocity": 80 }
      ] }
  ]
}
```

Como o domínio usa os campos:
- **Passo** = um item de `notes`. A sequência de uma execução é a concatenação dos `notes` de todos os trechos, em ordem (`practice: "whole"`).
- **Tempo de ataque** na vez guiada = acerto anterior + `delta_to_hit_ms` (§3.1). O instante absoluto `start_time_ms` só vale na demonstração.
- **Demonstração**: Note On em `start_time_ms`, Note Off em `start_time_ms + duration_ms × P-09`, velocity da nota.
- **Etiqueta do card** = `lesson.label` (“Lição {n}”) com o `label_lesson` do trecho do passo atual. Na `SONG1`, é a lição de origem do trecho.

Conteúdo da v1, a 60 BPM (um tempo = 1000 ms). Toda nota dura 1 tempo e a última de cada lição dura 2. Dedo = nota: 60→1, 62→2, 64→3, 65→4, 67→5.

| Item | Notas MIDI |
|---|---|
| L01 | 60 60 60 60 |
| L02 | 60 62 |
| L03 | 60 62 64 |
| L04 | 64 62 60 |
| L05 | 60 62 64 62 60 |
| L06 | 64 65 |
| L07 | 60 62 64 65 |
| L08 | 65 67 |
| L09 | 60 62 64 65 67 |
| L10 | 67 65 64 62 60 |
| L11 | 60 64 67 64 60 |
| L12 | 60 64 67 64 60 60 62 64 65 67 |
| SONG1 | 12 trechos, um por lição, na ordem: 50 passos. Cada trecho começa onde o anterior termina, então o intervalo depois da última nota de um trecho é de 2 tempos e marca a respiração entre as frases. |

Validações do formato (teste T-DOM-30), em duas camadas.

Valem para qualquer música, inclusive a importada no futuro:
- `lookahead_start_ms` é o `start_time_ms` do passo anterior e `delta_to_hit_ms` é a diferença. O primeiro passo da música tem os dois nulos.
- `start_time_ms` cresce dentro e entre os trechos. `duration_ms > 0`. `velocity` entre 1 e 127. `finger` entre 1 e 5. `pitch` entre 21 e 108.
- `chunk_index` começa em 1, sem buraco. O `start_time_ms` e o `end_time_ms` do trecho cobrem as notas dele.
- `bpm` entre 40 e 120.

Restrições da v1 (o domínio da v1 recusa o arquivo se uma delas falhar):
- `hand` = `RIGHT`, `active_hands` = `RH`, `chord_group_id` nulo, `bpm_map` vazio, `practice` = `whole`.
- Toda nota está em {60, 62, 64, 65, 67} e o dedo é coerente com a nota.
- O catálogo lista 12 lições existentes, sem repetição. Os trechos da `SONG1` são iguais às lições, com os tempos deslocados.

O nome exibido da nota vem da tabela de textos (`note.60` = “Dó” / “C”). `note_name` existe só para depuração. O domínio nunca guarda texto de interface.

## 6. Arquivo de progresso

O progresso fica num JSON no armazenamento privado do app (`progress.json`). Datas em `AAAA-MM-DD`.

```json
{
  "schemaVersion": 1,
  "language": "pt-BR",
  "handGuide": true,
  "checklistConfirmed": true,
  "calibration": {
    "anchorUuid": "…",
    "lowNote": 36,
    "corners": [[0,0,0],[0,0,0],[0,0,0],[0,0,0]],
    "savedAt": "2026-10-30"
  },
  "stars": { "L01": 3, "L02": 2, "SONG1": 0 },
  "demoSeen": ["L01", "L02"],
  "rhythm": { "L01": { "onTime": 3, "judged": 3 }, "L02": { "onTime": 0, "judged": 1 } },
  "streak": { "current": 2, "best": 4, "lastCountedDay": "2026-10-30" },
  "lastMemory": { "item": "L02", "day": "2026-10-30" },
  "lastSeenDay": "2026-10-30"
}
```

- `language` é `null` até a primeira escolha. `calibration` é `null` sem calibração. Os cantos ficam no espaço da âncora.
- Desbloqueio não é gravado: ele é calculado das estrelas (RF-41).
- Item ausente em `stars` vale 0. Item ausente em `demoSeen` é inédito.
- `rhythm` guarda, por item, a volta limpa com a maior fração no tempo. Não afeta estrelas nem desbloqueio.
- Gravação atômica (RNF-09): escreve `progress.json.tmp`, força para o disco, copia o atual para `progress.json.bak` e renomeia o `.tmp` por cima do principal.
- Leitura: principal → se não abre ou não valida, `.bak` → se também falhar, o arquivo ruim vira `progress.corrupt-<data>.json`, o app começa do zero e mostra um aviso uma vez.
- `schemaVersion` maior que o conhecido: o app não sobrescreve e abre como se o arquivo estivesse corrompido, sem apagar.
- Migração: uma função por versão (`v1→v2`), testada com arquivo de exemplo.

## 7. Parâmetros

Valores iniciais. Para mudar um valor, mude este quadro e rode de novo o teste indicado.

| ID | Nome | Valor | Origem do valor | Validado em |
|---|---|---|---|---|
| P-01 | Teto da faixa leve | velocity ≤ 49 | Divisão em terços, ajustada para o CT-S300 em Touch Response Normal | T-MIDI-03 |
| P-02 | Teto da faixa médio | velocity ≤ 89 (forte = 90–127) | Idem | T-MIDI-03 |
| P-03 | Note On iguais para velocity fixa | 8 | RF-13 | T-DOM-07, T-MIDI-04 |
| P-04 | Notas para rajada simultânea | 3 Note On | Mão única, posição de cinco dedos, lição de uma nota por vez | T-DOM-08 |
| P-05 | Janela da rajada simultânea | 50 ms | Abaixo do intervalo de um trilo humano rápido | T-DOM-08, T-MIDI-12 |
| P-06 | Rajada por ritmo | mais de 12 Note On em 1000 ms | Trilo humano fica abaixo de 10 por segundo | T-DOM-08b, T-MIDI-11, T-MIDI-12 |
| P-07 | Silêncio para sair da rajada | 3000 ms | — | T-DOM-08c |
| P-08 | Cauda do filtro de eco | 300 ms depois do último Note Off enviado | Folga sobre a latência USB | T-DOM-05, T-MIDI-07 |
| P-09 | Fração soando na demonstração | 70 % de `duration_ms` | Notas separadas, fáceis de contar | T-DOM-04, T-MIDI-08 |
| P-10 | Andamento das lições da v1 | 60 BPM | Um tempo por segundo, lento para iniciante | T-DOM-04, T-DOM-31 |
| P-11 | Velocity da demonstração quando a nota não traz uma | 80 | Faixa média | T-MIDI-08 |
| P-12 | Canal MIDI de saída | 1 | Canal padrão do CT-S300 | T-MIDI-08 |
| P-13 | Estabilidade da pose | 500 ms contínuos | RF-31 | T-POSE-01 |
| P-14 | Confiança mínima do tracking | “alta” do SDK para a mão direita | — | T-POSE-03 |
| P-15 | Ângulo da palma | normal da palma até 35° da vertical para baixo do teclado | — | T-POSE-02 |
| P-16 | Ponta do polegar e do mínimo | polegar sobre a tecla 60 ± 1 tecla branca; mínimo sobre a 67 ± 1 tecla branca, na projeção sobre o plano do teclado | Erro de ponta de dedo ~1,7 cm | T-POSE-01, T-POSE-02 |
| P-17 | Altura da palma | 2–12 cm acima da superfície das teclas | — | T-POSE-02 |
| P-18 | Janela de amostragem da calibração | de 250 ms a 50 ms antes do Note On | O dedo cobre a tecla no último instante | T-CAL-02 |
| P-19 | Tolerância de largura na calibração | ± 15 % de 35 × `P-20` | — | T-CAL-03 |
| P-20 | Largura nominal da tecla branca | 23,5 mm (medir no CT-S300 real) | Tecla de tamanho normal | T-CAL-01 |
| P-21 | Falhas de memória até voltar à Guiada | 3 seguidas | — | T-DOM-14 |
| P-22 | Pausa antes da memória | 1000 ms | — | T-DOM-12 |
| P-23 | Verde de acerto | 150 ms | — | T-UX-08 |
| P-24 | Vermelho de erro | 200 ms | — | T-UX-08 |
| P-25 | Âmbar depois de erro de memória | 1000 ms | — | T-DOM-13 |
| P-26 | Tempo até a dica do cabo de dados | 10 s | — | T-ACC-03 |
| P-27 | Teclas de atalho dos botões | 1º botão = nota baixa calibrada (36 com shift 0), 2º = +2, 3º = +4 | Fora de 60–67 | T-ACC-20 |
| P-28 | Alvo de latência Note On → realce | 80 ms | RNF-01 | T-MIDI-09 |
| P-29 | Intensidade inicial do cinza | 15 % | Quase imperceptível no começo da espera | T-DOM-32, T-UX-13 |
| P-30 | Tolerância para “adiantado” | 150 ms antes do tempo de ataque | Folga para iniciante a 60 BPM | T-DOM-33 |
| P-31 | Tolerância para “atrasado” | 250 ms depois do tempo de ataque | A tecla já está colorida; dá tempo de reagir à cor | T-DOM-33 |
| P-32 | Aviso “Adiantado” / “Atrasado” | 600 ms | — | T-UX-14 |
| P-33 | Silêncio até a dica de memória | 3000 ms | — | T-DOM-34 |
| P-34 | Piscada da dica de memória | 400 ms | — | T-DOM-34, T-UX-15 |
| P-35 | Fade da mão branca ao fim da demonstração | 400 ms | — | T-ACC-06 |

## 8. Regras de cálculo, em resumo

```text
estrelasDaExecução = memória concluída ? (execuçãoTeveFalha || execuçãoUsouDica ? 2 : 3)
                                       : (houve volta limpa ? 1 : 0)
estrelasSalvas     = max(estrelasSalvas, estrelasDaExecução)

ritmoDaExecução    = ritmo da primeira volta limpa da execução
ritmoSalvo         = o de maior noTempo / avaliadas (empate: o mais recente)

demonstraçãoAutomática(item) = item ∉ demoSeen   (revisão: nunca)

aberto(L01)   = checklistConfirmado && calibração válida
aberto(Ln)    = estrelas(Ln-1) >= 2
aberto(SONG1) = todas as L01..L12 com estrelas >= 2

ao concluir memória no dia D:
  se streak.lastCountedDay == D       → nada
  senão se lastCountedDay == D - 1    → current += 1
  senão                               → current = 1
  best = max(best, current); lastCountedDay = D
  lastMemory = { item (ou o item revisado), D }

ao abrir no dia D:
  D = max(D, lastSeenDay); lastSeenDay = D
  se lastCountedDay < D - 1 → current exibido = 0 (e gravado como 0)
```
