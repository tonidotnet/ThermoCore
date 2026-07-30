# ADR-002: Documentation site stack

**Status:** Accepted  
**Date:** 2026-07-31

## Context

ThermoCore engineering documentation lives as Markdown under `docs/`. Contributors and users need a browsable portal with search, Mermaid diagrams, and math rendering, deployable to GitHub Pages without inventing a custom static-site stack.

## Decision

Use **MkDocs** with the **Material for MkDocs** theme as the documentation portal.

Enable:

- Material search;
- Mermaid via `pymdownx.superfences` custom fence;
- Math via MathJax (`pymdownx.arithmatex`).

Site configuration lives in repository-root `mkdocs.yml`. Dependencies are pinned in `requirements-docs.txt`.

## Alternatives

- Docusaurus / VitePress: stronger JS ecosystem, heavier Node toolchain for a .NET-first repo.
- DocFX: excellent for API docs, weaker fit for existing multi-folder engineering Markdown.
- Hand-rolled HTML: not maintainable.

## Consequences

- Local preview: `pip install -r requirements-docs.txt && mkdocs serve`
- CI publishes `mkdocs build` output to GitHub Pages.
- Existing `docs/**/*.md` paths remain the source of truth; MkDocs navigation is a curated subset plus folder browse where listed.

## Related documents

- `mkdocs.yml`
- `requirements-docs.txt`
- `docs/00_Project/IMPLEMENTATION_PROGRESS.md` (DOCSITE-001+)
- `.github/workflows/docs.yml`
