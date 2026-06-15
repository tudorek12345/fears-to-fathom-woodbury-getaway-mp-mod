/* ============================================================
   Woodbury Co-op status site — vanilla JS, no build, no CDN
   ============================================================ */

(function () {
  "use strict";

  // ---------- 0. Theme toggle ---------------------------------
  const root = document.documentElement;
  const themeBtn = document.getElementById("theme-toggle");
  const savedTheme = localStorage.getItem("wbc-theme");
  if (savedTheme === "light" || savedTheme === "dark") {
    root.setAttribute("data-theme", savedTheme);
  }
  themeBtn?.addEventListener("click", () => {
    const next = root.getAttribute("data-theme") === "light" ? "dark" : "light";
    root.setAttribute("data-theme", next);
    localStorage.setItem("wbc-theme", next);
  });

  // ---------- 0a. FX / reading-mode toggle (persists) -----------
  const fxBtn = document.getElementById("fx-toggle");
  if (localStorage.getItem("wbc-fx") === "off") document.body.classList.add("fx-off");
  fxBtn?.addEventListener("click", () => {
    const off = document.body.classList.toggle("fx-off");
    localStorage.setItem("wbc-fx", off ? "off" : "on");
  });

  // ---------- 0a. REC found-footage timestamp ticker ------------
  (function recTicker() {
    var el = document.getElementById("rec-time");
    if (!el || document.body.classList.contains("fx-off")) return;
    var s = Math.floor(Math.random() * 5400); // start somewhere into the "tape"
    function pad(n) { return String(n).padStart(2, "0"); }
    setInterval(function () {
      s++;
      el.textContent = pad(Math.floor(s / 3600)) + ":" + pad(Math.floor(s / 60) % 60) + ":" + pad(s % 60);
    }, 1000);
  })();

  // ---------- 0b. Visitor counter + gallery (run independently) ----
  initVisitorCounter();
  initGallery();

  function initVisitorCounter() {
    var el = document.getElementById("visit-count");
    var pill = document.getElementById("visit-pill");
    if (!el || !pill) return;
    var base = "https://api.counterapi.dev/v1/woodbury-coop/site-visits";
    var counted = false;
    try { counted = localStorage.getItem("wbc_visited") === "1"; } catch (e) {}
    // Increment once per browser (dedupe); on return visits just read the total.
    var url = counted ? base + "/" : base + "/up";
    fetch(url)
      .then(function (r) { return r.json(); })
      .then(function (d) {
        if (d && typeof d.count === "number") {
          el.textContent = d.count.toLocaleString();
          pill.hidden = false;
          try { localStorage.setItem("wbc_visited", "1"); } catch (e) {}
        }
      })
      .catch(function () { pill.hidden = true; });
  }

  function initGallery() {
    var gallery = document.getElementById("gallery");
    if (!gallery) return;
    var imgs = Array.prototype.slice.call(gallery.querySelectorAll(".shot img"));
    var hasEmbed = !!gallery.querySelector(".imgur-embed-pub");
    if (!imgs.length && !hasEmbed) return;
    function evaluate() {
      var anyLoaded = hasEmbed;
      imgs.forEach(function (img) {
        var ok = img.complete && img.naturalWidth > 0;
        var fig = img.closest(".shot");
        if (fig) fig.classList.toggle("shot-missing", !ok);
        if (ok) anyLoaded = true;
      });
      gallery.style.display = anyLoaded ? "" : "none";
    }
    imgs.forEach(function (img) {
      img.addEventListener("load", evaluate);
      img.addEventListener("error", evaluate);
    });
    evaluate();
    setTimeout(evaluate, 1000);
  }

  // ---------- 1. Fetch all data -------------------------------
  Promise.all([
    fetch("data/sync-status.json").then(safeJson),
    fetch("data/systems.json").then(safeJson),
    fetch("data/changelog.json").then(safeJson),
    fetch("data/roadmap.json").then(safeJson),
  ])
    .then(([syncStatus, systems, changelog, roadmap]) => {
      renderHero(syncStatus.meta);
      renderScenes(syncStatus.scenes);
      renderSystems(systems.systems);
      renderTimeline(changelog);
      renderRoadmap(roadmap);
      renderGitHubPanel(syncStatus.meta, roadmap);
      initScrollSpy();
      initFilters();
      initProgressObservers();
    })
    .catch((err) => {
      console.error("Failed to load site data:", err);
      const main = document.querySelector("main");
      if (main) {
        const banner = document.createElement("div");
        banner.style.cssText = "padding:20px;margin:20px;background:#7b1f1f;color:#fff;border-radius:10px;";
        banner.textContent = "Failed to load site data — open via http://, not file://. Try: cd site && python -m http.server";
        main.prepend(banner);
      }
    });

  function safeJson(res) {
    if (!res.ok) throw new Error(`HTTP ${res.status} fetching ${res.url}`);
    return res.json();
  }

  // ---------- 2. Hero -----------------------------------------
  function renderHero(meta) {
    if (!meta) return;
    const sub = document.getElementById("hero-subhead");
    if (sub && meta.subhead) sub.textContent = meta.subhead;

    const badges = document.getElementById("hero-badges");
    if (badges) {
      badges.innerHTML = "";
      addBadge(badges, "badge-plugin", `PLUGIN ${meta.plugin}`);
      addBadge(badges, "badge-proto", `PROTOCOL v${meta.protocol}`);
      addBadge(badges, "badge-bep", `${meta.bepInEx}`.toUpperCase());
      addBadge(badges, "badge-target", String(meta.targetFramework).toUpperCase());
      addBadge(badges, "badge-status", "WIP");
      addBadge(badges, "badge-updated", `UPDATED ${meta.lastUpdated}`);
    }

    const ringPct = document.getElementById("ring-percent");
    if (ringPct) {
      ringPct.firstChild && (ringPct.firstChild.nodeValue = String(meta.overallPercent));
    }

    const ring = document.getElementById("ring-fill");
    if (ring) {
      ring.dataset.targetPercent = String(meta.overallPercent);
    }

    const repo = document.getElementById("hero-repo");
    if (repo && meta.repo) {
      repo.href = meta.repo;
    }

    const hmeta = document.getElementById("hero-meta");
    if (hmeta) {
      hmeta.innerHTML = `Last commit <code>${escapeHtml(meta.lastCommit)}</code> · updated <b>${escapeHtml(meta.lastUpdated)}</b>`;
    }
  }

  function addBadge(parent, cls, text) {
    const b = document.createElement("span");
    b.className = `badge ${cls}`;
    b.textContent = text;
    parent.appendChild(b);
  }

  // ---------- 3. Scene cards ----------------------------------
  function renderScenes(scenes) {
    const grid = document.getElementById("scene-grid");
    if (!grid || !scenes) return;
    grid.innerHTML = "";

    for (const s of scenes) {
      const card = document.createElement("details");
      card.className = "scene-card";
      card.dataset.scene = s.id;

      const head = `
        <summary>
          <div class="scene-card-head">
            <h3>${escapeHtml(s.name)}</h3>
            <div class="scene-card-percent" data-target="${s.percent}">0%</div>
          </div>
          <div class="scene-card-status">${escapeHtml(s.status)}</div>
          <p class="scene-card-summary">${escapeHtml(s.summary)}</p>
          <div class="scene-bar"><div class="scene-bar-fill" data-target="${s.percent}"></div></div>
          <div class="expand-hint"><span>Show details</span></div>
        </summary>
        <div class="scene-card-detail">
          <div>
            <h4>Synced (${s.synced.length})</h4>
            <ul>${s.synced.map((x) => `<li>${escapeHtml(x)}</li>`).join("")}</ul>
          </div>
          <div class="not-synced">
            <h4>Not yet (${s.notSynced.length})</h4>
            <ul>${s.notSynced.map((x) => `<li>${escapeHtml(x)}</li>`).join("")}</ul>
          </div>
        </div>
        ${
          s.syncedFiles && s.syncedFiles.length
            ? `<div class="scene-card-files">Files: ${s.syncedFiles
                .map(
                  (f) =>
                    `<a href="https://github.com/tudorek12345/fears-to-fathom-woodbury-getaway-mp-mod/blob/main/${escapeHtml(
                      f
                    )}" target="_blank" rel="noopener noreferrer"><code>${escapeHtml(
                      f.split("/").pop()
                    )}</code></a>`
                )
                .join(" · ")}</div>`
            : ""
        }
      `;
      card.innerHTML = head;
      grid.appendChild(card);
    }
  }

  // ---------- 4. System matrix --------------------------------
  function renderSystems(rows) {
    const grid = document.getElementById("systems-grid");
    if (!grid || !rows) return;
    grid.innerHTML = "";
    for (const r of rows) {
      const row = document.createElement("div");
      row.className = "system-row";
      row.dataset.status = r.status;
      row.innerHTML = `
        <div>
          <div class="system-row-name">${escapeHtml(r.name)}</div>
          <div class="system-row-note">${escapeHtml(r.note)}</div>
        </div>
        <span class="pill pill-${r.status}">${r.status}</span>
      `;
      grid.appendChild(row);
    }
  }

  function initFilters() {
    const chips = document.querySelectorAll("#filter-chips .chip");
    chips.forEach((chip) => {
      chip.addEventListener("click", () => {
        chips.forEach((c) => {
          c.classList.remove("is-active");
          c.setAttribute("aria-selected", "false");
        });
        chip.classList.add("is-active");
        chip.setAttribute("aria-selected", "true");
        const f = chip.dataset.filter;
        document.querySelectorAll("#systems-grid .system-row").forEach((row) => {
          row.classList.toggle("is-hidden", !(f === "all" || row.dataset.status === f));
        });
      });
    });
  }

  // ---------- 5. Timeline -------------------------------------
  function renderTimeline(changelog) {
    const tl = document.getElementById("timeline");
    if (!tl || !changelog) return;
    tl.innerHTML = "";

    // Unreleased (grouped)
    const unr = document.createElement("details");
    unr.className = "tl-entry";
    unr.open = true;
    const groupsHtml = changelog.unreleased.groups
      .map(
        (g) => `
        <h4 style="margin:14px 0 6px;font-size:13px;text-transform:uppercase;letter-spacing:1.5px;color:var(--accent-2);">${escapeHtml(
          g.section
        )}</h4>
        <ul>${g.bullets.map((b) => `<li>${escapeHtml(b)}</li>`).join("")}</ul>
      `
      )
      .join("");
    unr.innerHTML = `
      <summary>
        <div class="tl-head">
          <h3>${escapeHtml(changelog.unreleased.label || "Latest")}</h3>
          <span class="tl-meta">${escapeHtml(changelog.unreleased.rangeNote || "")}</span>
        </div>
      </summary>
      <div class="tl-body">${groupsHtml}</div>
    `;
    tl.appendChild(unr);

    // History (collapsed)
    for (const v of changelog.history.slice(0, 12)) {
      const ent = document.createElement("details");
      ent.className = "tl-entry";
      ent.innerHTML = `
        <summary>
          <div class="tl-head">
            <h3>v${escapeHtml(v.version)}</h3>
            <span class="tl-meta">${v.bullets.length} change${v.bullets.length === 1 ? "" : "s"}</span>
          </div>
        </summary>
        <div class="tl-body">
          <ul>${v.bullets.map((b) => `<li>${escapeHtml(b)}</li>`).join("")}</ul>
        </div>
      `;
      tl.appendChild(ent);
    }
  }

  // ---------- 5b. Roadmap -------------------------------------
  function renderRoadmap(roadmap) {
    if (!roadmap) return;

    // Headline features (top of roadmap section)
    const fs = document.getElementById("feature-strip");
    if (fs && roadmap.headlineFeatures) {
      fs.innerHTML = roadmap.headlineFeatures
        .map(
          (f) => `
        <article class="feature-card">
          <span class="feature-status">${escapeHtml(f.status)}</span>
          <h4>${escapeHtml(f.title)}</h4>
          <p>${escapeHtml(f.summary)}</p>
          <div class="feature-meta">
            ${f.scenes && f.scenes.length ? `<span>${escapeHtml(f.scenes.join(" · "))}</span>` : ""}
          </div>
        </article>`
        )
        .join("");
    }

    // Bugs
    const bugList = document.getElementById("bug-list");
    if (bugList && roadmap.bugs) {
      bugList.innerHTML = roadmap.bugs
        .map(
          (b) => `
        <article class="bug-card" data-sev="${escapeHtml(b.severity)}">
          <header class="bug-head">
            <h4>${escapeHtml(b.title)}</h4>
            <span class="bug-effort">${escapeHtml(b.effort)}</span>
          </header>
          <p class="bug-detail">${escapeHtml(b.detail)}</p>
          ${
            b.sites && b.sites.length
              ? `<div class="bug-sites">${b.sites
                  .map((s) => `<code>${escapeHtml(s)}</code>`)
                  .join(" · ")}</div>`
              : ""
          }
        </article>`
        )
        .join("");
    }

    // Unsynced scenes
    const usList = document.getElementById("unsynced-list");
    if (usList && roadmap.unsyncedScenes) {
      usList.innerHTML = roadmap.unsyncedScenes
        .map(
          (s) => `
        <article class="unsynced-card">
          <header class="unsynced-head">
            <h4>${escapeHtml(s.name)}</h4>
          </header>
          <p class="bug-detail">${escapeHtml(s.impact)}</p>
        </article>`
        )
        .join("");
    }

    // Milestones
    const ms = document.getElementById("milestone-row");
    if (ms && roadmap.milestones) {
      ms.innerHTML = roadmap.milestones
        .map(
          (m) => `
        <div class="milestone-card">
          <div class="milestone-id">v${escapeHtml(m.id)}</div>
          <h4>${escapeHtml(m.title)}</h4>
          <p class="milestone-sub">${escapeHtml(m.subtitle)}</p>
          <ul>${m.items.map((i) => `<li>${escapeHtml(i)}</li>`).join("")}</ul>
        </div>`
        )
        .join("");
    }

    // Distribution
    const dist = document.getElementById("dist-grid");
    if (dist && roadmap.distribution) {
      dist.innerHTML = roadmap.distribution
        .map(
          (d) => `
        <div class="dist-card">
          <h4>${escapeHtml(d.title)}</h4>
          <p>${escapeHtml(d.impact)}</p>
          <div class="dist-effort">Effort: ${escapeHtml(d.effort)}</div>
        </div>`
        )
        .join("");
    }

    // Popularity callout
    const pop = document.getElementById("popularity-callout");
    if (pop && roadmap.popularityProjection) {
      const tiers = roadmap.popularityProjection.tiers
        .map((t) => {
          const cls =
            t.likelihood.toLowerCase().indexOf("low") >= 0
              ? "low"
              : t.likelihood.toLowerCase().indexOf("very likely") >= 0
              ? "likely"
              : "plausible";
          return `
            <div class="pop-tier">
              <div class="pop-tier-id">${escapeHtml(t.tier)}</div>
              <div>
                <div class="pop-tier-label">${escapeHtml(t.label)}</div>
                <div style="font-size:12px;color:var(--fg-mute);">${escapeHtml(t.ceiling)}</div>
              </div>
              <span class="pop-tier-likelihood ${cls}">${escapeHtml(t.likelihood)}</span>
            </div>`;
        })
        .join("");
      const drivers = roadmap.popularityProjection.drivers
        .map((d) => `<li>${escapeHtml(d)}</li>`)
        .join("");
      pop.innerHTML = `
        <h4>Popularity projection</h4>
        <div class="popularity-tiers">${tiers}</div>
        <div class="pop-drivers">
          <b>What actually decides between tiers:</b>
          <ul style="margin:6px 0 0 0;padding-left:18px;">${drivers}</ul>
        </div>`;
    }
  }

  // ---------- 5c. GitHub panel --------------------------------
  function renderGitHubPanel(meta, roadmap) {
    if (!meta || !meta.repo) return;
    const base = meta.repo.replace(/\/$/, "");
    const set = (id, href) => {
      const el = document.getElementById(id);
      if (el) el.href = href;
    };
    set("hero-repo",         base);
    set("get-card-gh",       base);
    set("gh-link-code",      base);
    set("gh-link-commits",   base + "/commits/main");
    set("gh-link-issues",    base + "/issues");
    set("gh-link-pulls",     base + "/pulls");
    set("gh-link-releases",  base + "/releases");
    set("gh-link-readme",    base + "#readme");
    set("gh-link-changelog", base + "/blob/main/CHANGELOG.md");
    set("gh-link-actions",   base + "/actions");
    set("gh-more",           base + "/commits/main");

    if (meta.download && meta.download.url) {
      const dl = document.getElementById("download-build");
      if (dl) {
        dl.href = meta.download.url;
        dl.title = meta.download.label || "Download current test build";
      }
    }

    const list = document.getElementById("commit-list");
    if (list && roadmap && roadmap.recentCommits) {
      list.innerHTML = roadmap.recentCommits
        .map(
          (c) => `
        <li class="commit-row">
          <a class="commit-sha" href="${escapeHtml(base)}/commit/${escapeHtml(
            c.sha
          )}" target="_blank" rel="noopener noreferrer">${escapeHtml(c.sha)}</a>
          <span class="commit-subject">${escapeHtml(c.subject)}</span>
        </li>`
        )
        .join("");
    }
  }

  // ---------- 6. Deploy tabs ----------------------------------
  function initDeployTabs() {
    const btns = document.querySelectorAll("#deploy .tab-btn");
    const panes = document.querySelectorAll("#deploy .tab-pane");
    btns.forEach((btn) => {
      btn.addEventListener("click", () => {
        btns.forEach((b) => {
          b.classList.remove("is-active");
          b.setAttribute("aria-selected", "false");
        });
        btn.classList.add("is-active");
        btn.setAttribute("aria-selected", "true");
        const t = btn.dataset.tab;
        panes.forEach((p) => p.classList.toggle("is-active", p.dataset.pane === t));
      });
    });
  }

  // ---------- 7. Scroll-spy nav -------------------------------
  function initScrollSpy() {
    const links = document.querySelectorAll(".topnav a");
    const map = new Map();
    links.forEach((a) => {
      const id = a.getAttribute("href").slice(1);
      const sec = document.getElementById(id);
      if (sec) map.set(sec, a);
    });
    if (!map.size) return;

    const io = new IntersectionObserver(
      (entries) => {
        // pick the entry whose top is closest to the top of the viewport while still intersecting
        let candidate = null;
        for (const e of entries) {
          if (e.isIntersecting) {
            if (!candidate || e.boundingClientRect.top < candidate.boundingClientRect.top) {
              candidate = e;
            }
          }
        }
        if (!candidate) return;
        links.forEach((a) => a.classList.remove("is-active"));
        const link = map.get(candidate.target);
        link && link.classList.add("is-active");
      },
      { rootMargin: "-30% 0px -60% 0px", threshold: [0, 0.25, 0.5, 1] }
    );

    for (const sec of map.keys()) io.observe(sec);
  }

  // ---------- 8. Progress observers ---------------------------
  function initProgressObservers() {
    // Ring (hero) — animate stroke-dashoffset to target percent
    const ring = document.getElementById("ring-fill");
    if (ring) {
      const target = Number(ring.dataset.targetPercent || 0);
      const circumference = 2 * Math.PI * 52;
      ring.style.strokeDasharray = String(circumference);
      ring.style.strokeDashoffset = String(circumference);
      const heroIO = new IntersectionObserver(
        (entries, obs) => {
          for (const e of entries) {
            if (e.isIntersecting) {
              ring.style.strokeDashoffset = String(circumference * (1 - target / 100));
              obs.unobserve(e.target);
            }
          }
        },
        { threshold: 0.4 }
      );
      heroIO.observe(ring);
    }

    // Scene bars + percents
    const fills = document.querySelectorAll(".scene-bar-fill[data-target]");
    const fillIO = new IntersectionObserver(
      (entries, obs) => {
        for (const e of entries) {
          if (!e.isIntersecting) continue;
          const target = Number(e.target.dataset.target || 0);
          e.target.style.right = `${100 - target}%`;
          obs.unobserve(e.target);
        }
      },
      { threshold: 0.3 }
    );
    fills.forEach((f) => fillIO.observe(f));

    // Animated percent counters
    const counters = document.querySelectorAll(".scene-card-percent[data-target]");
    const countIO = new IntersectionObserver(
      (entries, obs) => {
        for (const e of entries) {
          if (!e.isIntersecting) continue;
          animateCount(e.target, Number(e.target.dataset.target || 0));
          obs.unobserve(e.target);
        }
      },
      { threshold: 0.3 }
    );
    counters.forEach((c) => countIO.observe(c));
  }

  function animateCount(el, target) {
    const dur = 1200;
    const start = performance.now();
    function step(now) {
      const t = Math.min(1, (now - start) / dur);
      const eased = 1 - Math.pow(1 - t, 3);
      el.textContent = `${Math.round(eased * target)}%`;
      if (t < 1) requestAnimationFrame(step);
    }
    requestAnimationFrame(step);
  }

  // ---------- 9. utils ----------------------------------------
  function escapeHtml(str) {
    if (str == null) return "";
    return String(str)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }
})();
