---
phase: 03-rename-to-billament
plan: 03
subsystem: data
tags: [migration, settings, backups, restore, audit]

requires:
  - phase: 03-rename-to-billament
    provides: DataFolderMigration.Run, DataFolders, the AUDITING.md move rows (plan 03-02)
provides:
  - DataFolderMigration.RemapFolder, the pure rule that re-anchors a folder setting from the old data folder to the new one
  - DataFolderMigration.UpdateFolderSettings, the raw two-key rewrite of BackupFolder and InvoiceFolder, run after the move, after a finishing start and after a restore (D-25)
  - DatabaseBackup that lists, restores and prunes printinvoicer-* backups by the date in the name, and makes no second daily backup on the upgrade day
  - Restore a backup picker that shows billament-*.db and printinvoicer-*.db together
  - One AUDIT tag (36 under src) and an AUDITING.md row for the settings rewrite
affects: [03-04, 03-05, 03-06]

plan_head_before: 04f1f88c8093765fc382b0ed7c245a5b862abcb5
plan_head_after: a4c41b0538d3f696c4d06b63f2a3a9ef7f060601

actuals:
  tokens: 7350
  tasks: 3
  commits: 6

tech-stack:
  added: []
  patterns:
    - "Settings rows changed by raw SQL on named keys, read first through a read-only connection, written only when a value changes"
    - "Prune orders by a regex date group, then by name, so two file-name prefixes sort by date"

key-files:
  created: []
  modified:
    - src/Billament.Core/Data/DataFolderMigration.cs
    - src/Billament.Core/Data/DatabaseBackup.cs
    - src/Billament.App/ViewModels/SettingsViewModel.cs
    - tests/Billament.Tests/DataFolderMigrationTests.cs
    - tests/Billament.Tests/BackupTests.cs
    - AUDITING.md

key-decisions:
  - "The settings rewrite uses raw SQL on BackupFolder and InvoiceFolder only; SaveSettings and LoadSettings are never used, so no other row can change and a bad value elsewhere cannot stop the move"
  - "The database file is opened read-only first and opened for writing only when a value actually changes; a move with no custom folders leaves billament.db byte for byte unchanged"
  - "A failed settings rewrite is a notice and the move still counts as done (data is safe either way)"
  - "UpdateFolderSettings is public and shared by Run and the Restore button (D-25); a failed rewrite after a restore shows a warning box and the restart goes ahead"
  - "Letter-case rule is a parameter (default ignore case on Windows, exact elsewhere), tested both ways on Linux"
  - "DatabaseBackup.List also requires the .db suffix, so a name such as billament-x.dbx can never be listed"

patterns-established:
  - "One shared public rewrite method for the same fix-up at two entry points (first start of 4.0 and Restore), reusing the one tagged open"

requirements-completed: [NAME-04]

coverage:
  - id: D1
    description: "A BackupFolder or InvoiceFolder set to Documents\\PrintInvoicer or a folder inside it points at the same place under Documents\\Billament after the move, and the next daily backup lands there; blank, relative, sibling, '..' and other-drive values are left character for character"
    requirement: NAME-04
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/DataFolderMigrationTests.cs (Remaps_only_paths_inside_the_old_folder, Blank_and_relative_values_are_left_as_they_are, Custom_folders_inside_the_old_folder_follow_the_move, Folders_set_elsewhere_are_left_exactly_as_they_are)"
        status: pass
    human_judgment: false
  - id: D2
    description: "Only the two folder rows can change: a full dump of every table differs in exactly those two rows and the finalized rows are equal; nothing is opened for writing when no value needs a change"
    requirement: NAME-04
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/DataFolderMigrationTests.cs (Only_the_two_folder_settings_change, Folders_set_elsewhere_are_left_exactly_as_they_are, A_database_without_a_settings_table_is_moved_and_left_alone)"
        status: pass
    human_judgment: false
  - id: D3
    description: "The letter-case rule is passed in and tested both ways; a failed settings update is a notice that points to the Settings tab; the finishing start also rewrites the two settings"
    requirement: NAME-04
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/DataFolderMigrationTests.cs (The_letter_case_rule_comes_from_the_caller, A_settings_update_that_fails_is_a_notice, The_finishing_start_also_updates_the_folder_settings)"
        status: pass
    human_judgment: false
  - id: D4
    description: "Backups named printinvoicer-* are listed (and only backup names: no other .db, no .partial, no folder) and restore like any other; the safety copy is named billament-...-before-restore.db"
    requirement: NAME-04
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/BackupTests.cs (Backups_made_before_the_rename_are_listed_and_restored, Only_backup_names_are_listed)"
        status: pass
    human_judgment: false
  - id: D5
    description: "Prune keeps the 14 newest daily backups by date across both prefixes and never deletes manual, before-restore or pre-rename copies; the upgrade day gets no second daily backup"
    requirement: NAME-04
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/BackupTests.cs (Daily_backups_with_both_names_are_pruned_by_date, No_second_daily_backup_on_the_day_of_the_upgrade, Keeps_the_newest_daily_backups_and_every_manual_one)"
        status: pass
    human_judgment: false
  - id: D6
    description: "Restoring a pre-4.0 backup runs the two-key folder rewrite on the restored database (D-25) and leaves finalized rows equal"
    requirement: NAME-04
    verification:
      - kind: unit
        ref: "tests/Billament.Tests/DataFolderMigrationTests.cs (A_restored_backup_from_before_the_rename_gets_its_folders_updated, Updating_folder_settings_changes_nothing_when_no_folder_is_inside_the_old_one)"
        status: pass
    human_judgment: false
  - id: D7
    description: "The new database open has an AUDIT comment directly above it (9 tags in DataFolderMigration.cs, 5 in DatabaseBackup.cs, 36 under src); AUDITING.md has the settings row and says 36; App and UiTests build with 0 warnings and 0 errors"
    requirement: NAME-04
    verification:
      - kind: other
        ref: "awk tag gate on DataFolderMigration.cs, git grep tag count, grep 'There are 36 tags' AUDITING.md, dotnet build src/Billament.App and tests/Billament.UiTests -p:EnableWindowsTargeting=true"
        status: pass
    human_judgment: false
  - id: D8
    description: "On a real 3.7.0 copy: the backup and invoice folders show under Documents\\Billament after the first start of 4.0.0, Back up now writes there, and the Restore a backup picker lists old printinvoicer-*.db backups"
    requirement: NAME-04
    verification: []
    human_judgment: true
    rationale: "Windows-only (the file picker on screen, a real restore in the running app). Deferred to plan 03-05 Task 2."

duration: 6min
completed: 2026-11-02
status: complete
---

# Phase 3 Plan 03: Custom folders follow the move, and old backups keep working Summary

**A backup or invoice folder set inside the old data folder is re-anchored to Documents\Billament by a two-key raw SQL rewrite (also after a restore of a pre-4.0 backup), and printinvoicer-* backups are listed, restored, picked and pruned by date next to billament-* ones.**

## Performance

- **Duration:** 6 min (2026-11-02T05:11:14Z to about 05:17Z)
- **Tasks:** 3 (all TDD, 6 commits)
- **Files modified:** 6 (no new files)

## Accomplishments

- `DataFolderMigration.RemapFolder` is a pure string rule: only fully qualified values, normalized with `GetFullPath`, same place under the new folder for the old folder or a folder inside it (directory-boundary rule, so `PrintInvoicerOld` and `..` escapes never match), anything else returned as the same string.
- `DataFolderMigration.UpdateFolderSettings` reads the two keys through a read-only connection, remaps each, and opens the file for writing only when a value changed, then updates just those rows in one transaction. `Run` calls it right after the database rename (fresh move and finishing start); a failure becomes a notice pointing at the Settings tab and the move still counts as done. The `pathComparison` parameter defaults to ignore case on Windows and exact elsewhere.
- Tests prove: a full row dump of every table differs in exactly the two folder rows, finalized rows are equal, `billament.db` is byte for byte the old file when nothing needs a change, a trigger that aborts every settings update gives a notice and a `SqliteException`, and a database without a `settings` table is moved and left alone.
- `DatabaseBackup` lists `billament-*` and `printinvoicer-*` backups only, restores either (content is checked, not the name), prunes daily ones by the date group across both prefixes (manual, before-restore and pre-rename copies never match), and makes no second daily backup on the upgrade day.
- The Restore a backup picker offers both name patterns. After a successful restore, `SettingsViewModel.RestoreBackup` runs `UpdateFolderSettings` on the restored database (D-25); a failure shows a warning box and the restart still goes ahead.
- One new `// AUDIT:` tag (the settings database open): 9 in `DataFolderMigration.cs`, 5 in `DatabaseBackup.cs`, 36 under `src`. AUDITING.md has the settings rewrite row and says "There are 36 tags".

## Task Commits

1. **Task 1 RED:** `d62ba24` (test) - 10 new facts and theory cases; 9 failed on the planned assertions, 25 passed (negatives and pins)
2. **Task 1 GREEN:** `b678045` (feat) - `RemapFolder`, the rewrite inside `Run`, tagged open, AUDITING.md row; 34 passed
3. **Task 2 RED:** `4cfc639` (test) - 4 new facts; 3 failed on the planned assertions, 45 passed
4. **Task 2 GREEN:** `d03cc68` (feat) - legacy prefix in `List`, picker filter, public `UpdateFolderSettings` and the restore hook; 48 passed
5. **Task 3 RED:** `8af9edd` (test) - 2 new facts, both failed on the planned assertions
6. **Task 3 GREEN:** `a4c41b0` (feat) - date-group regex, `Prune` by date, `DailyIfDue` under either name; 14 backup facts passed

No refactor commit was needed.

**Plan metadata:** committed separately after this file (docs: complete plan)

The Task 1 tracer gate was satisfied automatically: the automated verify passed on the committed tree and the task carries no human-observable evidence, so execution expanded to Task 2.

## RED evidence

- Task 1: `Failed: 9, Passed: 25` (34 total). The four positive theory cases failed with the placeholder returning the input (`Expected ...docs/Billament...`, `Actual ...docs/PrintInvoicer...`); `Custom_folders_inside_the_old_folder_follow_the_move`, `Only_the_two_folder_settings_change`, `A_settings_update_that_fails_is_a_notice`, `The_finishing_start_also_updates_the_folder_settings` and `The_letter_case_rule_comes_from_the_caller` failed because nothing was rewritten. Semantic assessment: each ran and failed on the planned behavior. The negative theory cases, `Blank_and_relative...`, `Folders_set_elsewhere...` and `A_database_without_a_settings_table...` passed against the placeholder because they pin "leave alone". The placeholder `RemapFolder` and the unused `pathComparison` parameter were committed with the tests so the build compiled and the failures are assertions, not build errors.
- Task 2: `Failed: 3, Passed: 45`. `Backups_made_before_the_rename_are_listed_and_restored` (`List` returned `[]`), `Only_backup_names_are_listed` (missing the printinvoicer manual file), `A_restored_backup_from_before_the_rename_gets_its_folders_updated` (`Expected: True / Actual: False` from the placeholder `UpdateFolderSettings`). Semantic assessment: the planned reasons.
- Task 3: `Failed: 2, Passed: 12`. `Daily_backups_with_both_names_are_pruned_by_date` kept the old-prefix files 10-01 to 10-06 that should have been pruned; `No_second_daily_backup_on_the_day_of_the_upgrade` returned a new path instead of null. Semantic assessment: the planned reasons.
- (The `gsd_run check tdd-red-evidence` classifier does not support xUnit reports, so no machine record was produced; the console output above is the evidence.)

## Decisions Made

See `key-decisions` above. All follow D-13, D-25 and RESEARCH Patterns 2 and 3 as planned. One small hardening of my own: `IsBackupName` also requires the `.db` suffix, because on Windows the `*.db` file pattern can match longer extensions.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing critical functionality] AUDITING.md "Restoring a backup" paragraph did not mention the new write**
- **Found during:** Task 2
- **Issue:** Task 2 makes a restore write the two settings rows (D-25), but the plan only listed the AUDITING.md row for the start-up rewrite, so the file-writing paragraph for restores would have been out of date.
- **Fix:** Added one sentence to the "Restoring a backup" paragraph naming the two rows and `DataFolderMigration.UpdateFolderSettings`. No tag was added; the tag count stays 36.
- **Files modified:** `AUDITING.md`
- **Verification:** `grep 'There are 36 tags'` still passes; no em or en dashes in the file.
- **Committed in:** `d03cc68`

---

**Total deviations:** 1 auto-fixed (1 missing critical documentation)
**Impact on plan:** Documentation only; 03-04 rewrites the Backups row and can keep or fold this sentence.

Two points that are choices within the plan, not deviations:

- Task 1 keeps the rewrite private and Task 2 lifts it into the public `UpdateFolderSettings`, as the plan says. The RED commit of Task 2 holds a one-line public placeholder so the new fact compiled.
- The plan's Task 2 text says the restart message adds a sentence. The restart has no message today, so a warning box with that sentence is shown only when the rewrite fails.

## Issues Encountered

None. One test-helper nullability warning (`List<string?>`) appeared while writing the Task 3 RED test and was fixed before that commit.

## Deferred to Windows (plan 03-05 Task 2)

- A 3.7.0 copy whose Settings point the backup folder at `Documents\PrintInvoicer\My backups` and the invoice folder at `Documents\PrintInvoicer\Invoices 2026` shows the same folders under `Documents\Billament` after the first start of 4.0.0; Back up now writes there.
- The Restore a backup picker on screen lists old `printinvoicer-*.db` daily backups next to new ones, and a real restore of an old backup updates the two folder settings after the restart.
- UI tests (`tests/Billament.UiTests`) were only built here (0 warnings, 0 errors), not run.

## Known Stubs

None. (The two RED-commit placeholders, `RemapFolder` and `UpdateFolderSettings`, were replaced in the matching GREEN commits.)

## Threat Flags

None. The only new access line is the tagged `OpenDatabase` in `DataFolderMigration.cs`; it is listed in AUDITING.md and covered by T-03-12, T-03-13 and T-03-16 in the plan's threat model. No new delete call was added (`Prune` keeps its one tagged delete).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 03-04. Notes for it: AUDITING.md "Backups" row still describes `billament-` names only and its prune text should mention `printinvoicer-` (the code comment above `f.Delete()` already does); the final old-name search needs its allowlist to include the deliberate `printinvoicer-` strings in `DatabaseBackup.cs`, the picker filter in `SettingsViewModel.cs` and the new test data.

## Self-Check: PASSED

- Files exist: `src/Billament.Core/Data/DataFolderMigration.cs`, `src/Billament.Core/Data/DatabaseBackup.cs`, `src/Billament.App/ViewModels/SettingsViewModel.cs`, `tests/Billament.Tests/DataFolderMigrationTests.cs`, `tests/Billament.Tests/BackupTests.cs`, `AUDITING.md`.
- Commits `d62ba24`, `b678045`, `4cfc639`, `d03cc68`, `8af9edd`, `a4c41b0` are ancestors of HEAD; measured `commits: 6` from ledger base `04f1f88`.
- Verification re-run: `DataFolderMigrationTests` 36 passed, `BackupTests` 14 passed, full Linux suite 525 passed, 0 failed; App and UiTests build 0 warnings, 0 errors; awk tag gate clean; 9 tags in `DataFolderMigration.cs`, 5 in `DatabaseBackup.cs`, 36 under `src`; AUDITING.md says 36 and has the settings row; no `SaveSettings`, `LoadSettings` or `ReadWriteCreate` in `DataFolderMigration.cs`; the whole-name ordering is gone; no em or en dashes in `src`, `tests` or `AUDITING.md`.

---
*Phase: 03-rename-to-billament*
*Completed: 2026-11-02*
