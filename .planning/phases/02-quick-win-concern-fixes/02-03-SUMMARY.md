---
phase: 02-quick-win-concern-fixes
plan: 03
subsystem: updates-and-backup
tags: [update-check, size-cap, backup, partial-file, audit]
requires:
  - phase: 02-02
    provides: AUDITING.md edits this plan builds on (tag count and Backups row)
provides:
  - LatestReleaseReply (Core) with a 256 KB capped reader for GitHub's latest-release reply
  - UpdateService reads headers first, turns away over-cap replies, reads the body through the capped reader
  - DatabaseBackup.Copy with a per-copy GUID .partial file that deletes only its own temp file on failure
affects: [02-04 (also edits AUDITING.md)]
status: complete
requirements-completed: [FIX-04, FIX-05]
actuals:
  tokens: 4000
  tasks: 3
  commits: 3
plan_head_before: 80f247aa49c9b530d84afce79b22895fe23438ea
plan_head_after: ad39c3880e62f6502fbc5b73050fb6e3963f7af7
tech-stack:
  added: []
  patterns: ["bounded stream read in Core (cap plus one 8 KB chunk) returning null past the cap", "unique temp name per copy, cleanup of own temp only"]
key-files:
  created:
    - src/PrintInvoicer.Core/Updates/LatestReleaseReply.cs
    - tests/PrintInvoicer.Tests/LatestReleaseReplyTests.cs
  modified:
    - src/PrintInvoicer.App/UpdateService.cs
    - src/PrintInvoicer.Core/Data/DatabaseBackup.cs
    - tests/PrintInvoicer.Tests/BackupTests.cs
    - AUDITING.md
key-decisions:
  - "JsonException from a damaged GitHub reply is not wrapped in InvalidDataException: the CLAUDE.md reader rule is about files, and the update check already catches JsonException and falls back to the last known answer"
  - "An over-cap reply (declared or actual) returns null like a 404: no update info, last check time saved"
  - "A .partial file left by a process killed mid-copy is not swept; AUDITING.md says so"
coverage:
  - id: D1
    description: "Update reply read through LatestReleaseReply.ReadAsync: exactly 262,144 bytes is read, one byte more gives null, endless stream stops below 2x the cap, cancelled read throws OperationCanceledException, damaged reply throws JsonException, {} gives null fields, null gives null"
    requirement: FIX-05
    verification:
      - kind: test
        ref: "tests/PrintInvoicer.Tests/LatestReleaseReplyTests.cs (8 facts)"
        status: pass
    human_judgment: false
  - id: D2
    description: "UpdateService.FetchLatestAsync: ResponseHeadersRead, Content-Length (long?) compared with MaxBytes, same single GET with its AUDIT comment, FetchAndRememberAsync catches OperationCanceledException"
    requirement: FIX-05
    verification:
      - kind: other
        ref: "src/PrintInvoicer.App builds on Linux with -p:EnableWindowsTargeting=true, 0 warnings 0 errors; Task 1 grep acceptance criteria all pass"
        status: pass
      - kind: manual_procedural
        ref: "Windows: Settings > Check now against the live GitHub API"
        status: unknown
    human_judgment: true
  - id: D3
    description: "Each backup copy writes {target}.{guid}.partial, a stray .partial is never deleted, a failed copy removes only its own temp, same-second manual backups and parallel daily plus manual backups both finish"
    requirement: FIX-04
    verification:
      - kind: test
        ref: "tests/PrintInvoicer.Tests/BackupTests.cs (4 new facts; 10 pass, repeated 3 times)"
        status: pass
    human_judgment: false
  - id: D4
    description: "AUDITING.md says 27 tags, the 256 KB limit, and the new Backups row; git grep finds 27 AUDIT tags under src"
    requirement: FIX-04
    verification:
      - kind: other
        ref: "git grep -c 'AUDIT:' -- src totals 27; grep 'There are 27 tags' passes"
        status: pass
    human_judgment: false
duration: 3min
completed: 2026-11-02
---

# Phase 2 Plan 03: Capped update-check reply, and backup copies that never share a temporary file Summary

The update check now reads GitHub's reply through `LatestReleaseReply.ReadAsync` (256 KB cap, null beyond it, so an oversized reply means "no update info" like a 404), and every database backup copy writes its own GUID-named `.partial` file and deletes only that file if it fails.

## Accomplishments

- New Core type `PrintInvoicer.Core.Updates.LatestReleaseReply` (`TagName`, `HtmlUrl`, `MaxBytes = 256 * 1024`, `ReadAsync`). It reads in 8 KB chunks, returns null as soon as the total would pass the cap, and lets out only `JsonException` and `OperationCanceledException`.
- `UpdateService.FetchLatestAsync` uses `HttpCompletionOption.ResponseHeadersRead`, returns null when `Content-Length` (a `long?`, not narrowed) is above the cap, then reads the body through the capped reader. Still exactly one GET of the same address, with its `// AUDIT: network.` comment directly above. The private `LatestRelease` class and the `System.Text.Json.Serialization` using are gone. `FetchAndRememberAsync` now catches `OperationCanceledException` in place of `TaskCanceledException`.
- `DatabaseBackup.Copy` names its temp file `{target}.{guid:N}.partial`, no longer deletes an existing `.partial` first, and on `IOException`, `UnauthorizedAccessException` or `SqliteException` deletes only its own temp file (new `// AUDIT: file delete.` tag) and rethrows the original error.
- AUDITING.md: Network section states the 256 KB limit; tag count is 27; the Backups row describes the per-copy `.partial` file and that a copy cut off by a power cut can leave one behind.

## Task Commits

| Task | Name | Commit |
|------|------|--------|
| 1 | Capped update read (Core reader, UpdateService wiring, 3 boundary facts) | 16082af |
| 2 | Hostile reply facts (5 more) and the AUDITING.md Network sentence | 855d617 |
| 3 | Per-copy backup temp file, 4 BackupTests, AUDITING.md 27 tags and Backups row | ad39c38 |

## Verification

- `LatestReleaseReplyTests`: 8 passed. `BackupTests`: 10 passed (6 existing + 4 new), run three more times with no flake.
- Full Linux suite `dotnet test tests/PrintInvoicer.Tests`: 386 passed, 0 failed (was 374 before this plan).
- `dotnet build src/PrintInvoicer.App -p:EnableWindowsTargeting=true` and `dotnet build tests/PrintInvoicer.UiTests -p:EnableWindowsTargeting=true`: 0 warnings, 0 errors (both build on Linux).
- All acceptance greps for Tasks 1 to 3 pass, including: one `Http.GetAsync` call, AUDIT comment directly above the request, `File.Delete(temp)` appears once with its AUDIT tag directly above, `git grep -c 'AUDIT:' -- src` totals 27, no em or en dashes.
- TDD for Task 3: the stray-`.partial` and failed-copy facts failed on the old code (2 failed, 8 passed) and passed after the change. The Task 2 facts passed on first run because `ReadAsync` already handled them, so no change to the reader was needed.

## Deviations from Plan

None - plan executed exactly as written.

Notes, not deviations: Task 1 committed only the three boundary facts and Task 2 added the other five, as the plan splits them. The plan's `grep -c ... | grep -qx 2` build check counts the Warning and Error summary lines; I read the build summaries directly and both show 0 and 0.

## Known Stubs

None.

## Threat Flags

None. The one new file access (the cleanup delete in `DatabaseBackup.Copy`) is tagged and listed in AUDITING.md; no new network request.

## Pending Human Checks

- **WINDOWS-ONLY (not run, not marked passed):** `dotnet test PrintInvoicer.sln` on Windows, or the CI screen-tests job on a pull request to main, including `ScreenTests.Update_bar_shows_only_for_a_newer_release`. UI tests were not run here.
- **WINDOWS-ONLY, with network (not run, not marked passed):** start the built app, open Settings and click Check now. The status should read "You have the latest version (...)" or "Version X is available", not "Could not reach GitHub to check." This confirms a real GitHub reply passes through the 256 KB cap and the headers-first read. It was not tested against the live GitHub API from this environment.

## Self-Check: PASSED

- FOUND: src/PrintInvoicer.Core/Updates/LatestReleaseReply.cs, tests/PrintInvoicer.Tests/LatestReleaseReplyTests.cs
- FOUND: commits 16082af, 855d617, ad39c38 are ancestors of HEAD
