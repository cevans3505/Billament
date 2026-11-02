---
phase: 03-rename-to-billament
plan: 02
subsystem: data
tags: [migration, folders, sqlite-backup, audit, wpf-startup]

requires:
  - phase: 03-rename-to-billament
    provides: Billament.* projects and namespaces (plan 03-01)
provides:
  - DataFolderMigration.Run in Core, the one-time safe rename of Documents and LocalAppData folders and the database file
  - DataFolders, FolderMove, DataMoveResult and FileOps types (FileOps is the failure test seam)
  - App.MoveOldData, called first in App.OnStartup before the database opens
  - Eight AUDIT tags (35 under src) and AUDITING.md rows for the move
affects: [03-03, 03-04, 03-05, 03-06]

plan_head_before: c27a7834640d961f9e5a7d53e61c25a77bbcf3eb
plan_head_after: fce82642fdc816ddaa63c234745e2bb901129f4a

actuals:
  tokens: 10000
  tasks: 2
  commits: 4

tech-stack:
  added: []
  patterns:
    - "Folders and time passed in as parameters, with a FileOps record of delegates as the failure seam (no interfaces)"
    - "One tagged helper per kind of disk access, so every access line sits directly under its AUDIT: comment"

key-files:
  created:
    - src/Billament.Core/Data/DataFolderMigration.cs
    - tests/Billament.Tests/DataFolderMigrationTests.cs
  modified:
    - src/Billament.App/App.xaml.cs
    - AUDITING.md

key-decisions:
  - "A failed Documents move stops start-up with a plain message and Shutdown(1) before any new Database, so no new data folder is created"
  - "A failed %LOCALAPPDATA% move is not fatal and the user is told nothing; details go to the error file"
  - "An unrelated Documents\\Billament blocks the move (D-08) and gives a once-only notice that says where the old data is"
  - "Only the main database file is renamed, not -journal/-wal/-shm siblings (the code never enables WAL and a hot journal makes the read-only safety copy fail first)"
  - "Repair rule instead of a marker file: printinvoicer.db in Documents\\Billament with no billament.db is renamed on the next start, touching only the new folder"
  - "The dangling-link notice condition uses FolderExists on the new name, so a file or link at that name gives no notice that wrongly says a folder exists"

patterns-established:
  - "Tests that need failures use a FileOps seam, not chmod (root ignores permission bits on the Linux container)"

requirements-completed: [NAME-03]

coverage:
  - id: D1
    description: "First start with only old folders makes the pre-rename safety copy, moves both folders and renames the database; nothing is copied, merged, overwritten or deleted"
    requirement: NAME-03
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/DataFolderMigrationTests.cs#Moves_both_folders_and_renames_the_database"
        status: pass
    human_judgment: false
  - id: D2
    description: "Finalized totals and snapshots are byte for byte unchanged; the database file is identical; the two fixture invoices still price to 87.20 and 40.40 with no warnings"
    requirement: NAME-03
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/DataFolderMigrationTests.cs#Finalized_invoices_are_byte_for_byte_the_same_after_the_move"
        status: pass
    human_judgment: false
  - id: D3
    description: "Skip rules: a new name held by a folder, file or dangling link leaves that old folder untouched; each pair decided on its own; nothing to move creates nothing; once only"
    requirement: NAME-03
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/DataFolderMigrationTests.cs (Leaves_old_folders_alone..., Each_pair..., A_file..., A_dangling_link..., Nothing_to_move..., Runs_only_once)"
        status: pass
    human_judgment: false
  - id: D4
    description: "Failures leave the data in one named place with a plain message: failed copy, failed folder move, failed local move (not fatal), failed rename with move back, double failure finished by the next start, later start after a failed one"
    requirement: NAME-03
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/DataFolderMigrationTests.cs (Failed_backup_moves_nothing, Failed_folder_move..., A_failed_local_move..., A_failed_rename..., When_the_move_back_also_fails..., A_later_start...)"
        status: pass
    human_judgment: false
  - id: D5
    description: "App.OnStartup runs the move before the database opens; a Documents failure shows the plain message, saves the details and shuts down; compiles with 0 warnings"
    requirement: NAME-03
    verification:
      - kind: other
        ref: "awk order gate on App.xaml.cs; dotnet build src/Billament.App and tests/Billament.UiTests -p:EnableWindowsTargeting=true (0 warnings, 0 errors)"
        status: pass
    human_judgment: false
  - id: D6
    description: "Every disk access in DataFolderMigration.cs has an AUDIT: comment directly above it (8 tags, 35 under src) and AUDITING.md lists the move and says 35"
    requirement: NAME-03
    verification:
      - kind: other
        ref: "awk tag gate, git grep tag count, grep 'There are 35 tags' AUDITING.md"
        status: pass
    human_judgment: false
  - id: D7
    description: "The real upgrade on Windows from a copy of a 3.7.0 install, and 3.7.0 still running, shows the plain message and creates no Documents\\Billament"
    requirement: NAME-03
    verification: []
    human_judgment: true
    rationale: "Windows-only (running the app, locked folders). Deferred to plan 03-05 Task 2."

duration: 6min
completed: 2026-11-02
status: complete
---

# Phase 3 Plan 02: The one-time safe move of the data folders Summary

**A once-only, never-deleting move of Documents and LocalAppData data folders to the new name, with a pre-rename safety backup, a database rename that rolls back, byte-for-byte finalized totals, and a plain message plus Shutdown(1) before any new database when it cannot finish.**

## Performance

- **Duration:** 6 min
- **Tasks:** 2 (both TDD)
- **Files modified:** 4 (2 created, 2 modified)

## Accomplishments

- `DataFolderMigration.Run(DataFolders, DateTime, FileOps?)` in Core: safety copy with `DatabaseBackup.Manual(..., "pre-rename")` into the old Backups folder, then the local folder, then the Documents folder, then `printinvoicer.db` to `billament.db`. Folders and time are parameters, so all of it runs on Linux.
- `App.MoveOldData()` runs as the first line of the `try` in `OnStartup`, ahead of `new Database(Database.DefaultPath)`. A Documents failure shows the plain message (with the saved details path), saves details through `ErrorReport.Save` and calls `Shutdown(1)`. The outer catch message now uses `DataFolders.OldName`, so the App has no literal old name.
- 15 new facts beyond the 3 tracer facts cover every skip rule and failure. The pre-rename backup survives `Prune(keep: 0)`. The database file bytes and the (id, finalized_total, price_snapshot) rows are equal before and after; jobs 1 and 2 price to 87.20 and 40.40 with no warnings.
- Eight `// AUDIT:` tags in the new file (35 under `src`), three new "Files written or deleted" rows, a "Files read" row, the "one-time move in 4.0" paragraph and `Directory.Move`/`File.Move` search hints in AUDITING.md, which says "There are 35 tags".

## Task Commits

TDD cycle for each task:

1. **Task 1 RED:** `7a4de1a` (test) - types with a placeholder `Run`, three facts; two failed on the planned assertions (Moved vs NothingToMove, database not at the new path)
2. **Task 1 GREEN:** `bf8494c` (feat) - `Run`, App wiring, AUDITING.md; 3 passed
3. **Task 2 RED:** `a447b5e` (test) - 15 new facts; 3 failed on the planned assertions (repair, unrelated-folder notice, two-database notice), 12 already passed against Task 1 code
4. **Task 2 GREEN:** `fce8264` (feat) - repair rule and the two notices; 18 passed

No refactor commit was needed.

**Plan metadata:** committed separately after this file (docs: complete plan)

The Task 1 tracer feedback gate was satisfied automatically: the automated verify passed on the committed tree and the task carries no human-observable evidence, so execution expanded to Task 2.

## RED evidence

- Task 1: `dotnet test --filter DataFolderMigrationTests` printed `Failed: 2, Passed: 1`; `Moves_both_folders_and_renames_the_database` failed with `Expected: Moved / Actual: NothingToMove`, and `Finalized_invoices_are_byte_for_byte_the_same_after_the_move` failed with `SQLite Error 14: unable to open database file` at the new path. Semantic assessment: both ran and failed because the move did not happen, the planned reason. The third fact (DefaultPath names) passed as expected since it pins existing code. The placeholder `Run` was committed with the tests so the build compiled and the failure is an assertion, not a build error.
- Task 2: `Failed: 3, Passed: 15`; `Two_database_files_are_both_kept` and `Tells_you_when_an_unrelated_new_folder_blocks_the_move` failed on the missing notices, `When_the_move_back_also_fails_the_next_start_finishes_it` on `Expected: Moved / Actual: NothingToMove` for the repair run. Semantic assessment: the planned reasons. (The `gsd_run check tdd-red-evidence` classifier does not support xUnit reports, so no machine record was produced; the console output above is the evidence.)

## Decisions Made

See `key-decisions` above. All come from the plan's recorded assumptions (a) to (c) and its "only the main database file is renamed" and "no marker file" notes. One small choice of mine: the unrelated-folder notice requires a real folder at the new name (`FolderExists`), so a plain file or dangling link there does not produce a notice that says "a folder already exists".

## Deviations from Plan

None - plan executed exactly as written.

Two points that are choices within the plan, not deviations:

- The plan said "Occupied ... on one line". The tag sits directly above a method whose single `return` is that line, so the awk gate and the tag count both hold.
- The Tests use `DataFolders.OldName.ToLowerInvariant()` to build the old-name daily backup file name rather than typing the old name again, so the deliberate old-name strings stay in `DataFolderMigration.cs` and AUDITING.md.

## Issues Encountered

None.

## Deferred to Windows (plan 03-05)

- The real upgrade from a copy of a 3.7.0 `Documents\PrintInvoicer` and `%LOCALAPPDATA%\PrintInvoicer`: first start of 4.0.0 makes the backup, moves both folders, renames the database, shows every job and finalized total unchanged.
- 3.7.0 still running: 4.0.0 shows the plain message, leaves `Documents\PrintInvoicer` in place, creates no `Documents\Billament` and closes.
- UI tests (`tests/Billament.UiTests`) were only built here (0 warnings, 0 errors), not run. They never call `OnStartup`, so no dialog can appear.

## Known Stubs

None.

## Threat Flags

None. All new file and folder access is in `DataFolderMigration.cs`, tagged and listed in AUDITING.md; the plan's threat model (T-03-05 to T-03-11) covers it. No delete call exists in the file.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 03-03: `Run` is the single place to add the rewrite of the `BackupFolder` and `InvoiceFolder` settings (D-13) and the old-backup-name handling. Note for 03-03: plan 03-03 makes old-name daily backups prunable, and the Task 1 test asserts the old daily backup survives the move before any prune.

## Self-Check: PASSED

- Files exist: `src/Billament.Core/Data/DataFolderMigration.cs`, `tests/Billament.Tests/DataFolderMigrationTests.cs`, `src/Billament.App/App.xaml.cs`, `AUDITING.md`.
- Commits `7a4de1a`, `bf8494c`, `a447b5e`, `fce8264` are ancestors of HEAD; measured `commits: 4` from ledger base `c27a783`.
- Verification re-run: `DataFolderMigrationTests` 18 passed, 0 failed; full Linux suite 503 passed, 0 failed; App and UiTests build 0 warnings, 0 errors; awk tag gate clean; 8 tags in the file, 35 under `src`; AUDITING.md says 35; no delete call and no special-folder or clock call in Core file; no em or en dashes in `src`, `tests` or docs.

---
*Phase: 03-rename-to-billament*
*Completed: 2026-11-02*
