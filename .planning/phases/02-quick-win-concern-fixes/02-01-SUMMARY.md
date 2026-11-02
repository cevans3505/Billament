---
phase: 02-quick-win-concern-fixes
plan: 01
subsystem: testing
tags: [sqlite, legacy-upgrade, price-snapshot, xunit, triage]
requires:
  - phase: 01
    provides: milestone scaffolding and CONCERNS-TRIAGE.md scores
provides:
  - Pre-snapshot SQL fixture and LegacyDatabaseTests (2 facts) guarding invoices already sent
  - Re-score notes for C11, C27, C06, C09, C10, C14, C22 and the C01 rules-version note
affects: [phase-03 FIX-08 (C01), 02-02, 02-03, 02-04]
status: complete
requirements-completed: [FIX-02, TRIA-01]
actuals:
  tokens: 4900
  tasks: 3
  commits: 3
plan_head_before: 2b5965a8bd8dbb20c8ccc15151ebadad2081df67
plan_head_after: 06bbcaeb0c4ba995c4b932f9415b8aa50ddb4f11
tech-stack:
  added: []
  patterns: ["SQL fixture script run on a raw SqliteConnection, then opened with Database", "hand-derived decimal literals in tests, never taken from the calculator"]
key-files:
  created:
    - tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql
    - tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs
  modified:
    - .planning/CONCERNS-TRIAGE.md
key-decisions:
  - "No production code changed; src is byte-identical to the start of the plan"
  - "Legacy freeze runs under pricing rules version 2; fixture values give the same cents under version 1, so the test pins no rules quirk (flagged for Phase 3 FIX-08)"
coverage:
  - id: D1
    description: "Pre-snapshot fixture opened by Database freezes both finalized invoices at 87.20 and 40.40 and leaves the draft unfrozen at 12.70"
    requirement: FIX-02
    verification:
      - kind: unit
        ref: "tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs#Pre_snapshot_database_freezes_each_finalized_invoice_at_its_old_total"
        status: pass
    human_judgment: false
  - id: D2
    description: "Snapshots stay byte-identical after price changes and two reopens; draft follows prices to 16.05"
    requirement: FIX-02
    verification:
      - kind: unit
        ref: "tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs#Frozen_invoices_keep_their_snapshot_through_price_changes_and_reopening"
        status: pass
    human_judgment: false
  - id: D3
    description: "Re-score section in CONCERNS-TRIAGE.md with seven rows and the C01 note"
    requirement: TRIA-01
    verification:
      - kind: other
        ref: "awk row count over the section heading prints rows=7"
        status: pass
    human_judgment: false
duration: 6min
completed: 2026-11-02
---

# Phase 2 Plan 01: Legacy-database test and re-score notes Summary

A fixture database shaped like one from before price snapshots is opened by `Database` in a test, which shows each finalized invoice is frozen once at its hand-derived old total (87.20 and 40.40), with the draft left unfrozen (12.70, then 16.05 after a price change).

## Accomplishments

- `Fixtures/pre-snapshot.sql`: original `CREATE TABLE` statements minus the later-added columns, three jobs (two finalized, one draft), one filament, one printer, fixed dates, sample names only, no invoice counter setting.
- `LegacyDatabaseTests`: the first fact checks with `pragma_table_info` that the fixture lacks `price_snapshot` and `finalized_total`, opens it, and asserts the literal totals, separate snapshots per job, a null snapshot and total on the draft, one filament, one printer, and both new columns present. The second fact changes filament, printer, electricity and slicing prices, reopens twice, and asserts the snapshot text is byte-identical, totals unchanged, exactly two snapshotted jobs.
- `CONCERNS-TRIAGE.md`: new "Re-score after checking the code (Phase 2)" section with seven rows (C11, C27, C06, C09, C10, C14, C22), the C01 / FIX-08 note about rules version 2, and the cross-reference sentence in "How items are scored". Original Scores table untouched.

## Task Commits

| Task | Name | Commit |
|------|------|--------|
| 1 | End-to-end legacy upgrade fact and fixture | 9db3291 |
| 2 | Exactly-once fact (price changes, two reopens) | 012b0f7 |
| 3 | Re-score notes in CONCERNS-TRIAGE.md | 06bbcae |

## Verification

- `dotnet test tests/PrintInvoicer.Tests --filter LegacyDatabaseTests`: 2 passed.
- Full Linux unit suite: 374 passed, 0 failed (was 372).
- `git diff 2b5965a -- src` is empty: no production file changed, including `Pricing` and `Data/Database.cs`.
- No em or en dashes in touched files; triage row count 36 (29 + 7).

## Deviations from Plan

None - plan executed exactly as written. The hand-derived totals (87.20, 40.40, 12.70, 16.05) matched on the first run, so no literal needed re-deriving. The plan's gate commands reference `fade8d5`; that commit is not the branch base here, so the same invariant was checked against the real plan start (`2b5965a`), with the same result.

## Known Stubs

None.

## Threat Flags

None. Test file access is limited to reading the fixture from the test output folder and a temporary database under `Path.GetTempPath()` that is deleted in `Dispose`; under the repository convention this needs no AUDIT tag or AUDITING.md row.

## Pending Human Checks

None for this plan (all checks were automated and ran on Linux).

## Self-Check: PASSED

- FOUND: tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql
- FOUND: tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs
- FOUND: commits 9db3291, 012b0f7, 06bbcae are ancestors of HEAD
