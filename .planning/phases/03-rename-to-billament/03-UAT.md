---
status: testing
phase: 03-rename-to-billament
source: [03-VERIFICATION.md]
started: 2026-11-02T05:42:40Z
updated: 2026-11-02T05:42:40Z
---

## Current Test

number: 1
name: 03-05 Task 1 step 1: run `dotnet test Billament.sln --nologo` on Windows (both test projects, including every screen test)
expected: |
  Two `Passed!  - Failed: 0` summary lines (unit project and screen-test project) and no `Failed!` line. The screen tests load the pack URI pack://application:,,,/Billament;component/Assets/Billament.ico and the renamed x:Class values at run time, and the update bar reads `Billament 3.4.0 is available. You have 3.3.0.`
awaiting: user response

## Tests

Every item below is from plans 03-05 (Windows) and 03-06 (GitHub rename and 4.0.0 release). None has run; none is passed.

### 1. 03-05 Task 1 step 1: run `dotnet test Billament.sln --nologo` on Windows (both test projects, including every screen test)
expected: Two `Passed!  - Failed: 0` summary lines (unit project and screen-test project) and no `Failed!` line. The screen tests load the pack URI pack://application:,,,/Billament;component/Assets/Billament.ico and the renamed x:Class values at run time, and the update bar reads `Billament 3.4.0 is available. You have 3.3.0.`
why_human: The UI tests are Windows-only (WPF, interactive desktop session). Built here with -p:EnableWindowsTargeting=true (0 warnings, 0 errors), never run.
result: [pending]

### 2. Prior-phase UI test files that the regression gate could not run on Linux: tests/Billament.UiTests/ReadmeImageTests.cs, tests/Billament.UiTests/SampleShop.cs, tests/Billament.UiTests/ScreenTests.cs, tests/Billament.UiTests/WpfHost.cs (they run as part of the UI test suite in the step above)
expected: All pass offscreen after the rename (no window shown or focused, no dialog opened); SampleShop still builds the sample shop with 'Harbor Dive Shop' style names; ReadmeImageTests reads the BILLAMENT_README_IMAGES environment variable; WpfHost uses %TEMP%\Billament UI tests
why_human: Windows-only. They compile on Linux; they have not run since the namespace, env var and temp folder renames.
result: [pending]

### 3. 03-05 Task 1 step 2: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./make-readme-images.ps1`, then `git status --porcelain -- docs/images`
expected: Exit 0, no `was not rebuilt` or `The README image test failed` text. The three pictures docs/images/main-jobs.png, job-editor.png and invoice-page-1.png are rebuilt; they show no app name, so a diff is rendering noise (restore with `git checkout -- docs/images`) or a real UI change (review, then commit). `git status --porcelain -- docs/images` prints nothing at the end of the task.
why_human: Needs Windows, PowerShell 5.1 and WPF rendering; whether a pixel change is noise is a visual judgment.
result: [pending]

### 4. 03-05 Task 1 step 3 (optional, RESEARCH A5): with PrintInvoicer 3.7.0 installed and running, run publish.ps1 once
expected: It stops with `Billament (or the old PrintInvoicer) is running. Close it and run this again.` (Get-Process with two names works in Windows PowerShell 5.1). If 3.7.0 is not installed, record `3.7.0 not installed, A5 not checked`.
why_human: Needs Windows PowerShell 5.1 and a running old program.
result: [pending]

### 5. 03-05 Task 1 steps 4 and 5: record md5sums of Documents\PrintInvoicer and %LOCALAPPDATA%\PrintInvoicer into $TEMP/bm-data-before.txt, then run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./publish.ps1` for real, then diff
expected: publish.ps1 exits 0 (it runs the tests, publishes, installs). `%LOCALAPPDATA%\Programs\Billament\Billament.exe` and the Start menu `Billament.lnk` exist; the old `PrintInvoicer.lnk` and `%LOCALAPPDATA%\Programs\PrintInvoicer\PrintInvoicer.exe` are gone; `publish/Billament.exe` exists (kept for Task 2); the md5sum diff of the two old data folders is empty (publish.ps1 touched no data).
why_human: Real install and delete on a Windows profile. Includes the unreviewed edge from code review WR-02 (a nested or junctioned -InstallDir); use the default install folder for this run.
result: [pending]

### 6. 03-05 Task 1 human check: look at the Start menu shortcut, and open any changed README picture
expected: The shortcut is named Billament and shows the app icon. A changed picture shows only rendering differences (restored) or a real UI change (committed with a note).
why_human: Icon and pixel changes are visual judgments.
result: [pending]

### 7. 03-05 Task 1 summary: record in 03-05-SUMMARY.md the test totals, the picture decision, what publish.ps1 printed (installed, removed) and the A5 result
expected: 03-05-SUMMARY.md exists with those four records; `test -f publish/Billament.exe` exits 0
why_human: Written by whoever runs 03-05.
result: [pending]

### 8. 03-05 Task 2 prepare: in Windows Sandbox or a second Windows account (never the only copy of real data), put PrintInvoicer.exe from PrintInvoicer-3.7.0.zip, a COPY of real Documents\PrintInvoicer and %LOCALAPPDATA%\PrintInvoicer, and publish\Billament.exe. Start 3.7.0; write down the job count and three finalized totals; set Back up to `...\Documents\PrintInvoicer\My backups` and the invoice folder to `...\Documents\PrintInvoicer\Invoices 2026`; Save; click Back up now once; leave 3.7.0 open
expected: A setup with a known job count, three known finalized totals, two custom folders inside the old data folder and at least one printinvoicer-<date>.db backup
why_human: Needs a second Windows session, real data copy, and a person reading screens.
result: [pending]

### 9. 03-05 Task 2 case A (NAME-03, RESEARCH A4, covers behavior_unverified item above): with 3.7.0 still open, start Billament.exe
expected: A message titled Billament that names Documents\PrintInvoicer, says nothing was lost and asks you to close PrintInvoicer; Billament closes. Documents\Billament does not exist. Documents\PrintInvoicer is unchanged (its Backups folder now also holds billament-<date>-<time>-pre-rename.db). %LOCALAPPDATA%\Billament may exist (the local folder moves first). If the move is NOT stopped (A4 wrong), record a gap for `/gsd-plan-phase 3 --gaps` and stop before 03-06 (for example, a check in App.MoveOldData that refuses to move while a process named PrintInvoicer runs).
why_human: Needs real Windows file locking, a running 3.7.0 and WPF dialogs.
result: [pending]

### 10. 03-05 Task 2 case B (NAME-03): close 3.7.0, start Billament.exe
expected: No message; the window title says Billament; the job count and the three finalized totals equal what was written down (a difference stops the phase: it breaks the finalized-invoices rule); Documents\Billament\billament.db and Documents\Billament\Backups\billament-<date>-<time>-pre-rename.db exist; Documents\PrintInvoicer is gone.
why_human: Real data, real exe, a person comparing totals.
result: [pending]

### 11. 03-05 Task 2 case C (NAME-04, D-25): open Settings; click Back up now; open Restore a backup and cancel; then restore the pre-rename copy through Restore a backup and let Billament restart
expected: Back up to shows `...\Documents\Billament\My backups` and the invoice folder `...\Documents\Billament\Invoices 2026`; Back up now writes a new file into Documents\Billament\My backups; Restore a backup lists the old printinvoicer-*.db daily backups next to new ones; after restoring the pre-rename copy and the restart the job list matches and the Settings tab still shows both folders under `...\Documents\Billament`.
why_human: Real file dialog, restart through Process.Start, and settings read back in the real UI.
result: [pending]

### 12. 03-05 Task 2 case D (run once): close Billament and start it again
expected: No message, nothing moves.
why_human: Real second start on the moved data.
result: [pending]

### 13. 03-05 Task 2 case E (optional, decision c / D-24): fresh session with the old data copy plus an empty Documents\Billament folder, start Billament
expected: An information message saying where the old data is and how to use it; nothing moved.
why_human: Real dialog on Windows.
result: [pending]

### 14. 03-05 Task 2 own data: copy Documents\PrintInvoicer somewhere safe (for example a zip on the Desktop), then start the installed Billament from the Start menu on the developer's own account
expected: Same result as case B on the real data. Do this only after cases A to D passed on the copy. Record the outcome in 03-05-SUMMARY.md (or 03-UAT.md) as pass or fail per case, with before and after job count and three totals for case B.
why_human: Real data, one-way move (D-06); must follow the copy test.
result: [pending]

### 15. 03-06 Task 1 (run again on the final commit, after 03-05 may have committed pictures or fixes): the final search and release-readiness block
expected: `git grep -l -i printinvoicer` outside the three phase-01/02/03 folders lists only the allowlist (release.yml, CONCERNS-TRIAGE.md, PROJECT.md, REQUIREMENTS.md, ROADMAP.md, STATE.md, state.json, AUDITING.md, CLAUDE.md, README.md, packaging/Read me.txt, publish.ps1, UpdateService.cs, SettingsViewModel.cs, DataFolderMigration.cs, DatabaseBackup.cs, BackupTests.cs, DataFolderMigrationTests.cs); no tracked path and no on-disk path contains the old name; "PrintInvoicer" is in DataFolderMigration.cs, "printinvoicer-" is in DatabaseBackup.cs, <Version>4.0.0</Version> is in Billament.App.csproj, line 1 of Read me.txt is `Billament 4.0`, `git status --porcelain` is empty, and no em dash or en dash is outside .planning. 03-06-SUMMARY.md says the hits are unchanged from 03-04-SUMMARY.md or lists new ones with reasons. (This verification ran the same search at 2026-11-02 and it passed on the cloud commit.)
why_human: Must be re-run on the commit that is released, after the Windows plan.
result: [pending]

### 16. 03-06 Task 2 steps 1 and 2 (NAME-05, D-14): on GitHub rename cevans3505/PrintInvoicer to Billament (Settings > General > Repository name); optionally `git remote set-url origin https://github.com/cevans3505/Billament.git`
expected: The repository is cevans3505/Billament. Never create a new repository named PrintInvoicer under the account afterwards (it would end the redirects 3.7.0 depends on).
why_human: A GitHub account action only the owner takes; the plan forbids an agent doing it.
result: [pending]

### 17. 03-06 Task 2 step 3 (RESEARCH A1): `curl -sSIL -H "User-Agent: check" -H "Accept: application/vnd.github+json" https://api.github.com/repos/cevans3505/PrintInvoicer/releases/latest`, then the same without -I piped to `grep '"tag_name"'`
expected: First status line 301 or 307 with a `location:` header on api.github.com ending in `/releases/latest`, last status line 200; body check prints `"tag_name": "v3.7.0"`. A 200 with no tag_name line means the redirect lost the path.
why_human: Needs the repository to be renamed first.
result: [pending]

### 18. 03-06 Task 2 step 4 (RESEARCH A2): `curl -sSIL https://github.com/cevans3505/PrintInvoicer/releases`
expected: A redirect whose `location:` is `https://github.com/cevans3505/Billament/releases`, then 200.
why_human: Needs the renamed repository.
result: [pending]

### 19. 03-06 Task 2 step 5: in the 03-05 sandbox with PrintInvoicer 3.7.0 and a copy of its data, open Settings and click Check now
expected: `You have the latest version (3.7.0). Checked ...` (4.0.0 is not published yet). `Could not reach GitHub to check.` on a machine with internet means 3.7.0's client did not get through the redirect.
why_human: Needs the installed 3.7.0 client and the live redirect.
result: [pending]

### 20. 03-06 Task 2 step 7 (only if any of steps 3 to 5 failed): reply `wait`, `roll back` or `publish with fallback` and record the reply with its date
expected: Nothing is tagged or published before the reply is recorded (`git ls-remote --tags origin v4.0.0` prints nothing). `publish with fallback` records NAME-05 as `fallback accepted by the user`, never as passed. The pasted outputs of steps 3 to 5 are recorded under `Task 2 result` in 03-06-SUMMARY.md (or 03-UAT.md); `git remote get-url origin` shows the chosen address.
why_human: A decision only the owner can take (D-16, D-05).
result: [pending]

### 21. 03-06 Task 3 step 1: push main and wait for CI
expected: Both CI jobs pass (Build and unit tests, Screen tests) on the renamed repository.
why_human: Needs the push and the hosted Windows runner.
result: [pending]

### 22. 03-06 Task 3 steps 2 and 3: `git tag v4.0.0` and `git push origin v4.0.0`; watch the Release workflow
expected: The `Check the tag matches the app version` step passes with no read-me heading warning, and the run creates a draft release titled `Billament 4.0.0`. One-way (D-05); do it only after Task 2 is recorded as `checks passed` or as the user's own `publish with fallback` reply.
why_human: Pushing a tag and publishing is the owner's action; cannot be undone.
result: [pending]

### 23. 03-06 Task 3 step 4: download Billament-4.0.0.zip and Billament-4.0.0.zip.sha256 from the draft; `Get-FileHash .\Billament-4.0.0.zip -Algorithm SHA256`; unzip
expected: The hash equals the one in the .sha256 file and the `SHA-256:` line in the draft notes (case does not matter). The zip holds Billament.exe and Read me.txt; line 1 of Read me.txt is `Billament 4.0` and the file has the `Coming from PrintInvoicer` section.
why_human: Needs the built release asset and Windows PowerShell.
result: [pending]

### 24. 03-06 Task 3 step 5: write the release notes (keep the `SHA-256:` line and the `Upgrading from PrintInvoicer 3.7 or older` paragraph, add a short summary of 4.0.0) and publish the release
expected: Release v4.0.0 is published with both assets attached and the upgrade paragraph still in the notes.
why_human: One-way publication by the owner.
result: [pending]

### 25. 03-06 Task 3 step 6 (NAME-05, success criterion 4): in the sandbox with a fresh copy of the 3.7.0 data and PrintInvoicer 3.7.0, open Settings and click Check now; then click Download
expected: Status reads `Version 4.0.0 is available. You have 3.7.0.` and the update bar appears; Download opens the Billament releases page through the redirect. NAME-05 is recorded as passed only then; with the user's `publish with fallback` reply it is recorded as `fallback accepted by the user`; if no update shows although the Task 2 checks passed, record NAME-05 as failed and stop (4.0.0 cannot be taken back; plan a gap with `/gsd-plan-phase 3 --gaps`; do not rename the repository back or create one with the old name).
why_human: Needs the published release and an installed 3.7.0 client.
result: [pending]

### 26. 03-06 Task 3 step 7 (optional): `curl -sSL -H "User-Agent: check" https://api.github.com/repos/cevans3505/PrintInvoicer/releases/latest`
expected: Shows `"tag_name": "v4.0.0"`.
why_human: Needs the published release.
result: [pending]

### 27. Re-run the security audit after 03-05 and 03-06: `/gsd-secure-phase 03`
expected: The 9 pending threats T-03-21 to T-03-29 (T-03-21 to T-03-24 from 03-05, T-03-25 to T-03-29 from 03-06) are verified and closed; 03-SECURITY.md approval changes from pending.
why_human: Depends on the Windows and release evidence above.
result: [pending]

## Summary

total: 27
passed: 0
issues: 0
pending: 27
skipped: 0
blocked: 0

## Gaps
