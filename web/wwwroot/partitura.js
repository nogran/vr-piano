// Protótipo da partitura dinâmica (docs/09). Uma linha por trecho, pauta parada, cursor andando.
// O que aparece em cada fase ainda é decidido em main.js; o plano é levar isso ao domínio.
const NS = 'http://www.w3.org/2000/svg';
const L = 1000, H = 128, S = 11, BASE = 84, X0 = 120, TEMPOS = 8;
const PASSO_X = (L - X0 - 30) / TEMPOS;
const TINTA = '#1f2430';
const BRANCA = [0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6];

function el(pai, nome, attrs = {}, texto) {
  const e = document.createElementNS(NS, nome);
  for (const [k, v] of Object.entries(attrs)) e.setAttribute(k, v);
  if (texto !== undefined) e.textContent = texto;
  pai.append(e);
  return e;
}

// Dó central = 0, uma unidade por linha ou espaço da pauta.
function altura(nota) {
  const oitava = Math.floor((nota - 60) / 12);
  return oitava * 7 + BRANCA[nota - 60 - oitava * 12];
}

export function criarPartitura(caixa, cor) {
  const svg = el(caixa, 'svg', { role: 'img', 'aria-label': 'Partitura' });
  const folha = el(svg, 'g', { class: 'folha' });
  const cursor = el(svg, 'line', { y1: 32, y2: H - 8, stroke: cor.ativa, 'stroke-width': 2.5 });
  let chave = null, notas = [], linhaAtual = -1;

  function montar(seq, bpm) {
    folha.replaceChildren();
    notas = [];
    linhaAtual = -1;
    const tempoMs = 60000 / bpm;
    const trechos = [...new Set(seq.map((p) => p.trecho))];
    trechos.forEach((trecho, linha) => {
      const top = linha * H, yE = top + BASE;
      const g = el(folha, 'g');
      for (let i = 0; i < 5; i++) el(g, 'line', { x1: 14, x2: L - 14, y1: yE - i * S, y2: yE - i * S, stroke: TINTA, 'stroke-width': 1 });
      el(g, 'text', { x: 20, y: yE + 4, 'font-size': 66, fill: TINTA, 'font-family': "'Segoe UI Symbol','Noto Music','Apple Symbols',serif" }, '\u{1D11E}');
      for (const y of [yE - 2 * S - 2, yE - 2]) el(g, 'text', { x: 84, y, 'font-size': 24, 'font-weight': 700, 'text-anchor': 'middle', fill: TINTA }, '4');
      const xBarra = X0 + 4 * PASSO_X - PASSO_X * 0.35;
      for (const [x, w] of [[xBarra, 1.2], [L - 14, 2.6]]) el(g, 'line', { x1: x, x2: x, y1: yE, y2: yE - 4 * S, stroke: TINTA, 'stroke-width': w });

      const daLinha = seq.filter((p) => p.trecho === trecho);
      const inicio = daLinha[0].inicioMs;
      const itens = daLinha.map((p) => {
        const tempo = (p.inicioMs - inicio) / tempoMs;
        const tempos = p.duracaoMs / tempoMs;
        const a = altura(p.nota);
        return { p, tempo, tempos, x: X0 + tempo * PASSO_X, y: yE - (a - 2) * (S / 2), a, linha, top };
      });
      itens.forEach((n, i) => {
        n.g = el(g, 'g', { class: 'nota' });
        desenhar(n, itens[i - 1], itens[i + 1], yE, cor);
        notas.push(n);
      });
    });
  }

  function desenhar(n, antes, depois, yE) {
    const { g, x, y, a, tempos, tempo } = n;
    const meia = tempos >= 2;
    const pontuada = tempos === 1.5;
    const colcheia = tempos === 0.5;
    if (a <= 0) el(g, 'line', { x1: x - 13, x2: x + 13, y1: y, y2: y, stroke: TINTA, 'stroke-width': 1.2 });
    n.cabeca = el(g, 'ellipse', { cx: x, cy: y, rx: 7.4, ry: 5.3, transform: `rotate(-20 ${x} ${y})`, 'stroke-width': meia ? 2.2 : 1 });
    if (meia) n.cabeca.setAttribute('fill', 'none');
    n.haste = el(g, 'line', { x1: x + 6.9, x2: x + 6.9, y1: y - 1, y2: y - 38, 'stroke-width': 1.8 });
    n.extras = [];
    if (pontuada) n.extras.push(el(g, 'circle', { cx: x + 14, cy: y - (a % 2 === 0 ? 3 : 0), r: 2.2 }));
    const parceira = (o) => o && o.tempos === 0.5 && Math.floor(o.tempo) === Math.floor(tempo);
    if (colcheia && parceira(depois)) {
      n.extras.push(el(g, 'line', { x1: x + 6, x2: depois.x + 7.8, y1: y - 38, y2: depois.y - 38, 'stroke-width': 5 }));
    } else if (colcheia && !parceira(antes)) {
      n.extras.push(el(g, 'path', { d: `M${x + 7.5} ${y - 38} q3 11 13 15 q-3 -9 -13 -8 z` }));
    }
    n.dedo = el(g, 'text', { x, y: yE - 4 * S - 16, 'text-anchor': 'middle', 'font-size': 15, 'font-weight': 700 }, n.p.dedo);
    n.check = el(g, 'path', { d: `M${x - 6} ${yE + 25} l4.5 5 l8 -10`, fill: 'none', stroke: cor.acerto, 'stroke-width': 2.6, 'stroke-linecap': 'round', 'stroke-linejoin': 'round' });
  }

  function pintar(n, tinta, visivel, feita) {
    const estado = `${tinta}|${visivel}|${feita}`;
    if (n.estado === estado) return;
    n.estado = estado;
    n.g.style.display = visivel ? '' : 'none';
    n.cabeca.setAttribute('stroke', tinta);
    if (n.tempos < 2) n.cabeca.setAttribute('fill', tinta);
    n.haste.setAttribute('stroke', tinta);
    for (const e of n.extras) { e.setAttribute('fill', tinta); e.setAttribute('stroke', tinta); }
    n.dedo.setAttribute('fill', tinta);
    n.check.style.display = feita ? '' : 'none';
  }

  // visao: { id, seq, bpm, aberta, atual, progresso, revelar: { cor } | null }
  function atualizar(visao) {
    if (!visao) {
      caixa.hidden = true;
      chave = null;
      return;
    }
    caixa.hidden = false;
    if (visao.id !== chave) {
      chave = visao.id;
      montar(visao.seq, visao.bpm);
      const linhas = Math.min(2, notas.at(-1).linha + 1);
      svg.setAttribute('viewBox', `0 0 ${L} ${linhas * H}`);
    }
    const { atual, aberta, revelar } = visao;
    notas.forEach((n, i) => {
      const feita = i < atual;
      const daVez = i === atual;
      const tinta = feita ? cor.acerto : daVez && !visao.antecipando ? (revelar?.cor ?? cor.ativa) : TINTA;
      pintar(n, tinta, aberta || feita || (daVez && !!revelar), feita);
    });

    const n = notas[Math.min(Math.max(atual, 0), notas.length - 1)];
    const fim = atual >= notas.length;
    cursor.style.display = fim ? 'none' : '';
    if (fim) return;
    const proxima = notas[atual + 1];
    const destino = proxima && proxima.linha === n.linha ? proxima.x : L - 26;
    // Na guiada o cursor vai da nota anterior até a da vez; na demonstração, da nota que soa até a seguinte.
    const anterior = notas[atual - 1];
    const x = visao.chegando
      ? (anterior && anterior.linha === n.linha ? anterior.x + (n.x - anterior.x) * visao.progresso : n.x)
      : n.x + (destino - n.x) * visao.progresso;
    cursor.setAttribute('x1', x);
    cursor.setAttribute('x2', x);
    if (n.linha !== linhaAtual) {
      linhaAtual = n.linha;
      folha.style.transform = `translateY(${-linhaAtual * H}px)`;
    }
  }

  return { atualizar };
}
