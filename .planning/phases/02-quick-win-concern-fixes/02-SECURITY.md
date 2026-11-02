---
phase: "02"
slug: "quick-win-concern-fixes"
status: verified
# threats_open = count of OPEN threats at or above workflow.security_block_on severity (the blocking gate)
threats_open: 0
asvs_level: 1
created: "2026-11-02"
---

# Phase 02: Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| finalized invoice -> later code changes | A total a customer already has must not move when code or prices change | Invoice totals (business data) |
| repository fixture -> public repo | Whatever the fixture holds is published on GitHub | Sample data only |
| GitHub release page -> user's PC | A user downloads the zip and runs the exe on trust | Release binary |
| tag push -> release workflow (write token) | The draft-release job holds `contents: write` | Repository write token |
| project docs -> user's license decision | Users rely on the README to know whether the QuestPDF terms fit them | License guidance |
| GitHub API -> app | The latest-release reply is untrusted network input | JSON reply, up to 256 KB |
| app -> backup folder | The backup folder may be slow or shared, written by another copy of the backup code | Database copies |
| user-typed job data -> file system | Invoice number and job title become part of a file path | File names |
| user-typed Settings folder -> Explorer | A folder path typed in Settings is handed to another program | Process arguments |

---

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| T-02-01 | Tampering | Frozen totals of pre-3.2 finalized invoices | high | mitigate | No change under `src/PrintInvoicer.Core/Pricing` or `Data/Database.cs` since 2b5965a; `LegacyDatabaseTests.cs:71-135` pins hand-derived totals and byte-identical snapshot text | closed |
| T-02-02 | Information disclosure | `Fixtures/pre-snapshot.sql` | low | mitigate | Sample names only (`pre-snapshot.sql:42-63`) | closed |
| T-02-03 | Spoofing | Release zip downloaded from GitHub | medium | mitigate | `release.yml:81-96,132-147` writes and attaches `.sha256` and a `SHA-256:` note line; README "Verify a download" and AUDITING.md say it proves integrity, not origin | closed |
| T-02-04 | Elevation of privilege | `release.yml` token and run scripts | high | mitigate | Permissions unchanged (`release.yml:12-13,113-114`); new step uses only `steps.version.outputs.version`; no new action | closed |
| T-02-05 | Repudiation | QuestPDF license statement | low | mitigate | README.md:263-272 gives link, date read, QuestPDF's terms, not legal advice; wording re-checked 2026-11-02 | closed |
| T-02-06 | Denial of service | `UpdateService.FetchLatestAsync` | medium | mitigate | `ResponseHeadersRead`, Content-Length check, `LatestReleaseReply.ReadAsync` 256 KB cap (`UpdateService.cs:161-167`, `LatestReleaseReply.cs:21-39`); tests for boundary, endless stream, cancellation | closed |
| T-02-07 | Tampering | `DatabaseBackup.Copy` temp files | medium | mitigate | GUID temp name per copy, deletes only its own temp (`DatabaseBackup.cs:116-137`); `BackupTests.cs:131-175` | closed |
| T-02-08 | Spoofing | `html_url` used by the Download button | low | accept | See accepted risk AR-02-01 | closed |
| T-02-09 | Repudiation | File delete in `DatabaseBackup.Copy` | low | mitigate | `// AUDIT: file delete.` at `DatabaseBackup.cs:129`; AUDITING.md says 27 tags, matches `git grep` | closed |
| T-02-10 | Tampering | `InvoicePdf.Write` file name | high | mitigate | `SafeFileName.Compose` and `IsDirectlyInside` (`SafeFileName.cs:25-135`); `InvoicePdf.cs:48-58` throws before any write outside the folder; traversal tests in `SafeFileNameTests.cs:99-156` | closed |
| T-02-11 | Denial of service | Reserved device names, overlong names | low | mitigate | Device-name prefix and 60/30/120 limits without split surrogates (`SafeFileName.cs:12-150`); tests in `SafeFileNameTests.cs:182-252` | closed |
| T-02-12 | Elevation of privilege | `explorer.exe` arguments | medium | mitigate | `ShellCommands` passes each path as one `ArgumentList` entry (`ShellCommands.cs:15-25`); `ShellCommandsTests.cs:8-36`. Windows Explorer behavior check (A2) still pending on Windows | closed |
| T-02-13 | Tampering | Numbers that clean to the same text | low | accept | See accepted risk AR-02-02 | closed |
| T-02-SC | Tampering | NuGet / npm / pip installs | low | accept | See accepted risk AR-02-03 | closed |

*Status: open · closed · open, below high threshold (non-blocking)*
*Severity: critical > high > medium > low. Only open threats at or above workflow.security_block_on count toward threats_open*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-02-01 | T-02-08 | `UpdateService.Download` opens only addresses under this repository and otherwise the releases page (`UpdateService.cs:113-121`, unchanged this phase) | Plan 02-03 threat model; premise checked by security audit | 2026-11-02 |
| AR-02-02 | T-02-13 | Invoice numbers come from the counter; the editor asks before saving an exactly equal number. Two numbers that clean to the same text could replace a PDF of the same kind, which the user can regenerate | Plan 02-04 threat model; premise checked by security audit | 2026-11-02 |
| AR-02-03 | T-02-SC | No package was added: no `.csproj`, `.props` or `.targets` change since 2b5965a | Plans 02-01 to 02-04 threat models; premise checked by security audit | 2026-11-02 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-11-02 | 14 | 14 | 0 | gsd-security-auditor (ASVS L1, block_on high) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-11-02 (pending Windows A2 check is tracked in the phase verification, not as an open threat)
