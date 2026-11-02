# Billament

## What This Is

Billament is a Windows desktop app (.NET 10, WPF, SQLite, QuestPDF) that prices 3D print jobs (FDM and resin) from sliced files and makes PDF invoices. It is a public, MIT-licensed personal project shared as-is at https://github.com/cevans3505/Billament (version 4.0.0 is the first release under this name; Phase 3 renames the project and moves existing data once). This milestone makes the project easier to understand at a glance, pays down known concerns in the order that gives the most value per unit of effort, and opens a channel for people to send slice files so support and accuracy can improve.

## Core Value

Prices and invoices stay correct and trustworthy: a finalized invoice never changes, and totals match what the slice file and price book say.

## Requirements

### Validated

<!-- Inferred from the existing codebase (.planning/codebase/). -->

- ✓ Build a job from Bambu Studio / Orca (.3mf, .gcode.3mf) and ELEGOO .goo slice files — existing
- ✓ Price jobs from printers, filaments/resins, labor, overhead and markup via PricingCalculator and PriceBook — existing
- ✓ Freeze prices on finalize (PriceSnapshot) with versioned PricingRules so old invoices never change — existing
- ✓ Generate and print invoice PDFs with plate images (QuestPDF) — existing
- ✓ Store data in local SQLite with daily backups and restore — existing
- ✓ Check GitHub releases for updates; build an error report on failure — existing
- ✓ Core tests (cross-platform) and offscreen WPF UI tests via WpfHost — existing
- ✓ README shows generated images of the main job list, the job editor and page 1 of a sample invoice: Phase 1
- ✓ Images are produced by a repeatable offscreen test and script (`make-readme-images.ps1`) from sample data: Phase 1
- ✓ Every concern in `.planning/codebase/CONCERNS.md` is scored for benefit vs cost in `.planning/CONCERNS-TRIAGE.md`: Phase 2
- ✓ Concerns scored 2.0 and above are fixed (release checksums, legacy freeze test, Explorer arguments, backup temp names, update reply cap, safe invoice file names, QuestPDF license note): Phase 2

### Active

- [ ] The project is renamed to Billament everywhere (code, app, data folders, GitHub repository), existing data moves safely once, and it ships as 4.0.0 (Phase 3)
- [ ] Concerns scored 1.5 are fixed in score order (Phase 4); items that are not really fixable (license, scale limits) are noted and left alone
- [ ] A badly typed invoice or backup folder in Settings shows a plain message instead of the error report (review finding WR-04 from Phase 2)
- [ ] A GitHub issue form collects slice files plus printer make/model, printer type, slicer and version, and expected vs actual results, for both "unsupported printer/slicer" and "inaccurate calculation" reports
- [ ] The app has a menu item that opens that issue form in the browser
- [ ] The issue form and README warn that slice files can contain project names, paths and thumbnails, and should only be shared if the user is fine making them public

### Out of Scope

- In-app upload of slice files — needs a server or token and a new network path; the app keeps network access minimal
- In-app sanitizing of slice files before sharing — warning text is enough for now
- Screenshots of the printers / filaments setup screens — the three chosen images cover the story
- Fixing low-score concerns below the cutoff — tracked in the triage doc for later
- Updating CONCERNS.md with scores — the triage stays in .planning/ only

## Context

- Brownfield: codebase mapped on 2026-11-01 in `.planning/codebase/` (ARCHITECTURE, STACK, CONVENTIONS, CONCERNS, INTEGRATIONS, STRUCTURE, TESTING).
- Layout: `src/Billament.Core` (net10.0, no UI, runs on Linux), `src/Billament.App` (WPF), `tests/Billament.Tests`, `tests/Billament.UiTests` (Windows only, offscreen).
- Big files: `Data/Database.cs`, `Pdf/InvoicePdf.cs`, `ViewModels/JobEditorViewModel.cs`.
- Concerns include: repricing on revert-and-refinalize, retroactive price freeze on every start, string-typed money columns, implicit schema versioning, local-date handling, stale window after restore, backup overlap at startup, shell argument concatenation, restore accepting any DB with the main tables, output names from user data, full job list reload, unsigned releases, no crash recovery for in-progress jobs, no corruption check, and test coverage gaps.
- GitHub attachments do not accept .3mf / .goo directly; submitters will likely need to zip files (to confirm during planning).
- The app already opens GitHub URLs (UpdateService) with a repository allow-check; the new menu link should follow that pattern.

## Constraints

- **Finalized invoices never change**: pricing changes go behind a new `PricingRules` version; old totals must still match.
- **Schema changes**: only via `AddColumnIfMissing` / `CREATE TABLE IF NOT EXISTS` in `Database.EnsureCreated`, with defaults that keep existing data meaning the same.
- **Outside access**: any new network, process start, print or file read/write/delete gets an `// AUDIT:` comment and a row in `AUDITING.md`.
- **UI tests offscreen only**: never show, activate or focus a window, never trigger a dialog; use `WpfHost`. Applies to the screenshot generator too.
- **No personal data**: sample names like "Harbor Dive Shop", "Sample Printing Co." in code, tests, docs and README images.
- **Writing style**: plain English, no em dashes, in docs, comments and messages.
- **File readers**: treat files as untrusted, range-check, cap sizes, throw only `InvalidDataException` with a readable message.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| README images generated by script, offscreen, from sample data | Repeatable, stays current with UI changes, no personal data | ✓ Good: shipped in Phase 1 |
| Images: main job list, job editor, invoice PDF page 1 | Shows what it is and what it produces | ✓ Good: shipped in Phase 1 |
| Score all concerns, fix those above a cutoff in score order | Most value per token spent; skip unfixable items | ✓ Good: 2.0 and above fixed in Phase 2; 1.5 next |
| Triage doc lives in .planning/, not public docs | Internal planning artifact | ✓ Good |
| Submissions via GitHub issue form, plus an app menu link | No new server or network path; reuses GitHub | — Pending |
| Privacy: warning text only, no in-app sanitizer | Lower cost; users decide what to share | — Pending |
| Invoice PDF names: title 60, number 30 (keep the end), whole name 120 characters | Safe names that keep sequence digits; a few unusual old titles get a new name | ✓ Good: Phase 2 |
| Explorer paths passed as separate arguments (ArgumentList) | A path cannot change the command; checked on Windows | ✓ Good: Phase 2 |
| Release checksum verified on 3.7.0, right after Phase 2 merges | Can only run on a real tag | — Pending |
| Rename the project to Billament (bill plus filament), one word: `Billament.exe`, `Billament.*` projects and namespaces | One word makes a clean exe and folder name | — Pending (Phase 3) |
| Rename comes next, before the data-safety fixes | Later phases use the new name from the start | — Pending (Phase 3) |
| Move old data folders once on first start, after a backup; never touch them if the new folders exist | Users keep their data without manual steps; a failed move loses nothing | — Pending (Phase 3) |
| Rename the GitHub repository to cevans3505/Billament | Name matches everywhere; GitHub redirects keep old links and 3.7.0 update checks working | — Pending (Phase 3, manual step) |
| Ship the rename as 4.0.0 | New name, new data folders and a new exe name are a major change | — Pending (Phase 3) |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check: still the right priority?
3. Audit Out of Scope: reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-11-02 after adding Phase 3 (rename to Billament)*
