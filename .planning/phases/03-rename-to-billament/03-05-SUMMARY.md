---
phase: 03-rename-to-billament
plan: 05
subsystem: windows-checks
tags: [rename, windows, screen-tests, publish-script, upgrade-test]

requires:
  - phase: 03-rename-to-billament
    provides: plans 03-01 to 03-04 and the review fixes for WR-01 to WR-05 (264738c)
provides:
  - Task 1 results on Windows (full suite, README pictures, the A5 running check, publish.ps1)
  - A fix that keeps the screen tests from running the real start-up (ed3a80a)
affects: [03-06]

plan_head_before: 264738c
plan_head_after: ec65be8

status: complete
---

# 03-05 Summary: Windows checks

## Task 1: Windows build check (done, 2026-11-02)

| Check | Result |
|-------|--------|
| `dotnet test Billament.sln` | Passed: 537 unit tests and 11 screen tests, 0 failed (two "Passed!" lines, no "Failed!") |
| `make-readme-images.ps1` | Rebuilt all three pictures; `git status -- docs/images` empty, so the pictures are byte for byte the same and nothing was committed |
| A5: publish.ps1 while 3.7.0 runs | Refused with "Billament (or the old PrintInvoicer) is running. Close it and run this again."; 3.7.0 then closed normally |
| publish.ps1 | Tests passed again; installed `%LOCALAPPDATA%\Programs\Billament\Billament.exe` and `Billament.lnk`; removed `PrintInvoicer.lnk` and `%LOCALAPPDATA%\Programs\PrintInvoicer`; printed the taskbar unpin hint |
| Data folders across publish.ps1 | Checksums of `Documents\Billament` and `%LOCALAPPDATA%\Billament` the same before and after |
| `publish\Billament.exe` | Kept for Task 2 |
| Windows build warnings | One CS8604 in `DataFolderMigrationTests` (not seen by the Linux build); fixed in ec65be8, now 0 warnings and 0 errors |

## Deviation: the screen tests ran the real start-up (fixed)

The first full suite run moved the developer's real `Documents\PrintInvoicer` to `Documents\Billament` (and the
`%LOCALAPPDATA%` folder too) at 01:14:07, before any Billament.exe had started. A probe in `App.OnStartup` showed the
cause: WPF's `Application` constructor queues `OnStartup`, and it runs as soon as the thread's dispatcher runs, so
`WpfHost` ran the full real start-up on every screen test run. That was already true before this phase (real database
opened, a daily backup, a main window), but it became harmful once start-up moves folders.

- Fix (ed3a80a): `App.RunStartup` (internal, default true); `WpfHost` sets it to false before creating the App, and
  `OnStartup` returns right after `base.OnStartup` when it is false.
- Proof: checksums of both old and both new data folders were the same before and after a full `dotnet test` run with the fix.
- The move itself worked on the real data: `billament.db` matches the `pre-rename` safety copy in every table except
  the `InvoiceFolder` setting, which moved from `Documents\PrintInvoicer\Invoices` to `Documents\Billament\Invoices`
  as designed. All 7 jobs (7 invoiced) are there.
- Side effect: the A5 check started 3.7.0 after the move, and 3.7.0 made a new empty `Documents\PrintInvoicer` (0 jobs)
  and an `update-check.json`. With the user's OK these were moved aside (not deleted) to the session scratchpad.
- The plan's "first start on your own data" step has effectively happened (with the safety copy made by the move).

## Task 2: upgrade test in Windows Sandbox (done, 2026-11-02)

Run automatically in Windows Sandbox (networking off) by a script that started the programs, read every Billament and
PrintInvoicer window through UI Automation, took screenshots and recorded MD5 lists of both data folders after each
step. Inputs: `publish\Billament.exe`, `PrintInvoicer.exe` from the published 3.7.0 zip, and a copy of the developer's
3.7.0 data (the `pre-rename` copy as `printinvoicer.db`: 7 jobs, all finalized, $370.66) with "Back up to" set to
`Documents\PrintInvoicer\My backups`, the invoice folder set to `Documents\PrintInvoicer\Invoices 2026`, two old daily
backups and one old manual backup.

| Case | Result |
|------|--------|
| A: 3.7.0 open | Pass. Billament showed "Billament could not move your data from ...\Documents\PrintInvoicer to ...\Documents\Billament, so it has closed. Nothing was lost: your data is still in ...\Documents\PrintInvoicer. Close PrintInvoicer and any other program that may be using that folder, such as a sync app, and start Billament again." plus the error file path. `Documents\Billament` was not created; `Documents\PrintInvoicer` was unchanged apart from `Backups\billament-...-pre-rename.db` (and 3.7.0's own daily backup). `%LOCALAPPDATA%` moved first, as designed. RESEARCH A4 holds: a running 3.7.0 blocks the folder move. |
| B: real move | Pass. No message; window title Billament; 7 jobs, "Invoiced, unpaid $370.66", every job total the same as in 3.7.0 ($70.96, $78.50, $16.90, $39.20, $55.80, $65.00, $44.30). `Documents\PrintInvoicer` gone; `billament.db` and `Backups\billament-...-pre-rename.db` exist. `billament.db` equals the original in every table except the two folder settings. |
| C: settings and old backups | Pass for the data: "Back up to" became `Documents\Billament\My backups` and the invoice folder `Documents\Billament\Invoices 2026`; the old `printinvoicer-*.db` daily and manual backups moved along. Not clicked through: Back up now, the restore picker and the D-25 restore in the UI (covered by `BackupTests` and `DataFolderMigrationTests`). |
| D: second start | Pass. No message; nothing moved; the only changed file is `window-layout.json` (window position saved on close). |
| E: empty Documents\Billament | Pass. Information message: "Billament found your data from PrintInvoicer in ...\Documents\PrintInvoicer, but a folder named ...\Documents\Billament already exists, so nothing was moved ..." with how to use the old data. Documents untouched; `%LOCALAPPDATA%` moved, as designed. |
| Own data | Moved by the test-run start-up described above; `billament.db` matches its pre-rename copy apart from the invoice folder setting. |
