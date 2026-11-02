---
phase: 03-rename-to-billament
plan: 06
subsystem: release
tags: [rename, release, github, redirect]

requires:
  - phase: 03-rename-to-billament
    provides: plan 03-05 passed (Windows checks and the Sandbox upgrade test)
affects: []

status: task-1-done
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
