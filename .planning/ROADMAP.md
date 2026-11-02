# Roadmap: PrintInvoicer

## Overview

This milestone makes PrintInvoicer easier to understand at a glance, pays down known concerns in the order that gives the most value per unit of effort, and opens a way for people to send slice files. It starts with generated README images of the job list, the job editor and a sample invoice. Next come the concern fixes in triage score order: first the quick wins scored 2.0 and above, then the data-safety fixes scored 1.5. Before the data-safety fixes, the project is renamed to Billament and ships as 4.0.0, so the later work uses the new name. It ends with GitHub issue forms for slice file submissions, linked from the README and from a menu item in the app. Finalized invoices must never change at any point along the way.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: README Showcase** - Generated offscreen images of the job list, job editor and invoice page 1 in the README (completed 2026-11-01)
- [x] **Phase 2: Quick-Win Concern Fixes** - Concerns scored 2.0 and above fixed in score order (checksums, legacy database test, safer paths, names and reads) (completed 2026-11-01)
- [ ] **Phase 3: Rename to Billament** - The project, app, data folders and repository become Billament, with a one-time safe move of existing data; ships as 4.0.0
- [ ] **Phase 4: Data Safety Fixes** - Concerns scored 1.5 fixed in score order (freeze once, safe restore, ZIP cap, single instance, integrity check)
- [ ] **Phase 5: Slice File Submissions** - GitHub issue forms for slice files, linked from the README and an app menu item, with privacy warnings

## Phase Details

### Phase 1: README Showcase
**Goal**: A visitor to the README can see what PrintInvoicer looks like and what it produces, and a maintainer can regenerate those images with one command after UI changes.
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: SHOW-01, SHOW-02, SHOW-03, SHOW-04, SHOW-05
**Success Criteria** (what must be TRUE):
  1. The README shows an image of the main job list with sample jobs in draft, finalized and paid states
  2. The README shows an image of the job editor with a job built from a slice file, showing plates, filament use and the price breakdown
  3. The README shows page 1 of a sample invoice as a PNG
  4. A maintainer runs one command and all three images are rebuilt offscreen (no window or dialog appears) from seeded sample data that uses only sample names such as "Harbor Dive Shop"
  5. The README explains how to run that command to regenerate the images
**Plans**: 2/2 plans complete
Plans:
**Wave 1**
- [x] 01-01-PLAN.md - Tracer: job list picture through the one command (2x offscreen render, gated write, README section), then a paid job and fixed dates in SampleShop (SHOW-01, SHOW-04)

**Wave 2** *(blocked on Wave 1 completion)*
- [x] 01-02-PLAN.md - Job editor and invoice page 1 pictures on the same pipeline, the README regeneration note, AUDITING.md bullet and CI comment (SHOW-02, SHOW-03, SHOW-04, SHOW-05)
**UI hint**: yes

### Phase 2: Quick-Win Concern Fixes
**Goal**: The concerns with the best benefit for the cost (triage score 2.0 and above) are fixed in score order, so releases can be checked, invoices already sent are guarded by a test, and paths, file names and update checks are handled safely.
**Mode:** mvp
**Depends on**: Phase 1 (run order only; no code dependency)
**Requirements**: TRIA-01, FIX-01, FIX-02, FIX-03, FIX-04, FIX-05, FIX-06, FIX-07
**Success Criteria** (what must be TRUE):
  1. `.planning/CONCERNS-TRIAGE.md` scores every concern (TRIA-01, already done), and any item found to be already handled when its code is checked is re-scored there rather than changed
  2. Each release has a SHA-256 checksum file next to the zip, the release steps say to state it in the release notes, and the README and AUDITING.md state the QuestPDF Community license revenue-threshold condition
  3. A test opens a fixture database from before snapshots existed and shows that each finalized job gets a snapshot exactly once, with the expected totals
  4. Opening a file or folder whose path contains a quote works, and a manual backup and the startup daily backup never share the same `.partial` file
  5. An update-check response over the size cap is treated as "no update info", and invoice file and folder names built from user data containing `..`, `\`, `:` or reserved names like `CON` come out safe and length-limited, with tests
**Plans**: 4/4 plans complete
Plans:
**Wave 1**
- [x] 02-01-PLAN.md - Tracer: pre-snapshot fixture database opened by Database freezes each finalized invoice once at hand-derived totals, exactly-once check, and CONCERNS-TRIAGE re-score notes (FIX-02, TRIA-01) [cloud]
- [x] 02-02-PLAN.md - Tracer: release.yml writes, uploads and attaches PrintInvoicer-<version>.zip.sha256 with the hash in the draft notes; README Verify a download; QuestPDF USD 1,000,000 condition in README and AUDITING.md (FIX-01, FIX-07) [cloud; first real tag run on GitHub]

**Wave 2** *(blocked on Wave 1 completion: AUDITING.md)*
- [x] 02-03-PLAN.md - Tracer: update reply read through a 256 KB Core cap and wired into UpdateService; backup copies use their own .partial file with the missing AUDIT tag (FIX-05, FIX-04) [cloud code and tests; App compile-checked; run checks on Windows]

**Wave 3** *(blocked on Wave 2 completion: AUDITING.md and the 27-tag count)*
- [x] 02-04-PLAN.md - Tracer: SafeFileName in Core names every invoice and preview PDF and keeps it in its folder; ShellCommands passes Explorer paths through ArgumentList (FIX-06, FIX-03) [cloud code and tests; App compile-checked; run checks on Windows]

### Phase 3: Rename to Billament
**Goal**: PrintInvoicer becomes Billament everywhere (code, app, PDFs, scripts, docs, data folders and the GitHub repository), existing users keep every job and finalized invoice through a one-time safe move of their data, and the result ships as 4.0.0, so later phases are built under the new name from the start.
**Mode:** mvp
**Depends on**: Phase 2 (release 3.7.0 is the last PrintInvoicer release; run order only)
**Requirements**: NAME-01, NAME-02, NAME-03, NAME-04, NAME-05, NAME-06, NAME-07, NAME-08
**Success Criteria** (what must be TRUE):
  1. The solution is `Billament.sln` with projects, folders and namespaces `Billament.Core`, `Billament.App`, `Billament.Tests` and `Billament.UiTests`; the build makes `Billament.exe`, and window titles, dialogs, the update bar, the error report and the PDFs say Billament
  2. On first start with only `Documents\PrintInvoicer` and `%LOCALAPPDATA%\PrintInvoicer` present, a backup is made, then the data moves once to `Documents\Billament` (database `billament.db`) and `%LOCALAPPDATA%\Billament`; every finalized invoice total is the same before and after, and a custom backup or invoice folder inside the old folder still points at the moved files
  3. If the new folders already exist the old ones are never touched, and any failure during the move shows a plain message and leaves the old data where it was; the move logic is in Core with tests that run on Linux, and every new file or folder access has an `// AUDIT:` comment and an AUDITING.md row
  4. The update check, its allow-check, the issue page, README and docs point at `cevans3505/Billament`, and a 3.7.0 install still finds 4.0.0 through GitHub's redirect after the repository is renamed
  5. `publish.ps1`, `make-readme-images.ps1`, `tools/Make-Icon.ps1`, `ci.yml` and `release.yml` use the new name (install folder, Start menu shortcut with the old ones removed, `Billament-<version>.zip`, the "Billament 4.0" read-me heading check), and a case-insensitive search for "PrintInvoicer" finds only deliberate hits (migration old-folder names, history, changelog)
**Plans**: 3/6 plans executed
Plans:
**Wave 1**
- [x] 03-01-PLAN.md - Tracer: git mv the solution, projects and icon, one scripted replacement over code, build Billament.sln and pass the unit tests; then scripts, workflows and docs, version 4.0.0 and the "Billament 4.0" read-me heading (NAME-01, NAME-02, NAME-05, NAME-06, NAME-07, NAME-08) [cloud]

**Wave 2** *(blocked on Wave 1 completion: every later old-name string must be written after the bulk rename)*
- [x] 03-02-PLAN.md - Tracer: Core DataFolderMigration.Run (pre-rename backup, local folder, then Documents, then database rename with move-back), App.MoveOldData before new Database, Linux tests including byte-for-byte finalized totals, AUDIT tags and AUDITING.md rows (NAME-03) [cloud; App compile-checked, run on Windows in 03-05]

**Wave 3** *(blocked on Wave 2 completion: DataFolderMigration.cs and AUDITING.md)*
- [x] 03-03-PLAN.md - Tracer: rewrite BackupFolder and InvoiceFolder settings that point inside the old folder; printinvoicer-*.db backups listed, restored and pruned by the date in the name; restore picker shows both names (NAME-04) [cloud]

**Wave 4** *(blocked on Wave 3 completion: AUDITING.md and the finished move)*
- [ ] 03-04-PLAN.md - Upgrade notes in README, Read me.txt and release notes; redirect remark on UpdateService.Repository; publish.ps1 removes the old shortcut and install folder; final case-insensitive search against an allowlist (NAME-05, NAME-06, NAME-07, NAME-08) [cloud]

**Wave 5** *(blocked on Wave 4 completion)*
- [ ] 03-05-PLAN.md - Windows checks: full test suite with offscreen screen tests, make-readme-images.ps1, publish.ps1, and a real upgrade test on a copy of 3.7.0 data (3.7.0 still running, custom folders, old backups, run-once) (NAME-01, NAME-02, NAME-03, NAME-04, NAME-06, NAME-07) [windows]

**Wave 6** *(blocked on Wave 5 completion)*
- [ ] 03-06-PLAN.md - Manual end-of-phase checklist: final search again, rename the repository on GitHub, curl checks on the old API and web addresses (stops for your decision if they fail), then the 4.0.0 release and Check now in 3.7.0 (NAME-05, NAME-08) [windows, manual]
**UI hint**: yes

### Phase 4: Data Safety Fixes
**Goal**: The remaining v1 concerns (triage score 1.5) are fixed in score order, so the price freeze runs only once, a restore or a second copy of the app can't overwrite data, oversized slice archives are rejected, and database corruption is caught early.
**Mode:** mvp
**Depends on**: Phase 3 (built under the new name); Phase 2 (FIX-08 changes the freeze path that the FIX-02 legacy-database test guards)
**Requirements**: FIX-08, FIX-09, FIX-10, FIX-11, FIX-12
**Success Criteria** (what must be TRUE):
  1. Once the one-time freeze of older finalized jobs has run, `settings` records it and later starts skip it, while the Phase 2 legacy-database test still shows the same totals for invoices already frozen
  2. After a backup restore, nothing can save over the restored database before the restart, and the restart works when the app runs from an installed copy
  3. Opening a slice file whose ZIP entries add up to more than the total uncompressed cap shows a readable error (`InvalidDataException`), covered by a robustness test
  4. Starting a second copy of the app on the same PC shows a message and exits without opening the database
  5. The live database is checked with `PRAGMA integrity_check` before the daily backup and from a Settings button, and the user sees a warning when the check fails
**Plans**: TBD
**UI hint**: yes

### Phase 5: Slice File Submissions
**Goal**: People can send slice files and printer and slicer details through GitHub issue forms, reached from the README or a menu item in the app, and are warned about what slice files can reveal before they share them.
**Mode:** mvp
**Depends on**: Phase 1 (README layout); independent of the fix phases
**Requirements**: SUBM-01, SUBM-02, SUBM-03, SUBM-04, SUBM-05, SUBM-06
**Success Criteria** (what must be TRUE):
  1. On the repository's new issue page, a user can pick an "unsupported printer or slicer" form that asks for printer make, model and type (FDM or resin), slicer name and version, and slice file attachments
  2. A user can pick an "inaccurate calculation" form that asks for the same details plus expected and actual values for time, material and price
  3. Both forms explain how to attach files GitHub may not accept directly (for example zip them first), and both forms and the README warn that slice files can contain project names, file paths and thumbnails and should only be shared if the user is fine making them public
  4. The README links to both submission forms
  5. Picking the new menu item in the app opens the submission page in the browser, with the URL restricted to this repository and the call marked with an `// AUDIT:` comment and a row in AUDITING.md
**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1, 2, 3, 4, 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. README Showcase | 2/2 | Complete    | 2026-11-01 |
| 2. Quick-Win Concern Fixes | 4/4 | Complete    | 2026-11-01 |
| 3. Rename to Billament | 3/6 | In Progress | - |
| 4. Data Safety Fixes | 0/TBD | Not started | - |
| 5. Slice File Submissions | 0/TBD | Not started | - |
