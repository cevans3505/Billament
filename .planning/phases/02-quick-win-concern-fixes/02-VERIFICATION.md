---
phase: 02-quick-win-concern-fixes
verified: 2026-11-02T05:00:00Z
status: human_needed
score: 11/13 must-haves verified
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
  - "tests/PrintInvoicer.UiTests/ScreenTests.cs"
covered_digest: "v4:sha256:3953b18be288ea8210bb470cf48130a0b7b5557e82a048c33f6fd56c358f6e90"
behavior_unverified: 2
overrides_applied: 0
re_verification:
  previous_status: human_needed
  previous_score: 10/13
  gaps_closed:
    - "End-of-phase gate A2 (Explorer check): passed on Windows and recorded in 02-04-SUMMARY.md and 02-UAT.md"
    - "WR-01 (long invoice prefix cut the sequence digits): fixed in dee2ad2"
    - "WR-03 (trailing space or dot in the invoice folder): fixed in 5139a31, confirmed on Windows"
  gaps_remaining: []
  regressions: []
behavior_unverified_items:
  - truth: "UpdateService.FetchLatestAsync compares the reply's Content-Length (long?) with LatestReleaseReply.MaxBytes without narrowing, so any declared length above the cap is turned away before the body is read"
    test: "Windows only: point the update check at a local stub (or use a debugger) that declares a Content-Length above 262,144, then run Settings > Check now"
    expected: "No update information is shown, the body is never read, and no latest tag is saved with a new check time"
    why_human: "The comparison is in UpdateService.cs (App, Windows only). FetchLatestAsync is private and static over the shared HttpClient, so no test calls it. Core tests cover only LatestReleaseReply.ReadAsync. The live Check now run (UAT 2) proved the normal reply path, not the over-cap path."
  - truth: "An over-cap reply takes the same path as a 404: FetchLatestAsync returns null, FetchAndRememberAsync saves no latest tag with the check time, and repeating the check gives the same result"
    test: "Same stub as above; run Check now twice"
    expected: "Same answer both times; update-check.json holds no new release"
    why_human: "The null return and the save path are in the App project and no test exercises them with an over-cap reply."
coincidental_reliance_items: []
human_verification:
  - test: "Over-cap update reply through the real App path (the two behavior-unverified truths above)"
    expected: "Check now shows no update information for a reply declaring more than 262,144 bytes, twice in a row, and saves no new release"
    why_human: "No test reaches UpdateService.FetchLatestAsync / FetchAndRememberAsync with an over-cap reply."
  - test: "Release 3.7.0 (deferred follow-up, UAT 5): after phase 2 is merged, push the v3.7.0 tag"
    expected: "The draft release has PrintInvoicer-3.7.0.zip and PrintInvoicer-3.7.0.zip.sha256, a 'SHA-256:' line in the notes, the 'Write the SHA-256 checksum file' step passes, and Get-FileHash on the downloaded zip prints the same hash"
    why_human: "Needs a tag push and the GitHub windows-latest runner; cannot run before the merge. The maintainer chose on 2026-11-02 to check it on 3.7.0."
---

# Phase 2: Quick-Win Concern Fixes Verification Report

**Phase Goal:** The concerns with the best benefit for the cost (triage score 2.0 and above) are fixed in score order, so releases can be checked, invoices already sent are guarded by a test, and paths, file names and update checks are handled safely.
**Verified:** 2026-11-02
**Status:** human_needed
**Re-verification:** Yes, after the Windows UAT, the WR-01 and WR-03 fixes, and the code re-review

## Goal Achievement

The goal is achieved in the code. I read the changed source, did not take the SUMMARY files as evidence, and re-ran `dotnet test tests/PrintInvoicer.Tests` myself: 485 passed, 0 failed (matches the maintainer's Windows run of 485 Core + 11 UI tests at d60114c; no code has changed since). Everything the previous report left open that could be closed has been closed: the Explorer gate (A2) passed on Windows, WR-01 and WR-03 are fixed in code with tests, and the live "Check now", the hostile-name PDFs and the maintainer's wording review all passed.

Status stays `human_needed`, not `passed`, for two honest reasons. First, two App-side update-check truths still have no test that reaches them. Second, the release checksum workflow has never run on a real tag; the maintainer deferred that to release 3.7.0 with a recorded reason. Neither is a failure: nothing found contradicts the goal.

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | SC1: CONCERNS-TRIAGE.md scores every concern, and items found already handled are re-scored there, not changed | VERIFIED | `.planning/CONCERNS-TRIAGE.md` has the score table and a "Re-score after checking the code (Phase 2)" section (C11, C27, C06, C09, C10, C14, C22, plus the C01 note). No production code changed for it. |
| 2 | SC2a: each release has a SHA-256 checksum file next to the zip, and the steps say to state it in the notes | VERIFIED (static) | `release.yml`: step "Write the SHA-256 checksum file" after the zip step writes `hash  name` with an LF; the upload step lists both files with `if-no-files-found: error`; the draft job reads the hash, attaches both files and puts `SHA-256: $sha` in the notes. README "Verify a download" and "Making a release" steps 3 and 4, and the CLAUDE.md release line, say to keep that line. Live run is the 3.7.0 follow-up (human item). |
| 3 | SC2b: README and AUDITING.md state the QuestPDF Community revenue-threshold condition | VERIFIED | README License section (lines 263-272) and the AUDITING.md QuestPDF row give USD 1,000,000, the public-sector and publicly traded exclusions, the 90 days, the link, and "not legal advice". Maintainer approved the wording (UAT 6). |
| 4 | SC3: a test opens a pre-snapshot fixture database and shows each finalized job gets a snapshot exactly once at the expected totals | VERIFIED | `LegacyDatabaseTests` (2 facts) with `Fixtures/pre-snapshot.sql`: asserts the old shape, then literal totals 87.20, 40.40 (finalized) and 12.70, 16.05 (draft). The second test changes filament, printer and settings prices and reopens the database twice, then asserts the snapshot text is byte-identical, so a second freeze would be caught. Both pass. Totals are literals with the arithmetic in comments (UAT 6 confirmed). |
| 5 | SC4a: Explorer paths go through ArgumentList; a path with a quote works | VERIFIED | `ShellCommands.ShowFolder/ShowFile` add each path as its own `ArgumentList` entry; `SettingsViewModel.OpenFolder` and `ErrorReport.Show` use them; `ShellCommandsTests` includes quotes, a trailing backslash and `/select,C:\x`. Windows check done: the two-argument form highlights `MpCmdRun.exe` and a path with spaces opens (UAT 4). The other `Process.Start` sites open a single file or URL as `FileName`, not Explorer arguments. |
| 6 | SC4b: a manual backup and the startup daily backup never share the same `.partial` file | VERIFIED | `DatabaseBackup.Copy` writes to `{target}.{Guid:N}.partial`, deletes only its own file on failure, and carries `// AUDIT: file delete.` Tests: same-second manual backups, daily plus manual in parallel, another copy's partial file left alone, own partial removed on failure. All pass. (IN-02: the concurrency tests are weaker than their names; advisory.) |
| 7 | SC5a (Core): an update-check reply over the cap is treated as no update info | VERIFIED | `LatestReleaseReply.ReadAsync` stops once the total passes `MaxBytes` (262,144) and returns null. 8 test methods cover exactly-cap, one byte over, endless stream, cancelled, damaged, missing fields and JSON null. |
| 8 | SC5b: invoice file names from user data containing `..`, `\`, `:` or reserved names come out safe and length-limited, with tests | VERIFIED | `SafeFileName.Clean/Compose/CleanNumber/IsDirectlyInside/NormalizeFolder`; `InvoicePdf.Write` uses `Compose` for all three kinds, normalizes the folder, and throws before any write if the path is not directly in the folder. 70+ `SafeFileNameTests` cases including long prefixes keeping their sequence digits and folders with a trailing space or dot. Hostile-name PDFs confirmed on Windows (UAT 3). No folder is built from job data. |
| 9 | End-of-phase gate A2: the Windows Explorer check is run and recorded | VERIFIED | 02-04-SUMMARY.md "Windows Explorer check (A2)" now reads "File highlighted: two-argument form kept", dated 2026-11-02; 02-UAT.md test 4 passes. No fallback applied. |
| 10 | FetchLatestAsync compares Content-Length (long?) with MaxBytes without narrowing | PRESENT_BEHAVIOR_UNVERIFIED | `UpdateService.cs`: `response.Content.Headers.ContentLength > LatestReleaseReply.MaxBytes` after `ResponseHeadersRead`, and the body goes through `ReadAsync`. No test reaches this private App method. The live Check now run proved only the normal path. |
| 11 | An over-cap reply takes the same path as a 404 (null, no saved tag, repeatable) | PRESENT_BEHAVIOR_UNVERIFIED | Both over-cap routes return null like the 404 line, and `FetchAndRememberAsync` saves the null result. No test exercises it. |
| 12 | SafeFileName and ShellCommands keep no mutable static state | VERIFIED (by reading) | Only `const` values and read-only `static readonly` collections. |
| 13 | Prohibitions and scope: Database.cs and Pricing unchanged; token permissions unchanged; update check still one GET | VERIFIED | `git diff 2b5965a HEAD --stat` over `src/PrintInvoicer.Core/Pricing` and `Data/Database.cs` is empty; no `.csproj` changed (no new package); `release.yml` `permissions:` unchanged (`contents: write` only on the draft job); `Http.GetAsync` is the only request, same URL. Enforced by diff inspection, not by an automated test. |

**Score:** 11/13 truths verified (2 present and wired, behavior unverified)

### Prohibitions

| Prohibition | Tier | Result |
|-------------|------|--------|
| FIX-02: no change to how finalized totals are computed or frozen | test | Satisfied (empty diff on Pricing and Database.cs). By diff, not a dedicated test: flagged. |
| FIX-02: expected totals are hand-derived literals | judgment | Satisfied; maintainer confirmed in UAT 6. Not a silent pass: recorded as reviewed by a human. |
| FIX-01: do not imply the checksum proves who built a release | judgment | Satisfied; README line 95 and AUDITING.md line 130 say it does not. Maintainer approved. |
| FIX-07: QuestPDF text is not legal advice | judgment | Satisfied; says "not legal advice" and "QuestPDF's terms, not this project". Maintainer approved. |
| FIX-01: token permissions not widened | test | Satisfied by diff inspection: flagged, no automated check. |
| FIX-04: delete only files the copy created | test | Satisfied (tests pass). |
| FIX-05: no new network request | test | Satisfied by inspection: flagged, no automated check. |
| FIX-06: user's invoice folder not rewritten | test | Satisfied: only the file name goes through `SafeFileName`; the folder is only normalized the way Windows already does (WR-03 fix). |
| FIX-06: names inside the boundary unchanged | test | Satisfied (`Normal_invoice_names_are_unchanged`, `Titles_inside_the_boundary_keep_their_old_name`). |

### Requirements Coverage

Every ID in the plans (02-01: FIX-02, TRIA-01; 02-02: FIX-01, FIX-07; 02-03: FIX-04, FIX-05; 02-04: FIX-06, FIX-03) matches the eight IDs in the ROADMAP and in REQUIREMENTS.md for Phase 2. No orphaned IDs: FIX-08 to FIX-12 are mapped to Phase 3.

| Requirement | Source Plan | Status | Evidence |
|-------------|-------------|--------|----------|
| TRIA-01 | 02-01 | SATISFIED | Truth 1. |
| FIX-01 | 02-02 | SATISFIED in code; live tag run deferred to 3.7.0 | Truth 2. |
| FIX-02 | 02-01 | SATISFIED | Truth 4. |
| FIX-03 | 02-04 | SATISFIED | Truths 5 and 9. REQUIREMENTS.md now says the Windows check showed the two-argument form works and no fallback is used, which matches the code (the earlier wording mismatch is resolved). |
| FIX-04 | 02-03 | SATISFIED | Truth 6. |
| FIX-05 | 02-03 | SATISFIED for the reader; App wiring behavior-unverified | Truths 7, 10, 11. |
| FIX-06 | 02-04 | SATISFIED | Truth 8. |
| FIX-07 | 02-02 | SATISFIED | Truth 3. |

REQUIREMENTS.md coverage block now reads Complete: 13, matching the table (the earlier stale counter is fixed).

### Key Link Verification

| From | To | Status | Details |
|------|----|--------|---------|
| `InvoicePdf.Write` | `SafeFileName.Compose` / `NormalizeFolder` / `IsDirectlyInside` | WIRED | Three `Compose` calls, folder normalized before `CreateDirectory`, guard before `GeneratePdf`. |
| `SettingsViewModel.OpenFolder` | `ShellCommands.ShowFolder` | WIRED | `Process.Start(ShellCommands.ShowFolder(path))` under the AUDIT comment. |
| `ErrorReport.Show` | `ShellCommands.ShowFile` | WIRED | Same. |
| `UpdateService.FetchLatestAsync` | `LatestReleaseReply.ReadAsync` | WIRED | `ResponseHeadersRead`, Content-Length check, then `ReadAsync`; `OperationCanceledException` is in the catch filter. |
| `LegacyDatabaseTests` | `pre-snapshot.sql` | WIRED | Via `TestFiles.Fixture`. |
| `release.yml` | README step 4 | WIRED | `SHA-256:` line in the notes; README tells the maintainer to keep it. |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Core unit suite | `dotnet test tests/PrintInvoicer.Tests` | 485 passed, 0 failed | PASS |
| AUDIT tag count | `git grep -c 'AUDIT:' -- src`, summed | 27; AUDITING.md says 27 | PASS |
| Debt markers in phase-changed files | grep for TBD, FIXME, XXX, TODO, HACK | none | PASS |
| Em or en dashes in README, AUDITING.md, new Core code, new tests | git grep | none | PASS |
| Windows full run (maintainer, d60114c, no code change since) | `dotnet test PrintInvoicer.sln` | 485 Core + 11 UI passed | PASS (recorded in 02-UAT.md; I did not run the UI tests) |

### Probe Execution

SKIPPED: the phase declares no probe scripts.

### Anti-Patterns Found

None blocking. No debt markers, stubs or empty implementations in the changed files.

### Open code-review findings (02-REVIEW.md: 0 critical, 2 warnings, 5 info)

Weighed against the phase goal; none is a gap.

- **WR-02 (warning, advisory):** titles over 60 characters, empty titles, device names and similar get a new PDF name, so regenerating an old invoice's PDF can leave a second file. No total is affected, because names never feed pricing, so "finalized invoices never change" holds. The boundary is documented in 02-04-SUMMARY.md ("Which invoice names change") and pinned by tests, and the maintainer kept the 60/120 limits. Worth a release-notes line, not a fix before closing.
- **WR-04 (warning, advisory):** `OpenInvoiceFolder` still calls `Directory.CreateDirectory(InvoiceFolder)` on the raw setting without a try/catch (confirmed in `SettingsViewModel.cs`), so a badly typed folder reaches the global error dialog. This code existed before the phase; FIX-06 is about names built from job data, and no folder is built from job data. The PDF write itself is safe. Suggested for Phase 3 or a small follow-up.
- **IN-01** (unique `.partial` files left after a power cut are never pruned; documented in AUDITING.md), **IN-02** (backup concurrency tests weaker than their names), **IN-03** (an oversized reply clears the remembered update, as the code comment says it should), **IN-04** (bidirectional control characters kept in names), **IN-06** (a 30-character tail beginning `CON.` can still lose its last digit): all low risk and not goal-blocking.

### Deferred Items

None from this report. Rules-version-2 behaviour of the legacy freeze is intentionally left to Phase 3 FIX-08 and recorded in CONCERNS-TRIAGE.md.

### Human Verification Required

1. **Over-cap update reply through the real App path.** Test: point the update check at a local stub declaring more than 262,144 bytes, run Check now twice. Expected: no update information both times, no new release saved. Why human: `FetchLatestAsync` is private over a shared `HttpClient` and no test reaches it. This covers truths 10 and 11.
2. **Release 3.7.0 checksum (deferred by the maintainer, UAT 5).** Test: push `v3.7.0` after the merge. Expected: the draft has the `.zip` and `.zip.sha256`, a `SHA-256:` line in the notes, and `Get-FileHash` matches. Why human: needs a tag push and the GitHub runner; cannot run before the merge.

### Gaps Summary

No gaps. The phase goal is achieved in the code, and the Windows checks the previous report asked for have passed and are recorded. The two remaining items are a missing test seam for the App-side oversized-reply path and the first live release run, which the maintainer has scheduled for 3.7.0. If the maintainer is content to accept these two, they can be recorded as overrides and the phase can close.

---

_Verified: 2026-11-02_
_Verifier: Claude (gsd-verifier)_
