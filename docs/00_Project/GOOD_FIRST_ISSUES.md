# Good first issues (OSS-004)

Draft backlog for new contributors. Open as GitHub issues when ready; labels: `good first issue`, `documentation` or `enhancement`.

## 1. Expand MkDocs nav for a missing deep-link page

**Why:** Some engineering docs are not yet in `mkdocs.yml` nav.  
**Task:** Pick one Implemented doc under `docs/03_Components/` or `docs/02_Mathematics/` and add a nav entry; verify `mkdocs build`.  
**Skills:** Markdown, YAML.

## 2. Add a regression scenario JSON

**Why:** Scenario pack should grow toward `22_TestStrategy` §17.  
**Task:** Add `samples/scenarios/dry-cool-day.json` (or similar) cloning an existing scenario with colder/drier ambient; register in regression catalog if required.  
**Skills:** JSON, AWG config familiarity.

## 3. Improve Blazor empty-state copy

**Why:** Compare/persisted lists show brief empty messages.  
**Task:** Polish empty states on `/simulations/compare` and `/models` with links to Configuration / Quick run.  
**Skills:** Razor, CSS.

## 4. Document console `sweep` / `sensitivity` examples in README

**Why:** OPT commands exist but README quick start is thin.  
**Task:** Add a short “Optimization” subsection with copy-pasteable commands.  
**Skills:** Markdown.

## 5. Add unit test for series downsample helper

**Why:** `from`/`to`/`intervalSeconds` on series need edge-case coverage.  
**Task:** Extend Api or Application tests for empty slice, interval &gt; length, invalid range.  
**Skills:** xUnit, C#.
