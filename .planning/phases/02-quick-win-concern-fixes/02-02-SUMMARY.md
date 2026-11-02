---
phase: 02-quick-win-concern-fixes
plan: 02
subsystem: infra
tags: [github-actions, sha256, release, docs, questpdf-license]
requires:
  - phase: 01
    provides: milestone scaffolding and CONCERNS-TRIAGE.md scores
provides:
  - Release workflow step that writes PrintInvoicer-<version>.zip.sha256 and attaches it, with a SHA-256 line in the draft notes
  - README "Verify a download" section and updated "Making a release" steps
  - QuestPDF Community license condition stated in README and AUDITING.md
affects: [02-03, 02-04 (both edit AUDITING.md after this plan)]
status: complete
requirements-completed: [FIX-01, FIX-07]
actuals:
  tokens: 1900
  tasks: 3
  commits: 3
plan_head_before: 0ea5d099bd9f8916364da98c98f569b1493155f2
plan_head_after: 9a52fe2a118ff4f8a0f3f645ee24aa243e096443
tech-stack:
  added: []
  patterns: ["checksum file in sha256sum format written with [IO.File]::WriteAllText so the line ends in LF"]
key-files:
  created: []
  modified:
    - .github/workflows/release.yml
    - README.md
    - AUDITING.md
    - CLAUDE.md
key-decisions:
  - "Workflow permissions unchanged: contents: read at top level, contents: write only on draft-release"
  - "README license text lists QuestPDF's other eligible groups instead of saying the revenue test is the only way in, because the live page says so"
coverage:
  - id: D1
    description: "Tag build writes the .sha256 (lowercase hash, two spaces, zip name, LF) after the Zip step, uploads both files with if-no-files-found: error, and the draft job attaches both and puts a SHA-256 line in the notes"
    requirement: FIX-01
    verification:
      - kind: other
        ref: "python3 yaml structure check from Task 1 prints ok; grep for the format line and ToLowerInvariant exits 0"
        status: pass
      - kind: manual_procedural
        ref: "next real v<version> tag on GitHub: draft has zip and .sha256, notes have SHA-256 line, Get-FileHash matches"
        status: unknown
    human_judgment: true
  - id: D2
    description: "README Verify a download section (Get-FileHash and sha256sum -c), release steps 3 and 4, CLAUDE.md release line, AUDITING.md bullet and code-signing paragraph; all say integrity, not origin"
    requirement: FIX-01
    verification:
      - kind: other
        ref: "awk/grep section-order and phrase checks from Task 2 all exit 0"
        status: pass
    human_judgment: false
  - id: D3
    description: "README License section and AUDITING.md QuestPDF row state the USD 1,000,000 annual gross revenue condition, exclusions, 90 days, date read, version and link, and say it is not legal advice"
    requirement: FIX-07
    verification:
      - kind: other
        ref: "joined-line phrase check from Task 3 prints task3-ok; QuestPDF table row still has three cells"
        status: pass
    human_judgment: false
duration: 12min
completed: 2026-11-02
---

# Phase 2 Plan 02: Release checksums and the QuestPDF license condition Summary

Every tagged release now builds `PrintInvoicer-<version>.zip.sha256` right after the zip, uploads and attaches both files, and puts a `SHA-256:` line (hash only) in the draft notes; README and AUDITING.md explain how to verify a download (integrity, not origin) and state the QuestPDF Community license revenue condition.

## Accomplishments

- `release.yml` build job: new step `Write the SHA-256 checksum file` between the Zip and upload steps (lowercase `Get-FileHash`, `[IO.File]::WriteAllText` with an LF line in `sha256sum -c` format). The upload step is renamed `Upload the zip and checksum for the release job`, lists both files, and keeps `if-no-files-found: error`.
- `release.yml` draft-release job: reads the hash from the first field of the `.sha256` line, adds `SHA-256: $sha` to the notes, attaches `"$zip.sha256"` next to the zip. `needs: build`, the duplicate-release check and every `permissions:` block are unchanged. The file header comment now mentions the checksum file.
- README: release steps 3 and 4 rewritten (workflow writes the checksum; keep the `SHA-256:` line in the published notes), new `### Verify a download` between "Install or update" and "Updates" with the `Get-FileHash` and `sha256sum -c` commands and the integrity-not-origin wording. CLAUDE.md release line updated the same way.
- AUDITING.md: `release.yml` bullet describes the checksum file; the code-signing paragraph gives the same limit. QuestPDF row carries the revenue condition.
- README `## License`: MIT line kept, plus a QuestPDF paragraph with date (November 2026), license version (3.0, effective 6 July 2026), link, USD 1,000,000 annual gross revenue, exclusions, 90 days, and "not legal advice".

## Task Commits

| Task | Name | Commit |
|------|------|--------|
| 1 | End-to-end release checksum (workflow, README steps, CLAUDE.md) | b31e0f1 |
| 2 | Verify a download in README, release notes in AUDITING.md | b17216d |
| 3 | QuestPDF Community license condition in README and AUDITING.md | 9a52fe2 |

## Verification

- Task 1 PyYAML structure check printed `ok` (step order, both files in upload path, `if-no-files-found: error`, permissions, `needs: build`, `"$zip.sha256"` and `SHA-256` in the draft script). Format grep for `"$hash  $zip`n"` and `ToLowerInvariant()` passed; `Get-FileHash`, `contents: write` and `contents: read` each appear exactly once.
- Task 2 and Task 3 automated checks and all acceptance greps exit 0. No em or en dashes in the four edited files. "There are 26 tags" in AUDITING.md is unchanged.
- `dotnet test tests/PrintInvoicer.Tests`: 374 passed, 0 failed (unchanged; no code changed).
- `pwsh` is not installed here, so the PowerShell lines were checked for syntax by reading only, not run.

## Deviations from Plan

### Auto-fixed / source-check differences

**1. [Source check - license wording] QuestPDF page lists more eligible groups than the plan text assumed**
- **Found during:** Task 3 (the plan told me to re-read both pages' own text before writing)
- **Issue:** Fetched https://www.questpdf.com/license/community.html and https://www.questpdf.com/license/guide.html on 2026-11-02. Version (3.0), effective date (6 July 2026), the USD 1,000,000 annual gross revenue figure, the public-sector and publicly traded exclusion, the 90-day transition and the "consolidated basis across entities under common control" wording all matched. But the page also says the license is free for charitable organisations, academic institutions and eligible open-source projects (and learning and evaluation), and the guide measures revenue for the most recently completed fiscal year. The plan's "only for individuals and organizations with annual gross revenue under USD 1,000,000" would have been incomplete.
- **Fix:** README says individuals and businesses can use it only under USD 1,000,000, and adds one sentence that QuestPDF lists other eligible groups (charities, academic institutions, eligible open-source projects) and to read its page. The AUDITING.md row says "for individuals and businesses only with annual gross revenue under USD 1,000,000". Every required phrase and check still passes.
- **Files modified:** README.md, AUDITING.md
- **Commit:** 9a52fe2

Otherwise the plan was executed as written. The YAML comment text in release.yml wraps the header sentence over two comment lines (the plan quoted it as one sentence).

## Known Stubs

None.

## Threat Flags

None. No new network, process, print or file code in `src`; the workflow adds one file write (the checksum) on the GitHub runner, outside the AUDIT tag count. Token permissions are unchanged (T-02-04 mitigation held).

## Pending Human Checks

- **GITHUB / WINDOWS-ONLY (not run, not marked passed):** push the next `v<version>` tag after its version bump is on main, open the draft release, and confirm it has `PrintInvoicer-<version>.zip` and `PrintInvoicer-<version>.zip.sha256` attached, a `SHA-256:` line in the notes, a passing `Write the SHA-256 checksum file` step, and that `Get-FileHash .\PrintInvoicer-<version>.zip -Algorithm SHA256` in Windows PowerShell matches the notes and the `.sha256` file (case ignored). The workflow runs only on a real tag push, and pwsh is not available in this environment.
- Judgment check for a maintainer: the QuestPDF paragraph in README is accurate against the page as read on 2026-11-02; re-read it if the QuestPDF terms change.

## Self-Check: PASSED

- FOUND: .github/workflows/release.yml, README.md, AUDITING.md, CLAUDE.md (all modified)
- FOUND: commits b31e0f1, b17216d, 9a52fe2 are ancestors of HEAD
