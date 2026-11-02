---
phase: 03-rename-to-billament
verified: 2026-11-02T08:30:00Z
status: passed
score: 13/13 must-haves verified
covered_files:
  - .github/workflows/ci.yml
  - .github/workflows/release.yml
  - .planning/phases/03-rename-to-billament/03-01-PLAN.md
  - .planning/phases/03-rename-to-billament/03-01-SUMMARY.md
  - .planning/phases/03-rename-to-billament/03-02-PLAN.md
  - .planning/phases/03-rename-to-billament/03-02-SUMMARY.md
  - .planning/phases/03-rename-to-billament/03-03-PLAN.md
  - .planning/phases/03-rename-to-billament/03-03-SUMMARY.md
  - .planning/phases/03-rename-to-billament/03-04-PLAN.md
  - .planning/phases/03-rename-to-billament/03-04-SUMMARY.md
  - .planning/phases/03-rename-to-billament/03-05-PLAN.md
  - .planning/phases/03-rename-to-billament/03-05-SUMMARY.md
  - .planning/phases/03-rename-to-billament/03-06-PLAN.md
  - .planning/phases/03-rename-to-billament/03-06-SUMMARY.md
  - AUDITING.md
  - Billament.sln
  - README.md
  - packaging/Read me.txt
  - publish.ps1
  - src/Billament.App/App.xaml.cs
  - src/Billament.App/Billament.App.csproj
  - src/Billament.App/UpdateService.cs
  - src/Billament.App/ViewModels/SettingsViewModel.cs
  - src/Billament.Core/Data/DataFolderMigration.cs
  - src/Billament.Core/Data/DatabaseBackup.cs
  - tests/Billament.Tests/BackupTests.cs
  - tests/Billament.Tests/DataFolderMigrationTests.cs
  - tests/Billament.UiTests/WpfHost.cs
covered_digest: "v4:sha256:7f278950c8e80ae051061e3ecba88f9072b915cd329be6b700af665a7ddcb977"
behavior_unverified: 0
overrides_applied: 0
re_verification:
  previous_status: human_needed
  previous_score: 11/13
  gaps_closed:
    - "Truth 5: the Documents failure path in the app (message, Shutdown(1), no new folder) now has Windows evidence from Sandbox case A"
    - "Truth 10: a 3.7.0 install finds 4.0.0 through GitHub's redirect (live checks and the Sandbox update check)"
  gaps_remaining: []
  regressions: []
---

# Phase 3: Rename to Billament Verification Report

**Phase Goal:** PrintInvoicer becomes Billament everywhere (code, app, PDFs, scripts, docs, data folders and the GitHub repository), existing users keep every job and finalized invoice through a one-time safe move of their data, and the result ships as 4.0.0, so later phases are built under the new name from the start.
**Verified:** 2026-11-02
**Status:** passed
**Re-verification:** Yes, after plans 03-05 (Windows checks and Sandbox upgrade test) and 03-06 (repository rename, redirect checks, 4.0.0 release) ran

## Scope of this run

The first run (status human_needed, 11/13) could only check plans 03-01 to 03-04. Both open items are now closed:

- Truth 5 (the app's reaction to a failed Documents move) was the only behavior-unverified truth. Windows Sandbox case A (3.7.0 left open) recorded the plain Billament message, the app closing, no `Documents\Billament` folder and the old folder unchanged.
- Truth 10 (the live redirect) was uncertain. It now has live evidence, re-checked in this run (below).

I did not rely on the SUMMARY text alone. Checks run in this session (read-only, nothing touched the user's Documents or AppData folders, the UI tests and the app were not run):

| Check | Result |
| ----- | ------ |
| `gh release view v4.0.0 --repo cevans3505/Billament` | `isDraft: false`, name `Billament 4.0.0`, published 2026-11-02T07:38:12Z; assets `Billament-4.0.0.zip` (sha256 digest `0d1ebf86feab9233f1a9d5b9f1b145cc36acb6644f8843af7c35fbd4a490c5b5`, same as the `SHA-256:` line in the notes and in 03-06-SUMMARY.md) and `Billament-4.0.0.zip.sha256` |
| `gh repo view cevans3505/Billament` | `nameWithOwner: cevans3505/Billament` |
| `curl -sSIL` on `api.github.com/repos/cevans3505/PrintInvoicer/releases/latest` | `301 Moved Permanently`, `Location: https://api.github.com/repositories/1439674558/releases/latest`, then `200 OK`; the body has `"tag_name":"v4.0.0"` |
| `curl -sSIL https://github.com/cevans3505/PrintInvoicer/releases` | `301`, `Location: https://github.com/cevans3505/Billament/releases`, then `200 OK` |
| `git remote get-url origin` and `git ls-remote --tags origin v4.0.0` | `https://github.com/cevans3505/Billament.git`; tag `v4.0.0` exists at 6ef50ef |
| `git diff --stat 6ef50ef HEAD` (tag to now) | Only `.planning` files, `AUDITING.md` (+2 lines), `README.md` (+2 lines, logo), `docs/images/logo.png`, `tools/make_logo.py`. No `src`, `tests`, `publish.ps1`, workflow or packaging change after the tag, so the shipped code is the code verified here |
| `dotnet test tests/Billament.Tests` | Passed 537, Failed 0 (the first run had 525; the review fixes WR-01 to WR-05 and 03-05 added tests) |
| `git grep -l -i printinvoicer` outside the phase 01 to 03 folders | 15 files, all on the allowlist: release.yml, CONCERNS-TRIAGE.md, REQUIREMENTS.md, ROADMAP.md, STATE.md, AUDITING.md, CLAUDE.md, README.md, `packaging/Read me.txt`, publish.ps1, UpdateService.cs, SettingsViewModel.cs, DataFolderMigration.cs, DatabaseBackup.cs, BackupTests.cs |
| `git ls-files | grep -i printinvoicer` | empty |
| `<Version>4.0.0</Version>`; `Read me.txt` line 1 | `Billament.App.csproj:21`; `Billament 4.0` followed by 13 `=` |
| Em or en dash outside `.planning` | none |
| Phases 01 and 02 changed since b87258a | none |
| `git status --short` | only `03-UAT.md` modified (the recorded UAT results, uncommitted at the start of this run) |

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
| -- | ----- | ------ | -------- |
| 1  | SC1/NAME-01: solution `Billament.sln`; projects, folders, namespaces `Billament.Core/App/Tests/UiTests`; exe and assembly `Billament`; `InternalsVisibleTo("Billament.UiTests")` only; icon `Billament.ico` | VERIFIED | Unchanged from the first run (renames with history, no old-name tracked path). Run-time load of the pack URI and renamed `x:Class` values is now proven: 11 screen tests passed on Windows (UAT 1) and in CI on 6ef50ef, and the real `Billament.exe` started in the Sandbox with the title Billament. |
| 2  | SC1/NAME-02: everything the user sees says Billament | VERIFIED | Static evidence as before. Windows evidence added: the Sandbox run read the Billament windows through UI Automation (window title, the safety-stop message, the information message all say Billament); the user confirmed the Start menu shortcut name and icon (UAT 6); `Read me.txt` and the release notes say Billament. |
| 3  | SC2/NAME-03: with only the old folders, a safety copy is made first, then local folder, then Documents folder, then `printinvoicer.db` becomes `billament.db`; finalized totals and snapshots byte for byte the same; pre-rename copy never pruned | VERIFIED | Core tests as before (all pass in the 537). Real run added: Sandbox case B (7 jobs, $370.66, every job total equal to 3.7.0, `billament.db` equal to the original in every table except the two folder settings, `Backups\billament-...-pre-rename.db` present, `Documents\PrintInvoicer` gone). The developer's own data moved too and `billament.db` equals its pre-rename copy except the invoice folder setting (see Deviation). |
| 4  | SC3/NAME-03: new folder present means old one untouched; failures leave old data and give a plain message; logic in Core with Linux tests; `// AUDIT:` and AUDITING.md rows | VERIFIED | Core tests as before. Real run added: case E (an empty `Documents\Billament` exists): information message, `Documents` untouched. Case D (second start): nothing moved. AUDITING.md count and rows were kept in step by the review fixes (WR-01 to WR-05 commits each touch AUDITING.md where tags changed). |
| 5  | SC3: on a Documents failure the app shows the plain message, saves details, calls `Shutdown(1)` before any `new Database` | VERIFIED | Code: `App.OnStartup` calls `if (!MoveOldData()) return;` before `Db = new Database(...)`; `MoveOldData` shows the message, saves details and calls `Shutdown(1)` (read in `App.xaml.cs` lines 70 to 125). Behavior now exercised on Windows: Sandbox case A with 3.7.0 open showed the plain message naming `Documents\PrintInvoicer`, Billament closed, `Documents\Billament` was not created, and `Documents\PrintInvoicer` was unchanged apart from the safety copy. RESEARCH A4 holds (a running 3.7.0 blocks the folder move). |
| 6  | SC2/NAME-04: a BackupFolder or InvoiceFolder inside the old folder points at the same place under the new one after the move; elsewhere untouched; only those two rows change | VERIFIED | Core tests as before; WR-03 (rewrite skipped when no database file was renamed) is now fixed in 3e2c4ce with new tests. Real run: case C, "Back up to" became `Documents\Billament\My backups` and the invoice folder `Documents\Billament\Invoices 2026`; case B shows only the two folder settings differ. |
| 7  | SC2/NAME-04: restoring a pre-4.0 backup applies the same two-key rewrite (D-25) | VERIFIED (Core and wiring) | `SettingsViewModel.RestoreBackup` calls `DataFolderMigration.UpdateFolderSettings` after `DatabaseBackup.Restore`; test `A_restored_backup_from_before_the_rename_gets_its_folders_updated` passes. Honest note: the real Restore dialog, Back up now and the restore picker were not clicked through in the Sandbox (UAT 11); the user accepted coverage by `BackupTests` and `DataFolderMigrationTests`. The old `printinvoicer-*` backups did move along and sit in the new folder. This is a recorded, accepted limit, not a gap. |
| 8  | NAME-04: `billament-*` and `printinvoicer-*` backups are listed, restored and pruned by date; upgrade-day skip; picker offers both | VERIFIED | `BackupTests` pass (537 total). Old daily and manual backups moved along with the data in Sandbox case C. |
| 9  | SC4/NAME-05 (code side): `UpdateService.Repository` is `cevans3505/Billament`; allow-check, releases page, API address and issue page derive from it; remark about the redirect | VERIFIED | Unchanged (`UpdateService.cs:31`). |
| 10 | SC4/NAME-05 (live side): a 3.7.0 install still finds 4.0.0 through GitHub's redirect | VERIFIED | Re-checked live in this run: the old API address redirects (301, then 200) and returns `tag_name v4.0.0` for a published, non-draft release; the old releases page redirects to the Billament releases page. Recorded evidence: PrintInvoicer 3.7.0 in Windows Sandbox with networking on said "You have the latest version (3.7.0)" before publishing and "PrintInvoicer 4.0.0 is available. You have 3.7.0." with the Download button after publishing. The Download button was shown but not clicked; its target is the releases page, whose redirect was checked. (The status text uses the 3.7.0 client's own wording, so the "Version 4.0.0 is available" text in the plan differs only in the name the old client prints.) |
| 11 | SC5/NAME-06: `publish.ps1`, `make-readme-images.ps1`, `tools/Make-Icon.ps1`, `ci.yml`, `release.yml` use the new names | VERIFIED | Static evidence as before, plus real runs: `publish.ps1` refused to run while 3.7.0 was open (A5 holds), then ran the tests, installed `Billament.exe` and `Billament.lnk`, removed `PrintInvoicer.lnk` and the old program folder, left the data folders unchanged. `make-readme-images.ps1` rebuilt all three pictures byte for byte the same. CI passed on 6ef50ef, and the Release workflow passed the tag check with no read-me heading warning and made the draft `Billament 4.0.0`. WR-02 (nested or junctioned `-InstallDir`) is fixed in 8bb3b28. |
| 12 | SC5/NAME-07/NAME-08: docs renamed; rename section; `Read me.txt` heading and "Coming from PrintInvoicer"; CLAUDE.md rule; version 4.0.0; search finds only deliberate hits | VERIFIED | Re-ran the search (table above): 15 allowlisted files, no tracked old-name path, version 4.0.0, `Billament 4.0`, no dashes outside `.planning`. The shipped zip holds only `Billament.exe` and `Read me.txt` (first line `Billament 4.0`, has "Coming from PrintInvoicer"). The README logo and `tools/make_logo.py` came after the tag; they are documented in README and AUDITING.md and hold no old name. |
| 13 | Prohibitions: no pricing or invoice-layout change; no QuestPDF metadata; no edit to historical phase folders or CONCERNS-TRIAGE; move never deletes/overwrites/merges; no new DB opened before the move; only the two settings rows written | VERIFIED | As before. Finalized totals were checked on a real database: unchanged in the Sandbox (7 jobs, $370.66, every job total the same). Phases 01 and 02 have no diff since b87258a. One deviation in how the move was first triggered is recorded below; it did not break any prohibition. |

**Score:** 13/13 truths verified, 0 behavior-unverified.

### Deviation: the developer's own data was moved by a screen test run

This is real and should stay on record. During 03-05 the first full `dotnet test` run on the developer's PC moved the real `Documents\PrintInvoicer` folder (and the `%LOCALAPPDATA%` one) to the Billament names at 01:14:07, before any `Billament.exe` had been started and before the planned copy. The cause: WPF's `Application` constructor queues `OnStartup`, so `WpfHost` ran the full real start-up on every screen test run. That was already true before the phase (real database opened, a daily backup, a main window); it became harmful once start-up moves folders.

- Nothing was lost. The move made its `pre-rename` safety copy first, and `billament.db` equals that copy in every table except the `InvoiceFolder` setting, which changed from `Documents\PrintInvoicer\Invoices` to `Documents\Billament\Invoices` as designed. All 7 jobs (7 invoiced) are there.
- Fix: commit ed3a80a adds `App.RunStartup` (internal, default true) and `WpfHost` sets it to false before creating the App (`WpfHost.cs:42`); `OnStartup` returns right after `base.OnStartup` when it is false (`App.xaml.cs:51`). Checksums of both old and both new data folders were the same before and after a full `dotnet test` run with the fix; CI on 6ef50ef also passed. Commit ec65be8 adds a test that the backup path is set before use.
- Side effect: starting 3.7.0 for the A5 check after the move made a new, empty `Documents\PrintInvoicer` (0 jobs) and an `update-check.json`; with the user's OK these were moved aside, not deleted.
- Effect on the verdict: the planned order (Sandbox on a copy first, then own data) was not followed for the own-data step, but the Sandbox cases A to E all ran on a copy afterward and passed, and the own-data move was proven lossless by comparing it with the safety copy. The finalized-invoices rule held. This is a process deviation, not a failed must-have.

### Deferred Items

None. Later phases (4 and 5) cover FIX-08 to FIX-12 and SUBM-01 to SUBM-06, none of which are open items here.

### Required Artifacts

All artifacts from the first run still hold (the files in `covered_files` were re-fingerprinted). Added or changed since: `App.RunStartup` in `App.xaml.cs` and `WpfHost.cs` (VERIFIED, wired by `WpfHost.cs:42`), the WR-01 to WR-05 changes in `DataFolderMigration.cs`, `App.xaml.cs` and `publish.ps1` (each with new tests or script guards), and the release assets `Billament-4.0.0.zip` and `Billament-4.0.0.zip.sha256` (VERIFIED, published, hash matches).

### Key Link Verification

All links from the first run remain WIRED. The two that were pending are now closed:

| From | To | Via | Status | Details |
| ---- | -- | --- | ------ | ------- |
| `UpdateService.cs` | GitHub repo `cevans3505/Billament` | `Repository` constant | WIRED | repository renamed; 3.7.0 reaches it through the 301 redirect |
| `MainWindow.xaml` | `Billament.App.csproj` | pack URI and `Resource` icon | WIRED | loaded at run time by the screen tests and the real app |

### Data-Flow Trace (Level 4)

Not applicable: no new rendered dynamic data. The data that matters (finalized totals through the move) was traced by the byte-for-byte test and by the Sandbox comparison.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Core unit suite | `dotnet test tests/Billament.Tests` | Passed 537, Failed 0 | PASS |
| Windows full suite | recorded, UAT 1 (not re-run, by instruction) | 537 unit and 11 screen tests passed; CI on 6ef50ef passed | recorded |
| Old API address reaches 4.0.0 | `curl -sSL` on the old address | `tag_name v4.0.0` | PASS |
| Old releases page redirects | `curl -sSIL` | 301 to the Billament releases page, then 200 | PASS |
| Release is published with both assets | `gh release view v4.0.0` | `isDraft: false`, zip and `.sha256` present, digest matches | PASS |

### Probe Execution

No `probe-*.sh` scripts exist and none are declared. Step 7c: SKIPPED.

### Requirements Coverage

| Requirement | Source Plans | Status | Evidence |
| ----------- | ------------ | ------ | -------- |
| NAME-01 | 03-01, 03-05 | SATISFIED | truths 1, 13; Windows suite and CI pass under the new names |
| NAME-02 | 03-01, 03-05 | SATISFIED | truth 2; Windows windows and shortcut checked |
| NAME-03 | 03-02, 03-05 | SATISFIED | truths 3, 4, 5; Sandbox cases A, B, D, E and the own-data move |
| NAME-04 | 03-03, 03-05 | SATISFIED (with a note) | truths 6, 7, 8; settings and old backups followed the move on a real run; the restore dialog itself was covered by tests, not clicked, and the user accepted that |
| NAME-05 | 03-01, 03-04, 03-06 | SATISFIED | truths 9, 10; live redirect checks and the 3.7.0 update-bar check; Download not clicked, target checked by curl |
| NAME-06 | 03-01, 03-04, 03-05 | SATISFIED | truth 11; real `publish.ps1`, README images script, CI and Release workflow runs |
| NAME-07 | 03-01, 03-04, 03-05 | SATISFIED | truth 12; pictures rebuilt and unchanged (they show no app name) |
| NAME-08 | 03-01, 03-04, 03-06 | SATISFIED | truth 12; published 4.0.0 and the final search re-run |

No orphaned IDs. The REQUIREMENTS.md checkboxes and traceability rows still say Pending; they can now be ticked (for the orchestrator, outside this report).

### Anti-Patterns Found

Debt-marker scan: no unreferenced `TBD`, `FIXME` or `XXX` in the phase's files. Code review: WR-01 to WR-05 are fixed (commits ba15e24, 8bb3b28, 3e2c4ce, 0c6e87d, 0d43bfc, recorded in 03-REVIEW-DISPOSITION.md). IN-01 to IN-05 are still `open`; they are info items that touch no must-have.

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| review items IN-01 to IN-05 | | error file blocks local move, AUDIT tags at call sites, `\d` in prune regex, repeated safety copies, wording | INFO | None on any must-have. Left for the owner. |

No blocker. No em or en dashes and no real names in the changed source files.

### Human Verification Required

None open. The 26 items from the first run are recorded as 27 UAT results in `03-UAT.md`, all pass (27/27). Two of them carry honest limits, noted above and accepted by the user: the restore picker and the D-25 restore were not clicked in the Sandbox (UAT 11), and the Download button was not clicked (UAT 25).

### Gaps Summary

No gaps. The rename is in the code, the docs, the scripts and the repository; the one-time move kept every job and every finalized total on a real run; the failure path stopped safely on a real run; the repository was renamed without breaking the 3.7.0 update check; and 4.0.0 is published with a matching checksum. The security audit (`03-SECURITY.md`) is verified with 30 threats closed and 0 open.

---

_Verified: 2026-11-02_
_Verifier: Claude (gsd-verifier)_
