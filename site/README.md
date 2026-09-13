# Woodbury Co-op status site

GitHub Pages deploys `site/` through `.github/workflows/site.yml`. There is no frontend build step: HTML, CSS, vanilla JavaScript, and four local JSON data files.

- `index.html`, `styles.css`, `app.js`: layout, theme/FX controls, chapter details, system filters, roadmap, download links.
- `data/sync-status.json`: estimated implementation coverage and gameplay evidence limits.
- `data/systems.json`, `data/roadmap.json`, `data/changelog.json`: curated feature status and implementation notes.
- `downloads/woodbury-coop-latest.7z`: all-in-one development test package.
- `downloads/build-info.json`: actual package version, protocol, DLL/archive hashes, source provenance, and validation status.

## Update

Use `scripts/Build-TestPackage.ps1` when the plugin changes, then `scripts/Update-SiteData.ps1`. The refresh script reads the source version and last source commit, excludes automated refresh commits from recent changes, verifies the archive hash/version, and refreshes status/README/badge data.

Coverage percentages are human estimates. They do not measure completed gameplay tests. Keep implemented features, historical gameplay evidence, and pending validation distinct. Update `CHANGELOG.md` for intentional changes.

The Pages workflow runs the standalone snapshot/log regressions and package metadata checks before deployment. It does not compile against game assemblies or perform a gameplay run on GitHub.

## Preview

```powershell
python -m http.server 8080 --directory site
```

Check desktop/mobile navigation, chapter details, system filters, theme/FX controls, download and manifest links. Use HTTP; local `file://` pages cannot fetch the JSON data reliably.

No external counter or analytics requests are made. Theme/FX preferences use localStorage when available. The showcase image is local; outbound album, video, community, and repository links remain external.
