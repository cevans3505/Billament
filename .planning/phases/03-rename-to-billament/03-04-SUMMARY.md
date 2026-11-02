---
phase: 03-rename-to-billament
plan: 04
subsystem: docs
tags: [rename, docs, release-notes, publish-script, audit, final-search]

requires:
  - phase: 03-rename-to-billament
    provides: the data move (03-02), the folder-setting rewrite and old backup names (03-03), the Billament names everywhere (03-01)
provides:
  - README section "Renamed from PrintInvoicer" with a pointer near the top, the "Coming from PrintInvoicer" section in Read me.txt and an upgrade paragraph in the draft release notes, all telling the same steps
  - A remark on UpdateService.Repository explaining the 3.7.0 redirect and the rule never to reuse the old repository name
  - publish.ps1 that refuses to run while Billament or PrintInvoicer is open and retires the old install (shortcut and program folder only) after the new one is in place
  - AUDITING.md and CLAUDE.md text for what stays under the old name, and the final case-insensitive search with its allowlist and reasons
affects: [03-05, 03-06]

plan_head_before: a25079d2a4b533eea1612aec9a1e721b40dba55f
plan_head_after: 95fc3ab07fbde4a89a9ec10dcf8c4cd4e57a3c69

actuals:
  tokens: 2350
  tasks: 3
  commits: 2

tech-stack:
  added: []
  patterns:
    - "Old-name strings are deliberate and listed: every remaining hit has a reason, and the CLAUDE.md Old name rule says not to rename them"

key-files:
  created: []
  modified:
    - src/Billament.App/UpdateService.cs
    - .github/workflows/release.yml
    - packaging/Read me.txt
    - README.md
    - publish.ps1
    - AUDITING.md
    - CLAUDE.md

key-decisions:
  - "The upgrade paragraph in release.yml is one long line, because GitHub shows single line breaks in release notes as breaks"
  - "The old-folder content check in publish.ps1 does not silence errors: if the folder cannot be listed the script stops instead of guessing there is no .db file"
  - "No allowlist entry was added; three allowlisted files (.planning/PROJECT.md, .planning/state.json, tests/Billament.Tests/DataFolderMigrationTests.cs) turned out to have no hit at all"

requirements-completed: [NAME-05, NAME-06, NAME-07, NAME-08]

coverage:
  - id: D1
    description: "A 3.7.0 user reads the same upgrade steps (close PrintInvoicer, run Billament.exe, safety copy and one-time move, delete the old exe and re-pin, where the copy is, folders that follow, old backups, failure behavior, existing Documents\\Billament, do not restart the old exe, update every PC together, restoring a pre-4.0 backup) in the README section, the Read me.txt section and the draft release notes; the README has a pointer near its top"
    requirement: NAME-07
    verification:
      - kind: other
        ref: "Task 1 verify commands: awk/grep of each fact in the README section and the Read me section, pointer link, python/yaml parse of release.yml with the here-string end marker intact"
        status: pass
    human_judgment: false
  - id: D2
    description: "The remark on UpdateService.Repository explains the redirect for 3.7.0 and older and says never to create a repository with the old name; the constant is unchanged and the App project builds with 0 warnings and 0 errors"
    requirement: NAME-05
    verification:
      - kind: other
        ref: "grep of the constant and the remark; dotnet build src/Billament.App -p:EnableWindowsTargeting=true (0 warnings, 0 errors)"
        status: pass
    human_judgment: false
  - id: D3
    description: "publish.ps1 checks both process names, removes the old shortcut (only when it targets PrintInvoicer.exe) and the old program folder (only when it holds PrintInvoicer.exe and no data and is not the new install folder) after the new shortcut is saved, never names a data folder in a delete, and has exactly four # AUDIT: tags"
    requirement: NAME-06
    verification:
      - kind: other
        ref: "Task 2 verify awk/grep gates (tag directly above both Remove-Item lines, delete after $shortcut.Save(), no Documents in a delete line, 4 tags) and the acceptance greps"
        status: pass
    human_judgment: false
  - id: D4
    description: "publish.ps1 runs correctly in Windows PowerShell 5.1 (two-name Get-Process with SilentlyContinue, the multi-line -and expression, the shortcut target check, the real removal of an old install)"
    requirement: NAME-06
    verification: []
    human_judgment: true
    rationale: "pwsh is not installed in this container, so the script could only be checked as text. The real run is plan 03-05 Task 1 (windows_verification in the plan)."
  - id: D5
    description: "AUDITING.md describes the publish.ps1 cleanup, the printinvoicer-* backup names and the old update address and still says 36 tags; CLAUDE.md has the Old name rule"
    requirement: NAME-07
    verification:
      - kind: other
        ref: "Task 2 verify: grep of the Building and releasing bullet, the Backups row, the old API address, 'There are 36 tags', '**Old name:**'"
        status: pass
    human_judgment: false
  - id: D6
    description: "A case-insensitive search for the old name finds only allowlisted files, no tracked path or path on disk has the old name, the deliberate old names that the code needs are present, and the full unit suite, the solution build, the version and the tag count are green"
    requirement: NAME-08
    verification:
      - kind: unit
        ref: "dotnet test tests/Billament.Tests (525 passed, 0 failed)"
        status: pass
      - kind: other
        ref: "git grep -l -i printinvoicer allowlist gate; git ls-files and find -ipath gate; dotnet build Billament.sln -p:EnableWindowsTargeting=true (0 warnings, 0 errors); <Version>4.0.0</Version>; 36 AUDIT tags under src"
        status: pass
    human_judgment: false
  - id: D7
    description: "A real 3.7.0 install reaching 4.0.0 through GitHub's redirect from the old repository name"
    requirement: NAME-05
    verification: []
    human_judgment: true
    rationale: "Needs the real GitHub rename and a published 4.0.0. Verified by plan 03-06 (Task 2 step 3 and Task 3 step 6), not by this plan (D-16)."

duration: about 10 min
completed: 2026-11-02
status: complete
---

# Phase 3 Plan 04: Upgrade notes, the old-install cleanup and the final search Summary

**The same upgrade story for 3.7.0 users in the README, Read me.txt and the draft release notes; a publish.ps1 that retires the old PrintInvoicer shortcut and program folder (never data) after the new install is in place; and a final case-insensitive search where every remaining old-name hit is deliberate and listed.**

## Performance

- **Duration:** about 10 min (ended 2026-11-02T05:22Z)
- **Tasks:** 3 (Task 1 and Task 2 committed; Task 3 is a read-only gate and made no commit)
- **Files modified:** 7

## Accomplishments

- `UpdateService.Repository` has a `<remarks>` block: 3.7.0 and older ask about `cevans3505/PrintInvoicer`, GitHub redirects it here, never create a repository with the old name. The constant is unchanged.
- The release workflow's draft notes get the "Upgrading from PrintInvoicer 3.7 or older" paragraph (no `$` in it; the here-string end marker is still on its own line, checked by parsing the YAML).
- `packaging/Read me.txt` has "Coming from PrintInvoicer" between Install and First time (8 numbered steps), and the Updates section says old links to the PrintInvoicer page lead to the releases page too. The heading is still `Billament 4.0`.
- `README.md` has a pointer under the intro and a `## Renamed from PrintInvoicer` section before `## Install or update`, with the same facts as the Read me section, the full safety copy path, the `printinvoicer-*.db` pattern and a link to the latest release. The "Install or update" text now says publish.ps1 also removes an old install's shortcut and program folder, never its data.
- `publish.ps1`: two-name running check; after `$shortcut.Save()` and before the icon refresh it removes the old `PrintInvoicer.lnk` only if its target ends in `\PrintInvoicer.exe`, then `%LOCALAPPDATA%\Programs\PrintInvoicer` only if it holds `PrintInvoicer.exe`, no `.db` anywhere inside, no `Backups` or `Invoices` folder, and resolves to a different path than the new install folder. The old path is fixed, never derived from `-InstallDir`. It ends with the unpin/re-pin note.
- `AUDITING.md`: publish.ps1 bullet describes the cleanup, the Backups row names `printinvoicer-...` copies and prune by date, and the Network note gives the old update address. `CLAUDE.md` has the **Old name:** rule.

## Task Commits

1. **Task 1 (tracer):** `f981e11` (docs) - README section and pointer, Read me section, release notes paragraph, `UpdateService.Repository` remark
2. **Task 2:** `95fc3ab` (feat) - publish.ps1 cleanup and AUDIT tags, AUDITING.md, CLAUDE.md rule, README sentence
3. **Task 3:** no commit; read-only gate, results below

The Task 1 tracer gate was satisfied automatically: the automated verify passed (and re-ran green after the commit) and the task has no human-observable evidence, so execution expanded to Task 2.

**Plan metadata:** committed separately after this file (docs: complete plan)

## Final search (Task 3)

Commands, run from the repository root:

```bash
git grep -n -i printinvoicer -- . ':!.planning/phases/01-readme-showcase' ':!.planning/phases/02-quick-win-concern-fixes' ':!.planning/phases/03-rename-to-billament'
git grep -l -i printinvoicer -- <same pathspec>        # file names, compared with the allowlist
git ls-files | grep -i printinvoicer                  # tracked paths
find . -ipath '*printinvoicer*' -not -path './.git/*'  # paths on disk (also run without the bin/obj exclusion)
git grep -n -i printinvoicer -- .claude .planning/codebase
```

Result: 99 hit lines in 15 files, every file on the allowlist; the path searches printed nothing (no tracked path, no path on disk, bin and obj included); `.claude` and `.planning/codebase` have no hit. The allowlist gate printed no file outside the list. Positive checks all passed: `"PrintInvoicer"` and `"printinvoicer.db"` in `DataFolderMigration.cs`, `"printinvoicer-"` in `DatabaseBackup.cs`, `printinvoicer-*.db` in `SettingsViewModel.cs`, `'PrintInvoicer.lnk'` in `publish.ps1`.

Allowlist reasons (the allowlist is exactly the plan's; nothing was added). Three allowlisted files have no hit and are simply not listed: `.planning/PROJECT.md`, `.planning/state.json` and `tests/Billament.Tests/DataFolderMigrationTests.cs` (the migration tests use the `DataFolders.OldName` constants, so they carry no literal old name).

| Group | Files | Reason |
| --- | --- | --- |
| Migration names | `src/Billament.Core/Data/DataFolderMigration.cs` | The old folder and database names the one-time move must find, and the AUDIT comments naming the folders it renames |
| Old backup names | `src/Billament.Core/Data/DatabaseBackup.cs`, `tests/Billament.Tests/BackupTests.cs`, `src/Billament.App/ViewModels/SettingsViewModel.cs` | Backups made before 4.0 are still listed, restored and pruned; the restore picker offers both patterns; the tests pin it |
| Old-install cleanup | `publish.ps1` | Removes the old shortcut and program folder, and the running check covers the old process name |
| Update-check redirect | `src/Billament.App/UpdateService.cs` | The remark explaining why 3.7.0 still works and why the old repository name must never be reused |
| Rename notes | `README.md`, `packaging/Read me.txt`, `AUDITING.md`, `CLAUDE.md`, `.github/workflows/release.yml` | The upgrade story for 3.7.0 users, the AUDIT rows for the move and cleanup, the Old name rule, and the draft-notes paragraph |
| Planning history | `.planning/REQUIREMENTS.md`, `.planning/ROADMAP.md`, `.planning/STATE.md`, `.planning/CONCERNS-TRIAGE.md` | Requirements and phase goals that describe the rename, and earlier phases' recorded history (e.g. 3.7.0 backup names) |

Heads-up for the user (not changed, because the allowlist is fixed): `.planning/REQUIREMENTS.md:1` is `# Requirements: PrintInvoicer` and `.planning/ROADMAP.md:1` is `# Roadmap: PrintInvoicer`. Both are inside the allowlisted files, but the headings could be renamed to Billament if wanted.

QuestPDF document metadata: none is set (checked with git grep for DocumentMetadata and WithMetadata in src)

## Deliberate PrintInvoicer hits

Output of `git grep -n -i printinvoicer` with the three phase folders excluded, grouped by reason. Lines are shortened with `...` where long (the file and line number are exact).

**Migration names (DataFolderMigration.cs)**

```text
src/Billament.Core/Data/DataFolderMigration.cs:8:/// The two pairs of folders of the one-time move in 4.0 (the app was called PrintInvoicer before). Passed in, so tests
src/Billament.Core/Data/DataFolderMigration.cs:14:    public const string OldName = "PrintInvoicer";
src/Billament.Core/Data/DataFolderMigration.cs:20:    public const string OldDatabaseName = "printinvoicer.db";
src/Billament.Core/Data/DataFolderMigration.cs:138:                // AUDIT: folder move. Renames %LOCALAPPDATA%\PrintInvoicer to %LOCALAPPDATA%\Billament, only when nothing has the new name.
src/Billament.Core/Data/DataFolderMigration.cs:170:                // AUDIT: folder move. Renames Documents\PrintInvoicer to Documents\Billament, only when nothing has the new name.
src/Billament.Core/Data/DataFolderMigration.cs:333:            // AUDIT: folder move. Puts Documents\Billament back to Documents\PrintInvoicer when the database rename failed.
src/Billament.Core/Data/DataFolderMigration.cs:353:        // AUDIT: file move. Renames printinvoicer.db to billament.db inside Documents\Billament; never over an existing file.
```

**Old backup names (DatabaseBackup.cs, SettingsViewModel.cs, BackupTests.cs)**

```text
src/Billament.Core/Data/DatabaseBackup.cs:11:/// half-written backup behind. Copies are named billament-..., and copies made before 4.0 (printinvoicer-...) are
src/Billament.Core/Data/DatabaseBackup.cs:19:    // Backups made before 4.0, when the app was called PrintInvoicer. They are still listed, restored and pruned.
src/Billament.Core/Data/DatabaseBackup.cs:20:    private const string LegacyPrefix = "printinvoicer-";
src/Billament.Core/Data/DatabaseBackup.cs:49:    // True for billament-*.db and printinvoicer-*.db. GetFiles lists files only, so a folder with such a name is never
src/Billament.Core/Data/DatabaseBackup.cs:59:        // put every printinvoicer-* file ahead of every billament-* file whatever its date.
src/Billament.Core/Data/DatabaseBackup.cs:67:        // AUDIT: file delete. Removes the oldest daily backups (billament-YYYY-MM-DD.db, or printinvoicer-YYYY-MM-DD.db from before 4.0) in the backup folder only.
src/Billament.Core/Data/DatabaseBackup.cs:169:    [GeneratedRegex(@"^(?:billament|printinvoicer)-(?<date>\d{4}-\d{2}-\d{2})\.db$")]
src/Billament.Core/Data/DatabaseBackup.cs:170:    // Daily backups are named billament-YYYY-MM-DD.db (printinvoicer-YYYY-MM-DD.db before 4.0); only these are pruned.
src/Billament.App/ViewModels/SettingsViewModel.cs:96:            Filter = "Billament backups (billament-*.db, printinvoicer-*.db)|billament-*.db;printinvoicer-*.db|All databases (*.db)|*.db",
tests/Billament.Tests/BackupTests.cs:195:        string legacy = Path.Combine(Folder, "printinvoicer-2026-10-20.db");
tests/Billament.Tests/BackupTests.cs:198:        Assert.Equal(["printinvoicer-2026-10-20.db"], DatabaseBackup.List(Folder).Select(f => f.Name));
tests/Billament.Tests/BackupTests.cs:217:                     "notes.db", "billament.db", "printinvoicer.db", "billament-2026-10-30.db",
tests/Billament.Tests/BackupTests.cs:218:                     "printinvoicer-2026-10-29-120000-manual.db", "billament-2026-10-30.db.0123456789abcdef.partial",
tests/Billament.Tests/BackupTests.cs:221:        Directory.CreateDirectory(Path.Combine(Folder, "printinvoicer-2026-10-29.db"));   // a folder, not a backup
tests/Billament.Tests/BackupTests.cs:225:        Assert.Equal(["billament-2026-10-30.db", "printinvoicer-2026-10-29-120000-manual.db"], names);
tests/Billament.Tests/BackupTests.cs:234:        for (int d = 1; d <= 10; d++) names.Add($"printinvoicer-2026-10-{d:00}.db");
tests/Billament.Tests/BackupTests.cs:236:        names.Add("printinvoicer-2026-10-05-120000-manual.db");
tests/Billament.Tests/BackupTests.cs:247:            "printinvoicer-2026-10-05-120000-manual.db", "billament-2026-11-02-090000-pre-rename.db",
tests/Billament.Tests/BackupTests.cs:249:        for (int d = 7; d <= 10; d++) expected.Add($"printinvoicer-2026-10-{d:00}.db");
tests/Billament.Tests/BackupTests.cs:253:        Assert.Contains("printinvoicer-2026-10-05-120000-manual.db", left);
tests/Billament.Tests/BackupTests.cs:262:        File.WriteAllBytes(Path.Combine(Folder, "printinvoicer-2026-11-02.db"), [1, 2, 3]);
```

**Old-install cleanup (publish.ps1)**

```text
publish.ps1:3:# It also removes the old PrintInvoicer install (its Start menu shortcut and program folder), never its data.
publish.ps1:12:if (Get-Process -Name 'Billament', 'PrintInvoicer' -ErrorAction SilentlyContinue) {
publish.ps1:13:    throw 'Billament (or the old PrintInvoicer) is running. Close it and run this again.'
publish.ps1:38:# The old PrintInvoicer install from before 4.0: only its Start menu shortcut and its program folder. Its data
publish.ps1:39:# (Documents\PrintInvoicer and %LOCALAPPDATA%\PrintInvoicer) is moved by Billament itself on its first start and is
publish.ps1:42:$oldLnk = Join-Path ([Environment]::GetFolderPath('Programs')) 'PrintInvoicer.lnk'
publish.ps1:43:# AUDIT: file read. Whether the old shortcut exists and points at PrintInvoicer.exe.
publish.ps1:44:if ((Test-Path -LiteralPath $oldLnk) -and ($shell.CreateShortcut($oldLnk).TargetPath -like '*\PrintInvoicer.exe')) {
publish.ps1:45:    # AUDIT: file delete. The old Start menu shortcut, only when it points at PrintInvoicer.exe.
publish.ps1:51:$oldDir = Join-Path $env:LOCALAPPDATA 'Programs\PrintInvoicer'
publish.ps1:52:# AUDIT: file read. Whether the old program folder holds PrintInvoicer.exe and no data (no .db file, no Backups or Invoices folder).
publish.ps1:53:$oldIsProgramOnly = (Test-Path -LiteralPath (Join-Path $oldDir 'PrintInvoicer.exe')) -and
publish.ps1:59:    # AUDIT: file delete. The old program folder (PrintInvoicer.exe and its files), never a data folder.
publish.ps1:63:Write-Host 'If PrintInvoicer was pinned to the taskbar, unpin it and pin Billament instead.'
```

**Update-check redirect remark (UpdateService.cs)**

```text
src/Billament.App/UpdateService.cs:26:    /// Copies of 3.7.0 and older ask GitHub about cevans3505/PrintInvoicer, the name before 4.0.0. GitHub redirects that
```

**Rename notes and AUDIT rows (README.md, Read me.txt, AUDITING.md, CLAUDE.md, release.yml)**

```text
.github/workflows/release.yml:145:          Upgrading from PrintInvoicer 3.7 or older: close PrintInvoicer, then run Billament.exe. ...
CLAUDE.md:39:- **Old name:** the app was called PrintInvoicer before 4.0.0. The old name stays on purpose only in the one-time data
AUDITING.md:35:`https://api.github.com/repos/cevans3505/PrintInvoicer/releases/latest`; GitHub redirects it to this repository, which
AUDITING.md:59:| `Documents\Billament\Backups\` (or the folder set in Settings) | Daily and manual copies of the database, named `billament-...`; copies made before 4.0 are named `printinvoicer-...` ...
AUDITING.md:65:| `Documents\PrintInvoicer\Backups\billament-<date>-<time>-pre-rename.db` | Once, on the first start of 4.0: a safety copy of the old database ...
AUDITING.md:66:| `Documents\PrintInvoicer` renamed to `Documents\Billament`, and `%LOCALAPPDATA%\PrintInvoicer` renamed to `%LOCALAPPDATA%\Billament` | Once, on the first start of 4.0 ...
AUDITING.md:67:| `printinvoicer.db` renamed to `billament.db` inside `Documents\Billament` | Right after the folder moves ...
AUDITING.md:68:| `settings` rows `BackupFolder` and `InvoiceFolder` in `Documents\Billament\billament.db` | Once, right after the database rename ...
AUDITING.md:73:`Documents\PrintInvoicer` (`DataFolderMigration.UpdateFolderSettings`); nothing else in the restored file is written.
AUDITING.md:75:**The one-time move in 4.0.** The app was called PrintInvoicer before 4.0. On the first start of 4.0 it copies the old
AUDITING.md:131:  removes the old PrintInvoicer install from before 4.0: the Start menu shortcut `PrintInvoicer.lnk`, only when it
AUDITING.md:132:  points at `PrintInvoicer.exe`, and the folder `%LOCALAPPDATA%\Programs\PrintInvoicer`, only when it holds
AUDITING.md:133:  `PrintInvoicer.exe`, no `.db` file and no `Backups` or `Invoices` folder, and is not the new install folder. It never
README.md:7:Billament was called PrintInvoicer before version 4.0.0. If you used PrintInvoicer, see
README.md:8:[Renamed from PrintInvoicer](#renamed-from-printinvoicer).
README.md:69:## Renamed from PrintInvoicer
README.md:71:Version 4.0.0 renamed PrintInvoicer to Billament. Old links to `github.com/cevans3505/PrintInvoicer` lead here.
README.md:72:If you used PrintInvoicer 3.7 or older:
README.md:74:- Close PrintInvoicer, then run `Billament.exe` from the [latest release](...).
README.md:77:- On its first start Billament saves a safety copy of your database, then moves your data from `Documents\PrintInvoicer`
README.md:79:  `%LOCALAPPDATA%\PrintInvoicer` to `%LOCALAPPDATA%\Billament`.
README.md:82:- After the move you can delete `PrintInvoicer.exe` and pin `Billament.exe` to the taskbar instead.
README.md:83:- A backup or invoice folder you set on the Settings tab inside `Documents\PrintInvoicer` is changed to the same place
README.md:85:- Old backups named `printinvoicer-*.db` still show up under **Restore a backup...** ...
README.md:86:  the Settings tab that pointed inside `Documents\PrintInvoicer` are moved to the same place inside `Documents\Billament`.
README.md:87:- If the move cannot finish (for example PrintInvoicer is still open, or a sync app is busy with the folder), Billament
README.md:90:- Do not start `PrintInvoicer.exe` again after the move. It would not find your data and would start an empty one in
README.md:91:  `Documents\PrintInvoicer`.
README.md:104:shortcut and program folder of an old PrintInvoicer install, never its data.
packaging/Read me.txt:16:Coming from PrintInvoicer
packaging/Read me.txt:18:Billament is the new name of PrintInvoicer, from version 4.0. Your jobs and finalized invoices come with you.
packaging/Read me.txt:19:1. Close PrintInvoicer, then run Billament.exe. After that you can delete PrintInvoicer.exe and pin Billament.exe to the
packaging/Read me.txt:21:2. On its first start Billament saves a safety copy of your database, then moves your data from Documents\PrintInvoicer
packaging/Read me.txt:23:   %LOCALAPPDATA%\PrintInvoicer to %LOCALAPPDATA%\Billament.
packaging/Read me.txt:26:4. A backup or invoice folder you set on the Settings tab inside Documents\PrintInvoicer is changed to the same place
packaging/Read me.txt:27:   inside Documents\Billament. Old backups named printinvoicer-<date>.db still show up under Restore a backup, and
packaging/Read me.txt:28:   restoring one is fine: folders on the Settings tab that pointed inside Documents\PrintInvoicer are moved to the same
packaging/Read me.txt:30:5. If the move cannot finish (for example PrintInvoicer is still open, or a sync app is busy with the folder),
packaging/Read me.txt:35:7. Do not start PrintInvoicer.exe again after the move. It would not find your data and would start an empty one in
packaging/Read me.txt:36:   Documents\PrintInvoicer.
packaging/Read me.txt:63:https://github.com/cevans3505/Billament/releases (old links to the PrintInvoicer page lead there too). Close the
```

**Planning history (REQUIREMENTS.md, ROADMAP.md, STATE.md, CONCERNS-TRIAGE.md)**

```text
.planning/REQUIREMENTS.md:1, 20, 23, 25   (title and the NAME-03, NAME-06, NAME-08 texts that describe the rename)
.planning/ROADMAP.md:1, 5, 24, 58, 67, 69, 73, 76, 86   (title, earlier phase goals and plan lines recorded under the old name, the Phase 3 goal and success criteria)
.planning/STATE.md:90   (decision line about printinvoicer-* backups)
.planning/CONCERNS-TRIAGE.md:66   (C09 note about the 3.7.0 backup names)
```

## Decisions Made

See `key-decisions` above. The plan's text was followed as written.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Verify and acceptance commands call `$HOME/.dotnet/dotnet`**
- **Found during:** Tasks 1 and 3
- **Issue:** The plan's commands use `$HOME/.dotnet/dotnet`; in this container `dotnet` is on PATH (`/usr/local/bin/dotnet`, 10.0.403) and there is no `$HOME/.dotnet`.
- **Fix:** Ran the same commands with `dotnet`.
- **Files modified:** none
- **Verification:** the same output patterns passed (525 passed; build 0 warnings, 0 errors).
- **Committed in:** n/a

**2. [Rule 1 - Bug] README publish.ps1 sentence first wrapped across two lines**
- **Found during:** Task 2
- **Issue:** The acceptance grep needs `program folder of an old PrintInvoicer install, never its data` on one line; my first edit split it.
- **Fix:** Re-wrapped before the commit.
- **Files modified:** `README.md`
- **Committed in:** `95fc3ab` (never committed in the wrong form)

---

**Total deviations:** 2 auto-fixed (1 environment path, 1 wrap fix before commit)
**Impact on plan:** none.

Not a deviation but worth knowing: the "Deliberate hits" blocks shorten some very long lines with `...`; the exact lines are in the repository at the file and line numbers shown. Five planning-history hits are given as line-number lists rather than copied.

## Issues Encountered

- `pwsh` is not installed here, so `publish.ps1` could not be parsed or run. It was checked as text (all verify gates, a careful read of the multi-line `-and` expression with leading `-not` on continuation lines, which Windows PowerShell 5.1 accepts). The real run is plan 03-05 Task 1.

## Deferred to Windows (plan 03-05 Task 1)

- Run `publish.ps1` in Windows PowerShell 5.1: the two-name `Get-Process -Name 'Billament', 'PrintInvoicer' -ErrorAction SilentlyContinue` (RESEARCH A5: names with no process must be ignored), the multi-line `$oldIsProgramOnly` expression, `$shell.CreateShortcut($oldLnk).TargetPath -like '*\PrintInvoicer.exe'`, removal of a real old shortcut and a real old program folder, and that `Documents\PrintInvoicer` and `%LOCALAPPDATA%\PrintInvoicer` are untouched.
- A script quirk to check there: `Get-ChildItem -LiteralPath $oldDir -Recurse -Force -Filter '*.db'` is not silenced on purpose; if the old folder cannot be listed the script stops (after the new install is already in place).

## Known Stubs

None.

## Threat Flags

None. The only new outside access lines are the four tagged lines in `publish.ps1` (two reads, two deletes), described in AUDITING.md and covered by T-03-17 in the plan.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 03-05 (Windows checks, including the real `publish.ps1` run). Notes: the final search in Task 3 is to be run again unchanged as the first task of 03-06; two headings in planning docs still say PrintInvoicer (`.planning/REQUIREMENTS.md:1`, `.planning/ROADMAP.md:1`), allowed by the allowlist but worth renaming if the user wishes.

## Self-Check: PASSED

- Files exist: `src/Billament.App/UpdateService.cs`, `.github/workflows/release.yml`, `packaging/Read me.txt`, `README.md`, `publish.ps1`, `AUDITING.md`, `CLAUDE.md`.
- Commits `f981e11` and `95fc3ab` are ancestors of HEAD; measured `commits: 2` from ledger base `a25079d`.
- Verification re-run: Task 1, 2 and 3 verify gates and acceptance greps passed; 525 unit tests passed, 0 failed; `Billament.sln` builds with 0 warnings, 0 errors; version 4.0.0; 36 AUDIT tags under `src` and "There are 36 tags" in AUDITING.md; exactly 4 `# AUDIT:` tags in `publish.ps1`; no em or en dashes outside `.planning`.

---
*Phase: 03-rename-to-billament*
*Completed: 2026-11-02*
