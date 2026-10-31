# Modelo do domínio

A regra do app escrita de forma que se implemente e se teste sem headset. Vale o que está aqui quando um texto dos outros documentos for menos preciso. Os requisitos citados estão em [01](01-requisitos-do-sistema.md).

O domínio é uma biblioteca pura. Ele recebe **entradas** (fatos do mundo, já traduzidos pelos adapters), guarda estado e devolve **eventos** (o que mudou) e **comandos de saída** (notas a enviar, progresso a gravar). Tempo só entra pelo `ClockPort` e por `Tick(t)`. Assim cada cenário de teste é uma lista de entradas com instantes e uma lista esperada de eventos.

## 1. Glossário

| Termo | Significado |
|---|---|
| Passo | Uma nota MIDI e um dedo (1 = polegar … 5 = mínimo). |
| Sequência | Lista ordenada de passos de uma lição ou da música. |
| Item | Uma lição (`L01`–`L12`) ou a música (`SONG1`). |
| Execução | Do início de um item até o resultado ou até “Sair da aula”. |
| Volta | Percorrer a sequência do passo 0 ao último no loop. Limpa = sem nenhum Note On errado. |
| Tentativa | Percorrer a sequência na memória. Falha no primeiro Note On errado. |
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
- Teclado desconectado em qualquer etapa depois de Conexão leva à tela de conexão, exceto dentro de uma lição, onde a lição pausa (RF-15).
- Na primeira abertura não há “Entre lições”: depois da calibração, a lição 1 começa direto.
- “Entre lições” abre a próxima lição aberta por padrão.

## 3. Máquina de estados da lição

Vale para lição e música. Estado interno: `fase`, `passo`, `voltaTemErro`, `falhasSeguidas`, `execuçãoTeveFalha`, `ignorarAté`, `pausa?`.

```text
            ┌────────────── OuvirAcionado ──────────────┐
            ▼                                           │
 ┌──────┐  pose ok (ou Mão off)  ┌──────┐  volta limpa  ┌────────────┐  P-22  ┌─────────┐ concluiu ┌───────────┐
 │ Pose │ ─────────────────────► │ Loop │ ────────────► │ Transição  │ ─────► │ Memória │ ───────► │ Resultado │
 └──────┘                        └──────┘               └────────────┘        └─────────┘          └───────────┘
    │ Ouvir                         ▲                                           │ erro
    ▼                               │ P-21 falhas seguidas                      ▼
 ┌──────────────┐  fim             │                                    ┌────────────────┐  P-25
 │ Demonstração │ ────────────────►│◄────────────────────────────────── │ RetornoDeErro  │ ─────► Memória (passo 0)
 └──────────────┘                                                       └────────────────┘

 Qualquer fase ── cabo caiu / rajada / foco perdido ──► Pausado(motivo, fase anterior) ── retomar ──► ver tabela
```

### Transições

| Fase | Entrada | Condição | Efeito |
|---|---|---|---|
| Pose | `PoseMudou(true)` | Mão ligada | Loop, passo 0. Evento `FaseMudou(Loop)`, `AlvoMudou`. |
| Pose | `MãoAlternada(false)` | — | Loop, passo 0. |
| Pose | `OuvirAcionado` | Conectado | Demonstração. Comandos `EnviarNoteOn/Off` agendados por `P-09`/`P-10`. |
| Pose | `NoteOn` | — | Ignorado (não é erro, não é acerto). Atalhos de botão (`P-27`) são tratados pelo adapter de UI antes. |
| Demonstração | `Tick` | Hora de uma nota | `EnviarNoteOn` ou `EnviarNoteOff` e `DemoNota(passo)`. |
| Demonstração | `Tick` | Última nota solta | Loop, passo 0. `ignorarAté = agora + P-08`. |
| Demonstração | `NoteOn` | — | Ignorado. |
| Loop | `NoteOn(n)` | `t < ignorarAté` | Ignorado. |
| Loop | `NoteOn(n)` | `n == alvo` | `NotaCerta`. Avança. No fim da sequência: `VoltaConcluída(limpa = !voltaTemErro)`. Se limpa → Transição; senão passo 0, `voltaTemErro = false`. |
| Loop | `NoteOn(n)` | `n != alvo` | `NotaErrada(n, alvo)`. `voltaTemErro = true`. Passo não muda. |
| Loop | `PoseMudou(*)` | — | Ignorado (RF-38). |
| Transição | entrada | — | Na primeira volta limpa da execução, se estrelas < 1: grava 1 (`EstrelasMudaram`). |
| Transição | `Tick` | `P-22` passou | Memória, passo 0. Note On nesse intervalo é ignorado. |
| Memória | `NoteOn(n)` | `n == alvo` | Avança sem evento visual de cor. No último passo → Resultado. |
| Memória | `NoteOn(n)` | `n != alvo` | `TentativaFalhou(n, alvo)`. `falhasSeguidas++`, `execuçãoTeveFalha = true`. Vai a RetornoDeErro. A falha só é confirmada `P-05` depois; se uma rajada for detectada nesse intervalo, ela é descartada. |
| RetornoDeErro | `NoteOn` | — | Ignorado. |
| RetornoDeErro | `Tick` | `P-25` passou | Se `falhasSeguidas ≥ P-21`: Loop, passo 0, `falhasSeguidas = 0`, `VoltouAoLoop`. Senão Memória, passo 0. |
| Resultado | entrada | — | Estrelas da execução: 3 se `!execuçãoTeveFalha`, senão 2. Grava se maior que a salva. Atualiza dia seguido e `últimaMemória`. Recalcula desbloqueios. |
| qualquer | `MidiDesconectado` | Fase ≠ Resultado | Pausado(Cabo). Na demonstração, envia Note Off pendente se ainda houver cabo e volta para Pose ao retomar. |
| qualquer | rajada detectada | Loop ou Memória | Pausado(Rajada). Loop: passo volta ao que era antes da primeira nota da janela. Memória: a tentativa será refeita. |
| qualquer | `FocoPerdido` | Fase ≠ Resultado | Pausado(Foco). |
| Pausado(Cabo) | `MidiConectado` | — | Retoma. |
| Pausado(Rajada) | `Tick` | `P-07` sem Note On | Retoma. |
| Pausado(Foco) | `Continuar` | — | Retoma. `SairDaAula` encerra a execução sem mudar estrelas. |

Retomada: Loop volta no mesmo passo. Memória volta no passo 0 sem somar falha. Transição e RetornoDeErro reiniciam o seu tempo. Demonstração volta para Pose.

### Revisão (RF-44)

```text
Memória(item) ─ erro ─► RetornoDeErro ─► Ofertas { Ouvir? , Com guia? }
      ▲                                     │ Ouvir      → Demonstração → Ofertas
      │                                     │ Com guia   → Loop por uma volta (limpa ou não) → Ofertas
      └──────── primeiro Note On ───────────┘
Memória concluída → `RevisãoConcluída`, dia seguido, sem mudar estrelas → Entre lições
```

- Item da revisão: `últimaMemória.item`; se for `SONG1`, usa `L12`.
- Na revisão não há fase Pose, não há volta automática ao loop por `P-21` e não há botão de pular.
- “Ouvir” e “Com guia” valem uma vez cada por revisão. Usados, somem das ofertas.
- Em Ofertas, um Note On igual ao primeiro passo inicia a tentativa e conta como acerto dela. Qualquer outro Note On (que não seja atalho de botão) inicia a tentativa e a falha na hora.

### Quando a revisão é pedida

Na abertura, depois da verificação do dó central: `hoje = max(ClockPort.hoje, últimoDiaVisto)`; pedir revisão se `últimaMemória != null` e `últimaMemória.dia < hoje`. Uma revisão concluída grava `últimaMemória.dia = hoje`, então ela não se repete no mesmo dia.

## 4. Eventos

### Entradas (adapters → domínio)

| Entrada | Dados | Origem |
|---|---|---|
| `NoteOn` | nota, velocity 1–127, t | `MidiInPort` |
| `NoteOff` | nota, t | `MidiInPort` (inclui Note On com velocity 0) |
| `MidiConectado` / `MidiDesconectado` | t | `MidiInPort` |
| `PoseMudou` | ok, t | `HandInputPort` (tolerâncias `P-13`–`P-17` aplicadas no adapter) |
| `OuvirAcionado`, `ComGuiaAcionado`, `Continuar`, `SairDaAula`, `Próxima`, `DeNovo` | t | Card (pinça ou tecla de atalho) |
| `MãoAlternada` | ligada | Card ou configurações |
| `FocoPerdido` / `FocoRecuperado` | t | Ciclo de vida do app |
| `Tick` | t | Uma vez por quadro |

### Saídas (domínio → adapters)

| Evento | Dados | Quem consome |
|---|---|---|
| `FaseMudou` | fase, item | Card, mão branca |
| `AlvoMudou` | nota, dedo, índice, lição do trecho (na música) | Realce âmbar, mão branca, card (nome do dedo, etiqueta) |
| `NotaCerta` | nota | Realce verde |
| `NotaErrada` | tocada, esperada | Realce vermelho |
| `VoltaConcluída` | limpa | Card |
| `TentativaFalhou` | tocada, esperada, falhasSeguidas | Âmbar na esperada, vermelho na tocada, mão branca por `P-25` |
| `VoltouAoLoop` | — | Card “Mais uma vez com guia.” |
| `MemóriaConcluída` | primeiraTentativa | Realce verde na última tecla |
| `DemoNota` | nota, dedo, ligada | Mão branca, realce |
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

## 5. Catálogo de lições

Embutido no app, lido pelo `LessonCatalogPort`. Formato:

```json
{
  "catalogVersion": 1,
  "items": [
    { "id": "L01", "number": 1, "noveltyKey": "lesson.01.novelty",
      "steps": [ {"note": 60, "finger": 1}, {"note": 60, "finger": 1},
                 {"note": 60, "finger": 1}, {"note": 60, "finger": 1} ] }
  ],
  "song": { "id": "SONG1", "segments": ["L01","L02","L03","L04","L05","L06","L07","L08","L09","L10","L11","L12"] }
}
```

Conteúdo completo (dedo = nota: 60→1, 62→2, 64→3, 65→4, 67→5):

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
| SONG1 | L01 + L02 + … + L12, 50 passos |

Validações do catálogo (teste T-DOM-30): toda nota em {60, 62, 64, 65, 67}; dedo coerente com a nota; ids únicos; números de 1 a 12 sem buraco; a música referencia só lições existentes.

O nome da nota vem da tabela de textos (`note.60` = “Dó” / “C”). O domínio nunca guarda texto.

## 6. Arquivo de progresso

Um JSON em armazenamento privado do app (`progress.json`). Datas em `AAAA-MM-DD`.

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
  "streak": { "current": 2, "best": 4, "lastCountedDay": "2026-10-30" },
  "lastMemory": { "item": "L02", "day": "2026-10-30" },
  "lastSeenDay": "2026-10-30"
}
```

- `language` é `null` até a primeira escolha. `calibration` é `null` sem calibração. Os cantos ficam no espaço da âncora.
- Desbloqueio não é gravado: é calculado das estrelas (RF-41).
- Item ausente em `stars` vale 0.
- Gravação atômica (RNF-09): escreve `progress.json.tmp`, força para o disco, copia o atual para `progress.json.bak`, renomeia o `.tmp` por cima do principal.
- Leitura: principal → se não abre ou não valida, `.bak` → se também falhar, o arquivo ruim vira `progress.corrupt-<data>.json`, o app começa do zero e mostra um aviso uma vez.
- `schemaVersion` maior que o conhecido: não sobrescreve; abre como se fosse corrompido, sem apagar.
- Migração: uma função por versão (`v1→v2`), testada com arquivo de exemplo.

## 7. Parâmetros

Valores iniciais. Mudar um valor é mudar este quadro e rodar de novo o teste indicado.

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
| P-09 | Duração da nota na demonstração | 700 ms | Andamento de 60 por minuto | T-MIDI-08 |
| P-10 | Silêncio entre notas na demonstração | 300 ms | Idem (1 nota por segundo) | T-MIDI-08 |
| P-11 | Velocity enviada na demonstração | 80 | Faixa média | T-MIDI-08 |
| P-12 | Canal MIDI de saída | 1 | Canal padrão do CT-S300 | T-MIDI-08 |
| P-13 | Estabilidade da pose | 500 ms contínuos | RF-31 | T-POSE-01 |
| P-14 | Confiança mínima do tracking | “alta” do SDK para a mão direita | — | T-POSE-03 |
| P-15 | Ângulo da palma | normal da palma até 35° da vertical para baixo do teclado | — | T-POSE-02 |
| P-16 | Ponta do polegar e do mínimo | polegar sobre a tecla 60 ± 1 tecla branca; mínimo sobre a 67 ± 1 tecla branca, na projeção sobre o plano do teclado | Erro de ponta de dedo ~1,7 cm | T-POSE-01, T-POSE-02 |
| P-17 | Altura da palma | 2–12 cm acima da superfície das teclas | — | T-POSE-02 |
| P-18 | Janela de amostragem da calibração | de 250 ms a 50 ms antes do Note On | O dedo cobre a tecla no último instante | T-CAL-02 |
| P-19 | Tolerância de largura na calibração | ± 15 % de 35 × `P-20` | — | T-CAL-03 |
| P-20 | Largura nominal da tecla branca | 23,5 mm (medir no CT-S300 real) | Tecla de tamanho normal | T-CAL-01 |
| P-21 | Falhas de memória até voltar ao loop | 3 seguidas | — | T-DOM-14 |
| P-22 | Pausa antes da memória | 1000 ms | — | T-DOM-12 |
| P-23 | Verde de acerto | 150 ms | — | T-UX-08 |
| P-24 | Vermelho de erro | 200 ms | — | T-UX-08 |
| P-25 | Âmbar e mão branca depois de erro de memória | 1000 ms | — | T-DOM-13 |
| P-26 | Tempo até a dica do cabo de dados | 10 s | — | T-ACC-03 |
| P-27 | Teclas de atalho dos botões | 1º botão = nota baixa calibrada (36 com shift 0), 2º = +2, 3º = +4 | Fora de 60–67 | T-ACC-20 |
| P-28 | Alvo de latência Note On → realce | 80 ms | RNF-01 | T-MIDI-09 |

## 8. Regras de cálculo, em resumo

```text
estrelasDaExecução = memória concluída ? (execuçãoTeveFalha ? 2 : 3)
                                       : (houve volta limpa ? 1 : 0)
estrelasSalvas     = max(estrelasSalvas, estrelasDaExecução)

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
