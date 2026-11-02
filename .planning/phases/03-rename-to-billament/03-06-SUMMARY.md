---
phase: 03-rename-to-billament
plan: 06
subsystem: release
tags: [rename, release, github, redirect]

requires:
  - phase: 03-rename-to-billament
    provides: plan 03-05 passed (Windows checks and the Sandbox upgrade test)
affects: []

status: complete
---

# 03-06 Summary: release

## Task 1: release readiness (done, 2026-11-02)

- Final search: `git grep -l -i printinvoicer` outside the phase 01 to 03 folders names 15 files, all on the
  allowlist. The hits are unchanged from 03-04-SUMMARY.md; 03-05 added only the start-up fix in `App.xaml.cs` and
  `WpfHost.cs`, which do not hold the old name.
- No tracked path holds the old name. On disk, the old `bin`/`obj`-only folders under `src` and `tests` (ignored build
  output from before the rename) were removed; `dist/PrintInvoicer-3.4.0.zip` is an ignored old release zip, kept.
- The deliberate old names are present (`"PrintInvoicer"` in `DataFolderMigration.cs`, `"printinvoicer-"` in
  `DatabaseBackup.cs`).
- `<Version>4.0.0</Version>`; `packaging/Read me.txt` starts with `Billament 4.0`.
- No em or en dash outside `.planning`. Working tree clean before this summary.

## Task 2: repository rename and redirect checks (done, 2026-11-02): checks passed

- Renamed with `gh repo rename Billament --repo cevans3505/PrintInvoicer` (user's choice); local `origin` is now
  `https://github.com/cevans3505/Billament.git`.
- Step 3, API headers: `HTTP/1.1 301 Moved Permanently`, `Location: https://api.github.com/repositories/1439674558/releases/latest`,
  then `HTTP/1.1 200 OK`. Body: `"tag_name":"v3.7.0"`.
- Step 4, releases page: `HTTP/1.1 301 Moved Permanently`, `Location: https://github.com/cevans3505/Billament/releases`,
  then `HTTP/1.1 200 OK`.
- Step 5, PrintInvoicer 3.7.0 in Windows Sandbox with networking on, Settings > Check now (clicked through UI
  Automation): "You have the latest version (3.7.0). Checked Nov 2, 2:27 AM."
- Rule from now on: never create a new repository named PrintInvoicer under cevans3505.

## Task 3: release 4.0.0 (done, 2026-11-02)

- `main` pushed at 6ef50ef; CI passed (Build and unit tests, Screen tests).
- Tag `v4.0.0` pushed; the Release workflow passed the tag/version check with no read-me heading warning and made the
  draft "Billament 4.0.0".
- Draft checked: `Billament-4.0.0.zip` holds only `Billament.exe` and `Read me.txt` (first line `Billament 4.0`, has
  "Coming from PrintInvoicer"); its SHA-256 `0d1ebf86feab9233f1a9d5b9f1b145cc36acb6644f8843af7c35fbd4a490c5b5` matches
  the `.sha256` file and the notes.
- Notes written (new name, data move, upgrade paragraph, SHA-256 line); the user published the release at 07:38 UTC.
- API through the old name: `"tag_name":"v4.0.0"`.
- PrintInvoicer 3.7.0 in Windows Sandbox, Settings > Check now: "PrintInvoicer 4.0.0 is available. You have 3.7.0."
  with the Download button shown (not clicked; its target is the releases page, whose redirect step 4 checked).
- Still to do: re-run `/gsd-secure-phase 03` for T-03-21 to T-03-29, then `/gsd-verify-work 03`.
