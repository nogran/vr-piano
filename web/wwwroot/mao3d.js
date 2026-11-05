// Mão branca em 3D (docs/03 §4). A forma é a malha de mão genérica do WebXR (@webxr-input-profiles/assets, MIT):
// anatomia de mão real, pele lisa e um esqueleto com uma junta por falange. O app só dobra as juntas para a pose de
// tocar. Fica por cima do teclado da tela, um dedo por tecla, com o polegar na tecla da âncora que o domínio calcula
// (0 = dó central). Unidade do mundo: a largura de uma tecla branca (23,5 mm). x para a direita (agudos), y para cima,
// z para quem toca; os dedos apontam para -z.
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

const MALHA = 'https://cdn.jsdelivr.net/npm/@webxr-input-profiles/assets@1.0.19/dist/profiles/generic-hand/right.glb';
const TECLA_M = 0.0235;
const FADE_MS = 400;               // P-35
const DESCIDA_MS = 120;            // tempo do dedo descer ou subir (docs/05 §7)
const DESLIZE_MS = 200;            // tempo da mão andar uma tecla branca (RF-64)
const INCLINACAO = 50 * Math.PI / 180; // olhar de quem toca, de cima e de trás
const COR_ATIVA = new THREE.Color('#f59e0b');
const ALTURA_DOS_NOS = 1.75;       // nós dos dedos acima da tecla, em teclas
const PONTA = 0.32;                // meia espessura da ponta do dedo
const APERTO = 0.2;                // quanto a junta da base dobra a mais para apertar (rad)

// Acabamento escolhido entre as opções testadas: cinza-claro fosco com contorno mais escuro.
const ESTILO = { cor: '#e9ecf1', aspereza: 0.85, borda: '#6b7487', forcaBorda: 0.55 };

const DEDOS = [
  { dedo: 1, nome: 'thumb', alvoX: 0 },
  { dedo: 2, nome: 'index-finger', alvoX: 1 },
  { dedo: 3, nome: 'middle-finger', alvoX: 2 },
  { dedo: 4, nome: 'ring-finger', alvoX: 3 },
  { dedo: 5, nome: 'pinky-finger', alvoX: 4 },
];

// Material da biblioteca com uma borda (fresnel) e a cor ativa por dedo, pesada pelos ossos de cada vértice.
function material(ativos) {
  const m = new THREE.MeshStandardMaterial({ color: ESTILO.cor, roughness: ESTILO.aspereza, metalness: 0 });
  m.onBeforeCompile = (s) => {
    s.uniforms.ativos = { value: ativos };
    s.uniforms.corAtiva = { value: COR_ATIVA };
    s.uniforms.corBorda = { value: new THREE.Color(ESTILO.borda) };
    s.uniforms.forcaBorda = { value: ESTILO.forcaBorda };
    s.vertexShader = s.vertexShader
      .replace('#include <common>', '#include <common>\nuniform float ativos[32];\nvarying float vAtivo;')
      .replace('#include <skinning_vertex>', `#include <skinning_vertex>
        vAtivo = ativos[int(skinIndex.x)] * skinWeight.x + ativos[int(skinIndex.y)] * skinWeight.y
               + ativos[int(skinIndex.z)] * skinWeight.z + ativos[int(skinIndex.w)] * skinWeight.w;`);
    s.fragmentShader = s.fragmentShader
      .replace('#include <common>', '#include <common>\nuniform vec3 corAtiva; uniform vec3 corBorda; uniform float forcaBorda;\nvarying float vAtivo;')
      .replace('#include <color_fragment>', '#include <color_fragment>\ndiffuseColor.rgb = mix(diffuseColor.rgb, corAtiva, clamp(vAtivo, 0.0, 1.0));')
      .replace('#include <dithering_fragment>', `
        float fres = pow(1.0 - clamp(abs(dot(normalize(normal), isOrthographic ? vec3(0.0, 0.0, 1.0) : normalize(vViewPosition))), 0.0, 1.0), 2.5);
        gl_FragColor.rgb = mix(gl_FragColor.rgb, corBorda, fres * forcaBorda * (1.0 - clamp(vAtivo, 0.0, 1.0)));
        #include <dithering_fragment>`);
  };
  return m;
}

// A malha tem poucos polígonos e as pontas dos dedos ficam facetadas de perto. Divide cada triângulo em quatro e
// arredonda os pontos novos pelas normais (tesselação de Phong), levando junto os pesos dos ossos.
function suavizar(geo) {
  const P = geo.attributes.position, N = geo.attributes.normal, I = geo.attributes.skinIndex, W = geo.attributes.skinWeight;
  const pos = [], nor = [], ind = [], pes = [];
  const a = new THREE.Vector3(), b = new THREE.Vector3(), na = new THREE.Vector3(), nb = new THREE.Vector3();
  const influencias = (v) => [[I.getX(v), W.getX(v)], [I.getY(v), W.getY(v)], [I.getZ(v), W.getZ(v)], [I.getW(v), W.getW(v)]];
  for (let v = 0; v < P.count; v++) {
    pos.push(P.getX(v), P.getY(v), P.getZ(v));
    nor.push(N.getX(v), N.getY(v), N.getZ(v));
    influencias(v).forEach(([i, w]) => { ind.push(i); pes.push(w); });
  }
  const meios = new Map();
  function meio(u, v) {
    const chave = u < v ? `${u}_${v}` : `${v}_${u}`;
    if (meios.has(chave)) return meios.get(chave);
    a.fromBufferAttribute(P, u); b.fromBufferAttribute(P, v);
    na.fromBufferAttribute(N, u); nb.fromBufferAttribute(N, v);
    const m = a.clone().add(b).multiplyScalar(0.5);
    const projA = m.clone().addScaledVector(na, -m.clone().sub(a).dot(na));
    const projB = m.clone().addScaledVector(nb, -m.clone().sub(b).dot(nb));
    const p = m.clone().multiplyScalar(0.25).add(projA.add(projB).multiplyScalar(0.375));
    const n = na.clone().add(nb).normalize();
    const soma = new Map();
    for (const [i, w] of [...influencias(u), ...influencias(v)]) soma.set(i, (soma.get(i) ?? 0) + w / 2);
    const quatro = [...soma].sort((x, y) => y[1] - x[1]).slice(0, 4);
    while (quatro.length < 4) quatro.push([0, 0]);
    const total = quatro.reduce((t, x) => t + x[1], 0);
    const novo = pos.length / 3;
    pos.push(p.x, p.y, p.z); nor.push(n.x, n.y, n.z);
    quatro.forEach(([i, w]) => { ind.push(i); pes.push(w / total); });
    meios.set(chave, novo);
    return novo;
  }
  const tris = [], idx = geo.index;
  for (let t = 0; t < idx.count; t += 3) {
    const [x, y, z] = [idx.getX(t), idx.getX(t + 1), idx.getX(t + 2)];
    const [xy, yz, zx] = [meio(x, y), meio(y, z), meio(z, x)];
    tris.push(x, xy, zx, xy, y, yz, zx, yz, z, xy, yz, zx);
  }
  const g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
  g.setAttribute('normal', new THREE.Float32BufferAttribute(nor, 3));
  g.setAttribute('skinIndex', new THREE.Uint16BufferAttribute(ind, 4));
  g.setAttribute('skinWeight', new THREE.Float32BufferAttribute(pes, 4));
  g.setIndex(tris);
  return g;
}

export function criarMao(container) {
  const canvas = document.createElement('canvas');
  canvas.className = 'mao3d';
  canvas.style.display = 'none';
  container.append(canvas);
  const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
  renderer.setClearColor(0x000000, 0);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  const cena = new THREE.Scene();
  cena.add(new THREE.HemisphereLight(0xffffff, 0x8a8f99, 1.6));
  const sol = new THREE.DirectionalLight(0xffffff, 1.6);
  sol.position.set(-2, 6, 4);
  cena.add(sol);
  const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, -50, 50);
  camera.position.set(0, Math.sin(INCLINACAO) * 20, Math.cos(INCLINACAO) * 20);
  camera.lookAt(0, 0, 0);
  camera.updateMatrixWorld();

  // Sombra macia no teclado.
  const tex = (() => {
    const c = document.createElement('canvas'); c.width = c.height = 128;
    const g = c.getContext('2d');
    const r = g.createRadialGradient(64, 64, 0, 64, 64, 64);
    r.addColorStop(0, 'rgba(0,0,0,0.45)'); r.addColorStop(1, 'rgba(0,0,0,0)');
    g.fillStyle = r; g.fillRect(0, 0, 128, 128);
    return new THREE.CanvasTexture(c);
  })();
  const sombra = new THREE.Mesh(new THREE.PlaneGeometry(6.5, 6), new THREE.MeshBasicMaterial({ map: tex, transparent: true, depthWrite: false }));
  sombra.rotation.x = -Math.PI / 2;
  sombra.position.set(2.0, 0.01, 0.6);
  cena.add(sombra);

  const ativos = new Array(32).fill(0);
  const aperto = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 };
  let pronta = null; // { posar(), deslizar(), ancora }
  let deslize = 0;   // onde a mão está agora, em teclas brancas a partir do dó central

  new GLTFLoader().load(MALHA, (gltf) => {
    const raiz = gltf.scene;
    const malha = raiz.getObjectByProperty('type', 'SkinnedMesh');
    const ossos = malha.skeleton.bones;
    malha.geometry = suavizar(suavizar(malha.geometry));
    malha.material = material(ativos);
    malha.frustumCulled = false;
    // Eixos da malha: dedos para -y, do indicador ao mínimo em +z, dorso em +x. No mundo: dedos para -z, mínimo à
    // direita, dorso para cima.
    raiz.quaternion.setFromRotationMatrix(new THREE.Matrix4().makeBasis(
      new THREE.Vector3(0, 1, 0), new THREE.Vector3(0, 0, 1), new THREE.Vector3(1, 0, 0)));
    raiz.scale.setScalar(1 / TECLA_M);
    cena.add(raiz);

    // Os ossos desta malha não formam cadeia: todos são filhos diretos da armação. A cadeia de cada dedo é feita
    // aqui: girar uma junta leva junto as juntas seguintes. Tudo no espaço da malha.
    const osso = (n) => raiz.getObjectByName(n);
    const cadeia = (d) => (d.dedo === 1
      ? ['thumb-metacarpal', 'thumb-phalanx-proximal', 'thumb-phalanx-distal', 'thumb-tip']
      : ['phalanx-proximal', 'phalanx-intermediate', 'phalanx-distal', 'tip'].map((n) => `${d.nome}-${n}`)).map(osso);
    const repouso = new Map(ossos.map((o) => [o, { p: o.position.clone(), q: o.quaternion.clone() }]));
    const DOBRA = new THREE.Vector3(0, 0, 1);  // dobrar leva a ponta para a palma (-x)
    const ABRIR = new THREE.Vector3(1, 0, 0);  // abrir e fechar os dedos no plano da palma
    const ROLAR = new THREE.Vector3(0, 1, 0);
    // giros[k]: lista de [eixo, ângulo] aplicada na junta k (eixos no referencial do dedo em repouso).
    function aplicarCadeia(d, giros) {
      const os = cadeia(d);
      const T = new THREE.Matrix4();
      const qT = new THREE.Quaternion();
      os.forEach((o, k) => {
        const r = repouso.get(o);
        const pos = r.p.clone().applyMatrix4(T);
        o.position.copy(pos);
        for (const [eixo, ang] of giros[k] ?? []) {
          const R = new THREE.Quaternion().setFromAxisAngle(eixo.clone().applyQuaternion(qT).normalize(), ang);
          const M = new THREE.Matrix4().makeTranslation(pos.x, pos.y, pos.z)
            .multiply(new THREE.Matrix4().makeRotationFromQuaternion(R))
            .multiply(new THREE.Matrix4().makeTranslation(-pos.x, -pos.y, -pos.z));
          T.premultiply(M);
          qT.premultiply(R);
        }
        o.quaternion.copy(qT).multiply(r.q);
      });
      raiz.updateMatrixWorld(true);
    }
    const ponta = (d) => cadeia(d)[3].getWorldPosition(new THREE.Vector3());
    function bissecao(lo, hi, f) { // f crescente; acha o zero
      for (let i = 0; i < 30; i++) { const m = (lo + hi) / 2; if (f(m) > 0) hi = m; else lo = m; }
      return (lo + hi) / 2;
    }

    // Altura: nós dos dedos a ALTURA_DOS_NOS acima da tecla; o nó do médio sobre o mi.
    raiz.updateMatrixWorld(true);
    const no = osso('middle-finger-phalanx-proximal').getWorldPosition(new THREE.Vector3());
    raiz.position.set(2 - no.x, ALTURA_DOS_NOS - no.y, 0);
    raiz.updateMatrixWorld(true);

    // Para cada dedo: dobra (base, meio, ponta na proporção 1 : 1,6 : 0,7) até a ponta encostar na tecla, e abre
    // até a ponta ficar no centro da sua tecla. O polegar fecha em direção ao indicador e desce rolando.
    const pose = new Map();
    function aplicar(d, base, abrir) {
      if (d.dedo === 1) aplicarCadeia(d, [[[ABRIR, abrir], [ROLAR, base]], [[DOBRA, -0.25]], [[DOBRA, -0.2]]]);
      else aplicarCadeia(d, [[[ABRIR, abrir], [DOBRA, -base]], [[DOBRA, -base * 1.6 - 0.35]], [[DOBRA, -base * 0.7 - 0.15]]]);
    }
    // Ajusta um ângulo até f(ângulo) zerar, testando os dois sentidos.
    const resolver = (lo, hi, f) => {
      const a = bissecao(lo, hi, f);
      return Math.abs(f(a)) < 0.05 ? a : bissecao(lo, hi, (x) => -f(x));
    };
    for (const d of DEDOS) {
      let base = 0.3, abrir = 0;
      for (let k = 0; k < 4; k++) {
        base = resolver(-1.2, 1.4, (b) => { aplicar(d, b, abrir); return PONTA - ponta(d).y; });
        abrir = resolver(-1.2, 1.2, (a) => { aplicar(d, base, a); return ponta(d).x - d.alvoX; });
      }
      pose.set(d.dedo, { base, abrir });
      aplicar(d, base, abrir);
    }

    // Ossos de cada dedo, para pintar o dedo da vez.
    const ossosDoDedo = new Map(DEDOS.map((d) => [d.dedo, ossos
      .map((o, i) => (o.name.startsWith(d.nome) && (d.dedo === 1 || !o.name.endsWith('metacarpal')) ? i : -1))
      .filter((i) => i >= 0)]));
    const meio = ponta(DEDOS[2]);
    const xDaRaiz = raiz.position.x, xDaSombra = sombra.position.x;
    pronta = {
      ancora: new THREE.Vector3(meio.x, 0, meio.z),
      deslizar(x) {
        raiz.position.x = xDaRaiz + x;
        sombra.position.x = xDaSombra + x;
      },
      posar() {
        for (const d of DEDOS) {
          const p = pose.get(d.dedo);
          aplicar(d, p.base + APERTO * aperto[d.dedo], p.abrir);
          for (const i of ossosDoDedo.get(d.dedo)) ativos[i] = aperto[d.dedo];
        }
      },
    };
  });

  let opacidade = 0, ultimo = performance.now(), larguraVista = 0, alturaVista = 0;
  return {
    // visivel: a mão deve estar na tela; dedos: os dedos (1 a 5) apertando agora; ancora: tecla branca do polegar,
    // calculada no domínio; teclas: { do: retângulo da tecla do dó central na tela }.
    atualizar(visivel, dedos, ancora, teclas, agora) {
      const dt = Math.min(100, agora - ultimo); ultimo = agora;
      const alvoOpacidade = visivel && pronta ? 1 : 0;
      opacidade += Math.sign(alvoOpacidade - opacidade) * Math.min(Math.abs(alvoOpacidade - opacidade), dt / FADE_MS);
      for (let f = 1; f <= 5; f++) {
        const alvo = dedos.has(f) ? 1 : 0;
        aperto[f] += Math.sign(alvo - aperto[f]) * Math.min(Math.abs(alvo - aperto[f]), dt / DESCIDA_MS);
      }
      // Escondida, ela já espera na âncora; visível, desliza até lá.
      deslize = opacidade === 0 ? ancora : deslize + Math.sign(ancora - deslize) * Math.min(Math.abs(ancora - deslize), dt / DESLIZE_MS);
      if (opacidade === 0) { canvas.style.display = 'none'; return; }
      canvas.style.display = '';

      // A tela define a escala: uma tecla branca do mundo tem a largura da tecla desenhada.
      const caixa = container.getBoundingClientRect();
      const k = teclas.do.width;
      const W = Math.round(k * 9), H = Math.round(teclas.do.height * 1.9);
      const esquerda = teclas.do.left - caixa.left - k * 2;
      const topo = teclas.do.top - caixa.top - teclas.do.height * 0.2;
      if (W !== larguraVista || H !== alturaVista) {
        renderer.setPixelRatio(window.devicePixelRatio || 1);
        renderer.setSize(W, H, false);
        canvas.style.width = W + 'px'; canvas.style.height = H + 'px';
        larguraVista = W; alturaVista = H;
      }
      canvas.style.left = esquerda + 'px'; canvas.style.top = topo + 'px';
      // A ponta do dedo médio cai a 60% do comprimento da tecla mi.
      const a = pronta.ancora.clone().applyMatrix4(camera.matrixWorldInverse);
      const px = teclas.do.left + teclas.do.width / 2 - caixa.left - esquerda + 2 * k;
      const py = teclas.do.top + teclas.do.height * 0.6 - caixa.top - topo;
      camera.left = a.x - px / k; camera.right = camera.left + W / k;
      camera.top = a.y + py / k; camera.bottom = camera.top - H / k;
      camera.updateProjectionMatrix();

      pronta.deslizar(deslize);
      pronta.posar();
      renderer.render(cena, camera);

      // Degradê do pulso, medido na tela.
      const yDe = (z) => (1 - new THREE.Vector3(2, 1.2, z).project(camera).y) / 2 * H;
      const mascara = `linear-gradient(to bottom, #000 ${yDe(pronta.ancora.z + 4.8)}px, transparent ${yDe(pronta.ancora.z + 6.4)}px)`;
      canvas.style.maskImage = mascara; canvas.style.webkitMaskImage = mascara;
      canvas.style.opacity = opacidade;
    },
  };
}
