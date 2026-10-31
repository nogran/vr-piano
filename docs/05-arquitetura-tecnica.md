# Arquitetura técnica

Como o app será construído, no nível de decisão. Sem código. A regra de negócio está em [06 · Modelo do domínio](06-modelo-de-dominio.md); aqui fica o que liga essa regra ao Quest, ao Android e ao CT-S300.

## 1. Stack

| Camada | Escolha | Observação |
|---|---|---|
| Engine | Unity 6 LTS, URP | Versão exata fixada no primeiro commit de código (§11). |
| XR | Meta XR Core SDK + Meta XR Interaction SDK, backend OpenXR | Passthrough, hand tracking, pinça, âncora espacial. |
| Linguagem | C# no app e no domínio; Kotlin na ponte MIDI Android | — |
| Domínio | Assembly separado, sem referência a `UnityEngine` (`noEngineReferences`) | Os mesmos fontes compilam num projeto .NET de testes (RNF-11). |
| Testes do domínio | NUnit via `dotnet test` | Roda sem licença Unity, em qualquer máquina e em CI. |
| Testes de adapter | Unity Test Framework (EditMode e PlayMode) | Persistência, mapa do teclado, pose com dados gravados. |
| Android | arm64-v8a, IL2CPP, Vulkan, `minSdk 32`, `targetSdk` exigido pelo Horizon OS na data do build | — |
| Distribuição | APK de desenvolvimento por `adb install` | Sem loja (RN-01). |

## 2. Estrutura do repositório (prevista)

```text
docs/                         planejamento (este conjunto)
domain/                       domínio puro (C#), sem Unity
domain.tests/                 NUnit, roda com dotnet test
unity/                        projeto Unity
  Assets/App/Adapters/        MIDI, mão, teclado, persistência, relógio, catálogo
  Assets/App/Presentation/    card, realce, mão branca, painéis
  Assets/App/Content/         catálogo e uma música por arquivo (formato do 06 §5), tabela de textos
  Assets/Plugins/Android/     AAR da ponte MIDI
android-midi-bridge/          projeto Kotlin que gera o AAR
tools/                        reprodutor de sessões MIDI gravadas
```

## 3. Ponte MIDI no Android

### Caminho principal: `android.media.midi`

1. `MidiManager` registra um `DeviceCallback` e lista os aparelhos `TYPE_USB`. O primeiro cujo nome contém “CASIO” é escolhido; se não houver, o primeiro USB-MIDI.
2. `openDevice` abre o aparelho. Se o sistema pedir permissão USB, o app mostra a linha do RF-10 e espera a resposta.
3. Porta de saída do teclado (`MidiOutputPort`, índice 0): recebe as notas. Porta de entrada do teclado (`MidiInputPort`, índice 0): envia a demonstração.
4. `MidiReceiver.onSend` chega numa thread do sistema. A ponte passa os bytes por um `MidiFramer` (resolve running status), descarta tudo que não é Note On/Off (RF-11) e grava `{status, nota, velocity, timestampNanos}` num buffer circular sem alocação.
5. O C# chama a ponte **uma vez por quadro** (`drainEvents`), recebe todos os eventos pendentes num array reaproveitado e os entrega ao domínio em ordem. Não há callback de Java para C# por nota.
6. Conexão e desconexão chegam pelo mesmo `DeviceCallback` e viram `MidiConectado` / `MidiDesconectado`.

### Caminho reserva: USB host direto

Se T-MIDI-00 mostrar que o `MidiManager` não lista o CT-S300 no Quest, a ponte usa `UsbManager`: pede permissão, abre a interface USB-MIDI (classe áudio, subclasse MIDI streaming), lê os pacotes de 4 bytes do endpoint bulk e aplica o mesmo filtro. A interface da ponte para o C# não muda.

### Relógio

- `timestampNanos` vem de `System.nanoTime()` (o mesmo relógio do `onSend`).
- O `ClockPort` do app usa esse mesmo relógio, pela ponte, para o instante monotônico. O dia local vem do relógio de parede do Android, no fuso atual.
- Em teste, o `ClockPort` é falso e controlado pelo cenário.
- A avaliação de ritmo (RF-32a) usa o `timestampNanos` do Note On, e não o instante do quadro em que o evento foi drenado. Assim a fila de até um quadro não vira atraso falso.
- O cinza dinâmico é calculado pelo adapter de realce a cada quadro, com `tAcerto` e `tAtaque` do evento `PróximaMudou` e o mesmo relógio. A tecla usa o mesmo material instanciado do realce, com a cor e a intensidade como propriedades.

### Envio

Comandos `EnviarNoteOn/Off` do domínio são executados no mesmo quadro, por uma fila na ponte que chama `MidiInputPort.send` fora da thread principal. A granularidade de quadro (≈14 ms) é suficiente para a demonstração nos andamentos do catálogo (40 a 120 BPM).

## 4. Manifest Android

| Item | Motivo |
|---|---|
| `uses-feature android.software.midi` (obrigatório) | Ponte MIDI. |
| `uses-feature android.hardware.usb.host` (obrigatório) | O Quest é host do CT-S300. |
| Recurso e permissão de hand tracking do Quest | Pinça, calibração, pose. |
| Recurso de passthrough do Quest | A sala é o fundo. |
| Permissão de âncora espacial do Quest | Calibração salva (RF-23). |
| Dispositivos suportados: Quest 3 e Quest 3S | Quest 2 fora. |
| **Sem** `android.permission.INTERNET` | RNF-04. O Unity pode adicioná-la sozinho; T-BUILD-02 confere o manifest final do APK. |

Os nomes exatos das chaves Meta seguem a documentação do SDK na versão fixada (§11) e são conferidos no manifest final.

## 5. Mão, pose e card

- **Pinça e botões do card:** Interaction SDK (raio e pinça), mão direita ou esquerda.
- **Ponta do indicador para calibrar:** pose do osso da ponta do indicador direito, guardada num buffer circular de 500 ms com o instante de cada quadro, para a janela `P-18`.
- **Detector de pose:** fica no adapter. A cada quadro, com a mão direita em confiança `P-14`, projeta palma, ponta do polegar e ponta do mínimo no plano do teclado calibrado e aplica `P-15`–`P-17`. Depois de `P-13` contínuos emite `PoseMudou(true)`; ao sair, `PoseMudou(false)`. O domínio só vê o booleano.
- **A mão do usuário não é desenhada:** a malha de mão do SDK fica desligada; só os dados de tracking são usados (RF-32b).

## 6. Teclado: geometria e calibração

### Geometria do CT-S300

61 teclas, 36 brancas e 25 pretas; com shift 0, MIDI 36 (dó) a 96 (dó). Medidas a confirmar na mesa em T-CAL-01 e gravar aqui:

| Medida | Valor inicial | Medido |
|---|---|---|
| Passo entre teclas brancas (`P-20`) | 23,5 mm | — |
| Comprimento visível da tecla branca | 140 mm | — |
| Largura da tecla preta | 0,58 × passo | — |
| Comprimento visível da tecla preta | 0,63 × comprimento da branca | — |
| Altura da tecla preta acima da branca | 10 mm | — |

Centro da tecla preta, em larguras de tecla branca a partir da borda esquerda do dó da oitava: dó# 0,90 · ré# 2,10 · fá# 3,88 · sol# 5,00 · lá# 6,12.

### Referencial do modelo

Origem na borda esquerda da frente da tecla branca mais grave, na superfície da tecla. X ao longo do teclado para a direita, Y para cima, Z para o fundo do teclado.

### Cálculo da calibração (RF-20 a RF-23)

1. Ponto E = mediana das pontas do indicador na janela `P-18` antes do Note On da tecla da esquerda. Ponto D = o mesmo para a direita.
2. Validação: as duas notas são brancas e `notaD − notaE = 60`; `|D − E|` está dentro de `P-19`.
3. Eixo X = `D − E` projetado no plano horizontal. Y = vertical do mundo (o teclado é tratado como plano e nivelado). Passo medido = `|D − E| / 35`.
4. Os pontos estão no centro das teclas, perto da frente: a origem fica em `E − 0,5 × passo` no X e recua um valor fixo (≈ 20 mm) no Z.
5. As quatro alças de ajuste mexem nos cantos do retângulo; o modelo é refeito a partir deles.
6. Salvar: cria uma âncora espacial na origem, guarda o UUID, `lowNote` e os cantos no espaço da âncora (06 §6).
7. Abrir: carrega a âncora pelo UUID. Se não achar ou se a verificação do dó central falhar, pede a calibração completa.

## 7. Mão branca (asset)

- Original (RN-04): modelada e animada para este app; nenhum asset de mão da Meta é reutilizado.
- Mão direita, rig com 5 dedos, até 10 mil triângulos.
- Anatomia de mão adulta real: palma, nós dos dedos, três falanges por dedo (duas no polegar), polegar saindo da base da palma. Sem unha, ruga ou textura de pele.
- Estilo das mãos do sistema do Quest: cinza-claro fosco, borda mais clara contra o fundo (fresnel), some em degradê no pulso. Contorno suave onde um dedo passa na frente de outro, para separar do passthrough claro.
- Sombra de contato falsa (decal) sob a mão, não sombra em tempo real (§8). Referência: `docs/visao/capa-licao1.jpg`.
- Pose base: posição de cinco dedos, dedos levemente curvos, polegar sobre a tecla 60.
- Animação por dedo: descer ~120 ms, segurar enquanto a nota soa, subir ~120 ms.
- Destaque do dedo na demonstração: o dedo que desce fica na cor ativa, igual à da tecla.
- Fim da demonstração: fade de `P-35` até sumir. A mão não aparece na vez guiada nem na memória.
- Escala: largura da mão casada com 5 passos de tecla branca do teclado calibrado.

## 8. Desempenho e latência

Orçamento de latência de Note On até o realce (RNF-01, alvo `P-28`):

| Trecho | Estimativa |
|---|---|
| USB e serviço MIDI do Android | até 5 ms |
| Espera até o próximo `drainEvents` | até 14 ms (um quadro a 72 Hz) |
| Domínio e atualização do material | < 1 ms |
| Render e compositor do Quest | 25 a 40 ms |
| **Total esperado** | **≈ 45 a 60 ms** |

Para os 72 fps (RNF-02): URP sem pós-processamento, foveated rendering fixo, poucos materiais transparentes (um por tecla, instanciado), texto em atlas pré-gerado, sem sombra em tempo real.

## 9. Registro para desenvolvimento

Só em build de desenvolvimento, sem rede:

- Log com o nome do aparelho MIDI, cada evento recebido com `timestampNanos` e o instante do quadro em que o realce mudou (T-MIDI-09).
- Gravador de sessão: grava em arquivo local, uma linha por entrada do domínio (`NoteOn`, `PoseMudou`, `Tick` reduzido…). O arquivo é puxado por `adb pull` e reproduzido no `tools/` contra o domínio, virando teste de regressão.

## 10. Bateria

O USB-C fica ocupado pelo teclado, então o headset roda na bateria. Medir em T-PERF-04 o consumo numa sessão de 30 minutos; se passar de 40 %, o app avisa no início quando a bateria estiver abaixo de 30 %.

## 11. Versões fixadas

Preencher no primeiro commit de código. Daí em diante, mudar uma versão é decisão registrada aqui.

| Componente | Versão |
|---|---|
| Unity | — |
| Meta XR Core SDK | — |
| Meta XR Interaction SDK | — |
| Android Gradle Plugin / Kotlin | — |
| `targetSdk` | — |
| .NET SDK dos testes de domínio | — |
| Firmware do Quest nos testes de aceite | — |

## 12. O que só o hardware responde

Cada item tem um teste que decide. Até o teste, vale a hipótese.

| Pergunta | Hipótese adotada | Teste |
|---|---|---|
| O `MidiManager` lista o CT-S300 no Quest? | Sim; se não, caminho reserva USB host | T-MIDI-00, T-MIDI-01 |
| O Quest pede permissão de USB? | Talvez; o fluxo aceita os dois casos | T-MIDI-13 |
| As notas da demonstração voltam como eco? | Não; o filtro existe por segurança | T-MIDI-07 |
| `MIDI Out Velocity = Off` deixa a velocity variar? | Sim | T-MIDI-03 |
| Ritmo ou song do teclado mandam notas pelo USB? | Possivelmente; a rajada protege | T-MIDI-11 |
| Medidas reais das teclas | Tecla de tamanho normal | T-CAL-01 |
| A pose é reconhecível com o teclado na frente? | Sim, com as tolerâncias do 06 §7 | T-POSE-01 a 03 |
| Consumo de bateria numa sessão | Menos de 40 % em 30 min | T-PERF-04 |
