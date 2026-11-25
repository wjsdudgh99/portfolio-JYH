// 포트폴리오 페이지(index.html) — 아래로 읽는 한 페이지 + A4 인쇄(제출 PDF)
//   순서: 머리(이름·정체성·연락처) → 자기소개 → 기술 스택 → 프로젝트마다 소개 + 기능 소개 → 한눈에 보기 → 연락처
//   기능 소개: 화면(그 기능 절의 이미지 highlights[].image만)은 왼쪽에 모으고, 기능 설명과 [슬라이드 열기]는 오른쪽 목록에 한 열로 맞춤
//   모든 글은 data/portfolio.json·data/projects/*.json 값 그대로입니다. 절 이름·버튼 같은 UI 문구는 문구 표(PF.t, assets/strings.js + theme/strings.tsv)에서 옵니다.
//   주소: #<절 id>(cover · intro · stack · p-<폴더> · p-<폴더>-core · glance · contact). 예전 포트폴리오 슬라이드 주소(#s=<장면 id> · #<번호>)는 같은 절로 넘깁니다.
//   점검: PFPage.whenReady() → PFPage.report() · ?pf-audit=1이면 #pf-audit에 report()의 base64 JSON(headless용, 슬라이드와 같은 형식)
(() => {
  const root = document.getElementById('app');
  const html = document.documentElement;
  const { esc, inline, isUrl, shortUrl, link, t } = PF;
  const T = (k, v) => esc(t(k, v));                                    // HTML에 넣는 문구
  const pad2 = (n) => String(n).padStart(2, '0');
  // 한눈에 보기 카드 그림을 프로젝트마다 고름(없으면 첫 기능 그림). 2026-11-25 수강생 요청: You're Right는 정답 클릭 GIF
  const GLANCE_PICK = { '1-3d-project-youre-right': 'images/answer_click.gif' };
  // 다룬 도구(2026-11-25 수강생 요청) — 프로젝트 문서의 "환경" 줄과 본문에 나온 도구만, 어느 프로젝트에서 썼는지와 함께. 고칠 때는 문서에서 근거를 확인하고 바꿀 것
  const YR = '1-3d-project-youre-right', SL = '2-2d-project-strikers-lite', CY = '3-2d-team-project-cy', MG = '4-minigolf-level-design';
  const TOOLS = [
    { group: '엔진 · 렌더링', items: [{ name: 'Unity 6', projects: [YR, SL, CY, MG] }, { name: 'URP · URP 2D', projects: [YR, SL, CY, MG] }] },
    { group: 'Unity 패키지 · 라이브러리', items: [{ name: 'Input System', projects: [YR, SL, MG] }, { name: 'TextMeshPro', projects: [SL, MG] },
      { name: 'UniTask', projects: [CY] }, { name: 'Newtonsoft.Json', projects: [CY] }, { name: 'WebSocket (자체 릴레이 서버)', projects: [CY] }] },
    { group: '레벨 제작', items: [{ name: 'ProBuilder', projects: [MG] }, { name: 'LDtk', projects: [CY] }] },
    { group: '협업', items: [{ name: 'Git · GitHub', projects: [YR, SL, CY, MG] }, { name: 'Notion (팀 노션)', projects: [CY, MG] }] },
  ];
  const info = { errors: [], sections: [], projects: 0 };
  html.dataset.pfReady = 'no';
  // 처음 주소로 도착한 절, 또는 새로고침 전 스크롤 위치. 글꼴·그림이 늦게 와 위치가 밀리면 settle()에서 한 번 더 맞춤(그사이 사용자가 움직이지 않았을 때만)
  let arrived = '', restoreY = null, userMoved = false;
  ['wheel', 'touchstart', 'keydown', 'pointerdown'].forEach((t) => addEventListener(t, () => { userMoved = true; }, { once: true, passive: true }));
  const POS = `pf-portfolio-y:${location.pathname}`;                 // 새로고침해도 읽던 곳으로(내용을 늦게 그려 브라우저가 스스로 되돌리지 못함)
  try { history.scrollRestoration = 'manual'; } catch { /* 그대로 */ }
  addEventListener('pagehide', () => { try { sessionStorage.setItem(POS, String(Math.round(scrollY))); } catch { /* 저장소 막힘 */ } });
  let resolveReady;
  const ready = new Promise((r) => { resolveReady = r; });
  window.PFPage = { whenReady: () => ready, report };

  start().catch((err) => {
    info.errors.push(String(err && err.stack || err));
    PF.notice(root, '페이지를 그리지 못했습니다', `<p>${esc(err && err.message || err)}</p>`);
  }).finally(settle);

  async function start() {
    if (PF.guardFileProtocol(root)) { info.errors.push('file:// — 로컬 서버로 열어야 합니다'); return; }
    let pf;
    const strings = PF.loadStrings();                                   // 실패해도 기본 문구(던지지 않음)
    try { pf = await PF.loadJSON('data/portfolio.json'); }
    catch (err) { info.errors.push(`data/portfolio.json: ${err && err.message || err}`); PF.noData(root, err); return; }
    PF.applyTheme(pf.settings?.accent);
    const projects = (await Promise.all((pf.projects || []).map((id) => PF.loadJSON(`data/projects/${encodeURIComponent(id)}.json`)
      .catch((err) => {                                                 // 그 프로젝트만 빼고 그리되, 점검(report().errors)에는 남김 — 조용히 사라지지 않게
        console.warn('[portfolio] 프로젝트 데이터 없음:', err.message);
        info.errors.push(`data/projects/${id}.json: ${err && err.message || err}`);
        return null;
      })))).filter(Boolean);
    await strings;
    render(pf, projects);
    bind();
    pager();
    // 새로고침 · 뒤로/앞으로 가기는 주소의 #절보다 마지막으로 보던 위치가 먼저(2026-11-25 수강생 요청: 문서를 보고 돌아오면 늘 첫 프로젝트로 가던 문제)
    //   — 주소의 #는 예전에 누른 메뉴 그대로 남아 있어, 그 뒤 스크롤·넘김으로 옮긴 곳과 다를 수 있음
    const navType = performance.getEntriesByType?.('navigation')[0]?.type;
    if (navType === 'reload' || navType === 'back_forward') {
      try { restoreY = sessionStorage.getItem(POS) === null ? null : +sessionStorage.getItem(POS); } catch { /* 저장소 막힘 */ }
    }
    if (restoreY !== null && Number.isFinite(restoreY)) scrollTo({ top: restoreY, behavior: 'instant' });
    else { restoreY = null; arrived = go(projects); }                 // 내용을 그린 뒤라 브라우저가 처음 주소의 절로 스스로 가지 못함
    addEventListener('hashchange', () => go(projects));                // 같은 페이지에서 예전 주소(#s=…)를 붙여 넣은 경우
  }

  function go(projects) {
    const want = legacyTarget(projects);
    const el = want && document.getElementById(want);
    if (!el) return '';
    if (location.hash !== `#${encodeURIComponent(want)}`) try { history.replaceState(null, '', `${location.pathname}${location.search}#${encodeURIComponent(want)}`); } catch { /* 그대로 */ }
    el.scrollIntoView({ behavior: 'instant', block: 'start' });        // 부드러운 스크롤은 가려진 창에서 멈춤 → 바로 이동
    return want;
  }

  // 예전 포트폴리오 슬라이드의 주소(#s=<장면 id> · #<번호> · #s-<번호> · 옛 랜딩 #skills 등) → 이 페이지의 절 id. 지금 주소가 절 id 그대로여도 돌려줌(그리고 나서 그 절로 스크롤)
  //   번호는 실제로 그린 절의 차례(예전 슬라이드도 그린 장면만 셈. 나뉜 장면 stack-2·glance-2는 이제 한 절). #0·#-3 → 첫 절, #99 → 마지막 절(슬라이드 키트와 같은 규칙)
  function legacyTarget(projects) {
    let h = location.hash.slice(1);
    try { h = decodeURIComponent(h); } catch { /* 그대로 */ }
    if (!h) return '';
    const nth = (n) => info.sections[Math.max(1, Math.min(info.sections.length, n)) - 1] || '';
    if (/^-?\d+$/.test(h)) return nth(+h);
    const m = /^s=(.+)$/.exec(h);
    if (m) h = m[1].replace(/^(stack|glance)-\d+$/, '$1');
    const sn = /^s-(\d+)$/.exec(h);
    if (sn) return nth(+sn[1]);
    return { skills: 'stack', projects: projects[0] ? `p-${projects[0].id}` : '', summary: 'glance' }[h] ?? h;
  }

  function render(pf, projects) {
    const prof = pf.profile || {};
    const byId = Object.fromEntries(projects.map((p) => [p.id, p]));
    const nameT = prof.name || t('name.portfolio');
    document.title = `${prof.name ? prof.name + ' · ' : ''}${t('name.portfolio')}`;
    document.querySelector('.skip-link')?.replaceChildren(t('ui.skip'));
    const total = pad2(projects.length);
    const btn = (href, label, kind, cls = 'btn') => link({ href, label, kind, cls });
    const profLinks = (prof.links || []).filter((l) => isUrl(l.url));
    // 인쇄(PDF)에서는 버튼 대신 칠 수 있는 주소를 보여 줌. rows: [이름, 보이는 주소, 링크, 종류] — PDF에서는 눌러도 열림(링크 규칙은 PF.link 그대로, ↗는 인쇄에서 숨김)
    const printUrls = (rows) => rows.length ? `<dl class="print-only purls">${rows.map(([k, u, href, kind]) => `<div><dt>${esc(k)}</dt><dd class="url">${href
      ? link({ href, label: esc(u), kind }) : esc(u)}</dd></div>`).join('')}</dl>` : '';
    const sec = (id, eyebrow, title, right, body, cls = '') => `<section id="${esc(id)}" class="sec${cls ? ' ' + cls : ''}"><div class="wrap">
      <div class="sec-head"><div><p class="eyebrow">${esc(eyebrow)}</p><h2>${esc(title)}</h2></div>${right ? `<p class="sec-r">${esc(right)}</p>` : ''}</div>${body}</div></section>`;
    const out = [];

    // ① 머리: 왼쪽 이름 · 영문 이름 · 한 줄 정체성 · 연락처, 오른쪽 게임 표지 2×2
    // (2026-11-25 수강생 요청: 첫 화면에서 게임이 바로 보이게)
    //   (2026-11-25 수강생 요청 2) 자기소개는 한 줄 정체성 바로 밑으로, 이메일 · GitHub는 게임 표지 4개 밑으로
    const heroShots = projects.filter((p) => p.cover).slice(0, 4);
    const heroBtns = `<div class="btns no-print">${[prof.email && link({ href: `mailto:${prof.email}`, label: esc(prof.email), kind: 'mail', cls: 'btn' }),
      isUrl(prof.github) && btn(prof.github, 'GitHub', 'external'), ...profLinks.map((l) => btn(l.url, esc(l.label), 'external'))].filter(Boolean).join('')}</div>`;
    // 기술 스택 — data의 문장 그대로, 근거 프로젝트 칩은 제목의 " — " 앞까지만
    const shortTitle = (p) => String(p.title || '').split(' — ')[0];
    const skillsHtml = !(pf.skills || []).length ? '' : `<div class="about-r"><div id="stack"><div class="stack-head"><div><p class="eyebrow">${T('en.skills')}</p><h2>${t('page.stack')}</h2></div><p class="sec-r">${t('page.stack-note')}</p></div>
        <ul class="skills2">${pf.skills.map((s) => `<li class="skill2"><h3>${esc(s.name)}</h3><ul>${(s.evidence || []).map((e) => `<li>${byId[e.project]
          ? link({ href: `#p-${e.project}`, label: esc(shortTitle(byId[e.project])), kind: 'anchor', cls: 'chip' }) : '<i></i>'}<span>${inline(e.text)}</span></li>`).join('')}</ul></li>`).join('')}</ul></div></div>`;
    out.push(`<section id="cover" class="hero"><p class="hero-mark no-print" aria-hidden="true">${esc(prof.nameEn || nameT)}</p><div class="wrap">
      <div class="hero-top"><p class="eyebrow">${T('en.portfolio')}</p>${pf.generatedAt ? `<p class="upd">${T('page.updated', { date: pf.generatedAt })}</p>` : ''}</div>
      <div class="hero-grid${heroShots.length ? '' : ' noshot'}"><div class="hero-l">
      <h1>${esc(nameT)}${prof.nameEn ? `<span class="en" lang="en">${esc(prof.nameEn)}</span>` : ''}</h1>
      ${prof.headline ? `<p class="headline">${inline(prof.headline)}</p>` : ''}
      ${prof.intro ? `<h2 class="pf-sr-only">${t('page.intro')}</h2><div class="about hero-about">${prof.intro.split(/\n+/).filter((l) => l.trim()).map((l) => `<p>${inline(l)}</p>`).join('')}</div>` : ''}
      ${heroShots.length ? '' : heroBtns}
      ${printUrls([prof.email && [t('page.print-email'), prof.email, `mailto:${prof.email}`, 'mail'], isUrl(prof.github) && ['GitHub', shortUrl(prof.github), prof.github, 'external'],
        ...profLinks.map((l) => [l.label, shortUrl(l.url), l.url, 'external']), [t('page.print-web'), PF.visibleUrl('')]].filter(Boolean))}
      </div>${heroShots.length ? `<div class="hero-r"><ul class="hero-shots">${heroShots.map((p) => `<li><a href="#p-${esc(p.id)}">
        <img src="${esc(PF.srcPath(p.id, p.cover))}" alt="${esc(p.title)} 대표 화면"><span>${esc(String(p.title || '').split(' — ')[0])}</span></a></li>`).join('')}</ul>${heroBtns}</div>` : ''}</div>
    </div></section>`);

    // ①-2 두 번째 화면: 왼쪽 기술 스택(근거 프로젝트 칩 + 문장), 오른쪽 다룬 도구(2026-11-25 수강생 요청 4)
    if (skillsHtml || TOOLS.length) out.push(`<section id="intro" class="sec about-sec"><div class="wrap"><div class="about-grid${TOOLS.length ? ' has-tools' : ''}">
      ${skillsHtml}${TOOLS.length ? `<div class="tools" id="tools"><div class="stack-head"><div><p class="eyebrow">TOOLS</p><h2>다룬 도구</h2></div></div>
        <ul class="tool-g">${TOOLS.map((g) => `<li><h3>${esc(g.group)}</h3><ul>${g.items.map((it) => {
          const used = it.projects.map((id) => byId[id]).filter(Boolean).map(shortTitle);
          return `<li title="${esc(used.join(' · '))}"><b>${esc(it.name)}</b><span>${esc(used.length === projects.length ? `프로젝트 ${used.length}개 모두` : used.join(' · '))}</span></li>`;
        }).join('')}</ul></li>`).join('')}</ul><p class="tool-note no-print">도구에 마우스를 올리면 쓴 프로젝트가 보입니다.</p></div>` : ''}
    </div></div></section>`);

    // ② 프로젝트 한눈에: 가로로 긴 카드 4줄(왼쪽 게임 화면 · 오른쪽 개인/팀 · 기간 · 한 줄 소개 · 문서의 장 · 링크). 카드 화면을 누르면 위 프로젝트 소개로
    //   (2026-11-25 수강생 요청: 연락처 바로 위로 옮김 — 프로젝트들을 다 본 뒤 정리)
    const glance = !projects.length ? '' : sec('glance', t('en.glance'), t('page.glance'), '', `<ul class="gcards">${projects.map((p) => {
      const pick = GLANCE_PICK[p.id], picked = pick && (p.highlights || []).find((h) => h.image?.src === pick)?.image;
      const shot = picked || (pick ? { src: pick } : null) || (p.highlights || []).find((h) => h.image?.src)?.image || (p.screenshots || [])[0] || (p.cover ? { src: p.cover } : null);
      const period = (p.meta || []).find((m) => m.label === '기간')?.value || '';
      const chs = ((p.deck && p.deck.chapters) || []).map((c) => String(c.title || '').split(' — ')[0].trim())
        .filter((c) => c && c !== '개요' && !c.startsWith('부록') && !c.startsWith('결과'));
      return `<li class="gcard">
        ${shot ? `<a class="gc-m" href="#p-${esc(p.id)}" aria-label="${esc(p.title)} 소개로"><img src="${esc(PF.srcPath(p.id, shot.src))}" alt=""${shot.w && shot.h ? ` width="${shot.w}" height="${shot.h}"` : ''}></a>` : ''}
        <div class="gc-b"><p class="gc-k">${esc([PF.teamLabel(p.team), period].filter(Boolean).join(' · '))}</p>
          <h3>${link({ href: `#p-${p.id}`, label: esc(p.title), kind: 'anchor' })}</h3>
          ${p.tagline ? `<p class="gc-t">${inline(p.tagline)}</p>` : ''}
          ${p.team?.role ? `<p class="g-role"><b>${T('name.role-short')}</b> ${inline(p.team.role)}</p>` : ''}
          <div class="gc-f">${chs.length ? `<ul class="gc-ch">${chs.map((c) => `<li>${esc(c)}</li>`).join('')}</ul>` : ''}
          <div class="g-l">${projLinks(p)}</div></div></div></li>`;
    }).join('')}</ul>`, 'tint');

    // ③ 프로젝트마다 소개 + 기능 소개
    projects.forEach((p, j) => {
      const no = t('page.project-no', { no: pad2(j + 1), total });
      out.push(projectIntro(p, no, sec, btn, printUrls));             // 소개 + 기능 소개를 한 화면에(2026-11-25 수강생 요청)
    });
    if (glance) out.push(glance);


    // ⑥ 연락처 + 프로젝트별 링크(그 밖의 링크는 주소까지)
    out.push(`<footer id="contact" class="sec contact"><div class="wrap">
      <p class="eyebrow">${T('en.contact')}</p>
      <div class="ct">
        <div class="ct-me"><p class="ct-name">${esc(nameT)}</p>
          ${prof.email ? `<p class="em">${link({ href: `mailto:${prof.email}`, label: esc(prof.email), kind: 'mail' })}</p>` : ''}
          ${isUrl(prof.github) ? `<p>${link({ href: prof.github, label: `<span class="url">${esc(shortUrl(prof.github))}</span>` })}</p>` : ''}
          ${profLinks.map((l) => `<p><span class="k">${esc(l.label)}</span> ${link({ href: l.url, label: `<span class="url">${esc(shortUrl(l.url))}</span>` })}</p>`).join('')}
          <p class="print-only"><span class="url">${esc(PF.visibleUrl(''))}</span></p></div>
        <div class="ct-lk">${projects.map((p) => `<div><b>${esc(p.title)}</b><span class="row">${projLinks(p)}</span>${(p.links?.extra || []).filter((l) => isUrl(l.url))
          .map((l) => `<span class="ex">${esc(l.label)} ${link({ href: l.url, label: `<span class="url">${esc(shortUrl(l.url))}</span>` })}</span>`).join('')}</div>`).join('')}</div>
      </div></div></footer>`);

    const nav = [['cover', t('page.nav-intro'), prof.intro],['stack', t('page.nav-stack'), (pf.skills || []).length],
      [projects[0] ? `p-${projects[0].id}` : '', t('page.nav-projects'), projects.length], ['glance', t('page.nav-glance'), projects.length],
      ['contact', t('page.nav-contact'), true]].filter((x) => x[0] && x[2]);
    root.innerHTML = `<header class="top no-print"><div class="wrap top-in">
        <a class="top-name" href="#cover">${esc(nameT)}</a>
        <nav aria-label="절 이동"><ul>${nav.map(([id, label]) => `<li><a href="#${esc(id)}">${esc(label)}</a></li>`).join('')}</ul></nav>
        ${PF.modeSwitch()}<button type="button" class="btn primary pdf" data-pdf>${T('ui.pdf')}</button></div></header>
      <main id="main">${out.join('')}</main>`;
    root.querySelectorAll('img').forEach((i) => { if (!i.hasAttribute('loading')) i.loading = 'lazy'; i.decoding = 'async'; });
    info.sections = [...root.querySelectorAll('main > section[id], main > footer[id]')].map((s) => s.id);
    info.projects = projects.length;
  }

  // 프로젝트 소개: 대표 이미지 · 메타(장르·기간·인원·환경) · 내 역할 · 개발 의도 · 태그 · 링크
  function projectIntro(p, no, sec, btn, printUrls) {
    const meta = (p.meta || []).map((m) => `<div${String(m.value || '').length > 36 ? ' class="wide"' : ''}><dt>${esc(m.label)}</dt><dd>${inline(m.value)}</dd></div>`);
    if (p.team?.role) meta.push(`<div class="wide"><dt>${T('name.role')}</dt><dd>${inline(p.team.role)}</dd></div>`);
    const extra = (p.links?.extra || []).filter((l) => isUrl(l.url));
    const btns = [btn(PF.projectUrl(p.id), T('name.deck'), 'page', 'btn primary'), btn(PF.docUrl(p.id), T('name.doc'), 'page'),
      isUrl(p.links?.github) && btn(p.links.github, 'GitHub', 'external'), isUrl(p.links?.video) && btn(p.links.video, T('name.video'), 'external'),
      ...extra.map((l) => btn(l.url, esc(l.label), 'external'))].filter(Boolean);
    const urls = [[t('name.deck'), PF.visibleUrl(PF.projectUrl(p.id)), PF.projectUrl(p.id), 'page'], [t('name.doc'), PF.visibleUrl(PF.docUrl(p.id)), PF.docUrl(p.id), 'page'],
      isUrl(p.links?.github) && ['GitHub', shortUrl(p.links.github), p.links.github, 'external'], isUrl(p.links?.video) && [t('name.video'), shortUrl(p.links.video), p.links.video, 'external'],
      ...extra.map((l) => [l.label, shortUrl(l.url), l.url, 'external'])].filter(Boolean);
    const cover = p.cover ? `<figure class="cover"><img src="${esc(PF.srcPath(p.id, p.cover))}" alt="${esc(p.title)} 대표 화면"></figure>` : '';
    // (2026-11-25 수강생 요청) 프로젝트 소개와 기능 소개를 한 화면에: 왼쪽 대표 이미지 · 메타 · 개발 의도 · 태그 · 링크, 오른쪽 기능 4줄(작은 화면 · 설명 · 그림 설명 · [슬라이드 열기])
    const fl = featureList(p);
    const intent = p.intent?.text ? `<div class="intent"><p class="k">${T('name.intent')}</p><p>${inline(p.intent.text)}</p></div>` : '';
    const body = `${p.tagline ? `<p class="tagline">${inline(p.tagline)}</p>` : ''}
      <div class="pp-g${fl ? '' : ' nofe'}"><div class="pp-l">${cover}
        ${meta.length ? `<dl class="meta">${meta.join('')}</dl>` : ''}
        ${fl ? '' : intent}
        ${(p.tags || []).length ? `<ul class="tags">${p.tags.map((t) => `<li>${esc(t)}</li>`).join('')}</ul>` : ''}
        <div class="btns no-print">${btns.join('')}</div>${printUrls(urls)}</div>
        ${fl ? `<div class="pp-r" id="p-${esc(p.id)}-core">${intent}<p class="eyebrow">${T('page.features')}</p>${fl}</div>` : ''}</div>`;
    return sec(`p-${p.id}`, no, p.title, PF.teamLabel(p.team), body, 'proj pp');
  }

  // 기능 줄: 그 기능 절의 화면(없으면 번호) · FEATURE 번호 · 제목 · 한 줄 · 그림 설명 · [슬라이드 열기]
  function featureList(p) {
    const hl = (p.highlights || []).slice(0, 4);
    if (!hl.length) return '';
    const capText = (c) => (typeof c === 'string' ? c : c?.text || '');
    return `<ol class="flist pp-f">${hl.map((h, i) => {
      const im = h.image?.src ? h.image : null, cap = im ? capText(im.caption) : '';
      const d = im && im.w && im.h ? ` width="${im.w}" height="${im.h}"` : '';
      return `<li data-fi="${i}"><div class="ff-m">${im ? `<img src="${esc(PF.srcPath(p.id, im.src))}" alt="${esc(cap || h.title)}"${d}>` : `<span class="n">${pad2(i + 1)}</span>`}</div>
        <div class="fr-t"><p class="fk">${T('en.feature')} ${pad2(i + 1)}</p><h3>${inline(h.title)}</h3>${h.what ? `<p>${inline(h.what)}</p>` : ''}${cap ? `<p class="cap">${inline(cap)}</p>` : ''}</div>
        <div class="fr-a">${link({ href: PF.projectUrl(p.id, PF.deckSceneFor(p, h.section)), label: T('page.see-in-deck'), kind: 'page', cls: 'pill',
          attrs: `aria-label="${esc(PF.normTitle(h.title))} — ${T('page.see-in-deck')} (새 탭)"` })}</div></li>`;
    }).join('')}</ol>`;
  }

  function projLinks(p) {
    return [link({ href: PF.projectUrl(p.id), label: T('name.deck'), kind: 'page' }), link({ href: PF.docUrl(p.id), label: T('name.doc-short'), kind: 'page' }),
      isUrl(p.links?.github) && link({ href: p.links.github, label: 'GitHub' }), isUrl(p.links?.video) && link({ href: p.links.video, label: T('name.video') })].filter(Boolean).join('');
  }

  // 화면과 설명 짝 강조(마우스·키보드 포커스) + [PDF로 저장]
  function bind() {
    const pair = (el, on) => {
      const item = el.closest('[data-fi]');
      const box = item?.closest('.fe');
      if (!box) return;
      box.querySelectorAll(`[data-fi="${item.dataset.fi}"]`).forEach((x) => x.classList.toggle('on', on));
    };
    root.addEventListener('mouseover', (e) => pair(e.target, true));
    root.addEventListener('mouseout', (e) => pair(e.target, false));
    root.addEventListener('focusin', (e) => pair(e.target, true));
    root.addEventListener('focusout', (e) => pair(e.target, false));
    root.querySelector('[data-pdf]')?.addEventListener('click', async (e) => {
      const b = e.currentTarget, label = b.textContent;
      b.disabled = true; b.textContent = t('ui.preparing');
      await loadAll(15000);                                            // 아래쪽 그림까지 받은 뒤 인쇄(늦게 받는 그림은 PDF에서 빠짐)
      b.disabled = false; b.textContent = label;
      window.print();
    });
    addEventListener('beforeprint', () => eager());                    // Ctrl+P: 아직 받지 않은 그림을 바로 받기 시작(할 수 있는 만큼)
  }

  // 한 장씩 넘기기 (2026-11-25 수강생 요청: C. 겹쳐 덮기 · 긴 칸 안은 보통 스크롤 · 마우스가 있는 큰 화면만, 모바일은 평소 스크롤)
  //   칸 = 머리 · 각 절 · 연락처. 칸 끝에서 휠을 need만큼 더 굴리면 다음 칸이 아래에서 올라와 앞 칸을 덮음(거꾸로는 걷어 냄)
  //   민감도 맞추기: 주소 끝에 ?pf-flip=tune → 오른쪽 아래 조절판(이 브라우저에만 저장). 정해지면 DEF 값으로 옮김
  function pager() {
    const ok = matchMedia('(hover: hover) and (pointer: fine) and (min-width: 960px)');
    const calm = matchMedia('(prefers-reduced-motion: reduce)');
    const KEY = 'pf-flip', DEF = { need: 100, ms: 650, quiet: 160 };
    const cfg = { ...DEF };
    try { Object.assign(cfg, JSON.parse(localStorage.getItem(KEY) || '{}')); } catch { /* 저장소 막힘 */ }
    const sync = () => html.classList.toggle('pf-paged', ok.matches);
    sync(); ok.addEventListener('change', sync);
    // 장 = 절 하나, 단 JOIN의 절은 앞 장에 붙여 한 장으로(2026-11-25 수강생 요청: 첫 화면 + 기술 스택·도구는 한 장 안에서 스크롤)
    const JOIN = new Set(['intro']);
    const pages = () => {
      const gs = [];
      for (const el of document.querySelectorAll('#cover, main > section[id], main > footer[id]')) {
        if (JOIN.has(el.id) && gs.length) gs[gs.length - 1].push(el); else gs.push([el]);
      }
      return gs;
    };
    const topH = () => document.querySelector('.top')?.offsetHeight || 0;
    const range = (g) => {                                             // 그 장 안에서 스크롤할 수 있는 범위 [맨 위, 맨 아래]
      const a = g[0].getBoundingClientRect().top + scrollY - topH();
      return [a, Math.max(a, g[g.length - 1].getBoundingClientRect().bottom + scrollY - innerHeight)];
    };
    const vis = (g) => g.find((el) => el.getBoundingClientRect().bottom > topH() + 1) || g[g.length - 1];   // 장 안에서 지금 화면 맨 위에 걸친 절
    const current = () => { let cur = null; for (const g of pages()) if (!cur || range(g)[0] <= scrollY + 2) cur = g; return cur; };
    let busy = false, lock = false, acc = 0, lastT = 0, want = null, raf = 0;

    const glide = () => {                                              // 칸 안 스크롤: 목표 위치로 부드럽게
      const d = want - scrollY;
      if (Math.abs(d) < 1) { scrollTo({ top: want, behavior: 'instant' }); want = null; return; }
      scrollTo({ top: scrollY + d * 0.25, behavior: 'instant' });
      raf = requestAnimationFrame(glide);
    };
    const stop = () => { cancelAnimationFrame(raf); want = null; };
    ['keydown', 'pointerdown'].forEach((t) => addEventListener(t, stop, { passive: true }));

    addEventListener('wheel', (e) => {
      if (!ok.matches || e.ctrlKey) return;
      const dy = e.deltaMode === 1 ? e.deltaY * 40 : e.deltaMode === 2 ? e.deltaY * innerHeight : e.deltaY;
      if (!dy || Math.abs(e.deltaX) > Math.abs(dy)) return;
      e.preventDefault();
      const now = performance.now(), gap = now - lastT;
      lastT = now;
      if (busy) return;
      if (lock) { if (gap < cfg.quiet) return; lock = false; }           // 넘긴 뒤 관성 휠이 멈출 때까지 무시
      if (gap > cfg.quiet || Math.sign(dy) !== Math.sign(acc)) acc = 0;
      const cur = current(), [a, b] = range(cur), base = want ?? scrollY;
      if (dy > 0 && base < b - 1) { acc = 0; want = Math.min(b, base + dy); cancelAnimationFrame(raf); raf = requestAnimationFrame(glide); return; }
      if (dy < 0 && base > a + 1) { acc = 0; want = Math.max(a, base + dy); cancelAnimationFrame(raf); raf = requestAnimationFrame(glide); return; }
      acc += dy;
      if (Math.abs(acc) < cfg.need) return;
      acc = 0;
      const ps = pages(), i = ps.findIndex((g) => g[0] === cur[0]), nx = ps[i + Math.sign(dy)];
      if (nx) turn(cur, nx, dy > 0);
    }, { passive: false });

    async function turn(curG, nxG, fwd) {                              // curG · nxG = 장(절 묶음). 움직이는 것은 화면에 보이는 절
      stop();
      lock = true;
      const y0 = scrollY, [na, nb] = range(nxG), y1 = fwd ? na : nb;
      if (calm.matches || cfg.ms < 60) { scrollTo({ top: y1, behavior: 'instant' }); return; }
      busy = true;
      const th = topH(), vh = innerHeight - th, mid = th + vh / 2;
      const cur = vis(curG);
      const shift = y1 - y0;                                           // 스크롤을 옮긴 만큼 앞 칸을 제자리에 붙잡아 둠
      const curTop = cur.getBoundingClientRect().top;
      scrollTo({ top: y1, behavior: 'instant' });
      const nx = fwd ? nxG[0] : vis(nxG);
      const over = fwd ? nx : cur, under = fwd ? cur : nx;            // 위에 덮는 칸 · 아래 깔리는 칸
      const underTop = fwd ? curTop : nx.getBoundingClientRect().top;
      [over, under].forEach((el, i) => Object.assign(el.style, { position: 'relative', zIndex: i ? '1' : '3' }));
      Object.assign(over.style, { backgroundColor: 'var(--paper)', boxShadow: '0 -24px 48px -24px rgb(0 0 0 / .6)' });
      under.style.transformOrigin = `50% ${mid - underTop}px`;
      const opt = { duration: cfg.ms, easing: 'cubic-bezier(.22,.7,.2,1)', fill: 'both' };
      const back = 'scale(.94)', dim = 'brightness(.5)';
      const anims = fwd
        ? [over.animate([{ transform: `translateY(${vh}px)` }, { transform: 'none' }], opt),
           under.animate([{ transform: `translateY(${shift}px)`, filter: 'none' }, { transform: `translateY(${shift}px) ${back}`, filter: dim }], opt)]
        : [over.animate([{ transform: `translateY(${shift}px)` }, { transform: `translateY(${shift + vh}px)` }], opt),
           under.animate([{ transform: back, filter: dim }, { transform: 'none', filter: 'none' }], opt)];
      await Promise.race([Promise.all(anims.map((x) => x.finished.catch(() => {}))),
        new Promise((r) => setTimeout(r, cfg.ms + 300))]);               // 가려진 창에서는 애니메이션이 멈춰 끝나지 않을 수 있음 → 시간이 지나면 정리
      anims.forEach((x) => x.cancel());
      [over, under].forEach((el) => ['position', 'zIndex', 'backgroundColor', 'boxShadow', 'transformOrigin'].forEach((k) => { el.style[k] = ''; }));
      busy = false;
    }

    if (new URLSearchParams(location.search).get('pf-flip') !== 'tune') return;
    const box = document.createElement('div');                         // 민감도 조절판(시험용)
    box.className = 'pf-flip-tune no-print';
    const rows = [['need', '민감도 — 칸 끝에서 더 굴릴 양(작을수록 잘 넘어감)', 20, 400, 10, ''], ['ms', '넘김 속도(ms)', 200, 1400, 50, ''],
      ['quiet', '넘긴 뒤 쉬는 시간(ms)', 0, 500, 10, '']];
    box.innerHTML = `<b>한 장씩 넘기기 조절</b>${rows.map(([k, l, lo, hi, st]) => `<label>${l}<output data-o="${k}">${cfg[k]}</output>
      <input type="range" min="${lo}" max="${hi}" step="${st}" value="${cfg[k]}" data-k="${k}"></label>`).join('')}<button type="button">기본값</button>`;
    const save = () => { try { localStorage.setItem(KEY, JSON.stringify(cfg)); } catch { /* 저장소 막힘 */ } };
    box.addEventListener('input', (e) => { const k = e.target.dataset.k; if (!k) return; cfg[k] = +e.target.value; box.querySelector(`[data-o="${k}"]`).value = cfg[k]; save(); });
    box.querySelector('button').addEventListener('click', () => {
      Object.assign(cfg, DEF); save();
      box.querySelectorAll('[data-k]').forEach((i) => { i.value = cfg[i.dataset.k]; box.querySelector(`[data-o="${i.dataset.k}"]`).value = cfg[i.dataset.k]; });
    });
    box.addEventListener('wheel', (e) => e.stopPropagation());
    document.body.appendChild(box);
  }

  const eager = () => document.querySelectorAll('img[loading="lazy"]').forEach((i) => { i.loading = 'eager'; });
  const loaded = (i) => (i.complete ? null : new Promise((r) => {     // decode()·rAF는 쓰지 않음: 가려진 창에서 끝나지 않음(슬라이드 키트와 같은 이유)
    i.addEventListener('load', r, { once: true }); i.addEventListener('error', r, { once: true }); }));
  async function loadAll(ms) {
    eager();
    await Promise.race([Promise.all([...document.images].map(loaded)), new Promise((r) => setTimeout(r, ms))]);
  }

  // 준비: 데스크톱 기기면 그림을 미리 다 받아 Ctrl+P도 빠짐없이 인쇄(휴대폰·데이터 절약 모드는 화면에 올 때 받음)
  async function settle() {
    const desk = matchMedia('(pointer: fine)').matches && !(navigator.connection && navigator.connection.saveData);
    if (desk) eager();
    await Promise.race([Promise.all([...document.querySelectorAll('img:not([loading="lazy"])')].map(loaded)), new Promise((r) => setTimeout(r, 20000))]);
    if (document.fonts) await Promise.race([document.fonts.ready, new Promise((r) => setTimeout(r, 3000))]);
    if (!userMoved) {                                                  // 글꼴이 바뀌며 절 높이가 달라져 도착 위치가 밀린 만큼 다시 맞춤
      const el = arrived && document.getElementById(arrived);
      if (el) el.scrollIntoView({ behavior: 'instant', block: 'start' });
      else if (restoreY) scrollTo({ top: restoreY, behavior: 'instant' });
    }
    html.dataset.pfReady = 'yes';
    resolveReady(report());
    if (new URLSearchParams(location.search).get('pf-audit') === '1') writeAudit();
  }

  function report() {
    const imgs = [...document.images];
    return {
      page: 'portfolio', ready: html.dataset.pfReady, sections: info.sections, projects: info.projects,
      images: { total: imgs.length, broken: imgs.filter((i) => i.complete && i.naturalWidth === 0).map((i) => i.getAttribute('src')),
                pending: imgs.filter((i) => !i.complete).map((i) => i.getAttribute('src')) },
      deckLinks: [...document.querySelectorAll('.flist a.pill')].map((a) => a.getAttribute('href')),
      overflowX: Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth),
      errors: info.errors, strings: PF.stringsReport(),
    };
  }
  function writeAudit() {
    let pre = document.getElementById('pf-audit');
    if (!pre) { pre = document.createElement('pre'); pre.id = 'pf-audit'; pre.hidden = true; document.body.appendChild(pre); }
    const bytes = new TextEncoder().encode(JSON.stringify(report()));
    let bin = '';
    for (let i = 0; i < bytes.length; i += 0x8000) bin += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
    pre.textContent = btoa(bin);
  }
})();
