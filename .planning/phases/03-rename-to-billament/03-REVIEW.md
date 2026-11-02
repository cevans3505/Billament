---
phase: 03-rename-to-billament
reviewed: 2026-11-02T00:00:00Z
depth: standard
files_reviewed: 16
files_reviewed_list:
  - src/Billament.Core/Data/DataFolderMigration.cs
  - src/Billament.Core/Data/DatabaseBackup.cs
  - src/Billament.App/App.xaml.cs
  - src/Billament.App/ViewModels/SettingsViewModel.cs
  - src/Billament.App/UpdateService.cs
  - tests/Billament.Tests/DataFolderMigrationTests.cs
  - tests/Billament.Tests/BackupTests.cs
  - publish.ps1
  - .github/workflows/release.yml
  - .github/workflows/ci.yml
  - src/Billament.App/Billament.App.csproj
  - Billament.sln
  - AUDITING.md
  - README.md
  - packaging/Read me.txt
  - CLAUDE.md
findings:
  critical: 0
  warning: 5
  info: 5
  total: 10
status: issues_found
---

# Phase 3: Code Review Report

**Reviewed:** 2026-11-02
**Depth:** standard
**Files Reviewed:** 16
**Status:** issues_found

## Summary

The migration is carefully built. It never deletes, merges or overwrites anything, and it never touches an old folder
whose new name is taken (links and dangling links count as taken). The safety copy runs first and is not matched by the
prune regex. The Documents move is undone if the database rename fails. The two-key settings rewrite matches on a folder
boundary, normalizes `..` away, and opens the file for writing only when a value changes. Exception filters are narrow
and match what each call can throw. AUDIT tags: 36 in `src`, which matches AUDITING.md. Spot checks of the mechanical
renames in `src` and `tests` found no change to pricing, `PricingRules`, snapshots or SQL. I found no em dashes and no
real names in the reviewed files.

I found no data-loss BLOCKER. The five warnings are edge cases in the move (leftover SQLite journal files, a misleading
startup message, an unvalidated relative path, a skipped settings rewrite) and one unsafe delete in `publish.ps1`.

## Warnings

### WR-01: SQLite sidecar files are not part of the database rename, and a hot journal blocks start-up with a misleading message

**File:** `src/Billament.Core/Data/DataFolderMigration.cs:117,354`
**Issue:** `RenameDatabase` renames only `printinvoicer.db`. A database left by a crash mid-write has a hot rollback
journal, `printinvoicer.db-journal` (or `-wal` / `-shm` if the journal mode was ever changed). SQLite finds that file by
the database's name, so after the rename the journal would be orphaned and the half-written transaction would never be
rolled back. Today this is hidden by accident: the safety copy opens the old database `ReadOnly`, and a read-only
connection cannot roll back a hot journal, so SQLite should raise `SqliteException`. `Run` then stops with "could not
make a safety copy ... close any other program that may be using that folder", which is wrong. Nothing else is using the
folder, and the stop would repeat on every start until the user runs 3.7 once to let it recover. I did not reproduce this
on Windows. The repair path (`repair`, line 95) also ignores sidecars.
**Fix:** Decide the sidecar policy and write it down. The simplest option is to refuse to migrate while any
`printinvoicer.db-journal`, `-wal` or `-shm` exists. Return a specific message such as "open PrintInvoicer once, close
it normally, then start Billament", and add a test with a fake `-journal` file. The other option is to rename the
sidecars together with the main file, in `RenameDatabase` and in its undo.

### WR-02: publish.ps1 deletes the old install folder recursively with no containment or link guard

**File:** `publish.ps1:50-62`
**Issue:** The guard compares only `Resolve-Path $oldDir` with `Resolve-Path $InstallDir` for equality. If `-InstallDir`
is a subfolder of `Programs\PrintInvoicer`, for example a custom `...\Programs\PrintInvoicer\app`, the script copies the
new build there and then runs `Remove-Item -Recurse -Force` on its parent, which deletes the fresh install. The same
happens if `-InstallDir` names the same folder through a junction or a subst drive, because `Resolve-Path` does not
resolve those. Separately, `$oldDir` itself may be a junction or symlink. `Get-ChildItem -Recurse` follows it, and in
Windows PowerShell 5.1 `Remove-Item -Recurse` on a link can delete the target's contents. The `.db`, `Backups` and
`Invoices` checks do not protect other user files, such as PDFs in a custom folder. This is the only recursive delete in
the phase. It runs unattended after a successful install.
**Fix:**
```powershell
$item = Get-Item -LiteralPath $oldDir -Force
$installFull = [IO.Path]::GetFullPath($InstallDir).TrimEnd('\')
$oldFull = [IO.Path]::GetFullPath($oldDir).TrimEnd('\')
$notNested = ($installFull -ne $oldFull) -and -not $installFull.StartsWith($oldFull + '\', [StringComparison]::OrdinalIgnoreCase)
$notLink = -not ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)
# add $notNested and $notLink to the $oldIsProgramOnly condition
```
Better still, delete only `PrintInvoicer.exe` and then remove the folder with a non-recursive `Remove-Item` that
succeeds only when it is empty.

### WR-03: The folder-settings rewrite is skipped when the folder moved but the database file was not renamed

**File:** `src/Billament.Core/Data/DataFolderMigration.cs:182-203,207-208`
**Issue:** `UpdateFolderSettings` runs only when `renamed` is true. If the Documents folder moved but no
`printinvoicer.db` was renamed, the code skips the rewrite. Two cases do this. The old folder held both `printinvoicer.db`
and `billament.db` (the notice branch, which is covered by `Two_database_files_are_both_kept`). The old folder held only
`billament.db`. The app then opens `billament.db` in the new folder. If its `BackupFolder` or `InvoiceFolder` points
inside `Documents\PrintInvoicer`, it keeps pointing there. The next daily backup (`Directory.CreateDirectory` in
`DatabaseBackup.Copy`) silently recreates `Documents\PrintInvoicer\Backups`, which splits the user's backups and PDFs
across two folders. This breaks D-13 in that state, and no test covers it.
**Fix:** Run the rewrite whenever the Documents folder moved in this run and `movedNewDb` exists, not only when
`renamed` is true. For example, set a `bool dbReady = FileExists(movedNewDb)` after the move block and use it in place of
`renamed`. Add a test that mirrors `Two_database_files_are_both_kept` with a custom `BackupFolder`.

### WR-04: The start-up failure message can say "nothing was changed" when the move already changed things

**File:** `src/Billament.App/App.xaml.cs:59-74`
**Issue:** `MoveOldData` runs inside the broad `catch (Exception)`, whose text is "Billament could not start, and
nothing was changed." The local folder and the Documents folder may already have been renamed when an unexpected
exception escapes. Examples are an exception from `Decide` or `Occupied` (`FileInfo.LinkTarget` is outside every `try`),
or a non-filtered exception from `UpdateFolderSettings`. The text is also wrong when two copies of Billament start at
the same moment. The second `Directory.Move` then throws `DirectoryNotFoundException`, which is an `IOException`, so the
user is told "Nothing was lost: your data is still in {old folder}" while it is really in the new one. That message points
at the wrong place.
**Fix:** Split the catch. Wrap `MoveOldData()` in its own `try` with text that names both folders ("your data is in
{new folder}, or still in {old folder} if it has not moved yet"). Do not say "nothing was changed" there. In `Run`, after
a failed `Directory.Move`, re-check `FolderExists(folders.OldDocuments)`. If the old folder is gone and the new one
exists, treat it as already moved, or tell the user where the data really is.

### WR-05: Empty special folders turn the migration into a working-directory-relative rename

**File:** `src/Billament.Core/Data/DataFolderMigration.cs:26-28`, `src/Billament.App/App.xaml.cs:84-87`
**Issue:** `Environment.GetFolderPath` returns `""` when a special folder cannot be resolved, for example some service
or redirected-profile setups. `DataFolders.Under("", "")` then gives the relative paths `PrintInvoicer` and
`Billament`. `Decide` and `Directory.Move` resolve those against the current directory. If Billament is started from a
folder that has a subfolder named `PrintInvoicer` (such as an unzipped download), that folder is silently renamed
to `Billament`, and the safety copy writes `PrintInvoicer\Backups`. This violates the rule that the move acts only on
the two known data folders. `RemapFolder` already guards against relative input for the same reason; the folder roots
get no such check.
**Fix:** In `DataFolders.Under` (or at the top of `Run`), throw `InvalidOperationException` if any root is empty or
`!Path.IsPathRooted`. `App.OnStartup` already reports an unexpected exception. Add a test with `Under("", "")`.

## Info

### IN-01: A saved error file can permanently block the local-folder move

**File:** `src/Billament.App/App.xaml.cs:90-92`, `src/Billament.Core/Data/DataFolderMigration.cs:125`
**Issue:** When the safety copy fails, `local` is `NotTried`, and the old local folder is still movable.
`ErrorReport.Save` then writes `%LOCALAPPDATA%\Billament\Errors`, which creates the new local folder. On the next start
the local pair is `NewFolderExists`, so the window layout and update-check state never move. This matches D-23 ("start
fresh"), but it contradicts `FolderMove.NotTried` meaning "try again later", and AUDITING.md says the pair is decided on
its own. The same happens after a failed local move.
**Fix:** Either document it in the `FolderMove.NotTried` summary, or save the error file under the old local folder
until the move has happened.

### IN-02: AUDIT tags are inconsistent at call sites and for the new legacy-name existence check

**File:** `src/Billament.Core/Data/DatabaseBackup.cs:29`, `src/Billament.App/App.xaml.cs:87`,
`src/Billament.App/ViewModels/SettingsViewModel.cs:122`, `src/Billament.Core/Data/DataFolderMigration.cs:154,187,212`
**Issue:** The project rule asks for an `// AUDIT:` comment on every line that reads, writes or deletes files. The helpers
are tagged, but the lines that start the access are not. These are `DataFolderMigration.Run(...)`,
`UpdateFolderSettings(...)` and `RenameDatabase(...)`, and the new `File.Exists(...LegacyPrefix...)` read in
`DailyIfDue` (the original `File.Exists(path)` was untagged too). `BackUpTodayAsync` and `RestoreBackup` do tag their
calls, so the style is mixed. Adding tags would change the "36 tags" count in AUDITING.md.
**Fix:** Tag the call sites, or state in AUDITING.md that a tag on the lowest-level method covers its callers. Update the
count if tags are added.

### IN-03: The daily-backup regex uses `\d`, which matches non-ASCII digits, and it decides which files are deleted

**File:** `src/Billament.Core/Data/DatabaseBackup.cs:169`
**Issue:** In .NET, `\d` matches any Unicode decimal digit unless `RegexOptions.ECMAScript` is used. A file such as
`billament-<Arabic-Indic digits>.db` in the backup folder counts as a daily backup and can be pruned. It is very
unlikely, but this regex gates `File.Delete`.
**Fix:** `@"^(?:billament|printinvoicer)-(?<date>[0-9]{4}-[0-9]{2}-[0-9]{2})\.db$"`.

### IN-04: Safety copies pile up when the move keeps failing

**File:** `src/Billament.Core/Data/DataFolderMigration.cs:112-118`
**Issue:** Every start that gets past the safety copy and then fails the folder move (a sync app holding the folder, for
example) writes another full `billament-<timestamp>-pre-rename.db` into the old `Backups` folder. These are never
pruned, so a user who retries many times fills the disk with identical copies.
**Fix:** Skip the safety copy when a `*-pre-rename.db` file already exists in the old `Backups` folder and its size and
newest-write time are not older than `printinvoicer.db`, or reuse the newest one.

### IN-05: Wording and naming leftovers

**File:** `README.md:109`, `tests/Billament.Tests/BackupTests.cs:9`
**Issue:** README line 109 says "keeps its own data in their `Documents\Billament`"; "their" does not agree with
"its". The temp folder prefix `pi-backup-` still carries the old initials.
**Fix:** "keeps its data in `Documents\Billament`". Rename the prefix to `bm-backup-`.
