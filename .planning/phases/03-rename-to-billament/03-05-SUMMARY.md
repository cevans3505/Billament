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

status: task-1-done, task-2-pending
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

## Task 2: upgrade test on a copy (pending, user)

Windows Sandbox is not turned on on this PC, so cases A to D (and optional E) need Sandbox turned on in Windows
Features, or a second Windows account. Inputs: `publish\Billament.exe`, `PrintInvoicer-3.7.0.zip` from the 3.7.0
release, and a copy of 3.7.0 data (for example the `pre-rename` safety copy renamed to `printinvoicer.db`).
