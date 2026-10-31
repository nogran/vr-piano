# Roadmap: importação de músicas

**Status:** implementação futura, fora do escopo da v1. A única parte que entra na v1 é o **formato de música** (§3): as lições nativas já são escritas nele, então o importador futuro só precisa gerar o mesmo JSON.

**Objetivo:** a pessoa escolhe um arquivo musical (Beethoven, Mozart, um arranjo próprio). O app processa o arquivo uma vez, fatia em trechos pedagógicos e gera a lição no formato do app: mão 3D na demonstração, cinza dinâmico na vez guiada e fase de memória.

## 1. Entregas

### Entrega 1: MIDI (`.mid`, `.midi`)

O importador lê eventos de tempo: Note On, Note Off, velocity, andamento (Set Tempo) e fórmula de compasso (Time Signature).

O MIDI não diz qual dedo usar e nem sempre marca compassos com exatidão. Por isso essa entrega precisa de três heurísticas próprias:

| Problema | Heurística inicial |
|---|---|
| Separar as mãos | Uma trilha ou canal por mão, quando o arquivo tem duas. Com uma trilha só: divisão por altura em torno do dó central, com histerese para a mão não trocar a cada nota. |
| Fatiar em trechos | Corte em pausas maiores que um tempo ou em notas longas, no fim de um compasso. Trecho entre 4 e 12 notas por mão. Se não houver pausa, corte a cada 2 compassos. |
| Dedilhado | Busca de menor custo sobre as posições da mão: mantém a mão parada enquanto as notas cabem nos cinco dedos, penaliza passagem de polegar, salto e dedo 4–5 em tecla preta. O resultado é uma sugestão, e a pessoa pode corrigir no app. |

### Entrega 2: partitura digital (`.musicxml`, `.mxl`)

O parser lê a estrutura formal da partitura e aproveita o que o MIDI não tem:
- a divisão exata de compassos (`<measure>`) para cortar os trechos;
- a separação por pauta (clave de sol e clave de fá, `<staff>`) para separar as mãos;
- o dedilhado escrito (`<fingering>`) para a mão 3D.

Com isso, a mão e os cortes ficam fiéis à partitura. A heurística da entrega 1 só entra onde a partitura não traz dedilhado.

## 2. Processamento

```text
arquivo escolhido ─► parser (MIDI ou MusicXML) ─► normalizador ─► validação ─► JSON no formato de música (§3)
                                                                                      │
                                                    salvo no armazenamento do app ◄───┘
```

- O processamento acontece **uma vez**, na importação. Ao tocar, o app lê só o JSON normalizado, nunca o arquivo bruto.
- O arquivo original é copiado para o armazenamento privado do app (`raw_file_url` aponta para essa cópia), para reprocessar quando o importador melhorar.
- Tudo é local. A v1 não pede internet (RNF-04), e a importação também não precisa: o arquivo vem do seletor de arquivos do Android, por cabo ou pelo armazenamento do headset. Upload para servidor só existiria se houvesse conta e nuvem, que estão fora do produto.
- A obra é do arquivo da pessoa. O app não distribui nem baixa músicas (RN-04).

## 3. Formato de música (o mesmo da v1)

Três níveis. As lições nativas da v1 usam este formato com as restrições do [06 §5](06-modelo-de-dominio.md#5-formato-de-música-e-catálogo).

### Nível 1: metadados da música (`SongMetadata`)

| Campo | Significado | Na v1 |
|---|---|---|
| `song_id` | Identificador único | `L01`…`L12`, `SONG1` |
| `title`, `composer` | Nome da obra e autor, das meta-tags do arquivo ou digitados pela pessoa | Chave de texto do 07 (`title_key`) em vez de texto fixo |
| `bpm` | Andamento base | 60 |
| `bpm_map` | Mudanças de andamento: lista de `{ "at_ms", "bpm" }` | Vazio |
| `time_signature` | Fórmula de compasso (`4/4`, `3/4`), para o metrônomo e o corte em compassos | `4/4` |
| `source_format` | `NATIVE`, `MIDI` ou `MUSICXML` | `NATIVE` |
| `raw_file_url` | Cópia do arquivo original no armazenamento do app | `null` |
| `practice` | `whole`: a execução percorre todos os trechos em sequência. `by_chunk`: cada trecho é praticado como uma lição | `whole` |

### Nível 2: trechos (`LessonChunks`)

| Campo | Significado |
|---|---|
| `chunk_index` | Ordem do trecho na música, a partir de 1 |
| `start_time_ms`, `end_time_ms` | Onde o trecho começa e termina na linha do tempo da música |
| `measure_range` | Compassos do trecho (`[1, 4]`) |
| `active_hands` | `RH`, `LH` ou `BOTH` |
| `label_lesson` | Na v1, o número da lição mostrado na etiqueta do card (na `SONG1`, a lição de origem do trecho). Em música importada é nulo e o card mostra o título |
| `notes` | Eventos de nota do trecho (nível 3) |

### Nível 3: eventos de nota (`NoteEvents`)

| Campo | Significado | Como o app usa |
|---|---|---|
| `order` | Ordem dentro do trecho | Índice do passo |
| `pitch` | Nota MIDI, de 21 a 108 (88 teclas) | Nota cobrada. Precisa caber no teclado calibrado (§4) |
| `note_name` | Nome em notação científica (`G4`, `Eb4`) | Só depuração. O app exibe o nome pela tabela de textos |
| `hand` | `LEFT` ou `RIGHT` | Qual mão 3D desce o dedo |
| `finger` | 1 (polegar) a 5 (mínimo) | Dedo da mão 3D e nome no card |
| `start_time_ms` | Ataque na linha do tempo da música | Demonstração: instante do Note On. Vez guiada: veja abaixo |
| `duration_ms` | Quanto tempo segurar | Demonstração: Note Off em `start_time_ms + duration_ms × P-09` |
| `lookahead_start_ms` | Início do cinza: o `start_time_ms` da nota anterior do trecho | Derivado, gravado para conferência. `null` no primeiro passo |
| `delta_to_hit_ms` | `start_time_ms − lookahead_start_ms` | Intervalo que o cinza leva para escurecer |
| `chord_group_id` | Notas com o mesmo id formam um acorde | `null` na v1 |
| `velocity` | 1 a 127 | Velocity enviada na demonstração. Ausente vale `P-11` |

**Tempo elástico.** Na vez guiada, o app não segue `start_time_ms` como relógio absoluto. Quando a pessoa acerta a nota anterior no instante `t`, o tempo de ataque da próxima é `t + delta_to_hit_ms` (06 §3.1). Se ela atrasa, o relógio da música espera por ela. Por isso o intervalo entre notas (`delta_to_hit_ms`) é o que importa para o cinza, e não o instante absoluto.

### Exemplo

Motivo inicial da 5ª Sinfonia, só mão direita:

```json
{
  "song_id": "beethoven_symphony_5_intro",
  "title": "5ª Sinfonia (Motivo Inicial)",
  "composer": "Ludwig van Beethoven",
  "bpm": 108,
  "bpm_map": [],
  "time_signature": "2/4",
  "source_format": "MIDI",
  "raw_file_url": "imports/beethoven5.mid",
  "practice": "by_chunk",
  "chunks": [
    {
      "chunk_index": 1,
      "start_time_ms": 500,
      "end_time_ms": 2750,
      "measure_range": [1, 2],
      "active_hands": "RH",
      "notes": [
        { "order": 1, "pitch": 67, "note_name": "G4", "hand": "RIGHT", "finger": 4,
          "lookahead_start_ms": null, "start_time_ms": 500, "duration_ms": 250,
          "delta_to_hit_ms": null, "chord_group_id": null, "velocity": 96 },
        { "order": 2, "pitch": 67, "note_name": "G4", "hand": "RIGHT", "finger": 4,
          "lookahead_start_ms": 500, "start_time_ms": 750, "duration_ms": 250,
          "delta_to_hit_ms": 250, "chord_group_id": null, "velocity": 96 },
        { "order": 3, "pitch": 67, "note_name": "G4", "hand": "RIGHT", "finger": 4,
          "lookahead_start_ms": 750, "start_time_ms": 1000, "duration_ms": 250,
          "delta_to_hit_ms": 250, "chord_group_id": null, "velocity": 96 },
        { "order": 4, "pitch": 63, "note_name": "Eb4", "hand": "RIGHT", "finger": 2,
          "lookahead_start_ms": 1000, "start_time_ms": 1250, "duration_ms": 1500,
          "delta_to_hit_ms": 250, "chord_group_id": null, "velocity": 110 }
      ]
    }
  ]
}
```

O primeiro passo tem `lookahead_start_ms` nulo porque, na vez guiada, o passo 0 de cada volta já começa colorido (06 §3.1).

## 4. O que o app precisa ganhar antes de aceitar música importada

O formato de música não muda. A tela do piano, o cinza dinâmico e a mão 3D continuam lendo o mesmo JSON. Mas algumas regras do domínio e alguns adapters da v1 foram feitos só para mão direita, uma nota por vez, dentro de dó–sol. Cada item abaixo é trabalho próprio:

| Limite da v1 | O que muda | Onde |
|---|---|---|
| Teclado de 61 teclas (36–96 com shift 0) e música de 88 | Na importação, transpor por oitava para caber no alcance calibrado ou recusar com aviso. Validar contra o alcance do teclado, não contra 21–108 | Normalizador, `KeyboardPosePort` |
| Uma nota por vez | Acorde (`chord_group_id`): todas as teclas do grupo escurecem juntas e o acerto exige todas as notas dentro de uma janela curta (novo parâmetro) | Máquina de estados (06 §3), realce |
| Só mão direita | Segunda mão 3D (asset espelhado), pose da mão esquerda, `hand` no `AlvoMudou`. Calibração não muda | `HandInputPort`, mão 3D, domínio |
| Pose fixa em dó–sol | A pose do trecho vem dos dedos das primeiras notas, e não de uma posição fixa | Detector de pose |
| Andamento único | `bpm_map` muda o intervalo do cinza ao longo da música | Domínio, demonstração |
| Demonstração automática por item | Com `practice: by_chunk`, o “inédito” é por trecho (`demoSeen` guarda `song_id#chunk_index`) | Domínio, progresso |
| Estrelas e desbloqueio das 12 lições | Música importada tem estrelas por trecho e não abre nem fecha lição nativa | Progresso |
| Textos fixos do 07 | `title` e `composer` vêm do arquivo e são exibidos como estão, sem tradução | Card, catálogo |

## 5. Critérios para começar

- A v1 aprovada pelo critério do RN-03.
- O formato de música da v1 estável: as 12 lições e a música carregam dele sem campo extra.
- Uma decisão sobre acordes e mão esquerda (§4), porque a maior parte das músicas importadas vai ter os dois.

## 6. Testes previstos (quando a entrega começar)

- Arquivos MIDI de referência: mão única, duas trilhas, uma trilha com duas mãos, com e sem Set Tempo, com mudança de andamento.
- Arquivos MusicXML de referência: com e sem `<fingering>`, duas pautas, acorde.
- Ida e volta: importar, gerar JSON, validar contra o esquema do 06 §5, tocar a demonstração e comparar o MIDI enviado com o original (mesma ordem, intervalos dentro de ± 1 ms).
- Arquivo fora do alcance do teclado: transposto ou recusado, nunca truncado em silêncio.
