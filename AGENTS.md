# AGENTS.md

This repository is the active Woodbury co-op mod workspace.

## Repo Maintenance

- Follow `CLAUDE.md` when it is present locally for project architecture, build, runtime, and diagnostics notes.
- For every intentional code, tooling, site, or release-metadata change, update `CHANGELOG.md` under `## Unreleased` with a factual bullet.
- When a change affects user-facing status, versioning, release metadata, roadmap/site data, or docs, also update the intended files for that surface, such as `STATUS.md`, `README.md`, `README_STATUS.md`, `site/data/*.json`, generated badges, or release scripts.
- Prefer existing refresh scripts, such as `scripts/Update-SiteData.ps1`, when generated site/status metadata is involved.
- Do not add changelog noise for failed experiments, pure local scratch files, or no-op formatting unless they change shipped behavior or workflow.
