---
phase: 03-rename-to-billament
verified: 2026-11-02T06:00:00Z
status: human_needed
score: 11/13 must-haves verified
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
  - .planning/phases/03-rename-to-billament/03-06-PLAN.md
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
covered_digest: "v4:sha256:68ed98a630df7780c7bc93b48ea1febd7a3c85eccf0faaffdc2e5426cbd0bdbb"
behavior_unverified: 1
overrides_applied: 0
behavior_unverified_items:
  - truth: "On a Documents failure the app shows the plain message, saves details with ErrorReport.Save, and calls Shutdown(1) before any new Database is opened, so no new data folder is created (plan 03-02)"
    test: "Start Billament.exe on a copy of 3.7.0 data while PrintInvoicer 3.7.0 still has its database open (03-05 case A)"
    expected: "A Billament message names Documents\\PrintInvoicer and says nothing was lost; Billament closes; Documents\\Billament does not exist; Documents\\PrintInvoicer is unchanged"
    why_human: "The Core half (DataFolderMigration.Run returning DocumentsFailed with the old data in place and no new folder) is covered by Linux tests. The App half (MoveOldData, MessageBox, Shutdown(1), return before new Database) is WPF code that cannot run on Linux and no test exercises it. Whether an open 3.7.0 database really makes the folder rename fail (RESEARCH A4) is also only knowable on Windows."
human_verification:
  - test: "03-05 Task 1 step 1: run `dotnet test Billament.sln --nologo` on Windows (both test projects, including every screen test)"
    expected: "Two `Passed!  - Failed: 0` summary lines (unit project and screen-test project) and no `Failed!` line. The screen tests load the pack URI pack://application:,,,/Billament;component/Assets/Billament.ico and the renamed x:Class values at run time, and the update bar reads `Billament 3.4.0 is available. You have 3.3.0.`"
    why_human: "The UI tests are Windows-only (WPF, interactive desktop session). Built here with -p:EnableWindowsTargeting=true (0 warnings, 0 errors), never run."
  - test: "Prior-phase UI test files that the regression gate could not run on Linux: tests/Billament.UiTests/ReadmeImageTests.cs, tests/Billament.UiTests/SampleShop.cs, tests/Billament.UiTests/ScreenTests.cs, tests/Billament.UiTests/WpfHost.cs (they run as part of the UI test suite in the step above)"
    expected: "All pass offscreen after the rename (no window shown or focused, no dialog opened); SampleShop still builds the sample shop with 'Harbor Dive Shop' style names; ReadmeImageTests reads the BILLAMENT_README_IMAGES environment variable; WpfHost uses %TEMP%\\Billament UI tests"
    why_human: "Windows-only. They compile on Linux; they have not run since the namespace, env var and temp folder renames."
  - test: "03-05 Task 1 step 2: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./make-readme-images.ps1`, then `git status --porcelain -- docs/images`"
    expected: "Exit 0, no `was not rebuilt` or `The README image test failed` text. The three pictures docs/images/main-jobs.png, job-editor.png and invoice-page-1.png are rebuilt; they show no app name, so a diff is rendering noise (restore with `git checkout -- docs/images`) or a real UI change (review, then commit). `git status --porcelain -- docs/images` prints nothing at the end of the task."
    why_human: "Needs Windows, PowerShell 5.1 and WPF rendering; whether a pixel change is noise is a visual judgment."
  - test: "03-05 Task 1 step 3 (optional, RESEARCH A5): with PrintInvoicer 3.7.0 installed and running, run publish.ps1 once"
    expected: "It stops with `Billament (or the old PrintInvoicer) is running. Close it and run this again.` (Get-Process with two names works in Windows PowerShell 5.1). If 3.7.0 is not installed, record `3.7.0 not installed, A5 not checked`."
    why_human: "Needs Windows PowerShell 5.1 and a running old program."
  - test: "03-05 Task 1 steps 4 and 5: record md5sums of Documents\\PrintInvoicer and %LOCALAPPDATA%\\PrintInvoicer into $TEMP/bm-data-before.txt, then run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./publish.ps1` for real, then diff"
    expected: "publish.ps1 exits 0 (it runs the tests, publishes, installs). `%LOCALAPPDATA%\\Programs\\Billament\\Billament.exe` and the Start menu `Billament.lnk` exist; the old `PrintInvoicer.lnk` and `%LOCALAPPDATA%\\Programs\\PrintInvoicer\\PrintInvoicer.exe` are gone; `publish/Billament.exe` exists (kept for Task 2); the md5sum diff of the two old data folders is empty (publish.ps1 touched no data)."
    why_human: "Real install and delete on a Windows profile. Includes the unreviewed edge from code review WR-02 (a nested or junctioned -InstallDir); use the default install folder for this run."
  - test: "03-05 Task 1 human check: look at the Start menu shortcut, and open any changed README picture"
    expected: "The shortcut is named Billament and shows the app icon. A changed picture shows only rendering differences (restored) or a real UI change (committed with a note)."
    why_human: "Icon and pixel changes are visual judgments."
  - test: "03-05 Task 1 summary: record in 03-05-SUMMARY.md the test totals, the picture decision, what publish.ps1 printed (installed, removed) and the A5 result"
    expected: "03-05-SUMMARY.md exists with those four records; `test -f publish/Billament.exe` exits 0"
    why_human: "Written by whoever runs 03-05."
  - test: "03-05 Task 2 prepare: in Windows Sandbox or a second Windows account (never the only copy of real data), put PrintInvoicer.exe from PrintInvoicer-3.7.0.zip, a COPY of real Documents\\PrintInvoicer and %LOCALAPPDATA%\\PrintInvoicer, and publish\\Billament.exe. Start 3.7.0; write down the job count and three finalized totals; set Back up to `...\\Documents\\PrintInvoicer\\My backups` and the invoice folder to `...\\Documents\\PrintInvoicer\\Invoices 2026`; Save; click Back up now once; leave 3.7.0 open"
    expected: "A setup with a known job count, three known finalized totals, two custom folders inside the old data folder and at least one printinvoicer-<date>.db backup"
    why_human: "Needs a second Windows session, real data copy, and a person reading screens."
  - test: "03-05 Task 2 case A (NAME-03, RESEARCH A4, covers behavior_unverified item above): with 3.7.0 still open, start Billament.exe"
    expected: "A message titled Billament that names Documents\\PrintInvoicer, says nothing was lost and asks you to close PrintInvoicer; Billament closes. Documents\\Billament does not exist. Documents\\PrintInvoicer is unchanged (its Backups folder now also holds billament-<date>-<time>-pre-rename.db). %LOCALAPPDATA%\\Billament may exist (the local folder moves first). If the move is NOT stopped (A4 wrong), record a gap for `/gsd-plan-phase 3 --gaps` and stop before 03-06 (for example, a check in App.MoveOldData that refuses to move while a process named PrintInvoicer runs)."
    why_human: "Needs real Windows file locking, a running 3.7.0 and WPF dialogs."
  - test: "03-05 Task 2 case B (NAME-03): close 3.7.0, start Billament.exe"
    expected: "No message; the window title says Billament; the job count and the three finalized totals equal what was written down (a difference stops the phase: it breaks the finalized-invoices rule); Documents\\Billament\\billament.db and Documents\\Billament\\Backups\\billament-<date>-<time>-pre-rename.db exist; Documents\\PrintInvoicer is gone."
    why_human: "Real data, real exe, a person comparing totals."
  - test: "03-05 Task 2 case C (NAME-04, D-25): open Settings; click Back up now; open Restore a backup and cancel; then restore the pre-rename copy through Restore a backup and let Billament restart"
    expected: "Back up to shows `...\\Documents\\Billament\\My backups` and the invoice folder `...\\Documents\\Billament\\Invoices 2026`; Back up now writes a new file into Documents\\Billament\\My backups; Restore a backup lists the old printinvoicer-*.db daily backups next to new ones; after restoring the pre-rename copy and the restart the job list matches and the Settings tab still shows both folders under `...\\Documents\\Billament`."
    why_human: "Real file dialog, restart through Process.Start, and settings read back in the real UI."
  - test: "03-05 Task 2 case D (run once): close Billament and start it again"
    expected: "No message, nothing moves."
    why_human: "Real second start on the moved data."
  - test: "03-05 Task 2 case E (optional, decision c / D-24): fresh session with the old data copy plus an empty Documents\\Billament folder, start Billament"
    expected: "An information message saying where the old data is and how to use it; nothing moved."
    why_human: "Real dialog on Windows."
  - test: "03-05 Task 2 own data: copy Documents\\PrintInvoicer somewhere safe (for example a zip on the Desktop), then start the installed Billament from the Start menu on the developer's own account"
    expected: "Same result as case B on the real data. Do this only after cases A to D passed on the copy. Record the outcome in 03-05-SUMMARY.md (or 03-UAT.md) as pass or fail per case, with before and after job count and three totals for case B."
    why_human: "Real data, one-way move (D-06); must follow the copy test."
  - test: "03-06 Task 1 (run again on the final commit, after 03-05 may have committed pictures or fixes): the final search and release-readiness block"
    expected: "`git grep -l -i printinvoicer` outside the three phase-01/02/03 folders lists only the allowlist (release.yml, CONCERNS-TRIAGE.md, PROJECT.md, REQUIREMENTS.md, ROADMAP.md, STATE.md, state.json, AUDITING.md, CLAUDE.md, README.md, packaging/Read me.txt, publish.ps1, UpdateService.cs, SettingsViewModel.cs, DataFolderMigration.cs, DatabaseBackup.cs, BackupTests.cs, DataFolderMigrationTests.cs); no tracked path and no on-disk path contains the old name; \"PrintInvoicer\" is in DataFolderMigration.cs, \"printinvoicer-\" is in DatabaseBackup.cs, <Version>4.0.0</Version> is in Billament.App.csproj, line 1 of Read me.txt is `Billament 4.0`, `git status --porcelain` is empty, and no em dash or en dash is outside .planning. 03-06-SUMMARY.md says the hits are unchanged from 03-04-SUMMARY.md or lists new ones with reasons. (This verification ran the same search at 2026-11-02 and it passed on the cloud commit.)"
    why_human: "Must be re-run on the commit that is released, after the Windows plan."
  - test: "03-06 Task 2 steps 1 and 2 (NAME-05, D-14): on GitHub rename cevans3505/PrintInvoicer to Billament (Settings > General > Repository name); optionally `git remote set-url origin https://github.com/cevans3505/Billament.git`"
    expected: "The repository is cevans3505/Billament. Never create a new repository named PrintInvoicer under the account afterwards (it would end the redirects 3.7.0 depends on)."
    why_human: "A GitHub account action only the owner takes; the plan forbids an agent doing it."
  - test: "03-06 Task 2 step 3 (RESEARCH A1): `curl -sSIL -H \"User-Agent: check\" -H \"Accept: application/vnd.github+json\" https://api.github.com/repos/cevans3505/PrintInvoicer/releases/latest`, then the same without -I piped to `grep '\"tag_name\"'`"
    expected: "First status line 301 or 307 with a `location:` header on api.github.com ending in `/releases/latest`, last status line 200; body check prints `\"tag_name\": \"v3.7.0\"`. A 200 with no tag_name line means the redirect lost the path."
    why_human: "Needs the repository to be renamed first."
  - test: "03-06 Task 2 step 4 (RESEARCH A2): `curl -sSIL https://github.com/cevans3505/PrintInvoicer/releases`"
    expected: "A redirect whose `location:` is `https://github.com/cevans3505/Billament/releases`, then 200."
    why_human: "Needs the renamed repository."
  - test: "03-06 Task 2 step 5: in the 03-05 sandbox with PrintInvoicer 3.7.0 and a copy of its data, open Settings and click Check now"
    expected: "`You have the latest version (3.7.0). Checked ...` (4.0.0 is not published yet). `Could not reach GitHub to check.` on a machine with internet means 3.7.0's client did not get through the redirect."
    why_human: "Needs the installed 3.7.0 client and the live redirect."
  - test: "03-06 Task 2 step 7 (only if any of steps 3 to 5 failed): reply `wait`, `roll back` or `publish with fallback` and record the reply with its date"
    expected: "Nothing is tagged or published before the reply is recorded (`git ls-remote --tags origin v4.0.0` prints nothing). `publish with fallback` records NAME-05 as `fallback accepted by the user`, never as passed. The pasted outputs of steps 3 to 5 are recorded under `Task 2 result` in 03-06-SUMMARY.md (or 03-UAT.md); `git remote get-url origin` shows the chosen address."
    why_human: "A decision only the owner can take (D-16, D-05)."
  - test: "03-06 Task 3 step 1: push main and wait for CI"
    expected: "Both CI jobs pass (Build and unit tests, Screen tests) on the renamed repository."
    why_human: "Needs the push and the hosted Windows runner."
  - test: "03-06 Task 3 steps 2 and 3: `git tag v4.0.0` and `git push origin v4.0.0`; watch the Release workflow"
    expected: "The `Check the tag matches the app version` step passes with no read-me heading warning, and the run creates a draft release titled `Billament 4.0.0`. One-way (D-05); do it only after Task 2 is recorded as `checks passed` or as the user's own `publish with fallback` reply."
    why_human: "Pushing a tag and publishing is the owner's action; cannot be undone."
  - test: "03-06 Task 3 step 4: download Billament-4.0.0.zip and Billament-4.0.0.zip.sha256 from the draft; `Get-FileHash .\\Billament-4.0.0.zip -Algorithm SHA256`; unzip"
    expected: "The hash equals the one in the .sha256 file and the `SHA-256:` line in the draft notes (case does not matter). The zip holds Billament.exe and Read me.txt; line 1 of Read me.txt is `Billament 4.0` and the file has the `Coming from PrintInvoicer` section."
    why_human: "Needs the built release asset and Windows PowerShell."
  - test: "03-06 Task 3 step 5: write the release notes (keep the `SHA-256:` line and the `Upgrading from PrintInvoicer 3.7 or older` paragraph, add a short summary of 4.0.0) and publish the release"
    expected: "Release v4.0.0 is published with both assets attached and the upgrade paragraph still in the notes."
    why_human: "One-way publication by the owner."
  - test: "03-06 Task 3 step 6 (NAME-05, success criterion 4): in the sandbox with a fresh copy of the 3.7.0 data and PrintInvoicer 3.7.0, open Settings and click Check now; then click Download"
    expected: "Status reads `Version 4.0.0 is available. You have 3.7.0.` and the update bar appears; Download opens the Billament releases page through the redirect. NAME-05 is recorded as passed only then; with the user's `publish with fallback` reply it is recorded as `fallback accepted by the user`; if no update shows although the Task 2 checks passed, record NAME-05 as failed and stop (4.0.0 cannot be taken back; plan a gap with `/gsd-plan-phase 3 --gaps`; do not rename the repository back or create one with the old name)."
    why_human: "Needs the published release and an installed 3.7.0 client."
  - test: "03-06 Task 3 step 7 (optional): `curl -sSL -H \"User-Agent: check\" https://api.github.com/repos/cevans3505/PrintInvoicer/releases/latest`"
    expected: "Shows `\"tag_name\": \"v4.0.0\"`."
    why_human: "Needs the published release."
  - test: "Re-run the security audit after 03-05 and 03-06: `/gsd-secure-phase 03`"
    expected: "The 9 pending threats T-03-21 to T-03-29 (T-03-21 to T-03-24 from 03-05, T-03-25 to T-03-29 from 03-06) are verified and closed; 03-SECURITY.md approval changes from pending."
    why_human: "Depends on the Windows and release evidence above."
---

# Phase 3: Rename to Billament Verification Report

**Phase Goal:** PrintInvoicer becomes Billament everywhere (code, app, PDFs, scripts, docs, data folders and the GitHub repository), existing users keep every job and finalized invoice through a one-time safe move of their data, and the result ships as 4.0.0, so later phases are built under the new name from the start.
**Verified:** 2026-11-02
**Status:** human_needed
**Re-verification:** No, initial verification

## Scope of this run

Only plans 03-01 to 03-04 (cloud) have run. Plans 03-05 (Windows: UI tests, README images, real `publish.ps1`, real 3.7.0 upgrade test) and 03-06 (GitHub rename, redirect check, v4.0.0 tag and release) have NOT run. Nothing from them is faked or marked passed here. Everything in them is listed as a pending item under `human_verification`.

Checks run in the Linux container (dotnet 10): `dotnet build Billament.sln -p:EnableWindowsTargeting=true --no-incremental` (0 warnings, 0 errors), `dotnet test tests/Billament.Tests` (525 passed, 0 failed; the plan floor was 485), `git grep`, `git ls-files`, file reads, `git diff -M` against the pre-phase commit b87258a.

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
| -- | ----- | ------ | -------- |
| 1  | SC1/NAME-01: solution `Billament.sln`; projects, folders, namespaces `Billament.Core/App/Tests/UiTests`; exe and assembly `Billament`; `InternalsVisibleTo("Billament.UiTests")` only; icon `Billament.ico` | VERIFIED | `Billament.sln` lists the four projects at `src\Billament.*\Billament.*.csproj`; `git ls-files | grep -i printinvoicer` empty; `find -ipath '*printinvoicer*'` empty; `Billament.App.csproj` has AssemblyName/RootNamespace/Product `Billament`, `<ApplicationIcon>Assets\Billament.ico`, `<Resource Include="Assets\Billament.ico" />`; `AssemblyInfo.cs` is the only `InternalsVisibleTo`, naming `Billament.UiTests`; both windows use `pack://application:,,,/Billament;component/Assets/Billament.ico`; `ReaderContract` checks `"Billament.Core.Slicing."`. Moves are renames (`git diff -M --numstat` shows `{PrintInvoicer.Core => Billament.Core}`; `git log --follow` on PricingCalculator.cs shows its history). Run-time load of the pack URI is pending (human item 1). |
| 2  | SC1/NAME-02: everything the user sees says Billament | VERIFIED | `MainWindow.xaml` Title "Billament"; ErrorReport caption `Billament: something went wrong`, saved header `Billament {version}`; update bar `Billament {v} is available. You have {x}.`; User-Agent `Billament`; print job name `Billament, {n} invoices`; folders `Documents\Billament\Invoices`, `%TEMP%\Billament previews`, `%TEMP%\Billament UI tests`, `%LOCALAPPDATA%\Billament\...`. No QuestPDF metadata call exists (`grep Metadata` finds only slice-file metadata). Windows appearance at run time is pending. |
| 3  | SC2/NAME-03: with only the old folders, a safety copy is made first, then local folder, then Documents folder, then `printinvoicer.db` becomes `billament.db`; finalized totals and snapshots byte for byte the same; pre-rename copy is never pruned | VERIFIED | `DataFolderMigration.Run` read in full: `DatabaseBackup.Manual(..., "pre-rename")` before any move; `ClearAllPools`; local move; Documents move; `RenameDatabase`; no `Delete`/copy/merge call in the file. Linux tests pass: `Moves_both_folders_and_renames_the_database`, `Finalized_invoices_are_byte_for_byte_the_same_after_the_move` (compares `finalized_total`/`price_snapshot` and the whole file bytes, then prices to 87.20 and 40.40), `Only_the_two_folder_settings_change`, `Runs_only_once`, `A_later_start_succeeds_after_a_failed_one`. The pre-rename name does not match the prune regex `^(?:billament|printinvoicer)-\d{4}-\d{2}-\d{2}\.db$`. |
| 4  | SC3/NAME-03: new folder present means old one untouched; failures leave old data and give a plain message; logic in Core with Linux tests; `// AUDIT:` and AUDITING.md rows | VERIFIED (Core half) | `Decide`/`Occupied` treat folder, file, link and dangling link as taken. Tests: `Leaves_old_folders_alone_when_the_new_ones_exist`, `Each_pair_follows_its_own_rule`, `A_file_with_the_new_name_counts_as_taken`, `A_dangling_link_with_the_new_name_counts_as_taken`, `Failed_backup_moves_nothing`, `Failed_folder_move_leaves_old_data_and_creates_no_new_folder`, `A_failed_local_move_is_not_a_failure`, `A_failed_rename_moves_the_folder_back`, `When_the_move_back_also_fails_the_next_start_finishes_it`, `Tells_you_when_an_unrelated_new_folder_blocks_the_move`. Folders and time are parameters (no `GetFolderPath`, no clock in Core). `git grep -c AUDIT: -- src` totals 36, DataFolderMigration.cs has 9, DatabaseBackup.cs 5, and AUDITING.md says "There are 36 tags" with rows for the move, rename, settings rewrite, publish.ps1 deletes and the old backup names. |
| 5  | SC3: on a Documents failure the app shows the plain message, saves details, calls `Shutdown(1)` before any `new Database` | PRESENT_BEHAVIOR_UNVERIFIED | `App.OnStartup` calls `if (!MoveOldData()) return;` before `Db = new Database(...)`, and `MoveOldData` shows `result.Problem`, saves details through `ErrorReport.Save`, calls `Shutdown(1)` and returns false. It compiles, but it is WPF code no Linux test can run. Pending in human items (03-05 case A). |
| 6  | SC2/NAME-04: a BackupFolder or InvoiceFolder inside the old folder points at the same place under the new one after the move; settings elsewhere untouched; only those two rows change; DB not opened for writing when nothing changes | VERIFIED (standard upgrade path) | `RemapFolder` (fully-qualified check, `GetFullPath` normalization, separator boundary) and `UpdateFolderSettings` (raw SQL on the two keys, read-only open first, write only if a value changes). Tests pass: `Remaps_only_paths_inside_the_old_folder` (theory), `Blank_and_relative_values_are_left_as_they_are`, `The_letter_case_rule_comes_from_the_caller`, `Custom_folders_inside_the_old_folder_follow_the_move`, `Folders_set_elsewhere_are_left_exactly_as_they_are`, `Only_the_two_folder_settings_change`, `A_settings_update_that_fails_is_a_notice`, `The_finishing_start_also_updates_the_folder_settings`, `Updating_folder_settings_changes_nothing_when_no_folder_is_inside_the_old_one`. Edge case WR-03 (rewrite skipped when the folder moved but no database file was renamed) is a warning, not a failure of this truth; see below. |
| 7  | SC2/NAME-04: restoring a pre-4.0 backup applies the same two-key rewrite (D-25) | VERIFIED (Core) | `SettingsViewModel.RestoreBackup` calls `DataFolderMigration.UpdateFolderSettings(_db.FilePath, DataFolders.Under(...))` after `DatabaseBackup.Restore`, with a notice on failure. Test `A_restored_backup_from_before_the_rename_gets_its_folders_updated` passes. The WPF flow itself is pending (03-05 case C). |
| 8  | NAME-04: `billament-*` and `printinvoicer-*` backups are listed, restored and pruned by the date in the name; `DailyIfDue` makes no second backup on the upgrade day; the picker offers both patterns | VERIFIED | `DatabaseBackup`: `List` accepts both prefixes (files only, no `.partial`), `Prune` orders by the regex `date` group, then name, `DailyIfDue` checks both names, `LooksLikeBillament` checks content not name; picker filter `billament-*.db;printinvoicer-*.db`. Tests in `BackupTests.cs` pass (legacy list, mixed prune by date, manual/before-restore/pre-rename never pruned under either name, upgrade-day skip). |
| 9  | SC4/NAME-05 (code side): `UpdateService.Repository` is `cevans3505/Billament`; allow-check, releases page, API address and issue page derive from it; remark about the redirect and never reusing the old name | VERIFIED | `UpdateService.cs:31`; `Download` allow-check line 124 uses `{Repository}`; API URL line 167; `ErrorReport.IssuesPage` uses `UpdateService.Repository`; the remark above `Repository` states the redirect rule. README, Read me.txt and release notes link `cevans3505/Billament`. |
| 10 | SC4/NAME-05 (live side): a 3.7.0 install still finds 4.0.0 through GitHub's redirect after the repository is renamed | UNCERTAIN (pending 03-06) | Cannot be checked before the repository is renamed and 4.0.0 is published. Listed in human items (03-06 Task 2 steps 3 to 5, Task 3 step 6). Not marked passed. |
| 11 | SC5/NAME-06: `publish.ps1`, `make-readme-images.ps1`, `tools/Make-Icon.ps1`, `ci.yml`, `release.yml` use the new names (install folder, `Billament.lnk`, old shortcut and old program folder removed, `Billament-<version>.zip` and `.sha256`, `^Billament <major>.<minor>` heading check, release title); `BILLAMENT_README_IMAGES` matches in script and test | VERIFIED (static) | Read all five files: names as above; `publish.ps1` refuses to run while `Billament` or `PrintInvoicer` runs, installs first, then deletes only the old `.lnk` (target `*\PrintInvoicer.exe`) and the fixed old program folder (needs `PrintInvoicer.exe`, no `*.db`, no `Backups`/`Invoices`, differs from `-InstallDir`) with `-LiteralPath`, and names no data folder in a delete; both workflows parse as YAML. Real runs pending (human items). Review WR-02 (nested or junctioned `-InstallDir`) is an open warning. |
| 12 | SC5/NAME-07/NAME-08: docs renamed; "Renamed from PrintInvoicer" section near the README top; `Read me.txt` heading `Billament 4.0` over a 13-character underline plus "Coming from PrintInvoicer"; CLAUDE.md names the deliberate old-name strings; version 4.0.0; a case-insensitive search finds only deliberate hits | VERIFIED | `git grep -l -i printinvoicer` outside `.planning/phases/01,02,03` lists: release.yml, CONCERNS-TRIAGE.md, REQUIREMENTS.md, ROADMAP.md, STATE.md, AUDITING.md, CLAUDE.md, README.md, `packaging/Read me.txt`, publish.ps1, UpdateService.cs, SettingsViewModel.cs, DataFolderMigration.cs, DatabaseBackup.cs, BackupTests.cs. All are inside the 03-06 allowlist, and each hit line in `src`, `tests`, `publish.ps1`, `.github`, `packaging` was read: migration names, legacy backup prefix, the upgrade text, the old-install cleanup, the redirect remark. `.claude/CLAUDE.md`, `.planning/codebase/*.md` and `.planning/PROJECT.md` have no hit. `Read me.txt` line 1 is `Billament 4.0` followed by `=============` (13). `<Version>4.0.0</Version>`. No em or en dash outside `.planning`. |
| 13 | Prohibitions: no pricing or invoice-layout change; no QuestPDF metadata; no edit to historical phase folders or CONCERNS-TRIAGE; move never deletes/overwrites/merges; no new DB opened before the move; only the two settings rows written | VERIFIED | `git diff -M b87258a HEAD` on Pricing and InvoicePdf shows only `using`, `namespace` and old-name text lines (7/7 and 2/2 line changes); Database.cs diff is names and doc/AUDIT text only (no SQL, no schema); `git diff --stat b87258a HEAD` on phases 01, 02 and CONCERNS-TRIAGE is empty; `DataFolderMigration.cs` contains no `Delete` or copy call; `OnStartup` order is sequential code. |

**Score:** 11/13 truths verified (1 present, behavior-unverified: truth 5; 1 uncertain and pending 03-06: truth 10).

### Deferred Items

None. Later phases (4 and 5) cover other requirements (FIX-08 to FIX-12, SUBM-01 to SUBM-06) and none of the open items here.

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `Billament.sln` | four renamed projects | VERIFIED | exists, builds 0 warnings 0 errors with `EnableWindowsTargeting` |
| `src/Billament.App/Billament.App.csproj` | exe, product, version 4.0.0, icon | VERIFIED | contains `<Version>4.0.0</Version>` |
| `src/Billament.App/AssemblyInfo.cs` | `InternalsVisibleTo("Billament.UiTests")` | VERIFIED | only InternalsVisibleTo in `src` |
| `src/Billament.App/MainWindow.xaml` | new title and pack URI | VERIFIED | Title "Billament", pack URI as required |
| `src/Billament.App/UpdateService.cs` | `Repository = "cevans3505/Billament"` | VERIFIED | line 31, redirect remark above |
| `tests/Billament.Tests/ReaderContract.cs` | `"Billament.Core.Slicing."` | VERIFIED | line 81 |
| `packaging/Read me.txt` | heading `Billament 4.0` | VERIFIED | line 1 |
| `src/Billament.Core/Data/DataFolderMigration.cs` | `DataFolders`, `FolderMove`, `DataMoveResult`, `FileOps`, `Run`, `RemapFolder`, `UpdateFolderSettings` | VERIFIED | substantive (about 400 lines), used by `App.MoveOldData` and `SettingsViewModel.RestoreBackup` |
| `tests/Billament.Tests/DataFolderMigrationTests.cs` | Linux tests of the move | VERIFIED | 29 test methods, all pass |
| `src/Billament.App/App.xaml.cs` | `MoveOldData` before `new Database` | VERIFIED (wiring) | `if (!MoveOldData()) return;` precedes `Db = new Database(...)` |
| `src/Billament.Core/Data/DatabaseBackup.cs` | `LegacyPrefix`, date-based prune | VERIFIED | `private const string LegacyPrefix = "printinvoicer-";` |
| `tests/Billament.Tests/BackupTests.cs` | legacy backup tests | VERIFIED | includes `Daily_backups_with_both_names_are_pruned_by_date` |
| `src/Billament.App/ViewModels/SettingsViewModel.cs` | both-name picker, rewrite after restore | VERIFIED | lines 96 and 118 to 129 |
| `AUDITING.md` | rows and "There are 36 tags" | VERIFIED | line 19, matches `git grep` count of 36 |
| `README.md` | `## Renamed from PrintInvoicer` | VERIFIED | line 69, pointer at line 8 |
| `.github/workflows/release.yml` | upgrade paragraph in draft notes | VERIFIED | line 145 |
| `publish.ps1` | two-name running check, old-install cleanup | VERIFIED | lines 12 and 38 to 63 |
| `CLAUDE.md` | `**Old name:**` rule | VERIFIED | line 39 |

### Key Link Verification

| From | To | Via | Status | Details |
| ---- | -- | --- | ------ | ------- |
| `MainWindow.xaml` | `Billament.App.csproj` | pack URI names the assembly `Billament`; icon is a `Resource` | WIRED | assembly name and `Assets\Billament.ico` match; run-time load pending |
| `release.yml` | `packaging/Read me.txt` | heading regex `^Billament <major>.<minor>` | WIRED | regex at line 50; read-me line 1 `Billament 4.0` |
| `make-readme-images.ps1` | `ReadmeImageTests.cs` | `BILLAMENT_README_IMAGES` | WIRED | set in script line 15; test reads the same name (static; not run) |
| `App.xaml.cs` | `DataFolderMigration.cs` | `MoveOldData` calls `Run(DataFolders.Under(...), DateTime.Now)` | WIRED | |
| `DataFolderMigration.cs` | `DatabaseBackup.cs` | `DatabaseBackup.Manual(oldDb, ..., "pre-rename")` | WIRED | name not matched by the prune regex |
| `DataFolders.NewName/NewDatabaseName` | `Database.DefaultPath` | constants equal the path parts | WIRED | pinned by `The_new_database_is_where_Database_opens_it` |
| `DataFolderMigration.cs` | `AppSettings` | `nameof(AppSettings.BackupFolder)` / `InvoiceFolder` | WIRED | |
| `SettingsViewModel.cs` | `DatabaseBackup.cs` | picker patterns match `List` prefixes | WIRED | |
| `UpdateService.cs` | GitHub repo `cevans3505/Billament` | `Repository` constant | WIRED in code; repository not yet renamed (pending 03-06) | |

### Data-Flow Trace (Level 4)

Not applicable to this phase: no new rendered dynamic data. The data that matters (finalized totals through the move) is traced by the byte-for-byte test above.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Whole solution compiles incl. WPF markup | `dotnet build Billament.sln -p:EnableWindowsTargeting=true --no-incremental` | Build succeeded, 0 warnings, 0 errors | PASS |
| Core unit suite | `dotnet test tests/Billament.Tests` | Passed 525, Failed 0 (floor 485) | PASS |
| Finalized totals survive the move | test `Finalized_invoices_are_byte_for_byte_the_same_after_the_move` (part of the 525) | passes; 87.20 and 40.40 | PASS |
| No old-name path | `git ls-files | grep -i printinvoicer`; `find -ipath` | empty | PASS |
| Working tree clean | `git status --porcelain` before this report | empty | PASS |

### Probe Execution

No `probe-*.sh` scripts exist and none are declared in the plans. Step 7c: SKIPPED.

### Requirements Coverage

Every requirement ID in the PLAN frontmatter was cross-checked against REQUIREMENTS.md. All eight are declared by at least one plan and mapped to Phase 3; there are no orphaned IDs.

| Requirement | Source Plans | Description | Status | Evidence |
| ----------- | ------------ | ----------- | ------ | -------- |
| NAME-01 | 03-01, 03-05 | code base named Billament | SATISFIED in code; run-time load pending 03-05 | truths 1, 13 |
| NAME-02 | 03-01, 03-05 | everything the user sees says Billament | SATISFIED in code; Windows look pending 03-05 | truth 2 |
| NAME-03 | 03-02, 03-05 | one-time safe move, logic in Core, AUDIT rows | SATISFIED in Core by Linux tests; real move and App message path pending 03-05 | truths 3, 4, 5 |
| NAME-04 | 03-03, 03-05 | custom folders follow the move; old backups listed, restored, pruned | SATISFIED in Core by Linux tests; real flow pending 03-05; edge WR-03 open | truths 6, 7, 8 |
| NAME-05 | 03-01, 03-04, 03-06 | update check, allow-check, issue page, docs point at the new repo; 3.7.0 finds 4.0.0 via redirect | PARTIAL: code and docs satisfied; redirect NEEDS HUMAN (03-06) | truths 9, 10 |
| NAME-06 | 03-01, 03-04, 03-05 | scripts and workflows use the new name | SATISFIED statically; real runs pending 03-05 and the tag run in 03-06 | truth 11 |
| NAME-07 | 03-01, 03-04, 03-05 | docs use the new name, pictures regenerated | SATISFIED for docs; picture regeneration pending 03-05 | truth 12 |
| NAME-08 | 03-01, 03-04, 03-06 | version 4.0.0, search finds only deliberate hits | SATISFIED for the repository state; "ships" and the final re-run pending 03-06 | truth 12 |

The REQUIREMENTS.md checkboxes and traceability rows still say Pending; they should stay so until 03-05 and 03-06 are done.

### Anti-Patterns Found

Debt-marker scan (`TBD|FIXME|XXX`) over the phase's source files: no unreferenced markers. The code review (03-REVIEW.md) found 0 critical, 5 warnings and 5 info items, all `open` in 03-REVIEW-DISPOSITION.md. I read the cited code to weigh each against the must-haves.

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| `src/Billament.Core/Data/DataFolderMigration.cs` | 182-208 | WR-03: `UpdateFolderSettings` runs only when `renamed` is true | WARNING | Confirmed by reading the code. It affects only an old folder that holds `billament.db` (alone, or next to `printinvoicer.db`, the `Two_database_files_are_both_kept` case). A 3.7.0 install has only `printinvoicer.db`, so the standard upgrade, and every success criterion and NAME-04 as written, is unaffected: the standard path renames the file and rewrites the two settings (tested). Not a gap in a must-have. It would be a gap if the owner wants D-13 to hold for hand-assembled folders; fix is a one-line condition plus a test. |
| `publish.ps1` | 50-62 | WR-02: old-folder delete guarded by path equality only; nested or junctioned `-InstallDir`, or a link at the old path, not handled | WARNING | Only reachable with a custom `-InstallDir` inside `Programs\PrintInvoicer` or a link. Default use is safe. Use the default folder in the real 03-05 run. |
| `src/Billament.Core/Data/DataFolderMigration.cs` | 117, 354 | WR-01: SQLite `-journal`/`-wal`/`-shm` sidecars are not renamed; a hot journal makes the read-only safety copy fail with a misleading "close any other program" message | WARNING | Fails closed (nothing moved), so no data loss; the message can mislead. Could not be reproduced here (needs a crashed 3.7.0). |
| `src/Billament.App/App.xaml.cs` | 59-74 | WR-04: broad catch says "nothing was changed" although the move may already have run | WARNING | The text was extended to say where the data is ("or still in the PrintInvoicer folder"), but still says "nothing was changed". Wording only. |
| `src/Billament.Core/Data/DataFolderMigration.cs` and `App.xaml.cs` | 26-28, 84-87 | WR-05: empty special folder gives relative paths | WARNING | Rare environment fault; low likelihood. |
| various | | IN-01 to IN-05 (error file blocks local move, AUDIT tags at call sites, `\d` in prune regex, repeated safety copies, wording) | INFO | Do not affect any must-have. |

No blocker anti-pattern. No em or en dashes, no real names in the changed files.

### Human Verification Required

All items are in the `human_verification` list in the frontmatter (26 items): the 03-05 Windows checks (full suite incl. screen tests, the four prior-phase UI test files `ReadmeImageTests.cs`, `SampleShop.cs`, `ScreenTests.cs`, `WpfHost.cs`, README images, `publish.ps1` real run with the data checksum diff, the upgrade test cases A to E and the developer's own data) and the 03-06 checks (final search re-run, repository rename, redirect checks, tag and release, checksum, 3.7.0 update-bar check, security re-run). Each carries the concrete expected result taken from the 03-05 and 03-06 plans. One of them is the `behavior_unverified` item (Documents failure handled by the App, case A).

### Gaps Summary

No gaps found in the cloud work. Each of the eight requirements is implemented and, where it can be tested on Linux, tested: the build is clean, 525 Core tests pass, the move is ordered and never deletes or overwrites, finalized totals are byte for byte unchanged across the move, custom folders and legacy backups follow it, the old name survives only in deliberate places, and the version is 4.0.0.

What stops the phase from being `passed` is work that has not run: the Windows run-time checks (plan 03-05), and the GitHub rename, redirect check, v4.0.0 release and the 3.7.0 update-bar check (plan 03-06). NAME-05's live redirect has no evidence yet and is not claimed. The phase goal's last clause ("ships as 4.0.0") is not met until 03-06 runs.

Recommendation on the open review warnings: none blocks the phase. WR-03 is the one worth fixing before release if the owner wants D-13 to hold in every folder state; it can be closed by a gap plan (`/gsd-plan-phase 3 --gaps`) without waiting for Windows. The others can be deferred or fixed at the owner's choice. WR-02 is covered by running 03-05 with the default `-InstallDir`.

---

_Verified: 2026-11-02_
_Verifier: Claude (gsd-verifier)_
