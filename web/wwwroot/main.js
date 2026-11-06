// Versão web/: Web MIDI, teclado na tela, card e registro do teste. As regras da lição vêm do motor em C# (domain/).
import { dotnet } from './_framework/dotnet.js';
import { criarMao } from './mao3d.js';
import { criarPartitura } from './partitura.js';

const { getAssemblyExports, getConfig } = await dotnet.create();
const Motor = (await getAssemblyExports(getConfig().mainAssemblyName)).VrPiano.Web.Motor;

// ---------- Textos (content/textos.json, gerado do docs/07) e números de apresentação (docs/06 §7) ----------

const TEXTOS = JSON.parse(Motor.Textos())['pt-BR'];
function t(chave, valores = {}) {
  return TEXTOS[chave].replace(/\{(\w+)\}/g, (_, nome) => valores[nome]);
}

const FORCA = { Leve: 'vel.soft', Medio: 'vel.medium', Forte: 'vel.loud' };
const POSICAO = { 60: 1, 62: 2, 64: 3, 65: 4, 67: 5 };
const ATALHOS = [36, 38, 40];               // P-27
const NOME_ATALHO = [60, 62, 64].map((n) => `${t(`note.${n}`)}2`);
const VERDE_MS = 150, VERMELHO_MS = 200, AMBAR_MS = 1000, AVISO_MS = 600, DICA_MS = 400; // P-23, P-24, P-25, P-32, P-34
const CINZA_INICIAL = 0.15;                  // P-29
const DEDO_INICIAL = 0.4;                    // força da cor no dedo da vez antes do tempo de ataque
const COR = {
  branca: '#ffffff', preta: '#16181d', borda: '#9ca3af', ativa: '#f59e0b', acerto: '#22c55e', erro: '#ef4444',
  dica: '#cbd5e1', premida: '#bfdbfe', cinzaDe: [0xe5, 0xe7, 0xeb], cinzaAte: [0x37, 0x41, 0x51],
};
const FASES_COM_TICK = new Set(['Demonstracao', 'Guiada', 'Transicao', 'Memoria', 'Juntar', 'RetornoDeErro']);

// ---------- Armazenamento local ----------

const PREFIXO = 'vrpiano-web:';
function ler(chave, padrao) {
  try { const v = localStorage.getItem(PREFIXO + chave); return v ? JSON.parse(v) : padrao; } catch { return padrao; }
}
function gravar(chave, valor) {
  try { localStorage.setItem(PREFIXO + chave, JSON.stringify(valor)); } catch { /* sem armazenamento: segue sem salvar */ }
}

// ---------- Estado da casca ----------

let participante = null;
let meta = {};            // { diaOffset, abrirTodas, ultimoItem }
let painel = null;
let hoje = null;
let estado = null;        // último retorno do motor durante a lição
let tela = 'entrada';
let execucao = null;      // registro da execução em andamento
let botoes = [];
let msgGuia = false;
let musicaAberta = null;  // título da música que esta execução abriu
let faixa = null;
let velocityFixa = false;
let teclaAtual = null;
let aviso = null;
const flashes = [];
const soando = new Map();    // nota → dedo
const premidas = new Set();

const $ = (id) => document.getElementById(id);

// ---------- Motor ----------

function entrada(tipo, nota = 0, velocity = 0, t = performance.now()) {
  const r = JSON.parse(Motor.Entrada(tipo, nota, velocity, t));
  if (r) aplicar(r, t);
}

function aplicar(r, t) {
  const faseAntes = estado?.fase;
  estado = r;
  for (const ev of r.eventos) tratar(ev, t);
  if (r.progresso) gravar(`progresso:${participante}`, r.progresso);
  if (r.fase === 'Encerrada') mostrarEntre();
  else if (r.eventos.length || r.fase !== faseAntes) desenharTela();
}

function tratar(ev, agora) {
  if (ev.tipo !== 'FaixaMudou' && ev.tipo !== 'DemoNota') registrar(ev.tipo, ev);
  const x = execucao;
  switch (ev.tipo) {
    case 'FaseMudou':
      if (ev.fase !== 'Demonstracao') soando.clear();
      if (ev.fase === 'Demonstracao') { x.demos++; demoIndice = -1; }
      if (ev.fase === 'Memoria') x.tentativas++;
      if (ev.fase === 'Transicao') msgGuia = false;
      if (ev.fase === 'Resultado' || ev.fase === 'Encerrada') fecharExecucao(ev.fase === 'Resultado');
      break;
    case 'NotaCerta': flash(ev.nota, COR.acerto, agora, VERDE_MS); teclaAtual = ev.nota; break;
    case 'NotaErrada': flash(ev.tocada, COR.erro, agora, VERMELHO_MS); x.errosGuiada++; break;
    case 'RitmoAvaliado':
      if (ev.classe !== 'NoTempo') aviso = { texto: t(ev.classe === 'Adiantado' ? 'rhythm.early' : 'rhythm.late'), nota: ev.nota, ate: agora + AVISO_MS };
      break;
    case 'VoltaConcluida': x.voltas++; if (!ev.limpa) x.voltasComErro++; break;
    case 'DicaDeMemoria': flash(ev.nota, COR.dica, agora, DICA_MS); x.dicas++; revelada = { cor: '#94a3b8', ate: agora + DICA_MS }; break;
    case 'TentativaFalhou':
      flash(ev.esperada, COR.ativa, agora, AMBAR_MS);
      flash(ev.tocada, COR.erro, agora, VERMELHO_MS);
      x.falhas++;
      revelada = { cor: COR.ativa, ate: agora + AMBAR_MS };
      break;
    case 'JuntarFalhou':
      flash(ev.esperada, COR.ativa, agora, AMBAR_MS);
      flash(ev.tocada, COR.erro, agora, VERMELHO_MS);
      x.falhasJuntar++;
      revelada = { cor: COR.ativa, ate: agora + AMBAR_MS };
      break;
    case 'CadeiaConcluida': x.juntouAte = ev.ateOTrecho; break;
    case 'VoltouAoGuiado': msgGuia = true; x.voltouAoGuiado++; break;
    case 'MemoriaConcluida': flash(ev.nota, COR.acerto, agora, VERDE_MS); break;
    case 'DemoNota':
      if (ev.ligada) { soando.set(ev.nota, ev.dedo); demoIndice++; demoDesde = agora; } else soando.delete(ev.nota);
      break;
    case 'EnviarNoteOn': tocarSaida(ev.nota, ev.velocity, ev.canal, ev.em, true); break;
    case 'EnviarNoteOff': tocarSaida(ev.nota, 0, ev.canal, ev.em, false); break;
    case 'MusicaDesbloqueada': musicaAberta = ev.tituloKey; break;
    case 'FaixaMudou': faixa = ev.faixa; break;
    case 'VelocityFixa': velocityFixa = ev.ligada; break;
  }
}

function flash(nota, cor, t, ms) { flashes.push({ nota, cor, ate: t + ms }); }

// ---------- Execuções e eventos (o registro do teste) ----------

function abrirExecucao(item, tipo) {
  execucao = {
    participante, dia: hoje, item, tipo, inicio: new Date().toISOString(), t0: performance.now(),
    demos: 0, voltas: 0, voltasComErro: 0, errosGuiada: 0, tentativas: 0, falhas: 0, dicas: 0, voltouAoGuiado: 0,
    falhasJuntar: 0, juntouAte: '',
  };
}

function fecharExecucao(concluiu) {
  const x = execucao;
  if (!x || x.fim) return;
  x.fim = new Date().toISOString();
  x.duracaoS = Math.round((performance.now() - x.t0) / 100) / 10;
  x.concluiu = concluiu;
  x.estrelas = estado?.estrelas ?? '';
  x.ritmoNoTempo = estado?.ritmo?.onTime ?? '';
  x.ritmoAvaliadas = estado?.ritmo?.judged ?? '';
  const lista = ler(`execucoes:${participante}`, []);
  lista.push(x);
  gravar(`execucoes:${participante}`, lista);
}

let eventosDoParticipante = [];

function registrar(tipo, dados) {
  if (!participante) return;
  eventosDoParticipante.push({
    quando: new Date().toISOString(), tMs: Math.round(performance.now()), participante, dia: hoje,
    item: execucao?.item ?? '', fase: estado?.fase ?? tela, tipo, dados: JSON.stringify(dados ?? {}),
  });
  gravar(`eventos:${participante}`, eventosDoParticipante);
}

function baixarCsv(nome, linhas, colunas) {
  const esc = (v) => { const s = String(v ?? ''); return /[",\n;]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s; };
  const csv = [colunas.join(','), ...linhas.map((l) => colunas.map((c) => esc(l[c])).join(','))].join('\n');
  const a = document.createElement('a');
  a.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }));
  a.download = nome;
  a.click();
  URL.revokeObjectURL(a.href);
}

function todos(prefixo) {
  return ler('participantes', []).flatMap((p) => ler(`${prefixo}:${p}`, []));
}

// ---------- Participante, dia e telas ----------

function diaLocal(offset) {
  const d = new Date(Date.now() + offset * 864e5);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function entrar(nome) {
  participante = nome.trim();
  const lista = ler('participantes', []);
  if (!lista.includes(participante)) gravar('participantes', [...lista, participante]);
  meta = ler(`meta:${participante}`, { diaOffset: 0, abrirTodas: false, ultimoItem: null });
  eventosDoParticipante = ler(`eventos:${participante}`, []);
  $('quem').textContent = participante;
  $('abrir-facilitador').hidden = false;
  abrirDia();
}

function abrirDia() {
  const progresso = ler(`progresso:${participante}`, null);
  const r = JSON.parse(Motor.Abrir(progresso ? JSON.stringify(progresso) : '', diaLocal(meta.diaOffset)));
  hoje = r.hoje;
  painel = r.painel;
  gravar(`progresso:${participante}`, r.progresso);
  registrar('Abrir', { hoje, revisao: r.revisao });
  if (!painel.musicas.some((m) => m.itens.some((i) => i.id === meta.ultimoItem))) meta.ultimoItem = null;
  if (r.revisao) iniciar(r.revisao, true);
  else if (r.itemInicial) iniciar(r.itemInicial, false);
  else mostrarEntre();
}

function iniciar(item, revisao) {
  meta.ultimoItem = revisao ? meta.ultimoItem : item;
  gravar(`meta:${participante}`, meta);
  comecar(item, revisao ? 'revisao' : 'licao', (t) => Motor.Iniciar(item, revisao, t));
}

function ouvirMusica(musica) {
  comecar(musica, 'ouvir', (t) => Motor.OuvirMusica(musica, t));
}

function comecar(item, tipo, chamarMotor) {
  tela = 'licao';
  msgGuia = false;
  musicaAberta = null;
  teclaAtual = null;
  aviso = null;
  flashes.length = 0;
  soando.clear();
  estado = null;
  abrirExecucao(item, tipo);
  const t = performance.now();
  aplicar(JSON.parse(chamarMotor(t)), t);
  desenharTela();
}

function mostrarEntre() {
  painel = JSON.parse(Motor.Painel());
  tela = 'entre';
  estado = null;
  desenharTela();
}

function aberto(item) { return meta.abrirTodas || item.aberto; }

function nomeDoItem(item) { return item.frase ? t('phrase.label', { n: item.frase }) : t('whole.label'); }

function textoDaEtiqueta(e) {
  const title = t(e.tituloKey);
  if (e.tipo === 'Frase') return t('card.phrase', { title, n: e.frase });
  return e.tipo === 'MusicaInteira' ? t('card.whole', { title }) : t('review.label');
}

function estrelasHtml(n) {
  return [1, 2, 3].map((i) => `<span class="${i <= n ? '' : 'apagada'}">★</span>`).join('');
}

function desenharTela() {
  for (const t of ['entrada', 'entre', 'licao']) $(`tela-${t}`).hidden = tela !== t;
  botoes = [];
  if (tela === 'entrada') desenharEntrada();
  else if (tela === 'entre') desenharEntre();
  else desenharCard();
  desenharBotoes();
}

function desenharEntrada() {
  $('participantes').innerHTML = ler('participantes', []).map((p) => `<option value="${escapar(p)}">`).join('');
}

function desenharEntre() {
  const negrito = (n) => `<b>${n}</b>`;
  const seq = painel.sequencia;
  $('resumo').innerHTML = [
    t('progress.stars', { n: negrito(painel.estrelas) }), t('progress.streak', { n: negrito(seq.current) }),
    t('progress.best', { n: negrito(seq.best) }), t('progress.mastered', { n: negrito(painel.frasesSemErro) }),
    t('progress.memorized', { n: negrito(painel.decoradas) }), t('web.day', { n: negrito(hoje) }),
  ].map((s) => `<span>${s}</span>`).join('');

  $('grade').innerHTML = '';
  for (const musica of painel.musicas) $('grade').append(blocoDaMusica(musica));
  botoes.push({ texto: t('between.next'), principal: true, acao: () => iniciar(painel.proximo, false) });
  if (meta.ultimoItem) botoes.push({ texto: t('between.again'), acao: () => iniciar(meta.ultimoItem, false) });
}

function blocoDaMusica(musica) {
  const bloco = document.createElement('section');
  bloco.className = 'musica';
  const topo = document.createElement('div');
  topo.className = 'musica-topo';
  topo.innerHTML = `<h2>${t(musica.tituloKey)}</h2>
    ${musica.decorada ? `<span class="decorada">${t('progress.memorized.badge')}</span>` : ''}
    <span class="cadeia">${musica.cadeia ? t('progress.chain', { n: musica.cadeia }) : ''}</span>`;
  if (musica.podeOuvir) {
    const ouvir = document.createElement('button');
    ouvir.className = 'discreto';
    ouvir.textContent = t('song.hear');
    ouvir.onclick = () => ouvirMusica(musica.id);
    topo.append(ouvir);
  }
  const itens = document.createElement('div');
  itens.className = 'itens';
  for (const item of musica.itens) {
    const b = document.createElement('button');
    b.className = 'item' + (item.id === painel.proximo ? ' proximo' : '');
    b.disabled = !aberto(item);
    const ritmo = item.ritmo ? t('progress.rhythm', { n: item.ritmo.onTime, m: item.ritmo.judged }) : '';
    b.innerHTML = `<div class="nome">${nomeDoItem(item)}</div>
      <div class="novidade">${aberto(item) ? '' : t('progress.locked')}</div>
      <div class="est">${'★'.repeat(item.estrelas)}${'☆'.repeat(3 - item.estrelas)}</div>
      <div class="ritmo">${ritmo}</div>`;
    b.onclick = () => iniciar(item.id, false);
    itens.append(b);
  }
  bloco.append(topo, itens);
  return bloco;
}

function desenharCard() {
  const e = estado;
  if (!e) return;
  const etiqueta = textoDaEtiqueta(e.etiqueta);
  let principal = '', sub = '';
  const nomeDaNota = (p) => (p ? `${t(`note.${p.nota}`)} · ${t(`finger.${p.dedo}`)}` : '');

  switch (e.fase) {
    case 'Pose':
      principal = t('lesson.pose');
      sub = t('web.pose.hint');
      botoes.push({ texto: t('web.start'), principal: true, acao: () => entrada('pose') });
      botoes.push({ texto: t('lesson.hear'), acao: () => entrada('ouvir') });
      break;
    case 'Demonstracao':
      principal = t('lesson.watch');
      sub = [...soando].map(([nota, dedo]) => nomeDaNota({ nota, dedo })).join('');
      break;
    case 'Guiada':
      principal = msgGuia ? t('lesson.backToGuide') : nomeDaNota(e.alvo);
      sub = msgGuia ? nomeDaNota(e.alvo) : '';
      if (!e.revisao) botoes.push({ texto: t('lesson.hear'), acao: () => entrada('ouvir') });
      break;
    case 'Juntar':
      principal = t(e.revisao ? 'review.title' : 'lesson.join');
      sub = e.revisao ? t('review.play') : t('lesson.join.phrase', { i: e.frase, n: e.frases });
      break;
    case 'Transicao':
    case 'Memoria':
    case 'RetornoDeErro':
      if (e.juntando) {
        principal = t(e.fase === 'Transicao' ? 'lesson.join.intro' : 'lesson.join.retry');
        sub = e.fase === 'Transicao' ? t('lesson.join.count', { n: e.frases }) : '';
        break;
      }
      principal = t(e.revisao ? 'review.title' : 'lesson.memory');
      sub = e.revisao ? t('review.play') : '';
      break;
    case 'Ofertas':
      principal = t('review.title');
      sub = t('review.play');
      if (e.ofertas.includes('Ouvir')) botoes.push({ texto: t('review.hear'), acao: () => entrada('ouvir') });
      if (e.ofertas.includes('ComGuia')) botoes.push({ texto: t('review.guide'), acao: () => entrada('comGuia') });
      break;
    case 'Resultado':
      if (e.revisao) {
        principal = t('review.done');
        sub = e.decorou ? t('progress.memorized.badge') : '';
        botoes.push({ texto: t('pause.continue'), principal: true, acao: mostrarEntre });
      } else {
        principal = `<div class="estrelas">${estrelasHtml(e.estrelas)}</div>${t(e.estrelas === 3 ? 'result.3' : 'result.2')}`;
        const ritmo = e.ritmo && e.ritmo.judged ? t('result.rhythm', { n: e.ritmo.onTime, m: e.ritmo.judged }) : '';
        const juntou = e.juntando ? t('result.join', { n: e.frases }) : '';
        sub = [juntou, ritmo, musicaAberta ? t('between.songOpen', { title: t(musicaAberta) }) : ''].filter(Boolean).join('<br>');
        painel = JSON.parse(Motor.Painel());
        if (painel.proximo !== e.item) botoes.push({ texto: t('between.next'), principal: true, acao: () => iniciar(painel.proximo, false) });
        botoes.push({ texto: t('between.again'), acao: () => iniciar(e.item, false) });
        botoes.push({ texto: t('between.progress'), acao: mostrarEntre });
      }
      break;
    case 'Pausado':
      principal = t(e.motivo === 'Cabo' ? 'pause.cable' : 'pause.focus');
      if (e.motivo === 'Foco') botoes.push({ texto: t('pause.continue'), principal: true, acao: () => entrada('continuar') });
      botoes.push({ texto: t('pause.leave'), acao: () => entrada('sair') });
      break;
  }

  // Força só tem sentido com o teclado de verdade; no computador toda nota tem a mesma velocity.
  const velocityReal = usandoMidi && midiEntrada;
  const faixaHtml = !velocityReal || velocityFixa || !faixa ? '' :
    `<div class="faixa" title="${t('web.velocity', { band: t(FORCA[faixa]) })}">${['Leve', 'Medio', 'Forte'].map((f) => `<i class="${f === faixa ? 'acesa' : ''}"></i>`).join('')}</div>`;
  const avisoFixa = velocityReal && velocityFixa ? `<div class="sub">${t('warn.fixedVelocity')}</div>` : '';
  const sair = e.fase === 'Resultado' || e.fase === 'Encerrada' ? '' : `<button class="sair" id="sair">${t('pause.leave')}</button>`;
  $('card').innerHTML = `${faixaHtml}${sair}<div class="etiqueta">${etiqueta}</div>
    <div class="principal">${principal}</div><div class="sub">${sub}</div>${avisoFixa}`;
  if (sair) $('sair').onclick = () => entrada('sair');
}

function desenharBotoes() {
  $('botoes').innerHTML = '';
  botoes.forEach((b, i) => {
    const el = document.createElement('button');
    el.className = b.principal ? 'principal' : '';
    el.innerHTML = `${b.texto}<small>${t('shortcut.hint', { key: NOME_ATALHO[i] })}</small>`;
    el.onclick = b.acao;
    $('botoes').append(el);
  });
}

function escapar(s) { return s.replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]); }

// ---------- Entrada de notas ----------

function noteOn(nota, velocity, t, origem) {
  premidas.add(nota);
  if (origem !== 'midi') somLocal(nota, velocity, true);
  registrar('NoteOn', { nota, velocity, origem });
  const atalho = ATALHOS.indexOf(nota);
  if (atalho >= 0 && botoes[atalho]) { botoes[atalho].acao(); return; }
  if (tela === 'licao' && estado) entrada('noteOn', nota, velocity, t);
}

function noteOff(nota, origem) {
  premidas.delete(nota);
  if (origem !== 'midi') somLocal(nota, 0, false);
}

// ---------- Web MIDI ----------

let midi = null;
let midiEntrada = null;
let midiSaida = null;
let usandoMidi = false;

async function iniciarMidi() {
  if (!navigator.requestMIDIAccess) { status(t('web.midi.unsupported'), 'aviso'); return; }
  try {
    midi = await navigator.requestMIDIAccess();
  } catch {
    status(t('midi.permissionDenied'), 'aviso');
    return;
  }
  midi.onstatechange = atualizarPortas;
  $('midi-entrada').onchange = () => escolherEntrada($('midi-entrada').value);
  $('midi-saida').onchange = () => { midiSaida = midi.outputs.get($('midi-saida').value) ?? null; };
  atualizarPortas();
}

function preferida(portas) {
  return portas.find((p) => /casio/i.test(p.name)) ?? portas[0] ?? null;
}

function atualizarPortas() {
  const entradas = [...midi.inputs.values()].filter((p) => p.state === 'connected');
  const saidas = [...midi.outputs.values()].filter((p) => p.state === 'connected');
  preencher($('midi-entrada'), entradas);
  preencher($('midi-saida'), saidas);

  const tinhaEntrada = midiEntrada !== null;
  const ainda = entradas.find((p) => p.id === midiEntrada?.id);
  escolherEntrada(ainda ? ainda.id : preferida(entradas)?.id);
  if (!saidas.some((p) => p.id === midiSaida?.id)) midiSaida = preferida(saidas);
  $('midi-saida').value = midiSaida?.id ?? '';

  if (midiEntrada) {
    status(t('web.midi.connected', { name: midiEntrada.name }), 'ok');
    if (estado?.fase === 'Pausado' && estado.motivo === 'Cabo') entrada('conectado');
  } else {
    status(t(usandoMidi ? 'midi.searching' : 'web.midi.none'), 'aviso');
    if (tinhaEntrada && tela === 'licao') entrada('desconectado');
  }
}

function preencher(select, portas) {
  select.innerHTML = '<option value="">—</option>' + portas.map((p) => `<option value="${p.id}">${escapar(p.name)}</option>`).join('');
}

function escolherEntrada(id) {
  if (midiEntrada) midiEntrada.onmidimessage = null;
  midiEntrada = id ? midi.inputs.get(id) ?? null : null;
  $('midi-entrada').value = midiEntrada?.id ?? '';
  if (!midiEntrada) return;
  usandoMidi = true;
  midiEntrada.onmidimessage = (e) => {
    const [status, nota, velocity] = e.data;
    const tipo = status & 0xf0;
    if (tipo === 0x90 && velocity > 0) noteOn(nota, velocity, e.timeStamp, 'midi');
    else if (tipo === 0x80 || tipo === 0x90) noteOff(nota, 'midi');
  };
}

function status(texto, classe) {
  $('midi-status').textContent = texto;
  $('midi-status').className = `pilula ${classe ?? ''}`;
}

// Demonstração: manda para o teclado com o instante agendado; sem saída MIDI (ou com "Som no computador"), toca aqui.
function tocarSaida(nota, velocity, canal, em, ligar) {
  if (midiSaida) {
    const status = (ligar ? 0x90 : 0x80) | ((canal - 1) & 0x0f);
    midiSaida.send([status, nota, ligar ? velocity : 0], Math.max(em, performance.now()));
  }
  if (!midiSaida || $('som-pc').checked) somLocal(nota, velocity, ligar, em);
}

// ---------- Som no computador (Web Audio) ----------

let audio = null;
const vozes = new Map();

function contexto() {
  if (!audio) { try { audio = new AudioContext(); } catch { return null; } }
  if (audio.state === 'suspended') audio.resume();
  return audio;
}

function somLocal(nota, velocity, ligar, em = performance.now()) {
  const c = contexto();
  if (!c) return;
  const quando = c.currentTime + Math.max(0, (em - performance.now()) / 1000);
  if (!ligar) {
    const v = vozes.get(nota);
    if (!v) return;
    vozes.delete(nota);
    v.ganho.gain.setTargetAtTime(0, quando, 0.08);
    v.osc.stop(quando + 0.6);
    return;
  }
  somLocal(nota, 0, false, em);
  const osc = c.createOscillator();
  const ganho = c.createGain();
  osc.type = 'triangle';
  osc.frequency.value = 440 * 2 ** ((nota - 69) / 12);
  const pico = 0.08 + 0.25 * (velocity / 127);
  ganho.gain.setValueAtTime(0, quando);
  ganho.gain.linearRampToValueAtTime(pico, quando + 0.01);
  ganho.gain.setTargetAtTime(pico * 0.35, quando + 0.01, 0.4);
  osc.connect(ganho).connect(c.destination);
  osc.start(quando);
  vozes.set(nota, { osc, ganho });
}

// ---------- Teclado na tela ----------

const PRIMEIRA = 36, ULTIMA = 96;
const BRANCA_L = 24, BRANCA_A = 150, PRETA_L = 14, PRETA_A = 95, TOPO = 42;
const teclas = new Map();
const mao = criarMao(document.querySelector('main'));
const ePreta = (n) => [1, 3, 6, 8, 10].includes(n % 12);

function construirTeclado() {
  const svg = $('teclado');
  const ns = 'http://www.w3.org/2000/svg';
  let x = 0;
  const brancas = [], pretas = [];
  for (let n = PRIMEIRA; n <= ULTIMA; n++) {
    if (ePreta(n)) pretas.push({ n, x: x - PRETA_L / 2 });
    else { brancas.push({ n, x }); x += BRANCA_L; }
  }
  svg.setAttribute('viewBox', `0 0 ${x} ${TOPO + BRANCA_A + 2}`);
  for (const { n, x: kx } of [...brancas, ...pretas]) {
    const preta = ePreta(n);
    const g = document.createElementNS(ns, 'g');
    const r = document.createElementNS(ns, 'rect');
    r.setAttribute('x', kx);
    r.setAttribute('y', TOPO);
    r.setAttribute('width', preta ? PRETA_L : BRANCA_L);
    r.setAttribute('height', preta ? PRETA_A : BRANCA_A);
    r.setAttribute('rx', 3);
    const rotulo = document.createElementNS(ns, 'text');
    rotulo.setAttribute('x', kx + (preta ? PRETA_L : BRANCA_L) / 2);
    rotulo.setAttribute('y', TOPO + (preta ? PRETA_A - 10 : BRANCA_A - 14));
    rotulo.setAttribute('text-anchor', 'middle');
    rotulo.setAttribute('font-size', '13');
    rotulo.setAttribute('font-weight', '700');
    rotulo.setAttribute('pointer-events', 'none');
    const acima = document.createElementNS(ns, 'text');
    acima.setAttribute('x', kx + (preta ? PRETA_L : BRANCA_L) / 2);
    acima.setAttribute('y', TOPO - 22);
    acima.setAttribute('text-anchor', 'middle');
    acima.setAttribute('font-size', '13');
    acima.setAttribute('fill', '#fcd34d');
    g.append(r, rotulo, acima);
    let nome = null;
    if (!preta) {
      nome = document.createElementNS(ns, 'text');
      nome.setAttribute('x', kx + BRANCA_L / 2);
      // Acima da tecla, onde a mão 3D não cobre o nome da tecla que ela está tocando.
      nome.setAttribute('y', TOPO - 6);
      nome.setAttribute('text-anchor', 'middle');
      nome.setAttribute('font-size', '11');
      nome.setAttribute('font-weight', '600');
      nome.setAttribute('pointer-events', 'none');
      g.append(nome);
    }
    svg.append(g);
    r.addEventListener('pointerdown', (ev) => { ev.preventDefault(); noteOn(n, 80, ev.timeStamp, 'tela'); });
    r.addEventListener('pointerup', () => noteOff(n, 'tela'));
    r.addEventListener('pointerleave', () => { if (premidas.has(n)) noteOff(n, 'tela'); });
    if (n === 60) {
      const c = document.createElementNS(ns, 'circle');
      c.setAttribute('cx', kx + BRANCA_L / 2);
      c.setAttribute('cy', TOPO + BRANCA_A + 8);
      c.setAttribute('r', 2.5);
      c.setAttribute('fill', '#6b7280');
      svg.append(c);
      svg.setAttribute('viewBox', `0 0 ${x} ${TOPO + BRANCA_A + 14}`);
    }
    teclas.set(n, { r, rotulo, acima, nome, preta, ultimo: '' });
  }
}

function cinza(progresso) {
  const i = CINZA_INICIAL + (1 - CINZA_INICIAL) * Math.min(1, Math.max(0, progresso));
  const [a, b] = [COR.cinzaDe, COR.cinzaAte];
  return `rgb(${a.map((v, k) => Math.round(v + (b[k] - v) * i)).join(',')})`;
}

function realces(agora) {
  const m = new Map();
  const pos = (n, dados) => m.set(n, { ...m.get(n), ...dados });
  const e = estado;
  if (tela !== 'licao' || !e) {
    for (const n of premidas) pos(n, { fill: COR.premida });
    return m;
  }
  switch (e.fase) {
    case 'Pose':
      for (const [n, d] of Object.entries(POSICAO)) pos(Number(n), { rotulo: d, stroke: COR.ativa });
      break;
    case 'Demonstracao':
      for (const [n, dedo] of soando) pos(n, { fill: COR.ativa, rotulo: dedo });
      break;
    case 'Guiada': {
      const alvo = e.alvo;
      if (teclaAtual !== null && premidas.has(teclaAtual) && !e.emAtaque && teclaAtual !== alvo.nota) pos(teclaAtual, { fill: COR.ativa });
      if (e.emAtaque || e.tAtaque === null) pos(alvo.nota, { fill: COR.ativa, rotulo: alvo.dedo });
      else {
        const progresso = (agora - e.tAcerto) / (e.tAtaque - e.tAcerto);
        pos(alvo.nota, { fill: cinza(progresso), rotulo: alvo.dedo, stroke: teclaAtual === alvo.nota ? COR.ativa : undefined });
      }
      break;
    }
  }
  for (const f of flashes) if (agora < f.ate) pos(f.nota, { fill: f.cor });
  return m;
}

function desenharTeclas(agora) {
  for (let i = flashes.length - 1; i >= 0; i--) if (flashes[i].ate <= agora) flashes.splice(i, 1);
  if (aviso && aviso.ate <= agora) aviso = null;
  const m = realces(agora);
  const comNomes = tela !== 'licao' || !estado || estado.nomesNasTeclas;
  for (const [n, k] of teclas) {
    const r = m.get(n) ?? {};
    const fill = r.fill ?? (k.preta ? COR.preta : COR.branca);
    const stroke = r.stroke ?? COR.borda;
    const larg = r.stroke ? 3 : 1;
    const rotulo = r.rotulo ?? '';
    const acima = aviso && aviso.nota === n ? aviso.texto : '';
    // As oitavas repetem os nomes da oitava do dó central (note.60 a note.71).
    const nome = k.nome && comNomes ? t(`note.${60 + (n % 12)}`) : '';
    const chave = `${fill}|${stroke}|${rotulo}|${acima}|${nome}`;
    if (chave === k.ultimo) continue;
    k.ultimo = chave;
    k.r.setAttribute('fill', fill);
    k.r.setAttribute('stroke', stroke);
    k.r.setAttribute('stroke-width', larg);
    k.rotulo.textContent = rotulo;
    k.rotulo.setAttribute('fill', escuro(fill) ? '#fff' : '#111');
    k.acima.textContent = acima;
    if (k.nome) {
      k.nome.textContent = nome;
      k.nome.setAttribute('fill', fill === COR.branca ? '#9ca3af' : '#fcd34d');
    }
  }
}

function escuro(cor) {
  if (cor === COR.preta) return true;
  const m = cor.match(/\d+/g);
  return m && cor.startsWith('rgb') ? m.slice(0, 3).reduce((s, v) => s + Number(v), 0) < 380 : false;
}

// ---------- Partitura (protótipo, docs/09) ----------

const partitura = criarPartitura($('partitura'), COR);
const passosPorMusica = new Map();
let demoIndice = -1, demoDesde = 0, revelada = null;

function passosDe(musica) {
  if (!passosPorMusica.has(musica)) passosPorMusica.set(musica, JSON.parse(Motor.PassosDaMusica(musica)));
  return passosPorMusica.get(musica);
}

// De 0 (acabou de acertar a nota anterior) a 1 (tempo de ataque da nota da vez).
function progressoDoAtaque(e, agora) {
  return e.tAtaque === null || e.emAtaque ? 1 : Math.min(1, Math.max(0, (agora - e.tAcerto) / (e.tAtaque - e.tAcerto)));
}

function visaoDaPartitura(agora) {
  const e = estado;
  if (tela !== 'licao' || !e) return null;
  const [musica, trecho] = e.item.split('#');
  const { bpm, passos } = passosDe(musica);
  const frase = trecho ? passos.filter((p) => p.trecho === Number(trecho)) : passos;
  const cadeia = e.frases ? passos.filter((p) => p.trecho <= e.frases) : passos;
  const lista = e.juntando ? cadeia : frase;
  const visao = (seq, aberta, atual, progresso = 0, chegando = false) => ({
    id: `${musica}:${seq[0].indice}-${seq.at(-1).indice}`, seq, bpm, aberta, atual, progresso, chegando,
    revelar: revelada && agora < revelada.ate ? revelada : null,
  });

  switch (e.fase) {
    case 'Pose': return trecho ? visao(frase, true, 0) : null;
    case 'Demonstracao': {
      if (e.revisao) return null;
      const seq = e.juntando ? cadeia : e.etiqueta.tipo === 'MusicaInteira' ? passos : frase;
      const atual = Math.max(demoIndice, 0);
      const progresso = demoIndice < 0 ? 0 : Math.min(1, (agora - demoDesde) / seq[atual].duracaoMs);
      return visao(seq, true, atual, progresso);
    }
    case 'Guiada': {
      if (e.revisao) return null;
      const progresso = progressoDoAtaque(e, agora);
      // Como a tecla: a nota só vira âmbar no tempo de ataque.
      return { ...visao(frase, true, e.passo, progresso, true), antecipando: progresso < 1 };
    }
    case 'Transicao': return visao(lista, false, e.juntando ? 0 : -1);
    case 'Memoria':
    case 'Juntar':
    case 'RetornoDeErro': return visao(lista, false, e.passo);
    case 'Resultado': return visao(lista, true, lista.length);
    default: return null;
  }
}

// O tempo da lição anda por timer, para não parar quando o navegador deixa de desenhar; o desenho segue o quadro.
function tick() {
  if (tela === 'licao' && estado && FASES_COM_TICK.has(estado.fase)) entrada('tick');
}

function quadro() {
  const agora = performance.now();
  desenharTeclas(agora);
  partitura.atualizar(visaoDaPartitura(agora));
  // Mão branca na pose, na demonstração e na vez guiada, onde aponta o dedo da vez; some em fade antes da memória.
  const guiada = estado?.fase === 'Guiada';
  const comMao = tela === 'licao' && (guiada || estado?.fase === 'Pose' || estado?.fase === 'Demonstracao');
  const destaque = guiada ? { dedo: estado.alvo.dedo, forca: DEDO_INICIAL + (1 - DEDO_INICIAL) * progressoDoAtaque(estado, agora) } : null;
  mao.atualizar(comMao, new Set(soando.values()), estado?.ancora ?? 0, { do: teclas.get(60).r.getBoundingClientRect() }, agora, destaque);
  requestAnimationFrame(quadro);
}

// ---------- Teclado do computador ----------

const MAPA = { a: 60, w: 61, s: 62, e: 63, d: 64, f: 65, t: 66, g: 67, y: 68, h: 69, u: 70, j: 71, k: 72 };
document.addEventListener('keydown', (ev) => {
  if (ev.repeat || ev.target.matches('input, select, textarea') || $('facilitador').open) return;
  const n = MAPA[ev.key.toLowerCase()];
  if (n) { noteOn(n, 80, ev.timeStamp, 'computador'); return; }
  const b = botoes[Number(ev.key) - 1];
  if (b) b.acao();
});
document.addEventListener('keyup', (ev) => {
  const n = MAPA[ev.key.toLowerCase()];
  if (n) noteOff(n, 'computador');
});

// Tirar o headset vira trocar de aba: a lição pausa (Foco).
document.addEventListener('visibilitychange', () => {
  if (document.hidden && tela === 'licao' && estado) entrada('foco');
});

// ---------- Facilitador ----------

$('form-entrada').onsubmit = (ev) => { ev.preventDefault(); entrar($('nome').value); };
$('abrir-facilitador').onclick = () => {
  $('fac-dia').textContent = meta.diaOffset ? t('web.fac.dayShifted', { day: hoje, n: meta.diaOffset }) : t('web.fac.day', { day: hoje });
  $('fac-abrir-todas').checked = !!meta.abrirTodas;
  $('facilitador').showModal();
};
function mudarDia(offset) {
  if (tela === 'licao' && estado) entrada('sair');
  meta.diaOffset = offset;
  gravar(`meta:${participante}`, meta);
  $('facilitador').close();
  abrirDia();
}
$('fac-mais-um-dia').onclick = () => mudarDia((meta.diaOffset ?? 0) + 1);
$('fac-dia-real').onclick = () => mudarDia(0);
$('fac-abrir-todas').onchange = (ev) => {
  meta.abrirTodas = ev.target.checked;
  gravar(`meta:${participante}`, meta);
  if (tela === 'entre') mostrarEntre();
};
$('fac-execucoes').onclick = () => baixarCsv('execucoes.csv', todos('execucoes'), [
  'participante', 'dia', 'item', 'tipo', 'inicio', 'fim', 'duracaoS', 'concluiu', 'estrelas', 'demos', 'voltas',
  'voltasComErro', 'errosGuiada', 'tentativas', 'falhas', 'dicas', 'voltouAoGuiado', 'falhasJuntar', 'juntouAte', 'ritmoNoTempo', 'ritmoAvaliadas',
]);
$('fac-eventos').onclick = () => baixarCsv('eventos.csv', todos('eventos'),
  ['quando', 'tMs', 'participante', 'dia', 'item', 'fase', 'tipo', 'dados']);
$('fac-apagar').onclick = () => {
  if (!confirm(t('web.fac.confirmErase', { name: participante }))) return;
  if (tela === 'licao' && estado) entrada('sair');
  try { localStorage.removeItem(`${PREFIXO}progresso:${participante}`); } catch { /* nada a apagar */ }
  meta = { diaOffset: 0, abrirTodas: false, ultimoItem: null };
  gravar(`meta:${participante}`, meta);
  $('facilitador').close();
  abrirDia();
};
$('fac-trocar').onclick = () => {
  if (tela === 'licao' && estado) entrada('sair');
  $('facilitador').close();
  participante = null;
  $('quem').textContent = '';
  $('abrir-facilitador').hidden = true;
  tela = 'entrada';
  desenharTela();
};

// ---------- Início ----------

const erros = JSON.parse(Motor.ValidarConteudo());
if (erros.length) console.error('Conteúdo inválido', erros);
construirTeclado();
status(t('midi.searching'));
iniciarMidi();
desenharTela();
setInterval(tick, 10);
requestAnimationFrame(quadro);
