# Woodbury Co-op — status site (`site/`)

Static status page for the Fears to Fathom: Woodbury Getaway co-op mod. No build step, no CDN dependencies, no analytics, works offline.

## Structure

```text
site/
├── index.html      shell + static wiki content + GitHub source panel + deploy tabs
├── styles.css      dark-default theme + light toggle
├── app.js          vanilla JS: theme, scroll-spy, progress fills, filters, tabs, roadmap renderer
├── data/
│   ├── sync-status.json   scene coverage % + synced/not-synced + RE verification meta
│   ├── systems.json       cross-scene system matrix rows
│   ├── changelog.json     compacted Unreleased + recent history
│   └── roadmap.json       bugs, unsynced scenes, milestones, distribution gaps, recent commits
├── assets/
│   └── favicon.svg
├── .nojekyll       disables Jekyll on GitHub Pages
└── README.md       this file
```

## Updating the data

Almost every change is a JSON edit — no HTML/JS/CSS touch needed:

| You want to… | Edit |
| --- | --- |
| Bump version / plugin / proto badges | `data/sync-status.json` → `meta.*` |
| Change overall % in the hero ring | `data/sync-status.json` → `meta.overallPercent` |
| Move a scene from 70% → 80% | `data/sync-status.json` → `scenes[*].percent` |
| Add a synced / not-synced bullet | `data/sync-status.json` → `scenes[*].synced` / `.notSynced` |
| Add a new cross-scene system | `data/systems.json` → push a new `{name, status, note}` |
| Add a changelog entry | `data/changelog.json` → push under `unreleased.groups[*].bullets` |
| Tick off a bug | `data/roadmap.json` → remove or update the `bugs[*]` entry |
| Add a release milestone item | `data/roadmap.json` → `milestones[*].items` |
| Refresh recent commits panel | `data/roadmap.json` → `recentCommits` (paste from `git log --oneline -8`) |

The site re-renders from JSON on every load — no rebuild, just refresh.

## Local preview

```powershell
cd site
python -m http.server 8080
# open http://localhost:8080
```

Or any static server:

```powershell
npx serve .
# or
caddy file-server --listen :8080
```

> JSON `fetch()` requires HTTP. Opening `index.html` via `file://` will fall back to an error banner.

## Privacy & footprint

The site is designed to leave **no trace** on the machine serving it or visiting it:

- **Zero external network requests** on page load. Only three local `data/*.json` fetches.
- **No analytics, no telemetry, no third-party scripts, no CDN, no web fonts.** System fonts only.
- **`<meta name="referrer" content="no-referrer">`** set globally — clicking out doesn't leak your URL.
- **`<meta name="robots" content="noindex,nofollow">`** while iterating. Remove or flip when you're ready to publish.
- **GitHub commits** in the Source panel are hand-encoded in `data/roadmap.json` — no GitHub API call at runtime, so visitors aren't beaconed back to GitHub on every page load.
- **All outbound links** use `rel="noopener noreferrer"` so opened tabs can't `window.opener`-back and the referrer isn't shipped.

To audit:

```powershell
# Should return only the three local data/*.json fetches and nothing else
Select-String -Path site\app.js -Pattern "fetch\(|XMLHttpRequest|new Image|sendBeacon"
```

## Deploy

### Cloudflare Pages (recommended primary host)

1. Cloudflare dashboard → **Workers & Pages → Create → Pages → Connect to Git**.
2. Pick this repo. Build command empty. **Build output directory: `site`**.
3. Save and Deploy. Live at `<project>.pages.dev` in ~30 s. Unlimited bandwidth, free.

### GitHub Pages

The branch+folder picker only accepts `/` or `/docs`, **not** `/site`. Two paths:

#### Path A — rename to `docs/` (5 min)

```powershell
git mv site docs
git commit -m "rename site → docs for GitHub Pages"
git push
```

Then Repo → **Settings → Pages** → Source `Deploy from a branch`, Branch `main`, Folder `/docs`.

#### Path B — keep `site/`, add an Actions workflow (10 min)

Create `.github/workflows/pages.yml` running `actions/upload-pages-artifact@v3` with `path: site` then `actions/deploy-pages@v4`. Set Pages source to *GitHub Actions*.

### Netlify

Drag-drop `site/` on `app.netlify.com/drop`, **or** connect repo with publish dir `site/`.

### Vercel

Import repo → root dir `site`, framework `Other`, no build. Hobby tier only — commercial use requires Pro ($20/mo).

### nginx (self-host)

```nginx
server {
  listen 80;
  server_name woodbury.example.com;
  root /var/www/woodbury-coop/site;
  index index.html;
  location ~* \.json$ { add_header Content-Type application/json; }
}
```

Then `nginx -t && systemctl reload nginx`. For HTTPS:

```bash
certbot --nginx -d woodbury.example.com
```

### Caddy (self-host)

```bash
caddy file-server --root ./site --listen :80
```

Or with auto-HTTPS:

```Caddyfile
woodbury.example.com {
  root * /var/www/woodbury-coop/site
  file_server
}
```

### IIS (Windows self-host)

1. IIS Manager → Add Website → physical path = your `site/` folder.
2. Default Document → ensure `index.html` is listed.
3. MIME Types → add `.json` → `application/json` if missing.
4. Add HTTPS binding with a cert if needed.

### Free drag-drop one-shot

When you want to share something *right now* without committing:

- **Netlify Drop** — `app.netlify.com/drop`, drag `site/`, get a `*.netlify.app` URL.
- **tiiny.host** — drop a `.zip` of `site/`, free tier expires after 7 days unless signed in.
- **EdgeOne Pages Drop** — drag-drop, no signup, fast.
- **Static.so** — drag-drop with SSL + subdomain.

## Design choices

- **Dark default, light toggle** — persists to `localStorage`.
- **No framework** — vanilla JS keeps the page under 50 KB gzipped and zero supply-chain risk.
- **No CDN** — every asset is local. Site works fully offline once cached.
- **System fonts** — matches whatever the OS already loaded; no FOIT/FOUT.
- **CSS Grid `auto-fit`** — scene + system + wiki + roadmap cards reflow cleanly on phones.
- **`IntersectionObserver`** drives the progress-bar fills, the hero ring, and the scroll-spy nav. Nothing animates until it scrolls into view, which feels alive on first interaction without burning CPU.
- **Static GitHub panel** — repo links + commit list rendered from `data/roadmap.json` instead of `api.github.com/repos/...`. Saves visitors' IPs from being shipped to GitHub on every page load.

## Updating after a release

When you bump plugin version or land a major sync:

1. Edit `data/sync-status.json` `meta.plugin`, `meta.lastUpdated`, `meta.overallPercent`, scene `percent`.
2. Move new bullets from `notSynced` → `synced`.
3. Add new bullets to the appropriate `data/changelog.json` `unreleased.groups[*]`.
4. Tick off resolved entries in `data/roadmap.json` (`bugs[*]`, `unsyncedScenes[*]`, `milestones[*].items`).
5. Refresh `data/roadmap.json` `recentCommits` from `git log --oneline -8`.
6. Commit, push, the host auto-rebuilds.

The CLAUDE.md and README_STATUS.md at the repo root are the canonical engineering notes — keep the JSONs in sync with those numbers when you bump them.
