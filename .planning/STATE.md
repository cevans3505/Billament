---
gsd_state_version: "1.0"
current_phase: 03
current_phase_name: Rename to Billament
status: executing
stopped_at: Completed 03-02-PLAN.md
last_updated: "2026-11-02T05:10:13.034Z"
last_activity: 2026-11-02
last_activity_desc: Phase 03 execution started
state_head: d3ce1c20b66541b4c09fc9bf44aa1fd91bce3875
progress:
  total_phases: 5
  completed_phases: 2
  total_plans: 12
  completed_plans: 8
  percent: 40
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-11-02)

**Core value:** Prices and invoices stay correct and trustworthy: a finalized invoice never changes, and totals match what the slice file and price book say.
**Current focus:** Phase 03 — Rename to Billament

## Current Position

Phase: 03 (Rename to Billament) — EXECUTING
Plan: 3 of 6
Status: Ready to execute
Last activity: 2026-11-02 — Phase 03 execution started

Progress: [████░░░░░░] 40%

## Performance Metrics

**Velocity:**
- Total plans completed: 6
- Average duration: -
- Total execution time: 0.0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01 | 2 | - | - |
| 02 | 4 | - | - |

**Recent Trend:**
- Last 5 plans: -
- Trend: -

*Updated after each plan completion*
**Per-Plan Metrics:**

| Plan | Duration | Tasks | Files |
|------|----------|-------|-------|
| Phase 02 P01 | 6min | 3 tasks | 3 files |
| Phase 02 P02 | 12min | 3 tasks | 4 files |
| Phase 02 P03 | 3 min | 3 tasks | 6 files |
| Phase 02 P04 | 15min | 3 tasks | 9 files |
| Phase 03 P01 | 3 min | 2 tasks | 99 files |
| Phase 03 P02 | 6 min | 2 tasks | 4 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Roadmap]: Four phases in the user's order: README images, fixes scored 2.0 and above, fixes scored 1.5, slice file submissions
- [Roadmap]: Rename to Billament inserted as Phase 3 (user asked for an integer phase); Data Safety Fixes is now Phase 4 and Slice File Submissions Phase 5
- [Roadmap]: TRIA-01 is already done (CONCERNS-TRIAGE.md committed) and is mapped to Phase 2 as Complete
- [Roadmap]: FIX-01 to FIX-12 stay in triage score order across Phases 2 and 3
- [Roadmap]: FIX-08 (freeze once) sits after FIX-02 (legacy-database test) so the test guards the change
- [Phase 02]: 02-01: legacy freeze runs under pricing rules v2; fixture values give same cents under v1; Phase 4 FIX-08 must keep frozen totals
- [Phase 02]: 02-02: README license text lists QuestPDF's other eligible groups (charities, academic, open-source) because the live page says so
- [Phase 02]: 02-03: JsonException from a damaged GitHub reply is not wrapped in InvalidDataException (the file-reader rule is about files); over-cap update reply is treated as no update info like a 404
- [Phase 02]: 02-03: a .partial backup file left by a killed process is not swept; AUDITING.md says so
- [Phase 02]: 02-04: title 60, number 30 (end kept), name 120 UTF-16 char limits; Explorer paths passed through ArgumentList; A2 Windows /select check passed
- [Phase 02]: UAT closed on Windows; release checksum check deferred to release 3.7.0 after the merge
- [Phase 03]: 03-01: rename done by git mv and scripted sed with gates; no old-name text written, later plans add deliberate ones by hand
- [Phase 03]: 03-02: A failed Documents move stops start-up (plain message, Shutdown(1)) before any new Database; a failed LocalAppData move is not fatal
- [Phase 03]: 03-02: Only the main database file is renamed; a repair rule (old-name db inside the new folder) finishes a half-done move instead of a marker file

### Pending Todos

None yet.

### Blockers/Concerns

- [Phase 4]: Triage costs are estimates from the map. Check the code first and re-score any item already handled.
- [Phase 4]: Every fix must keep finalized totals unchanged and follow the AUDIT, schema and untrusted-file rules in CLAUDE.md.
- [Phase 2]: Open review findings carried forward: WR-04 (Settings folder typos reach the error report), WR-02 (some old titles get new PDF names), IN-01 to IN-04, IN-06. See 02-REVIEW-DISPOSITION.md.
- [Phase 2]: Release 3.7.0 must show the .zip.sha256 file and the SHA-256 line (02-UAT.md test 5).
- [Phase 5]: GitHub attachments may not accept .3mf or .goo directly; confirm whether zipping is needed during planning.

### Roadmap Evolution

- Phase 3 inserted after Phase 2: Rename to Billament (4.0.0); Data Safety Fixes renumbered 3 to 4, Slice File Submissions 4 to 5

## Deferred Items

Items acknowledged and deferred at milestone close, most recent first:

| Category | Item | Status | Deferred At | Milestone |
|----------|------|--------|-------------|-----------|
| *(none)* | | | | |

## Session Continuity

Last session: 2026-11-02T05:10:12.940Z
Stopped at: Completed 03-02-PLAN.md
Resume file: None
