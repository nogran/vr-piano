# App sem VR

As mesmas lições do app VR, com o teclado desenhado na tela. As regras vêm do motor em C# de [`domain/`](../domain), compilado para WebAssembly; esta pasta só liga o motor à Web MIDI, ao som e ao desenho das teclas. Serve para testar o método de aula (assistir, tocar guiado, tocar de memória) com pessoas antes de ter o headset.

## Rodar

Precisa do .NET 10 SDK.

```bash
dotnet run --project web
```

Abra http://localhost:5280 no **Chrome** ou no **Edge** (o Firefox e o Safari não têm Web MIDI). A mão 3D carrega o three.js e a malha de mão do jsDelivr, então o PC precisa de internet. A malha é a `generic-hand` de [`@webxr-input-profiles/assets`](https://github.com/immersive-web/webxr-input-profiles) (MIT, Copyright 2019 Amazon).

## Ligar o CT-S300

1. Cabo de dados do teclado (Micro-B) ao PC. O teclado na tomada.
2. No teclado, o mesmo checklist do app VR: ritmo desligado, song parado, MIDI Out Octave Shift em 0, Touch Response em Normal, MIDI Out Velocity em Off, Local Control ligado.
3. O navegador pede permissão para dispositivos MIDI na primeira vez. Permita.
4. O app escolhe sozinho a entrada e a saída com "CASIO" no nome. A demonstração toca no próprio teclado.

Sem teclado, dá para tocar com as teclas `A` a `K` do computador (A = dó central) ou clicando nas teclas. Marque "Som no computador" para ouvir a demonstração no PC mesmo com o teclado ligado.

## O que muda em relação ao VR

| VR | Aqui |
|---|---|
| Teclas acesas em cima das reais | Teclas acesas na tela, olhar alterna entre tela e mão |
| Mão 3D mostra a pose e toca a demonstração | Uma mão 3D no estilo das mãos do Quest ([`mao3d.js`](wwwroot/mao3d.js)) fica sobre as teclas na pose e desce o dedo na cor ativa na demonstração; "Começar" faz o papel da pose reconhecida |
| Pinça nos botões do card | Clique, teclas `1` `2` `3`, ou Dó2, Ré2, Mi2 no teclado (`P-27`) |
| Calibração e âncora | Não existem: a Lição 1 já abre |
| Tirar o headset pausa | Trocar de aba pausa |

A detecção de rajada (`P-04` a `P-07`) ainda não está no motor.

## Teste com pessoas

- Cada pessoa entra com um nome ou código. Progresso, execuções e eventos ficam guardados no navegador, separados por nome.
- **Facilitador** (canto superior direito):
  - **Simular dia seguinte** testa a revisão sem esperar um dia.
  - **Abrir todas as lições** pula o desbloqueio.
  - **Baixar execuções (CSV)** exporta uma linha por execução de todos os participantes: estrelas, voltas guiadas, voltas com erro, falhas e dicas na memória, voltas ao guiado, falhas em Juntar, até qual frase juntou, ritmo e duração.
  - **Baixar eventos (CSV)** exporta tudo o que o motor emitiu, nota por nota.
- Sugestão de roteiro: 2 a 4 pessoas que nunca tocaram, Lições 1 a 3 no primeiro dia, a revisão no dia seguinte. A pergunta é se a frase sai de memória sem dica.
