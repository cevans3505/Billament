---
phase: 03
review: 03-REVIEW.md
titles: json
findings:
  - id: WR-01
    severity: warning
    disposition: open
    title: "SQLite sidecar files are not part of the database rename, and a hot journal blocks start-up with a misleading message"
  - id: WR-02
    severity: warning
    disposition: open
    title: "publish.ps1 deletes the old install folder recursively with no containment or link guard"
  - id: WR-03
    severity: warning
    disposition: open
    title: "The folder-settings rewrite is skipped when the folder moved but the database file was not renamed"
  - id: WR-04
    severity: warning
    disposition: open
    title: "The start-up failure message can say \"nothing was changed\" when the move already changed things"
  - id: WR-05
    severity: warning
    disposition: open
    title: "Empty special folders turn the migration into a working-directory-relative rename"
  - id: IN-01
    severity: info
    disposition: open
    title: "A saved error file can permanently block the local-folder move"
  - id: IN-02
    severity: info
    disposition: open
    title: "AUDIT tags are inconsistent at call sites and for the new legacy-name existence check"
  - id: IN-03
    severity: info
    disposition: open
    title: "The daily-backup regex uses `\\d`, which matches non-ASCII digits, and it decides which files are deleted"
  - id: IN-04
    severity: info
    disposition: open
    title: "Safety copies pile up when the move keeps failing"
  - id: IN-05
    severity: info
    disposition: open
    title: "Wording and naming leftovers"
open: 10
total: 10
recorded: 2026-11-02T05:30:11.778Z
---

# Phase 03: Code Review Disposition

| Finding | Severity | Disposition | Source |
|---------|----------|-------------|--------|
| WR-01 | warning | open | - |
| WR-02 | warning | open | - |
| WR-03 | warning | open | - |
| WR-04 | warning | open | - |
| WR-05 | warning | open | - |
| IN-01 | info | open | - |
| IN-02 | info | open | - |
| IN-03 | info | open | - |
| IN-04 | info | open | - |
| IN-05 | info | open | - |

Dispositions: `open` (recorded, not yet triaged), `fixed`, `skipped`, `deferred`.
Set `deferred` by hand and put the reason in the Source cell; both are preserved. A `|` in the reason is kept as prose and escaped on the next run.
Re-running the gate keeps every row it can. A row the current review no longer reports is kept and its Source cell flagged, so a finding does not leave this record silently. ONE exception: when a finding id is REUSED by a different finding, the earlier decision cannot keep a row — the id is taken — and it is dropped. A RECORDED decision (anything but `open`) is named on the console when that happens; a row still at `open` is replaced silently, because `open` records no decision to lose.
