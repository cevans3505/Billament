# Concerns Triage

**Source:** `.planning/codebase/CONCERNS.md` (mapped 2026-11-01)
**Scored:** 2026-11-01

## How items are scored

- **Benefit (1-5):** how much fixing it protects money, invoices or user data, or helps people trust the project.
- **Cost (1-5):** how complex it is to fix, which is roughly how many tokens it will take (1 = a small edit plus a test,
  5 = a broad refactor).
- **Score = Benefit / Cost.** Higher means more value for the effort.

**Cutoff:**
- Score 1.5 or higher: fix in this milestone (v1), in score order.
- Score 1.0 to 1.49: next milestone (v2).
- Below 1.0, or nothing to fix: left alone, with the reason noted.

Costs are estimates from reading the map, not the code. Planning for each phase should check the code first and
re-score anything that turns out to be already handled.
The Phase 2 items were checked this way; see Re-score after checking the code (Phase 2).

## Scores

| ID | Concern | Benefit | Cost | Score | Decision | Rationale |
|----|---------|---------|------|-------|----------|-----------|
| C11 | Releases not code-signed | 3 | 1 | 3.00 | v1 | Publishing a SHA-256 checksum file with each release is a small CI change. Code signing itself stays out of scope (needs a certificate). |
| C27 | No test for the legacy-database upgrade path | 5 | 2 | 2.50 | v1 | A test fixture built from the schema before snapshots guards totals on invoices that were already sent. Also covers the untested freeze path from the "pricing rules" fragile area. |
| C06 | Explorer / Process.Start arguments built by string concatenation | 2 | 1 | 2.00 | v1 | Switching to `ArgumentList` is a few lines in two places. Keep the AUDIT tags. |
| C09 | Startup backup and manual backup can use the same `.partial` file | 2 | 1 | 2.00 | v1 | Give each run its own temporary name, or check that the names always differ. |
| C10 | Update-check response is not size-capped | 2 | 1 | 2.00 | v1 | A capped read (about 256 KB) is a small change and matches the untrusted-input rule. |
| C14 | Output file names built from user data | 4 | 2 | 2.00 | v1 | Check the sanitizing, then add or tighten it, with a test for `..`, `\`, `:` and reserved names like `CON`. |
| C22 | QuestPDF Community license condition | 2 | 1 | 2.00 | v1 | Docs only: state the revenue-threshold condition in README and AUDITING. Moving to another PDF library is out of scope. |
| C01 | Retroactive price freeze query runs on every start | 3 | 2 | 1.50 | v1 | Record in `settings` that the one-time freeze has run. Invoices already frozen must not change. Do together with C27. |
| C08 + C19 | Stale data after a restore; restart uses `Environment.ProcessPath` | 3 | 2 | 1.50 | v1 | Block saves between the restore and the restart, and check the restart path. Same code area, so they're done together. |
| C12 | ZIP reader may not cap total uncompressed size | 3 | 2 | 1.50 | v1 | Check, and add a cap on the total across all entries plus a robustness test if it's missing. |
| C20 | Two copies of the app can open the same database | 3 | 2 | 1.50 | v1 | A single-instance check (named mutex) stops two copies writing the same file. Sharing between PCs is still out of scope. |
| C25 | Live database is never checked for corruption | 3 | 2 | 1.50 | v1 | Run `PRAGMA integrity_check` on the live database before the daily backup, and from a Settings button. Warn if it fails. |
| C02 | Reverting to draft and finalizing again reprices the invoice | 4 | 3 | 1.33 | v2 | High value, but needs a history table plus prompt changes. Next milestone. |
| C04 | Schema versioning is implicit | 4 | 3 | 1.33 | v2 | Add a `schema_version`, and refuse to write a database from a newer build. Also covers the "schema upgrade across versions" test gap. |
| C26a | No tests for MainViewModel finalize/generate or InvoicePdf | 4 | 4 | 1.00 | v2 | Worth doing. The README image generator in v1 builds some of the offscreen rendering this needs. |
| C07 | Dates use local `DateTime.Today` in many places | 3 | 3 | 1.00 | v2 | Capture "today" once per action, and test the year boundary of the invoice number. |
| C13 | Restore accepts any database with the three main tables | 2 | 2 | 1.00 | v2 | Depends on C04 (check the schema version and columns). |
| C18 | Invoice PDFs are re-rendered from current code | 3 | 3 | 1.00 | v2 | Add a test that compares an old invoice rendered before and after a change. Builds on the v1 image generator. |
| C21 | Manual backups are never pruned | 2 | 2 | 1.00 | v2 | Add a count or size limit, and show the total size in Settings. |
| C26b | Other Windows-only classes with no tests (PartViewModel, PlateImages, ErrorReport, SeedData, ...) | 2 | 4 | 0.50 | Left alone | Broad and lower risk. |
| C24 | No crash recovery for a job being edited | 3 | 4 | 0.75 | Left alone | Needs autosave and recovery UI. Its own feature later. |
| C03 | Large files with mixed responsibilities | 2 | 5 | 0.40 | Left alone | Broad refactor with little direct benefit to users. |
| C15 | Job list loads every job on each reload | 1 | 3 | 0.33 | Left alone | Not measured. Time it with a few hundred jobs before changing anything. |
| C16 | Pricing reruns on every change | 1 | 3 | 0.33 | Left alone | Cheap for real jobs. Only fix if a real job is slow. |
| C05 | Money stored as text | 1 | 4 | 0.25 | Left alone | Existing columns must not change (frozen data depends on them). Use integer minor units only for future columns. |
| C23 | Native SQLite engine ships with the app | 1 | 1 | n/a | Left alone | Nothing to fix. It's already pinned; run the backup tests when it's updated. |
| C17 | Pricing rules and frozen snapshots are fragile | n/a | n/a | n/a | Merged | The missing test is covered by C27. The rest is a rule already in CLAUDE.md. |
| C28 | Schema upgrade across versions not tested | n/a | n/a | n/a | Merged | Part of C04. |
| C29 | Local SQLite only, no multi-PC sharing | 1 | 5 | 0.20 | Left alone | Not planned. The single-instance part is C20. |

## Re-score after checking the code (Phase 2)

Checked against the code on 2026-11-02 while planning Phase 2. None of the seven items was fully handled already; two
were partly handled, so their notes say what is left.

| ID | Still open? | What the code showed | Score now | Decision |
|----|-------------|----------------------|-----------|----------|
| C11 | Yes | `release.yml` zips and attaches only the zip; there is no checksum anywhere. | 3.00 (unchanged) | Fix in Phase 2 (plan 02-02). |
| C27 | Partly | `Older_finalized_invoices_are_frozen_on_upgrade` (PriceSnapshotTests) covers one job on a current-schema database; still missing were a database without the snapshot columns, several jobs, a draft, literal totals and an exactly-once check. | 2.50 (unchanged; cost stays 2) | Fixed by `LegacyDatabaseTests` (plan 02-01). |
| C06 | Yes | Two sites: `SettingsViewModel.OpenFolder` and the Explorer line in `ErrorReport.Show` build an argument string by hand. The five other `Process.Start` calls pass the path or address as the program to open, so they already handle a quote. | 2.00 (unchanged) | Fix in Phase 2 (plan 02-04). |
| C09 | Partly | The daily name (`printinvoicer-YYYY-MM-DD.db`) and the manual name (`printinvoicer-YYYY-MM-DD-HHmmss-label.db`) can never be equal, so the startup and manual backups already never share a `.partial`. Left: two manual backups in the same second with the same label share one, and the copy deletes any existing `.partial` first, with no AUDIT tag. | 2.00 (benefit 2, cost 1: the remaining fix is the same small change) | Fix the remaining gap in Phase 2 (plan 02-03). |
| C10 | Yes | The update check reads GitHub's whole reply with no limit. | 2.00 (unchanged) | Fix in Phase 2 (plan 02-03). |
| C14 | Yes | Wider than described: only the job title is cleaned, with the operating system's list of bad characters (on Linux only two characters); the invoice number, which the user can type, is not cleaned at all, and there is no length limit. | 2.00 (unchanged) | Fix in Phase 2 (plan 02-04). |
| C22 | Yes | The README says nothing about QuestPDF's license (the concern text says it lists "Community license"; it does not), and AUDITING.md names the license without its condition. | 2.00 (unchanged) | Fix in Phase 2 (plan 02-02). |

**Note for C01 (Phase 3, FIX-08):** the one-time freeze gives an invoice finalized before snapshots existed today's
prices under the current pricing rules (version 2), even if it was first sent under version 1 rules (up to 3.4). The
Phase 2 legacy-database test uses values where both versions give the same cents, so it does not pin that behavior.
FIX-08 must keep the frozen totals of invoices already frozen.

## Summary

- **v1 (fix now, in score order):** C11, C27, C06, C09, C10, C14, C22, C01, C08+C19, C12, C20, C25
- **v2 (next milestone):** C02, C04, C26a, C07, C13, C18, C21
- **Left alone:** C26b, C24, C03, C15, C16, C05, C23, C29 (C17 and C28 were merged into other items)
