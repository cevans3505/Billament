---
phase: 02
review: 02-REVIEW.md
titles: json
findings:
  - id: WR-02
    severity: warning
    disposition: open
    title: "Titles over 60 characters (and a few other titles) now produce a different PDF name than before, so a regenerated invoice sits beside the old file"
  - id: WR-04
    severity: warning
    disposition: open
    title: "The folder cleanup is applied only to PDF writes; \"Open invoice folder\" and backups still use the raw typed folder, and a bad folder reaches the \"problem in PrintInvoicer\" dialog"
  - id: IN-01
    severity: info
    disposition: open
    title: "Interrupted backups now leave a uniquely named `.partial` file that nothing ever removes"
  - id: IN-02
    severity: info
    disposition: open
    title: "Backup concurrency tests do not exercise two copies writing the same target"
  - id: IN-03
    severity: info
    disposition: open
    title: "An oversized update reply clears the remembered update, unlike a network failure"
  - id: IN-04
    severity: info
    disposition: open
    title: "Bidirectional control characters are kept in file names"
  - id: IN-06
    severity: info
    disposition: open
    title: "A long number whose last 30 characters start with a reserved device name and a dot can still lose its last digit"
  - id: WR-01
    severity: warning
    disposition: fixed
    title: "Long invoice-number prefixes lose the sequence number, so different invoices get the same PDF name"
  - id: WR-03
    severity: warning
    disposition: fixed
    title: "`IsDirectlyInside` compares two differently normalized paths and may throw for a valid folder with a trailing space or dot on Windows"
  - id: IN-05
    severity: info
    disposition: fixed
    title: "The Windows behavior of the Explorer arguments has not been checked"
open: 7
total: 10
recorded: 2026-11-02T02:45:01.892Z
---

# Phase 02: Code Review Disposition

| Finding | Severity | Disposition | Source |
|---------|----------|-------------|--------|
| WR-02 | warning | open | - |
| WR-04 | warning | open | - |
| IN-01 | info | open | - |
| IN-02 | info | open | - |
| IN-03 | info | open | - |
| IN-04 | info | open | - |
| IN-06 | info | open | - |
| WR-01 | warning | fixed | dee2ad2: a long number is cut from the front, so the sequence digits survive (not in the current review) |
| WR-03 | warning | fixed | 5139a31: folder normalized like Windows before create, path and check (confirmed on Windows in 02-UAT.md) (not in the current review) |
| IN-05 | info | fixed | 4d3eb3a: checked on Windows (A2 passed, file highlighted); no code change needed (not in the current review) |

Dispositions: `open` (recorded, not yet triaged), `fixed`, `skipped`, `deferred`.
Set `deferred` by hand and put the reason in the Source cell; both are preserved. A `|` in the reason is kept as prose and escaped on the next run.
Re-running the gate keeps every row it can. A row the current review no longer reports is kept and its Source cell flagged, so a finding does not leave this record silently. ONE exception: when a finding id is REUSED by a different finding, the earlier decision cannot keep a row — the id is taken — and it is dropped. A RECORDED decision (anything but `open`) is named on the console when that happens; a row still at `open` is replaced silently, because `open` records no decision to lose.
