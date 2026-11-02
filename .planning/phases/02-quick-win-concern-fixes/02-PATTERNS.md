# Phase 2: Quick-Win Concern Fixes - Pattern Map

**Mapped:** 2026-11-02
**Files analyzed:** 17 (6 new source/test, 3 new test classes, 1 fixture, 7 modified)
**Analogs found:** 15 / 17 (YAML and docs edits have no code analog; the file itself is the pattern)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/PrintInvoicer.Core/Files/SafeFileName.cs` (new) | utility | transform | `src/PrintInvoicer.Core/Updates/ReleaseVersion.cs` | role-match (static pure helper, returns null/fallback, never throws) |
| `src/PrintInvoicer.Core/Files/ShellCommands.cs` (new) | utility | transform | `src/PrintInvoicer.Core/Updates/ReleaseVersion.cs` | role-match |
| `src/PrintInvoicer.Core/Updates/LatestReleaseReply.cs` (new) | utility | streaming (bounded read) + transform | `UpdateService.LatestRelease` + `FetchLatestAsync` (App) | role-match |
| `src/PrintInvoicer.Core/Data/DatabaseBackup.cs` (mod, `Copy`) | service | file-I/O | itself (lines 109-122) | exact |
| `src/PrintInvoicer.App/UpdateService.cs` (mod, `FetchLatestAsync`) | service | request-response | itself (lines 161-171) | exact |
| `src/PrintInvoicer.App/Pdf/InvoicePdf.cs` (mod, `Write`, `Safe`) | service | file-I/O | itself (lines 37-57, 647-651) | exact |
| `src/PrintInvoicer.App/ViewModels/SettingsViewModel.cs` (mod, `OpenFolder`) | view-model | event-driven | itself (lines 181-190) | exact |
| `src/PrintInvoicer.App/ErrorReport.cs` (mod) | utility | event-driven | itself (lines 52-66) | exact |
| `tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs` (new) | test | CRUD / file-I/O | `DatabaseTests.Older_database_gets_printed_column` + `PriceSnapshotTests.Older_finalized_invoices_are_frozen_on_upgrade` | exact |
| `tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql` (new) | fixture | batch | `CREATE TABLE` strings in `Database.EnsureCreated` (lines 81-122) | role-match |
| `tests/PrintInvoicer.Tests/BackupTests.cs` (mod) | test | file-I/O | itself | exact |
| `tests/PrintInvoicer.Tests/SafeFileNameTests.cs` (new) | test | transform | `tests/PrintInvoicer.Tests/ReleaseVersionTests.cs` | role-match |
| `tests/PrintInvoicer.Tests/ShellCommandsTests.cs` (new) | test | transform | `ReleaseVersionTests.cs` | role-match |
| `tests/PrintInvoicer.Tests/LatestReleaseReplyTests.cs` (new) | test | streaming | `tests/PrintInvoicer.Tests/BambuRobustnessTests.cs` (hostile input) | partial |
| `.github/workflows/release.yml` (mod) | config | batch | itself (zip / upload / `gh release create` steps) | exact |
| `README.md`, `AUDITING.md` (mod) | docs | n/a | existing sections | exact |
| `.planning/CONCERNS-TRIAGE.md` (mod) | docs | n/a | existing rows | exact |

## Pattern Assignments

### `src/PrintInvoicer.Core/Data/DatabaseBackup.cs` `Copy` (FIX-04)

**Analog:** itself, lines 109-122 (current code to be changed):
```csharp
// Copies the database with SQLite's backup API (consistent even while the app is using it) to a temporary name,
// then renames it into place, so an interrupted copy never leaves a half-written backup.
private static void Copy(string dbPath, string target)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
    string temp = target + ".partial";
    if (File.Exists(temp)) File.Delete(temp);          // untagged blind delete: remove
    using (var src = Open(dbPath, readOnly: true))
    using (var dst = Open(temp, readOnly: false))
        src.BackupDatabase(dst);
    // AUDIT: file write. Puts a finished backup in place in the backup folder.
    File.Move(temp, target, overwrite: true);
}
```
**Change to:** unique temp `$"{target}.{Guid.NewGuid():N}.partial"`, try/catch with filter `IOException or UnauthorizedAccessException or SqliteException`, delete only own temp under a new `// AUDIT: file delete.` comment, rethrow. Full snippet is in RESEARCH.md FIX-04. Unpooled `Open(path, readOnly)` helper (lines 126-136) stays as is. `SqliteException` is already imported (`catch (SqliteException)` at line 104). Update AUDITING.md tag count (26 to 27) and the Backups row.

---

### `src/PrintInvoicer.App/UpdateService.cs` `FetchLatestAsync` (FIX-05)

**Analog:** itself, lines 161-171. Keep the `// AUDIT: network.` comment directly above the `GetAsync` line.
```csharp
// AUDIT: network. The app's only network request: ...
using var response = await Http.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest", ct);
if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
response.EnsureSuccessStatusCode();
var release = await JsonSerializer.DeserializeAsync<LatestRelease>(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
return ReleaseVersion.Parse(release?.TagName) is Version v ? new ReleaseInfo(v, release!.HtmlUrl ?? ReleasesPage) : null;
```
**Change to:** add `HttpCompletionOption.ResponseHeadersRead`, return `null` when `Content.Headers.ContentLength > LatestReleaseReply.MaxBytes`, then `await LatestReleaseReply.ReadAsync(stream, ct)`; `null` means "no update info" (same path as the 404 branch).

**DTO to move into Core** (lines 143-148):
```csharp
private sealed class LatestRelease
{
    [JsonPropertyName("tag_name")] public string? TagName { get; set; }
    [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
}
```
**Error handling** to keep (lines 130-135): `catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException)`. Only `JsonException` may escape the new Core helper.

---

### `src/PrintInvoicer.Core/Updates/LatestReleaseReply.cs` (new, utility, bounded stream read)

**Analog (style/namespace/doc):** `src/PrintInvoicer.Core/Updates/ReleaseVersion.cs` lines 1-12.
```csharp
namespace PrintInvoicer.Core.Updates;

/// <summary>Version numbers from release tags, and when the startup update check is due.</summary>
public static class ReleaseVersion
{
    /// <summary>How often the startup check asks GitHub; launches in between reuse the last answer.</summary>
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
```
Use a file-scoped namespace, `/// <summary>` on the class and members, PascalCase `public const int MaxBytes`. Body: RESEARCH.md FIX-05 snippet (read chunks, return null when `buffer.Length + n > maxBytes`, then `JsonSerializer.Deserialize`). No network or file access in Core, so no `AUDIT:` tag.

---

### `src/PrintInvoicer.Core/Files/ShellCommands.cs` (new, utility)

**Analog:** `ReleaseVersion.cs` (static pure helper, namespace `PrintInvoicer.Core.Files`). Body from RESEARCH.md FIX-03 (`ShowFolder`, `ShowFile` with `ArgumentList`; never calls `Process.Start`, so no AUDIT tag).

**Call sites to rewire (keep their existing AUDIT comments):**

`src/PrintInvoicer.App/ViewModels/SettingsViewModel.cs` 181-190:
```csharp
// AUDIT: opens a program. Shows a folder in Explorer, only when the user clicks an Open button.
private static void OpenFolder(string path)
{
    try
    {
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }
```
becomes `Process.Start(ShellCommands.ShowFolder(path));` (add `using PrintInvoicer.Core.Files;`).

`src/PrintInvoicer.App/ErrorReport.cs` 57-62:
```csharp
// AUDIT: opens a program. Only if the user clicks Yes: ...
Process.Start(new ProcessStartInfo(IssuesPage) { UseShellExecute = true });
if (path is not null) Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
```
Second line becomes `Process.Start(ShellCommands.ShowFile(path))`. Catch filter `System.ComponentModel.Win32Exception or InvalidOperationException` stays. The five other `Process.Start` sites already pass the path as `FileName`; do not touch them.

---

### `src/PrintInvoicer.Core/Files/SafeFileName.cs` (new) and `InvoicePdf.Write` (FIX-06)

**Analog for call site:** `src/PrintInvoicer.App/Pdf/InvoicePdf.cs` lines 37-51 and 647-651.
```csharp
string number = job.InvoiceNumber ?? $"draft {job.CreatedDate:yyyy-MM-dd}";
string name = kind switch
{
    InvoiceKind.Preview => $"Preview {number} {Safe(job.Title)}.pdf",
    InvoiceKind.Internal => $"Invoice {number} {Safe(job.Title)} (detail).pdf",
    _ => $"Invoice {number} {Safe(job.Title)}.pdf",
};
string path = Path.Combine(folder, name);
...
// A job title made safe to use in a file name.
private static string Safe(string s)
{
    var invalid = Path.GetInvalidFileNameChars();   // OS dependent: only NUL and / on Linux
    return new string(s.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
}
```
Remove `Safe`; call `SafeFileName.Compose(prefix, number, title, suffix)` for all three kinds; keep the `// AUDIT: file write` on `GeneratePdf(path)`. Do NOT sanitize `folder` (user-chosen full path). Use a fixed forbidden-char list, not `Path.GetInvalidFileNameChars()`. Style analog for the static helper: `ReleaseVersion.Parse` (accepts `string?`, returns a fallback instead of throwing).

---

### `tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs` (new, FIX-02)

**Analog 1 (raw old-schema SQL then `new Database`):** `tests/PrintInvoicer.Tests/DatabaseTests.cs` 190-226:
```csharp
using (var c = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_path}"))
{
    c.Open();
    using var cmd = c.CreateCommand();
    cmd.CommandText = """
        CREATE TABLE jobs (id INTEGER PRIMARY KEY AUTOINCREMENT, ... notes TEXT NOT NULL);
        INSERT INTO jobs(title, customer, invoice_number, created_date, modeling_hours, post_processing, paid, notes)
        VALUES ('old', 'Harbor Dive Shop', '2026-001', '2026-10-28', '0', 0, 0, '');
        ...
        """;
    cmd.ExecuteNonQuery();
}
var db = new Database(_path);
var job = Assert.Single(db.GetJobs());
```
In the new test, read `Fixtures/pre-snapshot.sql` via `TestFiles.Fixture("pre-snapshot.sql")` (`TestFiles.cs` line 11) and `File.ReadAllText` into `cmd.CommandText`.

**Analog 2 (freeze on upgrade + reopen):** `PriceSnapshotTests.cs` 167-182:
```csharp
var reopened = new Database(_path);
var saved = reopened.GetJobs().Single();
Assert.NotNull(saved.PriceSnapshot);
Assert.Equal(before, saved.FinalizedTotal);
ChangeEveryPrice(reopened);
Assert.Equal(before, Total(reopened, reopened.GetJobs().Single()));
```
Extend with: precondition `pragma_table_info('jobs')` check, several finalized jobs, one draft, literal hand-derived totals, snapshot text compared with `Assert.Equal` after price changes and two more `new Database(_path)` calls. Test class pattern: `IDisposable`, `Dispose` does `SqliteConnection.ClearAllPools(); File.Delete(_path)`. Fixture rules: at least one filament row (else 13 seed rows), no `NextInvoiceNumber` setting, fixed `yyyy-MM-dd` dates, sample names only.

**Fixture analog:** `Database.EnsureCreated` CREATE TABLE statements, lines 81-122 (jobs at 87-93 given in RESEARCH.md). Copy them minus the `AddColumnIfMissing` columns (`excluded_extra_costs`, `discount_percent`, `price_snapshot`, `finalized_total`). Tracked-source check: `tests/PrintInvoicer.Tests/Fixtures/` is tracked; csproj line 26 `<None Include="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />` already copies it.

---

### `tests/PrintInvoicer.Tests/BackupTests.cs` (mod, FIX-04)

**Analog:** itself, lines 1-43. Add facts next to `One_daily_backup_a_day`:
```csharp
private readonly string _dir = Path.Combine(Path.GetTempPath(), $"pi-backup-{Guid.NewGuid():N}");
private string DbPath => Path.Combine(_dir, "printinvoicer.db");
private string Folder => Path.Combine(_dir, "Backups");
...
DbWithJob("Caps");
var day = new DateTime(2026, 10, 30, 8, 0, 0);
var first = DatabaseBackup.DailyIfDue(DbPath, Folder, day);
Assert.Empty(Directory.GetFiles(Folder, "*.partial"));
var copy = new Database(first!);
Assert.Equal("Caps", copy.GetJobs().Single().Title);
```
New tests: `DatabaseBackup.Manual(DbPath, Folder, same_now, "label")` called twice in sequence (both succeed, no `*.partial`, result opens); `Parallel.Invoke` of one daily and one manual (different targets, repeat about 20 times). Never write the same target from several threads (Windows flake).

---

### `tests/PrintInvoicer.Tests/SafeFileNameTests.cs`, `ShellCommandsTests.cs`, `LatestReleaseReplyTests.cs` (new)

**Analog:** `tests/PrintInvoicer.Tests/ReleaseVersionTests.cs` (pure-helper `[Theory]`/`InlineData`, null for invalid; not read in full, naming only per CLAUDE.md) and `BambuRobustnessTests.cs` for hostile-input style. Conventions: `namespace PrintInvoicer.Tests;`, underscore sentence names (`Hostile_invoice_number_cannot_leave_the_folder`), sample names only (`C:\Users\Sample\Harbor Dive Shop "draft"\notes.txt`). For FIX-05 use an endless custom `Stream` and a `MemoryStream` at exactly cap and cap+1.

---

### `.github/workflows/release.yml` (mod, FIX-01)

**Analog:** itself. Insert the checksum step after "Zip PrintInvoicer.exe and Read me.txt" in the `build` job (default shell pwsh), extend `upload-artifact` `path:` to both files (keep `if-no-files-found: error`), and attach `"$zip.sha256"` in `gh release create $tag $zip ... --draft`, echoing the hash line into `$notes`. Snippet in RESEARCH.md FIX-01 (LF line ending via `[IO.File]::WriteAllText`). Verify with PyYAML parse.

---

## Shared Patterns

### AUDIT comments
**Source:** `DatabaseBackup.cs` lines 119-121 and `UpdateService.cs` lines 120-121, 163-164.
**Apply to:** every new line that deletes or writes a file or starts a program (`Copy` delete). Style: `// AUDIT: <kind>. <one sentence of why>.` directly above the line; add or reword the matching `AUDITING.md` row and update "There are N tags" to the `git grep -c "AUDIT:" -- src` total (26 now).

### Narrow catch filters
**Source:** `UpdateService.cs` line 131 and `ErrorReport.cs` line 63.
```csharp
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException)
```
**Apply to:** the new `Copy` catch (`IOException or UnauthorizedAccessException or SqliteException`) and any wiring edits. No broad `catch (Exception)`.

### Core helper conventions
**Source:** `ReleaseVersion.cs`. File-scoped namespace matching the folder (`PrintInvoicer.Core.Files`, `PrintInvoicer.Core.Updates`), `/// <summary>` on each public type and member, no WPF or network use in Core, pure functions that take everything as parameters.

### Test cleanup
**Source:** `BackupTests.cs` lines 15-19: `SqliteConnection.ClearAllPools();` before deleting temp files.

### Writing style
No em or en dashes, plain English, sample names only. Run `grep -n $'\xe2\x80\x94\|\xe2\x80\x93'` on every edited file.

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `.github/workflows/release.yml` checksum step | config | batch | No hashing step exists; use RESEARCH.md snippet |
| README "Verify a download" and QuestPDF license text; AUDITING.md rows | docs | n/a | Wording comes from RESEARCH.md FIX-01 and FIX-07 |
| `.planning/CONCERNS-TRIAGE.md` re-score notes | docs | n/a | Use the "Re-score findings" table in RESEARCH.md (C27 and C09 are partly handled) |

## Metadata

**Analog search scope:** `src/PrintInvoicer.Core/{Data,Updates}`, `src/PrintInvoicer.App/{UpdateService,ErrorReport}.cs`, `ViewModels/SettingsViewModel.cs`, `Pdf/InvoicePdf.cs`, `tests/PrintInvoicer.Tests/{BackupTests,DatabaseTests,PriceSnapshotTests,TestFiles}.cs`
**Files scanned:** about 12 (targeted line ranges; RESEARCH.md read lines 1-519 of 616, the remainder is validation sampling detail)
**All analog paths are git-tracked source** (none under a `.gsd` capability mirror).
**Pattern extraction date:** 2026-11-02
