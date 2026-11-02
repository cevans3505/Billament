---
phase: "03"
slug: "rename-to-billament"
status: draft
# threats_open = count of OPEN threats at or above workflow.security_block_on severity (the blocking gate)
threats_open: 0
asvs_level: 1
created: "2026-11-02"
---

# Phase 03: Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

This file covers plans 03-01 to 03-04, which ran in the cloud. Plans 03-05 and 03-06 (Windows and manual) have not run.
Their threats, T-03-21 to T-03-29, are listed below as pending and must be verified by re-running
`/gsd-secure-phase 03` after those plans finish. Until then this file stays `status: draft`.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| old data folders -> new data folders | The one-time move on first start of 4.0.0 renames the user's folders and database | The whole database, backups and invoices (business data) |
| moved database -> folder settings | The rewrite edits two settings rows inside the user's database | Two folder paths |
| backup folder -> restore and prune | Files with either name prefix are listed, restored and deleted by date | Database copies |
| publish.ps1 -> developer's PC | The install script deletes the old program folder and shortcut | Program files |
| docs -> 3.7.0 users | Users follow the upgrade notes by hand | Instructions that can lose data if wrong |
| GitHub old name -> new name | 3.7.0 update checks rely on GitHub's redirect after the repository rename | Release information |

---

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| T-03-01 | Tampering | Scripted replacement over code, scripts and docs | medium | mitigate | File list from `git grep -l -I` over named paths; pricing and `InvoicePdf.cs` diff shows rename lines only; history folders unchanged | closed |
| T-03-02 | Denial of service | Pack URI, `x:Class`, `InternalsVisibleTo` after the assembly rename | medium | mitigate | `Billament.App.csproj` names, pack URIs in both windows, single `InternalsVisibleTo("Billament.UiTests")`; Linux build compiles the markup. Screen tests deferred to 03-05 | closed |
| T-03-03 | Spoofing | `UpdateService.Download` allow-check | low | mitigate | `UpdateService.cs:124` still requires `https://github.com/{Repository}/`, else `ReleasesPage`. Redirect check deferred to 03-06 | closed |
| T-03-04 | Denial of service | Release workflow heading check and zip names | low | mitigate | `release.yml:50` heading rule; `Read me.txt` heading `Billament 4.0`; both workflows parse. Tag run deferred to 03-06 | closed |
| T-03-05 | Denial of service | Data loss through the move | high | mitigate | Safety copy first (`DataFolderMigration.cs:117`); rename only, no delete call; failure facts pass | closed |
| T-03-06 | Tampering | Folder, file or link already at the new name | medium | mitigate | `Occupied` (`:361`) counts files and dangling links; pair skipped, old folder untouched | closed |
| T-03-07 | Denial of service | Half-done move | high | mitigate | `UndoMove` (`:328-348`) and the repair rule (`:95`); both facts pass | closed |
| T-03-08 | Tampering | Silent data fork after a failed move | high | mitigate | `App.xaml.cs:61` `MoveOldData()` gate before the only `new Database`; `Shutdown(1)` on failure | closed |
| T-03-09 | Tampering | Finalized totals changed by the move | high | mitigate | `Finalized_invoices_are_byte_for_byte_the_same_after_the_move`: rows and file bytes equal, fixtures price to 87.20 and 40.40 | closed |
| T-03-10 | Information disclosure | Paths in the failure message | low | accept | See AR-03-01 | closed |
| T-03-11 | Repudiation | Untagged disk access in the migration | low | mitigate | awk gate clean; 36 tags under `src`, `AUDITING.md` says 36 | closed |
| T-03-12 | Tampering | `RemapFolder` with `..` or sibling-prefix values | medium | mitigate | Fully qualified only, `GetFullPath`, directory-boundary rule (`:230-255`); theory covers `..`, siblings, trailing separator, case | closed |
| T-03-13 | Tampering | Settings rewrite writing more than intended | high | mitigate | Two named keys, read-only first, write only on change, one transaction (`:265-303`); full-dump fact passes | closed |
| T-03-14 | Denial of service | `Prune` deleting the newest backups across two prefixes | high | mitigate | Order by date group then name (`DatabaseBackup.cs:60-66`); daily regex excludes manual, before-restore and pre-rename copies; 10+10 fact passes | closed |
| T-03-15 | Tampering | Restoring a file chosen through the wider picker | low | accept | See AR-03-02 | closed |
| T-03-16 | Repudiation | Untagged database open in the rewrite | low | mitigate | `// AUDIT:` at `DataFolderMigration.cs:308` above `new SqliteConnection` | closed |
| T-03-17 | Tampering | publish.ps1 deleting the wrong folder or shortcut | high | mitigate | Fixed old path, delete after new install, requires `PrintInvoicer.exe`, no `.db`, no `Backups` or `Invoices`, path differs from new install, `-LiteralPath`, shortcut only when it targets `PrintInvoicer.exe` (`publish.ps1:44-62`). Residual: code review WR-02 (exact path equality only; a nested `-InstallDir` or a junction is not handled). User data stays protected by the other guards. Real run deferred to 03-05 | closed (L1), WR-02 residual open in 03-REVIEW-DISPOSITION.md |
| T-03-18 | Denial of service | Users following the notes and losing data | medium | mitigate | README and Read me.txt say not to start `PrintInvoicer.exe` after the move, where the safety copy is, update every PC; no note says to delete a data folder | closed |
| T-03-19 | Spoofing | Reuse of the old repository name | medium | mitigate | Remark on `UpdateService.Repository` (`:25-30`) and the 03-06 checklist forbid it. Checklist run pending in 03-06 | closed |
| T-03-20 | Repudiation | Stray or missing old name unnoticed | low | mitigate | Allowlist search re-run by the auditor; hit list in 03-04-SUMMARY.md | closed |
| T-03-SC | Tampering | NuGet installs | low | accept | See AR-03-03 | closed |
| T-03-21 | Denial of service | Real data moved before the build is proven | high | mitigate | Plan 03-05 (not run yet) | pending (03-05) |
| T-03-22 | Tampering | publish.ps1 touching data folders on the real profile | high | mitigate | Plan 03-05 checksum diff (not run yet) | pending (03-05) |
| T-03-23 | Denial of service | Move succeeding while 3.7.0 has the database open | medium | mitigate | Plan 03-05 case A (not run yet) | pending (03-05) |
| T-03-24 | Information disclosure | Real customer data in test output or pictures | low | mitigate | Plan 03-05 (not run yet) | pending (03-05) |
| T-03-25 | Spoofing | 3.7.0 update check following a redirect | low | accept | Plan 03-06 (not run yet) | pending (03-06) |
| T-03-26 | Denial of service | Redirects ending because a repository with the old name is created | medium | mitigate | Plan 03-06 (not run yet) | pending (03-06) |
| T-03-27 | Tampering | Release zip altered or incomplete | medium | mitigate | Plan 03-06 hash check (not run yet) | pending (03-06) |
| T-03-28 | Denial of service | 4.0.0 published before the rename | low | mitigate | Plan 03-06 task order (not run yet) | pending (03-06) |
| T-03-29 | Denial of service | 4.0.0 published while the redirect is broken | medium | mitigate | Plan 03-06 stop point (not run yet) | pending (03-06) |

*Status: open · closed · open, below high threshold (non-blocking) · pending (plan not run yet)*
*Severity: critical > high > medium > low; only open threats at or above workflow.security_block_on count toward threats_open*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-03-01 | T-03-10 | Failure messages name only the user's own data folders; technical details go to the local error file through `ErrorReport.Save` and are never sent anywhere | Plan 03-02 disposition (accept); premise checked by gsd-security-auditor | 2026-11-02 |
| AR-03-02 | T-03-15 | Restore keeps the `LooksLikeBillament` content check, `PRAGMA integrity_check` and the before-restore backup, even though the picker also offers the old name | Plan 03-03 disposition (accept); premise checked by gsd-security-auditor | 2026-11-02 |
| AR-03-03 | T-03-SC | No package, `.props`, `.targets` or lock file changed in this phase | Plans 03-01 to 03-04 disposition (accept); premise checked by gsd-security-auditor | 2026-11-02 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-11-02 | 21 verified (9 pending, plans 03-05 and 03-06 not run) | 21 | 0 | gsd-security-auditor (cloud, plans 03-01 to 03-04) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed for plans 03-01 to 03-04
- [ ] T-03-21 to T-03-29 verified after plans 03-05 and 03-06
- [ ] `status: verified` set in frontmatter

**Approval:** pending (re-run `/gsd-secure-phase 03` after plans 03-05 and 03-06)
