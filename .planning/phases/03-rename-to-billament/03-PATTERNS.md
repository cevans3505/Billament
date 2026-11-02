# Phase 3: Rename to Billament - Pattern Map

**Mapped:** 2026-11-02
**Files analyzed:** 8 (6 new or heavily changed, plus the mechanical rename as one group and AUDITING.md)
**Analogs found:** 8 / 8

All analog paths below use the pre-rename names (`PrintInvoicer.*`) because the rename has not run yet. After the
rename step they live under `Billament.*`. All are git-tracked (checked with `git ls-files` for the key ones).

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Billament.Core/Data/DataFolderMigration.cs` (new; `DataFolders`, `FolderMove`, `DataMoveResult`, `FileOps` records in same file) | service (static, folders and time passed in) | file-I/O, batch | `src/PrintInvoicer.Core/Data/DatabaseBackup.cs` | exact |
| `src/Billament.Core/Data/DatabaseBackup.cs` (modify: prefix, legacy prefix, Prune order, rename `LooksLike...`) | service | file-I/O | itself | exact |
| `tests/Billament.Tests/DataFolderMigrationTests.cs` (new) | test | file-I/O | `tests/PrintInvoicer.Tests/BackupTests.cs` + `LegacyDatabaseTests.cs` | exact |
| `tests/Billament.Tests/BackupTests.cs` (modify: legacy name cases) | test | file-I/O | itself | exact |
| `src/Billament.App/App.xaml.cs` (`OnStartup` wiring) | entry point | request-response (startup) | itself, lines 41-72 | exact |
| `publish.ps1` (old shortcut and old install folder cleanup) | config / script | file-I/O | itself, lines 22-41 | role-match |
| `AUDITING.md` (new rows) | doc | n/a | itself, tables at lines 24-60 | exact |
| Mechanical rename (about 99 files) | all | n/a | see the reference-kinds table at the end | n/a |

## Pattern Assignments

### `src/Billament.Core/Data/DataFolderMigration.cs` (service, file-I/O)

**Analog:** `src/PrintInvoicer.Core/Data/DatabaseBackup.cs`

**Header and class doc** (lines 1-16): usings sorted System first, then packages; file-scoped namespace; a summary that
says purpose in plain English.
```csharp
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace PrintInvoicer.Core.Data;

/// <summary>Copies of the database. ... written to a temporary name first so a dropped network share never leaves a
/// half-written backup behind.</summary>
public static partial class DatabaseBackup
{
    public const int KeepDaily = 14;
```

**Folders and time passed in, never read from Environment** (lines 22-37). The migration `Run(DataFolders f, DateTime now, FileOps? ops = null)` follows the same shape:
```csharp
public static string? DailyIfDue(string dbPath, string folder, DateTime now, int keep = KeepDaily)
{
    string path = Path.Combine(folder, $"{Prefix}{now:yyyy-MM-dd}.db");
    if (File.Exists(path)) return null;
    Copy(dbPath, path);
    ...
}
public static string Manual(string dbPath, string folder, DateTime now, string label = "manual")
{
    string path = Path.Combine(folder, $"{Prefix}{now:yyyy-MM-dd-HHmmss}-{label}.db");
    Copy(dbPath, path);
    return path;
}
```
Use `DatabaseBackup.Manual(oldDb, Path.Combine(oldDocs, "Backups"), now, "pre-rename")` for D-07; do not write a new copy routine.

**.partial write then rename, narrow catch, cleanup of own temp file only** (lines 113-138):
```csharp
string temp = $"{target}.{Guid.NewGuid():N}.partial";
try
{
    using (var src = Open(dbPath, readOnly: true))
    using (var dst = Open(temp, readOnly: false))
        src.BackupDatabase(dst);
    // AUDIT: file write. Puts a finished backup in place in the backup folder.
    File.Move(temp, target, overwrite: true);
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
{
    try
    {
        // AUDIT: file delete. Removes this copy's own half-written .partial file after a failure; never any other file.
        File.Delete(temp);
    }
    catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
    {
        // The copy already failed; that error is the one to report.
    }
    throw;
}
```
Copy this for the database file rename step (use `File.Move(a, b, overwrite: false)`, never overwrite). The migration must not delete anything.

**Unpooled connection for the raw two-key settings rewrite** (lines 140-152):
```csharp
// Unpooled, so the backup files are released as soon as the copy is done and can be moved or deleted.
private static SqliteConnection Open(string path, bool readOnly)
{
    // AUDIT: file read/write. Opens the database or a backup file for copying.
    var c = new SqliteConnection(new SqliteConnectionStringBuilder
    {
        DataSource = path,
        Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
        Pooling = false,
    }.ToString());
    c.Open();
    return c;
}
```
`Open` is private, so either make a small `internal`/own copy in the migration class with `ReadWrite` mode (not `ReadWriteCreate`, so a missing file never gets created), or reuse it by making it `internal`. `DatabaseBackup.LooksLikePrintInvoicer(path)` (lines 78-92, renamed `LooksLikeBillament`) is the content check to confirm the old database and the backup copy.

**Parameter style for the SQL** (from RESEARCH, matches `Database.Exec`): `"$k"`, `"$v"` named parameters, `nameof(AppSettings.BackupFolder)` for keys. Only `UPDATE settings SET value = $v WHERE key = $k`; never `SaveSettings`.

**Error handling:** inside Core, catch only `IOException or UnauthorizedAccessException or SqliteException` (add `ArgumentException or NotSupportedException` only around `Path.GetFullPath` in `RemapFolder`). Return a `DataMoveResult` with `Failed` instead of throwing; no UI, no `ErrorReport` in Core (Core has no WPF).

**Critical ordering rule:** write the legacy names (`"PrintInvoicer"`, `"printinvoicer.db"`) AFTER the bulk sed, so the sed does not turn the migration into a no-op. These are deliberate grep hits (D-19).

---

### `tests/Billament.Tests/DataFolderMigrationTests.cs` (test, file-I/O)

**Analogs:** `tests/PrintInvoicer.Tests/BackupTests.cs` (temp folder, Dispose) and `LegacyDatabaseTests.cs` (fixture database with finalized jobs).

**Temp-folder fixture and cleanup** (`BackupTests.cs` lines 1-26):
```csharp
using Microsoft.Data.Sqlite;
using PrintInvoicer.Core.Data;
using PrintInvoicer.Core.Models;

namespace PrintInvoicer.Tests;

public sealed class BackupTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"pi-backup-{Guid.NewGuid():N}");
    private string DbPath => Path.Combine(_dir, "printinvoicer.db");

    public BackupTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_dir, recursive: true);
    }
```
For the migration use one root temp dir with `docs` and `local` parents, build `DataFolders.Under(docs, local)`, and use a prefix such as `bm-move-{Guid:N}` (the new name; not `pi-`).

**Fixed dates, test names as sentences** (`BackupTests.cs` lines 28-42, 59 on): `var day = new DateTime(2026, 10, 30, 8, 0, 0);`, methods like `One_daily_backup_a_day`, `Keeps_the_newest_daily_backups_and_every_manual_one`. Use names such as `Moves_both_folders_and_renames_the_database`, `Skips_a_pair_when_the_new_folder_exists`, `Failed_move_leaves_old_data_and_creates_no_new_folder`.

**Failure seam instead of chmod** (container runs as root, so permission bits do not fail): `new FileOps(MoveDirectory: (_, _) => throw new IOException("in use"))`.

**Byte-for-byte totals test (D-09):** build the database from `Fixtures/pre-snapshot.sql` as `LegacyDatabaseTests.CreateFixtureDatabase` does (lines 25-38):
```csharp
using var c = new SqliteConnection($"Data Source={_path};Pooling=False");
c.Open();
using var script = c.CreateCommand();
script.CommandText = File.ReadAllText(TestFiles.Fixture("pre-snapshot.sql"));
script.ExecuteNonQuery();
```
Then open it once with `new Database(...)` (so jobs get frozen with `finalized_total` and `price_snapshot`), read both columns for every finalized job with raw SQL, run the migration, read them again from `billament.db`, and `Assert.Equal` the strings. Also test: settings outside the old folder untouched, `...\PrintInvoicerOld\x` not remapped, case rule with both `StringComparison` values, repair rule (old-named db inside the new folder, no `billament.db`).

Sample names only ("Harbor Dive Shop", paths like `C:\Users\Sample\Documents\PrintInvoicer\Invoices 2026` as strings for `RemapFolder`).

---

### `src/Billament.Core/Data/DatabaseBackup.cs` (legacy prefix handling)

**Analog:** itself. Current code to change (lines 16, 40-54, 154-156):
```csharp
private const string Prefix = "printinvoicer-";
...
? new DirectoryInfo(folder).GetFiles(Prefix + "*.db").OrderByDescending(f => f.LastWriteTimeUtc).ToList()
...
.OrderByDescending(f => f.Name, StringComparer.Ordinal)   // the date in the name, not the file time
...
[GeneratedRegex(@"^printinvoicer-\d{4}-\d{2}-\d{2}\.db$")]
private static partial Regex DailyName();
```
Target: `Prefix = "billament-"`, add `private const string LegacyPrefix = "printinvoicer-";`, `List` returns both prefixes, regex `^(billament|printinvoicer)-(\d{4}-\d{2}-\d{2})\.db$`, and `Prune` must order by the captured date group then name (ordinal name order would rank all `printinvoicer-*` above all `billament-*` and delete the newest backups). Keep the `// AUDIT:` comment above `f.Delete()` and update its text to name both patterns. Existing `BackupTests` get bulk-renamed to `billament-*`; add hand-written legacy cases.

---

### `src/Billament.App/App.xaml.cs` (`OnStartup` wiring)

**Analog:** itself, lines 41-72.

**Error handling to copy** (lines 48-71):
```csharp
DispatcherUnhandledException += (_, args) =>
{
    ErrorReport.Show("PrintInvoicer ran into an unexpected error. ...", args.Exception);
    args.Handled = true;
};
...
try
{
    Db = new Database(Database.DefaultPath);
    var backup = BackUpTodayAsync(Db);
    var main = new MainWindow();
    main.Show();
    backup.ContinueWith(_ => main.Dispatcher.BeginInvoke(() => main.ViewModel.Settings.RefreshBackupStatus()));
}
catch (Exception ex)
{
    ErrorReport.Show($"PrintInvoicer could not start. Its data is in {Database.DefaultPath}; nothing was changed.", ex);
    Shutdown(1);
}
```
Wiring: inside the same `try`, before `new Database(...)`, resolve the real folders with `Environment.GetFolderPath(MyDocuments)` and `LocalApplicationData`, call `DataFolderMigration.Run(DataFolders.Under(docs, local), DateTime.Now)`. If `result.DocumentsFailed`: `ErrorReport.Show(<plain message naming the OLD folder, saying nothing was lost>, result.Error)` then `Shutdown(1)` and `return` before any `new Database`, so no empty new folder is created. Do not reuse the generic catch message (it names `Database.DefaultPath`, which will be the new, possibly missing, folder). A non-fatal `Notice` goes to `ErrorReport.Save` only. The only AUDIT-worthy lines are in Core; the two `GetFolderPath` calls read no file.

---

### `publish.ps1` (cleanup block)

**Analog:** itself, lines 4-41.
```powershell
param([string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\PrintInvoicer'))
...
if (Get-Process PrintInvoicer -ErrorAction SilentlyContinue) {
    throw 'PrintInvoicer is running. Close it and run this again.'
}
...
$lnk = Join-Path ([Environment]::GetFolderPath('Programs')) 'PrintInvoicer.lnk'
```
Plan: default `InstallDir` becomes `Programs\Billament`; the running check tests both `Billament` and `PrintInvoicer`; after the new install add a block that removes `PrintInvoicer.lnk` and the folder `Programs\PrintInvoicer` only when present (`Test-Path` then `Remove-Item -Recurse -Force`), with a comment that data folders are never touched. Keep `$ErrorActionPreference = 'Stop'` and the plain `Write-Host` style. No analog for a deletion block exists in this script except `Remove-Item (Join-Path $out '*') -Recurse -Force` (line 19); scope the new one to the exact old paths. These old names are deliberate grep hits.

---

### `AUDITING.md` (new rows)

**Analog:** itself. Row format is a three-column or two-column Markdown table (lines 24-60); the data-files table is the right place:
```markdown
| Location | What | Where in the code |
| `Documents\PrintInvoicer\Backups\` (or the folder set in Settings) | Daily and manual copies ... | `DatabaseBackup` |
| `%LOCALAPPDATA%\PrintInvoicer\Errors\` | Details of unexpected errors ... | `ErrorReport.Save` |
```
Add rows for: folder existence checks; the `pre-rename` backup (into old `Backups`); `Directory.Move` of the two folders (Documents and local); `File.Move` rename of `printinvoicer.db` (and journal siblings) to `billament.db`; the raw settings rewrite (two keys only); the rollback `Directory.Move`. Also: change "There are 27 tags" (line 19) to the new count, add `Directory.Move` to the search hints, and change every other `PrintInvoicer` path in the table to the new name.

---

## Shared Patterns

### AUDIT comment
**Source:** `DatabaseBackup.cs` lines 52, 68, 122, 129, 143. Form: `// AUDIT: <kind of access>. <what and where, in a short sentence>.` on the line directly above the access.
**Apply to:** every folder check, backup, move, rename and settings write in `DataFolderMigration`.

### Narrow catch filters
**Source:** `DatabaseBackup.cs` line 125 and `App.xaml.cs` line 35.
```csharp
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
```
**Apply to:** all of the migration. Broad `catch (Exception)` only stays in `App.OnStartup`.

### Startup failure
**Source:** `App.xaml.cs` lines 67-71: `ErrorReport.Show(plain sentence, ex); Shutdown(1);`. Plain sentence first, details in the saved error file.

### Time and folders as parameters
**Source:** `DatabaseBackup.DailyIfDue(path, folder, now)`. No `DateTime.Now` or `Environment.GetFolderPath` in Core.

### Tests
**Source:** `BackupTests.cs` and `LegacyDatabaseTests.cs`: `IDisposable`, GUID temp dir, `SqliteConnection.ClearAllPools()` in `Dispose`, fixed dates, sentence-style test names, no personal data.

## Mechanical Rename: Reference Kinds

| Kind | Where | Becomes | Catch |
|------|-------|---------|-------|
| `git mv` paths | `.sln`, 4 project folders, 4 `.csproj`, `Assets/PrintInvoicer.ico` | `Billament.*` | clean untracked `bin/obj` first or old folders stay on disk |
| `namespace` / `using PrintInvoicer.*` | 163 lines in `.cs` | `Billament.*` | plus fully qualified uses in `ScreenTests.cs` lines 160 and 232 |
| String that mimics a namespace | `ReaderContract.cs` line 81 `StartsWith("PrintInvoicer.Core.Slicing.")` | `Billament.Core.Slicing.` | fails at runtime, not compile time |
| XAML | `x:Class`, `clr-namespace`, `Title`, tooltip, pack URI `/PrintInvoicer;component/Assets/PrintInvoicer.ico` in `MainWindow.xaml`, `JobEditorWindow.xaml`, `App.xaml` | Billament | pack URI must match `<AssemblyName>`; runtime only |
| csproj | `AssemblyName`, `RootNamespace`, `Product`, `ApplicationIcon`, `Resource`, `ProjectReference`, `Compile Include`, `<Version>` | Billament, `4.0.0` | UiTests links `..\PrintInvoicer.Tests\TestFiles.cs` |
| `InternalsVisibleTo` | `AssemblyInfo.cs` line 13 | `Billament.UiTests` | only one in repo |
| User-visible strings | `App.xaml.cs`, `ErrorReport.cs`, `UpdateService.cs` (`Repository`, User-Agent, `BarText`, state path), `MainViewModel.cs` line 417, `SettingsViewModel.cs` (filter, restart text), `InvoicePdf.cs` (`DefaultFolder`, `PreviewFolder`), `WindowLayout.cs`, `JobEditorWindow.xaml.cs` | Billament | `Database.DefaultPath` becomes `billament.db`; no QuestPDF metadata is set |
| In-memory key | `BambuFileReader.cs` `TooLargeKey` | `"Billament.TooLarge"` | never persisted |
| Test data | `BackupTests`, `SampleShop.cs`, `WpfHost.TempFolder`, `ScreenTests`, `ReadmeImageTests` (`PRINTINVOICER_README_IMAGES`), fixtures comment | Billament | env var in script and test must change together |
| Scripts and CI | `publish.ps1`, `make-readme-images.ps1`, `tools/Make-Icon.ps1`, `ci.yml`, `release.yml` (paths, zip, `.sha256`, heading regex `Billament <major>.<minor>`) | Billament | release heading check is a warning only |
| Docs | `README.md`, `AUDITING.md`, `CLAUDE.md`, `.claude/CLAUDE.md`, `packaging/Read me.txt` (shorten the `=` underline) | Billament | add the 4.0 data move note; leave `.planning/phases/01-*`, `02-*` alone |

Order: run the bulk sed and `git mv` first, then write the migration with its legacy names, so the sed cannot rewrite them. Gate with `dotnet build Billament.sln -p:EnableWindowsTargeting=true -c Release` and `dotnet test tests/Billament.Tests` (485 pass at baseline).

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| Folder rename logic (`Directory.Move`, rollback) | service | file-I/O | No directory move exists in the codebase; `DatabaseBackup` only moves single files. Use RESEARCH "Pattern 1" order and `FileOps` seam. |
| `RemapFolder` path containment | utility | transform | No path-prefix logic exists; use the RESEARCH normalize-and-boundary rules. |
| Test double for failing file ops | test | file-I/O | Existing tests use no delegate seam for file ops; follow the project style of delegates over interfaces (see `UpdateService` ctor `Func<...>? fetch = null`). |

## Metadata

**Analog search scope:** `src/PrintInvoicer.Core/Data`, `src/PrintInvoicer.App/App.xaml.cs`, `tests/PrintInvoicer.Tests`, `publish.ps1`, `AUDITING.md`
**Files scanned:** 8 read in full or in part, plus CONTEXT.md and RESEARCH.md (lines 1-409)
**Pattern extraction date:** 2026-11-02
