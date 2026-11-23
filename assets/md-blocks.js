// 프로젝트 문서 md 해석 공용 — doc.html(프로젝트 상세 문서) · project.html(슬라이드)가 common.js 다음에 싣습니다.
//   하는 일: marked 12.0.2(gfm, PF.mdHtml — 한국어 굵은 글씨 보완 포함) → 비활성 <template> → 상대경로(그림·영상) → 제목 slug(PF.slugger) → 제목의 조상 목록 → 원문 블록 찾기
//   <template>에 넣는 이유: 그 안의 내용은 비활성이라 문서 속 이미지(GIF 수십 MB)를 받지 않음(PF.headingsOf와 같은 방식)
//   원문 블록 참조 규칙은 .claude/skills/portfolio-build/schema.md 「슬라이드」: 절 경로(sectionPath) → 그 절 범위(다음 제목 전까지) → kind별 n번째, 지문(fp) 대조
//   이 파일은 글을 만들지 않습니다. 원문 블록을 찾아 그대로 돌려줄 뿐입니다.
(() => {
  const isRelative = (u) => u && !/^([a-z][a-z0-9+.-]*:|#|\/)/i.test(u);
  const decode = (u) => { try { return decodeURI(u); } catch (_) { return u; } };

  // md → { frag, headings: [{ el, level, text, id, parents }] }. 상대경로는 src/<폴더>/… 로 바꾸고 원래 값은 data-md-src에 둠
  function parse(md, id) {
    // CDN이 막혀 marked를 받지 못했으면 이유를 말하고 멈춤(부르는 쪽이 안내 화면으로 바꿈)
    if (!window.marked) throw new Error('Markdown 해석 라이브러리(marked)를 불러오지 못했습니다 — 네트워크에서 cdn.jsdelivr.net에 연결되는지 확인해 주세요');
    const tpl = document.createElement('template');
    tpl.innerHTML = PF.mdHtml(md);
    const frag = tpl.content;
    frag.querySelectorAll('img[src], video[src], source[src]').forEach((el) => {
      const s = el.getAttribute('src');
      if (isRelative(s)) { el.dataset.mdSrc = s; el.setAttribute('src', PF.srcPath(id, decode(s))); }
    });
    frag.querySelectorAll('video[poster]').forEach((el) => { const s = el.getAttribute('poster'); if (isRelative(s)) el.setAttribute('poster', PF.srcPath(id, decode(s))); });
    // 제목 앵커(GitHub 규칙). 링크 분류·각주가 섞이기 전의 원문 제목 글자로 계산(첫 H1 포함 같은 순서)
    const slug = PF.slugger();
    const stack = [];
    const headings = [...frag.querySelectorAll('h1, h2, h3, h4, h5, h6')].map((el) => {
      const text = el.textContent.trim();
      const h = { el, level: +el.tagName[1], text, id: slug(text) };
      el.id = h.id;
      while (stack.length && stack[stack.length - 1].level >= h.level) stack.pop();
      h.parents = stack.slice();
      stack.push(h);
      return h;
    });
    return { frag, headings, md, index: null };
  }

  // 제목마다 그 절 범위(다음 제목 전까지, 수준 무관)의 블록 목록. 표 안 이미지는 표째로만
  function index(P) {
    if (P.index) return P.index;
    const map = new Map();
    const bucket = () => ({ mermaid: [], code: [], table: [], image: [] });
    let cur = bucket();
    map.set(null, cur);
    const byEl = new Map(P.headings.map((h) => [h.el, h]));
    const w = document.createTreeWalker(P.frag, NodeFilter.SHOW_ELEMENT);
    for (let el = w.nextNode(); el; el = w.nextNode()) {
      if (byEl.has(el)) { cur = bucket(); map.set(byEl.get(el), cur); continue; }
      if (el.tagName === 'PRE') {
        const code = el.querySelector(':scope > code');
        if (code && /\blanguage-mermaid\b/.test(code.className)) cur.mermaid.push(el); else cur.code.push(el);
      } else if (el.tagName === 'TABLE') cur.table.push(el);
      else if (el.tagName === 'IMG' && !el.closest('table')) cur.image.push(el);
    }
    return (P.index = map);
  }

  // 절 경로 → 일치하는 제목 목록(정확히 일치가 있으면 그것만, 없으면 앞부분 일치). 앞 칸들은 조상에 순서대로 있어야 함
  const tier = (t, want) => (t === want ? 2 : t && want && (t.startsWith(want) || want.startsWith(t)) ? 1 : 0);
  function findSection(P, path) {
    const list = (Array.isArray(path) ? path : [path]).map(PF.normTitle).filter(Boolean);
    if (!list.length) return [];
    const last = list[list.length - 1], anc = list.slice(0, -1);
    let cands = P.headings.map((h) => [h, tier(PF.normTitle(h.text), last)]).filter(([, t]) => t);
    const best = Math.max(0, ...cands.map(([, t]) => t));
    cands = cands.filter(([, t]) => t === best).map(([h]) => h);
    return cands.filter((h) => {
      let i = 0;
      for (const a of h.parents) if (i < anc.length && tier(PF.normTitle(a.text), anc[i])) i++;
      return i === anc.length;
    });
  }

  // 지문: code·mermaid = 첫 번째 비어 있지 않은 줄(60자), table = 머리 칸 글자를 |로
  const squash = (s) => String(s || '').replace(/\s+/g, ' ').trim();
  function fpOf(el, kind) {
    if (kind === 'table') { const row = el.querySelector('tr'); return row ? squash([...row.children].map((c) => squash(c.textContent)).join('|')) : ''; }
    const line = (el.textContent || '').split('\n').map((x) => x.trim()).find((x) => x && !(kind === 'mermaid' && x.startsWith('%%'))) || '';   // mermaid는 %%{init…}%% 지시 줄을 건너뜀
    return squash(line).slice(0, 60);
  }
  const fpSame = (a, b, kind) => { const x = squash(a), y = squash(b); return kind === 'table' ? x === y : x.slice(0, 60) === y.slice(0, 60); };
  const normSrc = (s) => decode(String(s || '')).replace(/^\.\//, '');

  // 원문 블록 하나 찾기. ref = { kind, n?, src?, fp?, sectionPath? }, path = 장면의 sectionPath
  //   → { status: 'ok'|'missing'|'ambiguous'|'changed', el?, heading?, detail }
  function pick(P, ref, path) {
    const where = ref.sectionPath && ref.sectionPath.length ? ref.sectionPath : path;
    const hs = findSection(P, where);
    const label = (Array.isArray(where) ? where : [where]).filter(Boolean).join(' › ');
    if (hs.length === 0) return { status: 'missing', detail: `절 "${label}"` };
    if (hs.length > 1) return { status: 'ambiguous', detail: `절 "${label}" ${hs.length}곳` };
    const h = hs[0], b = index(P).get(h);
    const kind = ref.kind;
    if (!b || !b[kind]) return { status: 'missing', heading: h, detail: `${kind} 종류 없음` };
    let list = b[kind];
    if (kind === 'image') {
      list = list.filter((i) => normSrc(i.dataset.mdSrc || i.getAttribute('src')) === normSrc(ref.src));
      if (list.length > 1 && !ref.n) return { status: 'ambiguous', heading: h, detail: `이미지 ${ref.src} ${list.length}곳` };
    }
    const el = list[(ref.n || 1) - 1];
    if (!el) return { status: 'missing', heading: h, detail: `${label}의 ${kind}${kind === 'image' ? ' ' + ref.src : ' ' + (ref.n || 1) + '번째'}` };
    if (ref.fp && kind !== 'image' && !fpSame(fpOf(el, kind), ref.fp, kind)) return { status: 'changed', el, heading: h, detail: `${label}의 ${kind} ${ref.n || 1}번째: "${fpOf(el, kind)}"` };
    return { status: 'ok', el, heading: h };
  }

  // 그 절 범위의 첫 그림(이미지 → mermaid → 코드 순) — 예전 데이터 자동 구성용
  function firstVisual(P, h) {
    const b = index(P).get(h);
    if (!b) return null;
    if (b.image[0]) return { kind: 'image', el: b.image[0], src: b.image[0].dataset.mdSrc || b.image[0].getAttribute('src') };
    if (b.mermaid[0]) return { kind: 'mermaid', el: b.mermaid[0], n: 1 };
    if (b.code[0]) return { kind: 'code', el: b.code[0], n: 1 };
    return null;
  }

  // Mermaid 초기화(상세 문서·발표 덱 공용) — ELK 레이아웃(@mermaid-js/layout-elk 0.1.7, mermaid 11.4.1과 같은 날 배포)을 등록하고 기본 레이아웃으로 씀.
  //   ELK는 flowchart·stateDiagram에 적용되고(sequenceDiagram 등은 원래 방식), 받지 못하면 기본(dagre) 레이아웃으로 그림.
  //   다이어그램 하나만 dagre로 되돌리려면 원문 mermaid 첫 줄 앞에 %%{init: {"layout": "dagre"}}%% 를 넣음.
  const ELK_URL = 'https://cdn.jsdelivr.net/npm/@mermaid-js/layout-elk@0.1.7/dist/mermaid-layout-elk.esm.min.mjs';
  let elkReady = null;
  function loadElk() {
    if (!elkReady) {
      elkReady = (window.mermaid?.registerLayoutLoaders ? import(ELK_URL) : Promise.reject(new Error('registerLayoutLoaders 없음')))
        .then((m) => { mermaid.registerLayoutLoaders(m.default); return true; })
        .catch((err) => { console.warn('[mermaid] ELK 레이아웃을 불러오지 못해 기본 레이아웃으로 그립니다:', err); return false; });
    }
    return elkReady;
  }
  let baseOpts = {};
  const mmdConfig = (elk, extra) => ({ startOnLoad: false, theme: 'default', securityLevel: 'loose', fontFamily: 'Pretendard, sans-serif', ...(elk ? { layout: 'elk' } : {}), ...baseOpts, ...extra });
  async function initMermaid(opts = {}) {
    const elk = await loadElk();
    baseOpts = opts;
    mermaid.initialize(mmdConfig(elk));
    return elk ? 'elk' : 'dagre';
  }

  // 다이어그램 하나에만 ELK 설정 주기 — 원문 mermaid 첫 줄 앞에 %%{init: {"elk": {"cycleBreakingStrategy": "MODEL_ORDER"}}}%%
  //   (MODEL_ORDER = 되돌아가는 화살표가 있어도 적은 순서대로 위→아래로 놓음). mermaid 11.4.1은 지시 줄의 elk 설정을 ELK에 넘기지 않으므로,
  //   그 다이어그램을 그리는 동안만 전체 설정에 넣었다가 되돌림. 지문(fpOf)은 %% 줄을 건너뛰므로 슬라이드 참조는 그대로.
  function elkOf(text) {
    const m = /%%\{\s*init\s*:\s*([\s\S]*?)\}%%/.exec(String(text || ''));
    if (!m) return null;
    try { const o = JSON.parse(m[1]); return o && o.elk && typeof o.elk === 'object' ? o.elk : null; } catch { return null; }
  }
  async function withElk(text, fn) {
    const opts = elkOf(text);
    if (!opts) return fn();
    const elk = await loadElk();
    if (!elk) return fn();
    mermaid.initialize(mmdConfig(true, { elk: opts }));
    try { return await fn(); } finally { mermaid.initialize(mmdConfig(true)); }
  }

  PF.md = { parse, index, findSection, pick, fpOf, firstVisual, isRelative, decode, initMermaid, elkOf, withElk };
})();
