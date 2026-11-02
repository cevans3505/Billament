---
phase: 02-quick-win-concern-fixes
verified: 2026-11-02T02:30:00Z
status: human_needed
score: 10/13 must-haves verified
covered_files:
  - ".github/workflows/release.yml"
  - ".planning/phases/02-quick-win-concern-fixes/02-01-PLAN.md"
  - ".planning/phases/02-quick-win-concern-fixes/02-01-SUMMARY.md"
  - ".planning/phases/02-quick-win-concern-fixes/02-02-PLAN.md"
  - ".planning/phases/02-quick-win-concern-fixes/02-02-SUMMARY.md"
  - ".planning/phases/02-quick-win-concern-fixes/02-03-PLAN.md"
  - ".planning/phases/02-quick-win-concern-fixes/02-03-SUMMARY.md"
  - ".planning/phases/02-quick-win-concern-fixes/02-04-PLAN.md"
  - ".planning/phases/02-quick-win-concern-fixes/02-04-SUMMARY.md"
  - "AUDITING.md"
  - "CLAUDE.md"
  - "README.md"
  - "src/PrintInvoicer.App/ErrorReport.cs"
  - "src/PrintInvoicer.App/Pdf/InvoicePdf.cs"
  - "src/PrintInvoicer.App/UpdateService.cs"
  - "src/PrintInvoicer.App/ViewModels/SettingsViewModel.cs"
  - "src/PrintInvoicer.Core/Data/DatabaseBackup.cs"
  - "src/PrintInvoicer.Core/Files/SafeFileName.cs"
  - "src/PrintInvoicer.Core/Files/ShellCommands.cs"
  - "src/PrintInvoicer.Core/Updates/LatestReleaseReply.cs"
  - "tests/PrintInvoicer.Tests/BackupTests.cs"
  - "tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql"
  - "tests/PrintInvoicer.Tests/LatestReleaseReplyTests.cs"
  - "tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs"
  - "tests/PrintInvoicer.Tests/SafeFileNameTests.cs"
  - "tests/PrintInvoicer.Tests/ShellCommandsTests.cs"
covered_digest: "v4:sha256:46d3fcd4b7334378697b11c810e72580a5bc506ca111f5f3414e39299c560eff"
behavior_unverified: 2
overrides_applied: 0
behavior_unverified_items:
  - truth: "UpdateService.FetchLatestAsync compares the reply's Content-Length (long?) with LatestReleaseReply.MaxBytes without narrowing, so any declared length above the cap is turned away before the body is read"
    test: "Windows only: point the update check at a local stub (or inspect with a debugger) that declares a Content-Length above 262,144, then run Settings > Check now"
    expected: "No update information is shown, the body is never read, and no latest tag is saved with a new check time"
    why_human: "The comparison is present in UpdateService.cs (App, Windows-only); no test calls FetchLatestAsync, because it is private and static over the shared HttpClient. Core tests only cover LatestReleaseReply.ReadAsync."
  - truth: "An over-cap reply takes the same path as a 404: FetchLatestAsync returns null, FetchAndRememberAsync saves no latest tag with the check time, and repeating the check gives the same result"
    test: "Same stub as above, run Check now twice"
    expected: "Same answer both times; update-check.json holds no new release"
    why_human: "Null-return and save path live in the App project; no test exercises them. Backstop-tagged truth in plan 02-03."
coincidental_reliance_items: []
human_verification:
  - test: "Full dotnet test PrintInvoicer.sln on Windows (or the CI screen-tests job), including ScreenTests.Update_bar_shows_only_for_a_newer_release, ScreenTests.Every_invoice_kind_renders_for_every_job and the phase 1 ReadmeImageTests"
    expected: "All pass. InvoicePdf.Write (now using SafeFileName and the IsDirectlyInside guard) still returns a path inside the invoice folder; the update bar test is unaffected by the new read path; phase 1 README images still regenerate"
    why_human: "WPF and UI tests are Windows-only; none were run on Linux. Regression gate for phase 1 as well."
  - test: "Settings > Check now against live GitHub"
    expected: "Status says 'You have the latest version (...)' or 'Version X is available', not 'Could not reach GitHub'"
    why_human: "Needs Windows and the network; proves a real GitHub reply is read through the 256 KB cap."
  - test: "Hostile-name PDFs: draft job titled ..\\evil:CON with invoice number x\\..\\..\\y; click Preview, then Detail PDF. Also Preview on a normal existing job"
    expected: "Both PDFs open and sit directly in the previews folder and the invoice folder, with the cleaned names listed in 02-04-SUMMARY.md (for example 'Preview x-.-.-y -evil-CON.pdf'); the normal job's preview name is the usual one"
    why_human: "Needs the built Windows app; Linux tests only prove the name strings."
  - test: "Explorer gate A2: (a) Settings Open buttons open Explorer at each folder; (b) run explorer.exe /select, \"C:\\Program Files\\Windows Defender\\MpCmdRun.exe\" and also confirm the file is highlighted when started through ShellCommands.ShowFile (ErrorReport 'Yes' path); apply the documented fallback if not"
    expected: "Folder opens; file highlighted. Record the result under 'Windows Explorer check (A2)' in 02-04-SUMMARY.md (still reads PENDING) or in 02-UAT.md"
    why_human: "Explorer only runs on Windows. Plan 02-04 makes this an end-of-phase gate: while PENDING the phase cannot be passed."
  - test: "Next real v<version> tag"
    expected: "The draft release has PrintInvoicer-<version>.zip and PrintInvoicer-<version>.zip.sha256, a 'SHA-256:' line in the notes, and Get-FileHash on the downloaded zip prints the same hash"
    why_human: "Needs a tag push and the GitHub windows-latest runner; pwsh is not available here and a tag push publishes a draft."
  - test: "Judgment-tier prohibitions: unverified-prohibition, human review recommended (non-authoritative LLM verdict was 'satisfied' for each)"
    expected: "(1) FIX-02 asserted totals 87.20, 40.40, 12.70, 16.05 are hand-derived literals, not read from the calculator; (2) README and AUDITING.md do not imply a checksum proves who built a release; (3) QuestPDF text is not presented as legal advice and gives the date and version read"
    why_human: "Judgment-tier items are never a silent pass."
---

# Phase 2: Quick-Win Concern Fixes Verification Report

**Phase Goal:** The concerns with the best benefit for the cost (triage score 2.0 and above) are fixed in score order, so releases can be checked, invoices already sent are guarded by a test, and paths, file names and update checks are handled safely.
**Verified:** 2026-11-02
**Status:** human_needed
**Re-verification:** No, initial verification

## Goal Achievement

Everything that can be checked on Linux is in place and passes. I re-ran `dotnet test tests/PrintInvoicer.Tests` myself: 464 passed, 0 failed. The remaining items need Windows or GitHub, and the Explorer check (A2) that plan 02-04 made an end-of-phase gate still reads PENDING. Three open code-review warnings (WR-01 to WR-03) are recorded below as warnings, not gaps; WR-01 in particular needs a decision from the maintainer.

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | SC1: CONCERNS-TRIAGE.md scores every concern, and items found already handled are re-scored there, not changed | VERIFIED | `.planning/CONCERNS-TRIAGE.md` has the full score table and a "Re-score after checking the code (Phase 2)" section with C11, C27, C06, C09, C10, C14, C22 rows (C09 and C27 marked "Partly") plus the C01 rules-version-2 note. |
| 2 | SC2a: each release has a SHA-256 checksum file next to the zip, and the steps say to state it in the notes | VERIFIED (static) | `release.yml`: "Write the SHA-256 checksum file" step after Zip and before upload; LF-terminated `hash  name` line; upload lists both files with `if-no-files-found: error`; draft job attaches both and writes `SHA-256: $sha` into the notes. `permissions: contents: read` top level, `contents: write` only on draft-release (unchanged). README "Verify a download" and "Making a release" steps 3 and 4, and CLAUDE.md release line, say to keep the SHA-256 line. Live run is a human item. |
| 3 | SC2b: README and AUDITING.md state the QuestPDF Community revenue-threshold condition | VERIFIED | README License section and AUDITING.md QuestPDF row: USD 1,000,000 annual gross revenue, public-sector and publicly traded exclusions, 90 days, November 2026, license version 3.0 effective 6 July 2026, link, "not legal advice". |
| 4 | SC3: a test opens a pre-snapshot fixture database and shows each finalized job gets a snapshot exactly once at the expected totals | VERIFIED | `LegacyDatabaseTests` (2 facts) + `Fixtures/pre-snapshot.sql`: asserts the old shape with `pragma_table_info`, then 87.20, 40.40 (finalized) and 12.70 / 16.05 (draft) as literals; byte-identical snapshots after price changes and two reopens. Both pass in the 464-test run. |
| 5 | SC4a: Explorer / Process.Start paths go through ArgumentList; a path with a quote works | VERIFIED (shape) | `ShellCommands.ShowFolder/ShowFile` add each path as its own `ArgumentList` entry with empty `Arguments`; `SettingsViewModel.OpenFolder` and `ErrorReport.Show` use them under their existing AUDIT comments; `ShellCommandsTests` (8 cases) include a path with quotes, trailing backslash and `/select,C:\x`. Explorer's real handling is the A2 gate (truth 9). |
| 6 | SC4b: a manual backup and the startup daily backup never share the same `.partial` file | VERIFIED | `DatabaseBackup.Copy` writes to `{target}.{Guid:N}.partial`, deletes only its own file on failure, with the new AUDIT tag. `BackupTests`: same-second manual backups, daily+manual in parallel (20 rounds, different targets), `A_partial_file_from_another_copy_is_left_alone`, `A_failed_backup_removes_its_own_partial_file`. All pass. |
| 7 | SC5a (Core): an update-check reply over the cap is no update info | VERIFIED | `LatestReleaseReply.ReadAsync` stops at `MaxBytes` (262,144) and returns null; tests for exactly-cap, one byte over, endless stream, cancelled, damaged, missing fields, JSON null. |
| 8 | SC5b: invoice file names from user data containing `..`, `\`, `:` or reserved names come out safe and length-limited, with tests | VERIFIED (with warnings WR-01 to WR-03) | `SafeFileName.Clean/Compose/IsDirectlyInside`; `InvoicePdf.Write` uses `Compose` for all three kinds and throws if the path is not directly in its folder; `SafeFileNameTests` 70 cases pass. Folder part of FIX-06: no folder is built from job data (confirmed in `InvoicePdf.Write`: the folder is the preview folder, the default folder or the Settings folder, none from job fields). |
| 9 | End-of-phase gate A2: the Windows Explorer check is run and recorded | UNCERTAIN (human) | 02-04-SUMMARY.md "Windows Explorer check (A2)" still reads PENDING. Plan 02-04 says the phase is human_needed, never passed, while PENDING. |
| 10 | FetchLatestAsync compares Content-Length (long?) with MaxBytes without narrowing | PRESENT_BEHAVIOR_UNVERIFIED | Code at `UpdateService.cs` is `response.Content.Headers.ContentLength > LatestReleaseReply.MaxBytes` after `ResponseHeadersRead`, and the body goes through `ReadAsync`. No test reaches this private App method (backstop-tagged truth). |
| 11 | An over-cap reply takes the same path as a 404 (null, no saved tag, repeatable) | PRESENT_BEHAVIOR_UNVERIFIED | Both over-cap routes return null like the 404 line. No test exercises `FetchAndRememberAsync` with it (backstop-tagged). |
| 12 | SafeFileName and ShellCommands keep no mutable static state | VERIFIED (by reading) | Only `const` values and `static readonly` `char[]` / `HashSet<string>` that are only read. |
| 13 | Prohibitions and scope: Database.cs and Pricing unchanged; token permissions unchanged; update check still one GET | VERIFIED | `git diff 2b5965a HEAD --stat` shows no change to `Database.cs` or `src/PrintInvoicer.Core/Pricing/`; the release.yml diff touches no `permissions:`; `Http.GetAsync` is the only request, same URL. Enforcement is by diff inspection, not an automated test (flagged below). |

**Score:** 10/13 truths verified (2 present, behavior-unverified; 1 uncertain, waiting on the Windows A2 check)

### Prohibitions

| Prohibition | Tier | Result |
|-------------|------|--------|
| FIX-02: no change to how finalized totals are computed or frozen | test | Satisfied. No diff in `Pricing/` or `Database.cs`. Enforcement is the diff, not a dedicated test: flagged. |
| FIX-02: expected totals are hand-derived literals | judgment | Non-authoritative verdict: satisfied (decimal literals with the arithmetic in comments). unverified-prohibition, human review recommended. |
| FIX-01: do not imply the checksum proves who built a release | judgment | Non-authoritative verdict: satisfied (README and AUDITING.md say it does not). unverified-prohibition, human review recommended. |
| FIX-07: QuestPDF text is not legal advice | judgment | Non-authoritative verdict: satisfied. unverified-prohibition, human review recommended. |
| FIX-01: token permissions not widened | test | Satisfied by diff inspection; no automated check: flagged. |
| FIX-04: delete only files the copy created | test | Satisfied. `A_partial_file_from_another_copy_is_left_alone` and `A_failed_backup_removes_its_own_partial_file` pass. |
| FIX-05: no new network request | test | Satisfied by inspection (single `GetAsync`, same URL); no automated check: flagged. |
| FIX-06: user's invoice folder not rewritten | test | Satisfied by inspection of `InvoicePdf.Write`; only the file name is passed through `SafeFileName`. |
| FIX-06: names inside the boundary unchanged | test | Satisfied. `Normal_invoice_names_are_unchanged` and `Titles_inside_the_boundary_keep_their_old_name` pass. |

### Requirements Coverage

Every ID in the plans and in the ROADMAP (TRIA-01, FIX-01 to FIX-07) is present in REQUIREMENTS.md and in a plan's `requirements`. No orphaned IDs: REQUIREMENTS.md maps only these eight to Phase 2 (FIX-08 to FIX-12 are Phase 3).

| Requirement | Source Plan | Status | Evidence |
|-------------|-------------|--------|----------|
| TRIA-01 | 02-01 | SATISFIED | Truth 1. |
| FIX-01 | 02-02 | SATISFIED in code; live tag run is a human item | Truth 2. |
| FIX-02 | 02-01 | SATISFIED | Truth 4. |
| FIX-03 | 02-04 | SATISFIED in code; Explorer behavior pending A2 | Truths 5 and 9. See the wording mismatch below. |
| FIX-04 | 02-03 | SATISFIED | Truth 6. |
| FIX-05 | 02-03 | SATISFIED for the reader; App wiring behavior-unverified | Truths 7, 10, 11. |
| FIX-06 | 02-04 | SATISFIED, with warnings WR-01 to WR-03 open | Truth 8. |
| FIX-07 | 02-02 | SATISFIED | Truth 3. |

Note: REQUIREMENTS.md still shows only TRIA-01 in the "Complete: 1" line of the Coverage block, while the table marks all eight Complete. That counter is stale (cosmetic).

### FIX-03 wording check (asked by the orchestrator)

REQUIREMENTS.md FIX-03 now reads: "...so a path can never turn into extra Explorer switches; if Explorer needs one quoted string instead, a path containing a double quote is refused". This does NOT match the user's decision, which was to say a path containing a double quote is refused. It is a conditional sentence that is true either way, as the 02-04 executor said. It does match the code: `ShellCommands` does not refuse a quoted path (tests prove it stays one argument). Refusal exists only in the unbuilt A2 fallback. The wording also fits the ROADMAP success criterion 4 ("Opening a file or folder whose path contains a quote works"), which conflicts with an unconditional "refused". The maintainer should choose: keep the conditional wording (matches the code and the roadmap), or make the code refuse quotes (the user's literal decision, which would contradict roadmap SC4). I did not change either.

### Key Link Verification

| From | To | Status | Details |
|------|----|--------|---------|
| `InvoicePdf.Write` | `SafeFileName.Compose` / `IsDirectlyInside` | WIRED | Three `Compose` calls; guard before `GeneratePdf`. |
| `SettingsViewModel.OpenFolder` | `ShellCommands.ShowFolder` | WIRED | `Process.Start(ShellCommands.ShowFolder(path))` under the existing AUDIT comment. |
| `ErrorReport.Show` | `ShellCommands.ShowFile` | WIRED | Same. |
| `UpdateService.FetchLatestAsync` | `LatestReleaseReply.ReadAsync` | WIRED | Plus `OperationCanceledException` in the catch filter. |
| `LegacyDatabaseTests` | `pre-snapshot.sql` | WIRED | Via `TestFiles.Fixture`; fixture copied by the existing csproj rule. |
| `release.yml` | README step 4 | WIRED | `SHA-256:` line in notes; README tells the maintainer to keep it. |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Core unit suite | `dotnet test tests/PrintInvoicer.Tests` | 464 passed, 0 failed | PASS |
| AUDIT tag count | `git grep -c 'AUDIT:' -- src` summed | 27, AUDITING.md says 27 | PASS |
| Debt markers in phase files | grep for TBD, FIXME, XXX, TODO, HACK | none | PASS |
| Em/en dashes in new docs and Core code | grep | none | PASS |
| WPF/UI tests | not run (Windows only) | | SKIP, human item 1 |

### Probe Execution

SKIPPED: the phase declares no probe scripts.

### Anti-Patterns Found

None blocking. No debt markers, stubs or empty implementations in the changed files.

### Open code-review findings (all still "open" in 02-REVIEW-DISPOSITION.md)

These are WARNINGS, not gaps. They need a maintainer decision before or after Windows verification.

- **WR-01 (needs decision):** `Clean(number, 30)` keeps the head of the invoice number. `InvoicePrefix` is free text, so a prefix of about 28 or more characters cuts the 3-digit sequence off, and invoices with the same title then share one PDF name; the later one replaces the earlier. I confirmed this from `SafeFileName.Cut` and `AppSettings.FormatInvoiceNumber` (`prefix + sequence.ToString("000")`). It cannot happen with the default prefix. It is a regression from the old behaviour (numbers were never shortened). I did not mark it a gap because it needs an unusually long custom prefix; if the maintainer considers an invoice PDF being overwritten unacceptable, treat it as a gap and keep the tail of the number.
- **WR-02:** titles over 60 chars, empty titles, device names and similar now get a new PDF name, so regenerating a finalized invoice's PDF can leave a second file. Documented and pinned by tests in 02-04-SUMMARY.md "Which invoice names change"; the user decided to keep the 60/120 limits. No finalized total is affected (names never feed pricing).
- **WR-03:** `IsDirectlyInside` may throw `InvalidOperationException` on Windows for a Settings invoice folder with a trailing space or dot, and `GenerateInvoices` catches only `IOException`. Not provable on Linux; folded into human item 1 and 3 (try an invoice folder with a trailing space).
- IN-01 to IN-05: informational. IN-05 (Explorer arguments unchecked on Windows) is the A2 gate above.

### Deferred Items

None. Rules-version-2 behaviour of the legacy freeze is intentionally left to Phase 3 FIX-08 (user decision); it is recorded in CONCERNS-TRIAGE.md.

### Human Verification Required

See the `human_verification` list in the frontmatter. In short: (1) full Windows `dotnet test PrintInvoicer.sln` or CI screen-tests; (2) Check now against live GitHub; (3) hostile-name PDFs and a normal preview; (4) Explorer gate A2; (5) next release tag run with checksum; (6) maintainer review of the three judgment-tier prohibitions. None are marked passed.

### Gaps Summary

No gaps found in the code that can be checked on Linux. The phase is human_needed because Windows and GitHub checks are outstanding, the A2 gate is PENDING, and two App-side update-check truths have no behavioral test. Decisions for the maintainer: WR-01 (keep the number's tail?), the FIX-03 wording versus the code and roadmap, and whether to triage the eight "open" review findings.

---

_Verified: 2026-11-02_
_Verifier: Claude (gsd-verifier)_
