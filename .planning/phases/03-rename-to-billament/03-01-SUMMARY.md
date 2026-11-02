---
phase: 03-rename-to-billament
plan: 01
subsystem: infra
tags: [rename, git-mv, dotnet, wpf, github-actions, powershell]

requires:
  - phase: 02-quick-win-concern-fixes
    provides: 3.7.0 code base with 485 passing unit tests
provides:
  - Billament.sln with four Billament projects (Core, App, Tests, UiTests) moved with git mv
  - Namespaces, x:Class values, pack URIs, InternalsVisibleTo and all user-visible strings under the name Billament
  - Update check, allow-check and issue page pointing at cevans3505/Billament
  - Scripts, workflows, README, AUDITING.md, both CLAUDE files and codebase docs under the new name
  - Version 4.0.0 and read-me heading "Billament 4.0"
  - A zero-hit baseline for the old name outside the planning history
affects: [03-02, 03-03, 03-04, 03-05, 03-06]

plan_head_before: a7830f18b80338f14e96a2c10d434ffade2074c1
plan_head_after: d18c1c01c385a50273605674e052e5193906ffc3

actuals:
  tokens: 21000
  tasks: 2
  commits: 2

tech-stack:
  added: []
  patterns:
    - "Mechanical rename by git mv plus one scripted sed pass per area, checked by gates instead of file-by-file review"

key-files:
  created: []
  modified:
    - Billament.sln
    - src/Billament.App/Billament.App.csproj
    - src/Billament.App/UpdateService.cs
    - src/Billament.App/AssemblyInfo.cs
    - tests/Billament.Tests/ReaderContract.cs
    - publish.ps1
    - make-readme-images.ps1
    - .github/workflows/ci.yml
    - .github/workflows/release.yml
    - packaging/Read me.txt
    - .planning/PROJECT.md

key-decisions:
  - "No old-name string is written by this plan; later plans add the deliberate ones by hand so the bulk replacement cannot undo them"
  - "No QuestPDF metadata added (none was set before)"

requirements-completed: [NAME-01, NAME-02, NAME-05, NAME-06, NAME-07, NAME-08]

coverage:
  - id: D1
    description: "Solution, four projects, folders, icon file moved with git mv (81 renames); no tracked path or file under src/tests/solution has the old name"
    requirement: NAME-01
    verification:
      - kind: other
        ref: "git ls-files / find / git grep gate in plan Task 1 verify"
        status: pass
    human_judgment: false
  - id: D2
    description: "Billament.sln builds on Linux with 0 warnings and 0 errors; 485 unit tests pass"
    requirement: NAME-01
    verification:
      - kind: unit
        ref: "dotnet build Billament.sln -p:EnableWindowsTargeting=true; dotnet test tests/Billament.Tests (485 passed, 0 failed)"
        status: pass
    human_judgment: false
  - id: D3
    description: "User-visible strings, folder names, update repository, User-Agent and issue page say Billament"
    requirement: NAME-02
    verification:
      - kind: other
        ref: "Task 1 acceptance criteria greps (titles, captions, print job name, Repository constant, DefaultPath, Prefix)"
        status: pass
    human_judgment: false
  - id: D4
    description: "Scripts, workflows and docs renamed; version 4.0.0; read-me heading Billament 4.0 with 13-character underline; workflows parse as YAML"
    requirement: NAME-06
    verification:
      - kind: other
        ref: "Task 2 verify and acceptance criteria"
        status: pass
    human_judgment: false
  - id: D5
    description: "The offscreen screen tests open both windows with the new pack URI icon and the renamed x:Class values on Windows"
    requirement: NAME-01
    verification: []
    human_judgment: true
    rationale: "Windows-only; the UI tests cannot run on Linux. Deferred to plan 03-05 Task 1."

duration: 3min
completed: 2026-11-02
status: complete
---

# Phase 3 Plan 01: Rename the code, scripts, workflows and docs to Billament Summary

**PrintInvoicer became Billament across the solution, four projects, namespaces, pack URIs, strings, scripts, workflows and docs via 81 history-preserving git mv renames and two scripted replacements, shipping as version 4.0.0 with a clean Linux build and all 485 unit tests passing.**

## Performance

- **Duration:** 3 min
- **Started:** 2026-11-02T05:00:59Z
- **Completed:** 2026-11-02T05:03:21Z (before the SUMMARY commit)
- **Tasks:** 2
- **Files modified:** 99 (81 renamed and edited code files, 18 scripts and docs)

## Accomplishments

- Moved `PrintInvoicer.sln`, the four project folders, the four `.csproj` files and the icon with `git mv`, so `git log --follow` keeps history (81 renames staged and committed).
- Replaced the name in 77 code files, then 17 script, workflow and doc files, with three letter-case forms. Build: 0 warnings, 0 errors for `Billament.sln` (compiles the WPF markup, so `x:Class` and `clr-namespace` values are checked). Tests: `Passed! Failed: 0, Passed: 485, Total: 485`.
- `UpdateService.Repository` is `cevans3505/Billament`; the Download allow-check, releases page, API address and `ErrorReport.IssuesPage` derive from it. `ReaderContract` expects `"Billament.Core.Slicing."`. `InternalsVisibleTo("Billament.UiTests")` is the only grant.
- `<Version>4.0.0</Version>`; `packaging/Read me.txt` starts with `Billament 4.0` over a 13-character underline; `release.yml` heading check builds `^Billament <major>.<minor>`.
- `.planning/PROJECT.md` name lines updated by hand. After this plan, no tracked file outside the planning history contains the old name in any letter case.

## Task Commits

1. **Task 1: End-to-end rename of the code base** - `cf5e020` (refactor)
2. **Task 2: Scripts, workflows and docs, version 4.0.0, read-me heading** - `d18c1c0` (chore)

**Plan metadata:** committed separately after this file (docs: complete plan)

The Task 1 tracer feedback gate was satisfied automatically: the automated verify (path/grep gate, build, tests) passed on the committed tree and no human-observable evidence is attached to the task, so execution expanded to Task 2.

## Files Created/Modified

- `Billament.sln` and `src/Billament.{Core,App}/`, `tests/Billament.{Tests,UiTests}/` - renamed projects with namespaces, strings and paths replaced
- `src/Billament.App/Assets/Billament.ico` - renamed icon, referenced by the new pack URI and `<Resource>` entry
- `src/Billament.App/Billament.App.csproj` - AssemblyName, RootNamespace, Product, icon, `<Version>4.0.0</Version>`
- `publish.ps1`, `make-readme-images.ps1`, `tools/Make-Icon.ps1` - new solution, exe, shortcut, install folder and env var names
- `.github/workflows/ci.yml`, `.github/workflows/release.yml` - new paths, zip and checksum names, heading check, release title
- `README.md`, `AUDITING.md`, `CLAUDE.md`, `.claude/CLAUDE.md`, `packaging/Read me.txt`, `.planning/codebase/*.md`, `.planning/PROJECT.md` - new name

## Decisions Made

None beyond the plan: no old-name text was written anywhere (the upgrade notes and data move come in 03-02 to 03-04), and no QuestPDF metadata was added.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Pricing-diff acceptance criterion needed both old and new paths**
- **Found during:** Task 1 (acceptance criteria)
- **Issue:** The criterion `git diff -M HEAD -- src/Billament.Core/Pricing src/Billament.App/Pdf/InvoicePdf.cs` listed only the new paths, so git saw every moved file as a pure addition and printed 1020 instead of 0. This is a flaw in the check command, not in the code.
- **Fix:** Staged the changes and ran `git diff --cached -M HEAD` with both the old and new paths. Result: 34 changed lines in the pricing code and `InvoicePdf.cs`, 0 of them without the old or new name (only rename lines changed).
- **Files modified:** none (check only)
- **Verification:** the re-run printed 0
- **Committed in:** not applicable

The plan's `git clean -fdX -- src tests` step was skipped: `git status --ignored` showed no ignored build output under `src` or `tests`, so there was nothing to clean (and `git clean` is avoided as a rule).

---

**Total deviations:** 1 auto-fixed (1 blocking check-command fix). 
**Impact on plan:** None on the code; the intended check (pricing and invoice layout changed only on rename lines) passes.

## Issues Encountered

None.

## Deferred to Windows (plan 03-05)

- `dotnet test Billament.sln` including every screen test: opens `MainWindow` and `JobEditorWindow` offscreen, which loads the new pack URI to `Assets/Billament.ico` and the renamed `x:Class` values. Cannot run on Linux.
- `publish.ps1`, `make-readme-images.ps1` and the README pictures were edited as text only and not run.

## Known Stubs

None.

## Threat Flags

None. No new network, process, file or schema surface was added; only names changed. The T-03-01 to T-03-04 mitigations ran: file lists came from `git grep -l -I` over named paths, pricing and layout diff shows rename lines only, exact-string gates cover the pack URIs, AssemblyName, Resource, InternalsVisibleTo and Repository constant, both workflows parse as YAML, and the history folders are unchanged.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 03-02 (one-time safe data move). The code base is under the new name, builds clean and passes 485 tests, so the data-move code can be written under `Billament.Core.Data` with the old names typed by hand.

## Self-Check: PASSED

- Files: `Billament.sln`, `src/Billament.Core/Billament.Core.csproj`, `src/Billament.App/Billament.App.csproj`, `tests/Billament.Tests/Billament.Tests.csproj`, `tests/Billament.UiTests/Billament.UiTests.csproj`, `src/Billament.App/Assets/Billament.ico` all exist.
- Commits `cf5e020` and `d18c1c0` are ancestors of HEAD; measured `commits: 2` from the ledger base `a7830f1`.
- All Task 1 and Task 2 acceptance criteria re-run and pass (except the flawed pricing-diff command, re-run correctly as noted above).

---
*Phase: 03-rename-to-billament*
*Completed: 2026-11-02*
