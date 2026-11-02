# Phase 3: Rename to Billament - Research

**Researched:** 2026-11-02
**Domain:** Whole-repository rename (C#/WPF/.NET 10), one-time user data folder migration (SQLite + folders on Windows), GitHub repository rename and update-check continuity, release/install scripts
**Confidence:** HIGH for the repo inventory, build commands and migration design; MEDIUM for GitHub redirect specifics (the exact `Location` form could not be tested from this container)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

All decisions below were given by the user before discussion and are locked.

#### Name
- **D-01:** The new name is **Billament** (bill plus filament), one word, capital B, no space. It replaces "PrintInvoicer" in every place a user or developer sees it. Reversibility: one-way (the repository name, data folder names and exe name are public and land on users' PCs; renaming again would need a second migration).
- **D-02:** Names of things built from it:
  - exe and assembly: `Billament.exe` (`<AssemblyName>Billament</AssemblyName>`, `<Product>Billament</Product>`)
  - solution: `Billament.sln`
  - projects, folders and root namespaces: `Billament.Core`, `Billament.App`, `Billament.Tests`, `Billament.UiTests` (`src/Billament.Core/Billament.Core.csproj` and so on), every `namespace PrintInvoicer.*` and `using PrintInvoicer.*` becomes `Billament.*`, XAML `x:Class` and `clr-namespace` values follow
  - `InternalsVisibleTo("Billament.UiTests")` in `src/Billament.App/AssemblyInfo.cs`
  - release zip: `Billament-<version>.zip` and `Billament-<version>.zip.sha256`
  - database file: `billament.db` (lowercase, like today's `printinvoicer.db`)
- **D-03:** Use `git mv` for file and folder renames so history follows the files.

#### Placement and version
- **D-04:** This is Phase 3, the next phase, before Data Safety Fixes (now Phase 4) and Slice File Submissions (now Phase 5), so later phases are written under the new name from the start.
- **D-05:** Ships as **4.0.0**: `<Version>4.0.0</Version>` in `src/Billament.App/Billament.App.csproj`, and the heading of `packaging/Read me.txt` becomes `Billament 4.0`. The release workflow's heading check looks for `Billament <major>.<minor>`. Reversibility: one-way (a published 4.0.0 tag and release cannot be taken back without confusing the update check of everyone who saw it).

#### Existing data (one-time move)
- **D-06:** On first start, if the old folders exist and the new ones do not, move them **once**: `Documents\PrintInvoicer` to `Documents\Billament`, and `%LOCALAPPDATA%\PrintInvoicer` to `%LOCALAPPDATA%\Billament`. The database file inside is renamed from `printinvoicer.db` to `billament.db`. The move runs **before** `new Database(...)` opens anything, because the `Database` constructor writes (`EnsureCreated`, `FreezeFinalizedJobs`). Reversibility: one-way (after the move, 3.7.0 no longer finds the data where it looks; going back means moving folders by hand).
- **D-07:** Make a **backup first**, before anything is moved. The backup must survive a failed move. (Where it goes and its name are Claude's discretion, see below; it must not be pruned as a daily backup.)
- **D-08:** **Never touch the old folders when the new ones already exist.** If `Documents\Billament` exists, the Documents move is skipped entirely, even if `Documents\PrintInvoicer` also exists. The same rule applies on its own to the `%LOCALAPPDATA%` pair. Nothing is merged, overwritten or deleted.
- **D-09:** **Safety first.** Finalized invoices never change and no data is lost. A test shows that every finalized job's `finalized_total` and `price_snapshot` are byte-for-byte the same after the move as before.
- **D-10:** **Failures** show a plain message (what happened, where the data is, that nothing was lost) and leave the old data in place. A half-done move must not leave the user with two partial copies and no clear answer about which is real. Technical details go to the error file through `ErrorReport`, not into the message.
- **D-11:** **Every new file and folder access** (folder checks, the backup, the move, the rename, any settings rewrite) gets an `// AUDIT:` comment directly above it and a row in `AUDITING.md`.
- **D-12:** **The move logic lives in Core** (no WPF, no `Environment.GetFolderPath` inside the logic: the folders are passed in as parameters, the way `DatabaseBackup.DailyIfDue` takes its folder and time) and is covered by unit tests in `tests/Billament.Tests` that run on Linux. `App.OnStartup` only resolves the real folders and calls it.
- **D-13:** **Custom folders inside the old folder.** If the user's `BackupFolder` or `InvoiceFolder` setting points inside `Documents\PrintInvoicer` (for example `...\Documents\PrintInvoicer\Invoices 2026`), it is rewritten to the same place under `Documents\Billament` after the move, so backups and invoices keep working. Settings that point anywhere else are left exactly as they are. Only these two text settings may change; no job, part, plate, snapshot or total is written by the move.

#### GitHub repository
- **D-14:** The repository is renamed from `cevans3505/PrintInvoicer` to `cevans3505/Billament`. **The user renames it on GitHub by hand when the phase ships**; this is a manual step in the end-of-phase checklist, not something a plan or agent does. Reversibility: costly (GitHub keeps redirects from the old name only until a new repository takes the old name; links in 3.7.0 depend on them).
- **D-15:** `UpdateService.Repository` becomes `cevans3505/Billament`. Its allow-check (release page URL must start with `https://github.com/{Repository}/`), the error report's issue page (`ErrorReport.IssuesPage`), the User-Agent product name, the README, `packaging/Read me.txt` and all docs links point at the new name.
- **D-16:** **3.7.0 users must still find the update** through GitHub's redirect. Research must verify how `https://api.github.com/repos/cevans3505/PrintInvoicer/releases/latest` behaves after a rename (status code, `Location` header, whether .NET `HttpClient` follows it by default), and what 3.7.0's allow-check does with a `html_url` under the new name (expected: it falls back to `ReleasesPage` under the old name, which GitHub's web redirect sends on). If 3.7.0 would not find the update, the plan must say what to do about it.

#### Scope that plans must reach
- **D-17:** Every reference is in scope:
  - `src`, `tests`, the `.sln`, `.csproj` file and folder names, namespaces, `AssemblyInfo` `InternalsVisibleTo`
  - window titles and UI text, message box captions, PDF text and file names (for example the `"PrintInvoicer, {n} invoices"` combined PDF name), the preview temp folder, the UI test temp folder
  - pack URIs to the icon (`pack://application:,,,/PrintInvoicer;component/Assets/PrintInvoicer.ico`)
  - `publish.ps1`: install folder `%LOCALAPPDATA%\Programs\Billament`, Start menu shortcut `Billament.lnk`, the running-process check for both names, and **removal of the old `PrintInvoicer.lnk` shortcut and the old `%LOCALAPPDATA%\Programs\PrintInvoicer` install folder** (the program files only, never the data folders)
  - `make-readme-images.ps1` (including the `PRINTINVOICER_README_IMAGES` variable), `tools/Make-Icon.ps1`
  - `.github/workflows/ci.yml` and `release.yml` (paths, zip and checksum names, read-me heading check, release title and notes)
  - README, AUDITING.md, CLAUDE.md, `.claude/CLAUDE.md`, `packaging/Read me.txt` (plus a short note that 4.0 moves the data from `Documents\PrintInvoicer` to `Documents\Billament` on first start)
  - the README pictures, regenerated on Windows if the window title or UI text in them shows the name
  - the error-report folder, window-layout file and update-check state file (all under `%LOCALAPPDATA%\Billament`)
  - QuestPDF document metadata, if the code sets any
  - `.planning/codebase/*.md` only where the old name would mislead a future agent (paths, project names, commands)
- **D-18:** Leave historical `.planning` phase files (`phases/01-*`, `phases/02-*`) alone.
- **D-19:** **Final grep.** A case-insensitive search for `PrintInvoicer` across the repository (outside historical `.planning` phase files) finds only deliberate hits: the migration code's old folder and file names (and their tests and AUDIT rows), history (git, changelog or release notes), and any note that explains the rename. Each remaining hit is listed in the phase verification with its reason.

#### Where work runs
- **D-20:** The user prefers cloud work. Plans are marked **[cloud]** when they can be done and checked on Linux (Core code and tests, renames, docs, workflows, scripts edited as text) or **[windows]** when they need Windows (UI tests, the README pictures, running `publish.ps1`, a real upgrade test from a 3.7.0 data folder). App code is compile-checked in the cloud only as far as the Linux SDK allows; the Windows checks run on the user's PC afterwards.
- **D-21:** The phase ends with a **manual checklist** for the user: rename the repository on GitHub; run the upgrade test on Windows with a copy of a real 3.7.0 data folder; run the UI tests and `make-readme-images.ps1`; then do the 4.0.0 release (push `main`, wait for CI, push tag `v4.0.0`, check the draft's zip and `.sha256`, write notes, publish), and confirm a 3.7.0 install shows the update bar.

### Claude's Discretion
- Where the pre-move backup goes and its exact name (for example a `pre-rename` manual backup made with `DatabaseBackup.Manual` into the old Backups folder so it moves along with the data, or a copy left beside the old folder). It must not be pruned and the README / Read me should say where it is.
- How the move is done (`Directory.Move` when on the same volume, otherwise a checked copy then removal, or refuse and leave the data in place) and how a half-done move is detected and reported, within D-08 to D-10.
- The backup file prefix after the rename (`billament-`), and how `DatabaseBackup.List`, `Prune` and the restore file picker keep handling backups named `printinvoicer-*.db` made before the rename (NAME-04).
- The name of `DatabaseBackup.LooksLikePrintInvoicer` (for example `LooksLikeBillament`) and other identifiers that contain the old name; the `BambuFileReader.TooLargeKey` string value.
- Whether the old `%TEMP%\PrintInvoicer previews` folder is ignored or cleaned (it is disposable either way).
- The order of plans and waves, as long as the code renames land before the docs that describe them and the final grep is last.

### Deferred Ideas (OUT OF SCOPE)
- A new icon design for the new name: not requested; the icon file is renamed only.
- Code signing the 4.0.0 exe: out of scope for the milestone (see REQUIREMENTS.md Out of Scope).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| NAME-01 | Code base named Billament: sln, projects, folders, namespaces, `InternalsVisibleTo`, exe, icon file | Section "Rename Inventory" (every file/folder to `git mv`, 163 namespace/using lines, XAML, csproj, pack URI, traps); Linux compile command verified |
| NAME-02 | Everything the user sees says Billament, incl. PDF text, file names, metadata, temp folders | Inventory of user-visible strings; QuestPDF metadata is not set anywhere (nothing to rename); combined print name in `MainViewModel` line 417 |
| NAME-03 | One-time backup then move of both folders, new-exists rule, plain failure message, finalized totals unchanged, Core + Linux tests, AUDIT comments and rows | "Data Move Design" (API shape, order, failure handling, test list); Windows `Directory.Move` semantics |
| NAME-04 | Custom folder settings inside old folder follow the move; old daily backups still listed, restored, pruned | "Settings Rewrite" and "Backup Prefix Handling" (including the Prune sort trap) |
| NAME-05 | Update check, allow-check, issue page, docs point at `cevans3505/Billament`; 3.7.0 still finds 4.0.0 | "GitHub Rename and the Update Check" |
| NAME-06 | `publish.ps1`, `make-readme-images.ps1`, `Make-Icon.ps1`, `ci.yml`, `release.yml` use new name; old shortcut and install folder removed | Inventory plus "publish.ps1 Design" |
| NAME-07 | Docs use new name, Read me heading and move note, `.planning/codebase` where misleading | Inventory of docs hits and a rename note recommendation |
| NAME-08 | 4.0.0 and case-insensitive grep finds only deliberate hits | Version locations, grep commands, expected deliberate-hit list |
</phase_requirements>

## Summary

The rename itself is mechanical but wide: 99 tracked files and about 809 occurrences of the old name outside the historical phase files, including 163 `namespace`/`using` lines, 4 project/solution names, 70-odd file and folder paths for `git mv`, and a handful of string traps that a compiler will not catch (the `ReaderContract` namespace `StartsWith` check, the pack URI, the release workflow heading regex, the `printinvoicer-*.db` restore filter, uppercase `PRINTINVOICER_README_IMAGES`). The whole solution, including the WPF app and the UI test project, compiles on this Linux container with `-p:EnableWindowsTargeting=true`, and the 485 unit tests pass at baseline, so every code plan can have a cloud compile gate. [VERIFIED: ran the commands this session, see Environment Availability]

The risky part is the data move. Both folder pairs are siblings under the same parent (`Documents` and `%LOCALAPPDATA%`), so each move is a single same-volume directory rename: atomic on NTFS, no copy fallback needed. It fails (IOException or UnauthorizedAccessException) when any file inside is open elsewhere, which is the safe outcome (a running 3.7.0 copy, OneDrive sync). Recommended design: a public static Core class `DataFolderMigration.Run(DataFolders, DateTime now, FileOps?)` that (1) checks which pairs need moving, (2) makes a `pre-rename` manual backup inside the old Backups folder, (3) moves the local pair then the Documents pair, (4) renames the database inside the new folder (with rollback and a next-start repair rule), (5) rewrites only the `BackupFolder` and `InvoiceFolder` settings rows with raw SQL, using a normalized, directory-boundary path comparison. No marker file is needed: "new folder exists" is the once-only check, which is also exactly D-08. On a failed Documents move the app must show the plain message and exit without creating any new folder (otherwise the next start would see the new folder and strand the old data).

3.7.0 users will very probably see the 4.0.0 update bar: GitHub documents that API requests to a renamed repository get a 301 (or 307) redirect, and .NET's default handler follows redirects on the same host while keeping the User-Agent and Accept headers. After the redirect the reply's `html_url` is under `cevans3505/Billament`, which 3.7.0's allow-check rejects, so the Download button falls back to `https://github.com/cevans3505/PrintInvoicer/releases`, which GitHub's web redirect sends to the new repository. The only thing to do is order and verify: rename the repository before publishing 4.0.0, never create a new repository named `PrintInvoicer`, and check with `curl -i` plus 3.7.0's "Check now" button. Confidence is MEDIUM because the exact `Location` header form could not be fetched from this container (GitHub access is blocked for arbitrary repos).

**Primary recommendation:** Do the mechanical rename first as one scripted `git mv` + `sed` step gated by a Linux solution build and the 485 unit tests, then add the Core migration (with its legacy names written after the sed), then the App wiring, scripts, docs, and the final case-insensitive grep last; finish with the [windows] checklist.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Decide whether and how to move data folders, back up, rename db, rewrite two settings | Core (`Billament.Core.Data`) | none | D-12: logic in Core, folders and time passed in, tests on Linux |
| Resolve real `Documents` and `%LOCALAPPDATA%` folders, call migration, show message, exit | App (`App.OnStartup`) | none | Needs `Environment.GetFolderPath` and WPF dialogs; D-12 keeps these out of Core |
| Backup file naming, listing, pruning, legacy prefix support | Core (`DatabaseBackup`) | App (`SettingsViewModel` file picker filter) | Naming rules live in Core; the picker filter string is UI |
| Update check URL, allow-check, User-Agent | App (`UpdateService`) | Core (`LatestReleaseReply`) | Network is App-only (Core has no network code) |
| Install and remove old shortcut / old install folder | Build script (`publish.ps1`) | none | Developer-PC tool, never ships |
| Release zip, checksum, heading check | CI (`release.yml`) | none | Runs on GitHub, Windows runner |
| Repository rename and redirect | GitHub (manual, user) | none | D-14: a human step |

## Standard Stack

No new packages. The phase uses what is already referenced.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.Data.Sqlite | 10.0.14 | Raw SQL for the two-key settings rewrite and backup copies (already in Core) | Already the only persistence layer [VERIFIED: src/PrintInvoicer.Core/PrintInvoicer.Core.csproj `<PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.14" />`] |
| xunit | 2.9.3 | Linux unit tests for the migration | Existing test framework [VERIFIED: tests/PrintInvoicer.Tests/PrintInvoicer.Tests.csproj] |
| .NET SDK | 10.0.403 | Build and test | Pinned in global.json [VERIFIED: `{ "sdk": { "version": "10.0.403", "rollForward": "latestPatch" } }`] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Raw SQL update of two keys before `Database` opens | `LoadSettings` + `SaveSettings` after open | `SaveSettings` writes every setting row (`foreach (var p in SettingProps)` upsert), which violates D-13 "only these two text settings may change"; `LoadSettings` also throws on a bad value. Use raw SQL. |
| Directory rename (`Directory.Move`) | Copy then delete | Not needed: both pairs are siblings under one parent, so no volume change. Copy-then-delete would add a partial-copy failure mode that D-10 forbids. |
| A marker file to run once | State check ("new folder exists") | The state check is already required by D-08 and cannot get out of sync. No marker, so no extra AUDIT row. |

**Installation:** none.

## Package Legitimacy Audit

No external packages are added or changed by this phase, so the legitimacy seam was not run. All referenced packages are already in the three `.csproj` files and unchanged (CommunityToolkit.Mvvm 8.4.2, QuestPDF 2026.9.1, Microsoft.Data.Sqlite 10.0.14, xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4).

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

## Architecture Patterns

### System Architecture Diagram

```
App start (App.OnStartup)
   |
   | resolve Documents + LocalAppData (App only), build DataFolders.Under(docs, local)
   v
DataFolderMigration.Run(folders, now)  [Core, no WPF]
   |
   +--> Documents pair: old exists AND new NOT occupied?  --no--> NothingToMove / NewFolderExists (touch nothing)
   |        |yes
   |        v
   |   old\printinvoicer.db exists? --yes--> Manual backup into old\Backups (label pre-rename)
   |        |                                   fail --> Result.Failed (nothing moved)  --> App: plain message, exit, no new folder
   |        v
   +--> Local pair: old exists AND new not occupied? --yes--> Directory.Move  (failure: Result.Failed for Local, non-fatal, details to error file)
   |        v
   +--> Documents move: Directory.Move(oldDocs, newDocs)
   |        fail (IOException / UnauthorizedAccess: file in use, OneDrive, permissions) --> Result.Failed, old intact
   |        v
   |   rename newDocs\printinvoicer.db -> billament.db (+ -journal/-wal/-shm siblings)
   |        fail --> roll back Directory.Move(newDocs, oldDocs); rollback fails --> Failed + message names newDocs
   |        v
   |   raw SQL on billament.db: remap BackupFolder / InvoiceFolder only if inside old folder
   |        fail --> non-fatal Notice (data is already safe)
   v
Result returned
   |
   v
App: Failed? show plain message (ErrorReport.Save for details), Shutdown(1)
     else: new Database(Database.DefaultPath)  -> BackUpTodayAsync -> MainWindow
```

### Recommended Project Structure (after rename)
```
Billament.sln
src/Billament.Core/        # + Data/DataFolderMigration.cs (new), Data/DatabaseBackup.cs (prefix changes)
src/Billament.App/         # Assets/Billament.ico, AssemblyInfo.cs (InternalsVisibleTo Billament.UiTests)
tests/Billament.Tests/     # + DataFolderMigrationTests.cs (new), BackupTests.cs (legacy prefix cases)
tests/Billament.UiTests/
```

### Pattern 1: Core migration API shape (testable on Linux)

**What:** A static class with folders and time passed in, a result record, and a delegate seam for the two destructive-looking operations so Linux tests can force a failure (root in this container ignores permission bits, so chmod cannot simulate failure).

**Example (illustrative skeleton; the plan writes the real one, with AUDIT comments directly above each access line, narrow `catch ... when` filters, plain English messages, no em dashes):**
```csharp
namespace Billament.Core.Data;

/// <summary>The four folders of the one-time 4.0 move. Passed in so tests use temp folders.</summary>
public sealed record DataFolders(string OldDocuments, string NewDocuments, string OldLocal, string NewLocal)
{
    public const string OldName = "PrintInvoicer", NewName = "Billament";
    /// <summary>The usual layout: both folders are siblings under the given parents (no Environment calls here).</summary>
    public static DataFolders Under(string documents, string localAppData) => new(
        Path.Combine(documents, OldName), Path.Combine(documents, NewName),
        Path.Combine(localAppData, OldName), Path.Combine(localAppData, NewName));
}

public enum FolderMove { NothingToMove, NewFolderExists, Moved, Failed }

public sealed record DataMoveResult(FolderMove Documents, FolderMove Local, string? BackupPath,
                                    string? Problem, string? Notice, Exception? Error)
{
    public bool DocumentsFailed => Documents == FolderMove.Failed;
}

/// <summary>Test seam: the two file operations that can fail halfway.</summary>
public sealed record FileOps(Action<string, string>? MoveDirectory = null, Action<string, string>? MoveFile = null);

public static class DataFolderMigration
{
    public static DataMoveResult Run(DataFolders f, DateTime now, FileOps? ops = null, StringComparison? paths = null) { /* see order below */ }
    public static string? RemapFolder(string? value, string oldFolder, string newFolder, StringComparison comparison) { /* see rules below */ }
}
```

**Order inside `Run` (recommended, satisfies D-06 to D-11):**
1. `docsNeedsMove = Directory.Exists(OldDocuments) && !Occupied(NewDocuments)`; `localNeedsMove` likewise. `Occupied(p) = Path.Exists(p) || new DirectoryInfo(p).LinkTarget is not null` so a dangling symlink or junction, or a plain file, at the new name counts as "exists" and the pair is skipped (D-08).
2. If `docsNeedsMove` and `OldDocuments\printinvoicer.db` exists: `DatabaseBackup.Manual(oldDb, Path.Combine(OldDocuments, "Backups"), now, "pre-rename")`, then check the copy with `LooksLikeBillament`. Any `IOException`, `UnauthorizedAccessException`, `SqliteException` gives `Failed` with nothing moved. This satisfies D-07 literally (backup before anything moves) and the file survives a failed move because it sits in the old folder. Its name `billament-<yyyy-MM-dd-HHmmss>-pre-rename.db` never matches the daily-name regex, so `Prune` never deletes it.
3. Call `SqliteConnection.ClearAllPools()` before moving (belt and braces; `DatabaseBackup.Open` is already unpooled).
4. Move the **local** pair first (disposable state: `Errors\`, `window-layout.json`, `update-check.json`). Reason: if the Documents move later fails, `ErrorReport.Save` writes to `%LOCALAPPDATA%\Billament\Errors`; if the local pair were not moved yet, that write would create the new local folder and strand the old one (D-08 then skips it forever). A local failure is non-fatal: keep the old files, continue, put details in `Error` and a short `Notice`.
5. Move the **Documents** pair with `Directory.Move`. Failure: result `Failed`, old folder untouched, no new folder.
6. Rename `NewDocuments\printinvoicer.db` to `billament.db` with `File.Move(a, b, overwrite: false)`; do the same for `-journal`, `-wal`, `-shm` siblings if present (a crashed 3.7.0 can leave a hot journal; the default journal mode is rollback because the code sets no `journal_mode` pragma [VERIFIED: grep for journal/wal/PRAGMA in src found only `pragma_table_info` and `integrity_check`]). If `billament.db` already exists inside the moved folder, do not overwrite: report and leave both. If the rename throws, roll back with `Directory.Move(NewDocuments, OldDocuments)`; if that also throws, return `Failed` with a message naming the new folder.
7. Rewrite settings with raw SQL (rules below). A failure here is a `Notice`, not a failure: the data is already moved and backed up.
8. **Repair rule (replaces a marker):** if `NewDocuments` exists, contains `printinvoicer.db` and does not contain `billament.db`, finish step 6 (it only touches the new folder, so D-08 is respected). This covers a rename-then-rollback double failure; otherwise the next start would open an empty `billament.db` and look like data loss.
9. Never delete anything. `Directory.Move` is the only operation that removes a name, and it is a rename.

**Why no copy fallback:** `Documents\PrintInvoicer` and `Documents\Billament` share a parent, so there is no volume change; the same holds under `%LOCALAPPDATA%`. If `Documents\PrintInvoicer` is a junction or symlink to another drive, `Directory.Move` renames the link, not the target. [ASSUMED for the junction case; see A3]

**Windows semantics of `Directory.Move` (relevant to the failure cases the task listed):**
- Throws `IOException` for "a different volume", "destDirName already exists" (on all platforms since .NET Core 3.0) and "The directory or a file within it is being used by another process"; `UnauthorizedAccessException` for permission problems. [CITED: https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.move]
- A running 3.7.0 copy keeps SQLite pooled connections, so `printinvoicer.db` stays open and the rename fails with an `IOException`. This is the desired safe outcome; the message must tell the user to close other copies. [ASSUMED from Microsoft.Data.Sqlite pooling behavior and NTFS sharing rules; see A4]
- OneDrive Known Folder Move: `Environment.GetFolderPath(MyDocuments)` returns the redirected path, so both old and new are siblings inside the synced tree. A sync client holding a handle inside the folder can make the rename fail with a sharing violation; the user retries after sync pauses. Do not try to work around it. [ASSUMED; see A4]
- Network-share Documents: rename inside a share is a normal rename; an offline share gives `IOException`/`DirectoryNotFoundException`. Treat all as `Failed`.
- Trailing spaces are trimmed from path arguments by `Directory.Move`. [CITED: same Microsoft page]

### Pattern 2: Settings rewrite (NAME-04 first half)

**How settings are stored:** one row per public read-write property of `AppSettings` in table `settings(key TEXT PRIMARY KEY, value TEXT NOT NULL)`. `SaveSettings` writes with the property name as key: `Exec(c, "INSERT INTO settings(key, value) VALUES ($k, $v) ON CONFLICT(key) DO UPDATE SET value = excluded.value", ("$k", p.Name), ("$v", value));` [VERIFIED: src/PrintInvoicer.Core/Data/Database.cs `SaveSettings`]. So the keys are exactly `BackupFolder` and `InvoiceFolder` [VERIFIED: src/PrintInvoicer.Core/Models/AppSettings.cs `public string InvoiceFolder { get; set; } = "";` and `public string BackupFolder { get; set; } = "";`]. Use `nameof(AppSettings.BackupFolder)` rather than literals.

**Recommendation:** raw SQL on `billament.db` right after the rename and before `new Database(...)`, using an unpooled `SqliteConnection` (same pattern as `DatabaseBackup.Open`), inside one transaction, touching at most two rows:
```sql
SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'settings'   -- skip quietly if absent
SELECT value FROM settings WHERE key = $k
UPDATE settings SET value = $v WHERE key = $k      -- only when the value is inside the old folder
```
Do not use `LoadSettings`/`SaveSettings` (see Alternatives). A row that is missing or empty means "use the default folder" and stays untouched; the defaults (`Backups` next to the database, `Documents\Billament\Invoices` via `InvoicePdf.DefaultFolder`) already point at the new place.

**Path comparison rules for `RemapFolder` (Core, cross-platform, comparison injected):**
1. Blank or whitespace: leave. Not `Path.IsPathFullyQualified(value)`: leave (a relative value would resolve against the current directory and could falsely match).
2. `Path.GetFullPath` both values (collapses `..` and `/`), `Path.TrimEndingDirectorySeparator` both. Catch `ArgumentException`, `NotSupportedException`, `PathTooLongException` and leave the value unchanged.
3. Equal to the old folder: result is the new folder.
4. Inside: `full.Length > old.Length && full.StartsWith(old, comparison) && full[old.Length]` is a directory separator. This is the directory-boundary rule, so `...\PrintInvoicerOld\x` and `...\PrintInvoicer2026` are not matched.
5. Result is `newFolder + full[old.Length..]` (the remainder comes from the normalized path and so cannot contain `..`).
6. Comparison: default `OperatingSystem.IsWindows() ? OrdinalIgnoreCase : Ordinal`, but taken as a parameter so the Linux tests cover both.
7. Not handled on purpose (left unchanged): 8.3 short names, `\\?\` prefixes, subst drives, a path that reaches the old folder through another junction. Document the limit in the Read me note.

### Pattern 3: Backup prefix handling (NAME-04 second half)

Current code [VERIFIED: src/PrintInvoicer.Core/Data/DatabaseBackup.cs]: `private const string Prefix = "printinvoicer-";`, `List` uses `GetFiles(Prefix + "*.db")`, `Prune` filters with `DailyName()` and orders `.OrderByDescending(f => f.Name, StringComparer.Ordinal)`, and `[GeneratedRegex(@"^printinvoicer-\d{4}-\d{2}-\d{2}\.db$")]`.

Needed changes:
- `Prefix = "billament-"` for every new file (`DailyIfDue`, `Manual`, restore safety copy).
- Add `private const string LegacyPrefix = "printinvoicer-";` (a deliberate grep hit).
- `List`: return files matching either prefix (two `GetFiles` calls or one `GetFiles("*.db")` filtered with `StartsWith(..., OrdinalIgnoreCase)`), newest first by `LastWriteTimeUtc` as today. Keep the 3-letter-extension caution in mind: `*.db` has a 2-letter extension, so the Windows "short extension matches longer ones" quirk does not apply.
- `DailyName()` regex becomes `^(billament|printinvoicer)-(\d{4}-\d{2}-\d{2})\.db$`.
- **Trap:** `Prune` currently orders by the whole file name, "the date in the name, not the file time". With two prefixes, ordinal descending name order puts every `printinvoicer-...` file ahead of every `billament-...` file regardless of date, so the pruner would keep the wrong 14 and could delete the newest new-style backups. Order by the captured date group (and tie-break by name) instead. Add a test: 10 legacy dailies in October plus 10 new dailies in November, keep 14, assert the 14 newest dates survive.
- `DailyIfDue`: treat today as done if either `billament-<today>.db` or `printinvoicer-<today>.db` exists (avoids a second daily copy on the upgrade day). Optional but cheap.
- `Restore` and `LooksLikePrintInvoicer`: rename to `LooksLikeBillament`; the check is content-based (tables `jobs`, `settings`, `plates`), so old backups restore regardless of file name. Message becomes "is not a Billament database". Update the `Prune` AUDIT comment text (names both patterns).
- Restore file picker in `SettingsViewModel` [VERIFIED: line 96 `Filter = "PrintInvoicer backups (printinvoicer-*.db)|printinvoicer-*.db|All databases (*.db)|*.db",`] becomes `"Billament backups (billament-*.db;printinvoicer-*.db)|billament-*.db;printinvoicer-*.db|All databases (*.db)|*.db"`. Semicolon-separated extensions inside one entry are the documented format. [CITED: https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.filedialog.filter]
- Restoring a pre-4.0 backup brings back a `BackupFolder`/`InvoiceFolder` that may still point into the old `Documents\PrintInvoicer` tree (the settings live inside the database). That is an edge case outside NAME-04 as written; record it in Open Questions rather than silently expanding scope.

### Anti-Patterns to Avoid
- **Blanket `sed` after the migration code exists:** it would rewrite the legacy constants (`"PrintInvoicer"`, `printinvoicer.db`, `printinvoicer-`) into the new names and silently turn the migration into a no-op. Run the sed first, write the legacy names afterward.
- **Creating the new folder on failure:** if the Documents move fails and the app then opens `Documents\Billament\billament.db`, the next start sees the new folder and D-08 skips the old data forever. On failure show the message and `Shutdown(1)` before any `new Database(...)`.
- **Calling `ErrorReport.Show` before the local pair has moved** (it writes under `%LOCALAPPDATA%\Billament\Errors`). Hence local first.
- **`App.xaml.cs` catch-all text** currently says `$"PrintInvoicer could not start. Its data is in {Database.DefaultPath}; nothing was changed."` [VERIFIED: App.xaml.cs line 69]. After the rename `DefaultPath` is the new location, which may not exist when the move failed. Give the migration failure its own message that names the old folder.
- **Using `SaveSettings` for the rewrite** (rewrites every setting row).
- **Treating `Directory.Exists(new)` as the only "exists" test** (misses a file or dangling symlink at the new name).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Consistent copy of the database for the pre-move backup | File.Copy of a live `.db` | `DatabaseBackup.Manual(...)` (SQLite backup API, `.partial` then rename) | Already handles consistency and cleanup of its own temp file |
| "Is this one of our databases" check | A new table probe | `DatabaseBackup.LooksLike...` (renamed) | Existing, content-based, covers old and new backups |
| Folder move | Recursive copy + delete | `Directory.Move` (same-parent rename) | Atomic on NTFS; avoids the partial-copy state D-10 forbids |
| Path containment test | `value.StartsWith(old)` | Normalize + boundary rule above | `PrintInvoicerOld`, `..` segments, trailing separators |
| Settings read/write for two keys | `LoadSettings`/`SaveSettings` round trip | Parameterized raw SQL on two keys | D-13: nothing else may change |
| Error report | New dialog code | `ErrorReport.Save` / `ErrorReport.Show` | Existing plain message + saved details |
| Release checksum, zip | New script logic | Existing release.yml steps with names changed | Only the names change |

**Key insight:** every risky operation here already has a tested helper in the repo (backup, integrity probe, error report); the phase's new code is the orchestration and the path rules, which is exactly what needs Linux unit tests.

## Runtime State Inventory

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | SQLite `settings` rows `BackupFolder` / `InvoiceFolder` may contain `...\Documents\PrintInvoicer\...` [VERIFIED: keys per `SaveSettings`]. `plates.source_file` holds user slice-file paths (not under the data folder; left alone). Existing PDFs live in `Documents\PrintInvoicer\Invoices` and move with the folder. Old backups are named `printinvoicer-*.db`. `%LOCALAPPDATA%\PrintInvoicer\update-check.json` stores `LatestUrl` (an old-name release URL). | Data migration for the folders and the two settings (Core, tested). Code edit for the backup prefix. The stored `LatestUrl` needs no action: the allow-check falls back to `ReleasesPage` and a 4.0.0 install only shows the bar for a version newer than itself. |
| Live service config | GitHub repository name (manual, D-14); GitHub release assets and notes; README links. No webhooks, Pages, secrets or Actions secrets referenced in the repo (workflows use `github.repository` and `secrets.GITHUB_TOKEN`) [VERIFIED: release.yml, ci.yml read]. The user's local clone `origin` URL keeps working through the redirect; `git remote set-url` is optional tidiness. | Manual rename by the user; do not create any repository named `PrintInvoicer` afterwards (it would end the redirects). |
| OS-registered state | Start menu shortcut `PrintInvoicer.lnk` (created by `publish.ps1`); taskbar pins to the old exe; install folder `%LOCALAPPDATA%\Programs\PrintInvoicer`. No services, scheduled tasks, registry keys or startup entries [VERIFIED: AUDITING.md "What it never does"]. | `publish.ps1` removes the old shortcut and install folder; Read me tells users to re-pin and delete the old exe. |
| Secrets/env vars | `PRINTINVOICER_README_IMAGES` is set only for the duration of `make-readme-images.ps1` and read in `ReadmeImageTests`; no secrets keyed by the name. | Code edit in both files together (script and test constant), uppercase `BILLAMENT_README_IMAGES`. |
| Build artifacts | Untracked `bin/` and `obj/` under `src/PrintInvoicer.*` and `tests/PrintInvoicer.*` (this container has them now from the research builds), `publish/PrintInvoicer.exe`, `%TEMP%\PrintInvoicer previews`, `%TEMP%\PrintInvoicer UI tests`, single-file extraction cache. | After `git mv`, the old directories survive because of the untracked `bin/obj`; delete them (`git clean -fdX src tests` or `rm -rf`) so the filesystem grep and `find -ipath '*printinvoicer*'` are clean. Old temp folders are disposable: ignore them (recommended). |

## Common Pitfalls

### Pitfall 1: ReaderContract namespace check fails at runtime, not compile time
**What goes wrong:** `ReaderContract.AssertOwnMessage` has `thrower?.StartsWith("PrintInvoicer.Core.Slicing.", StringComparison.Ordinal) == true` [VERIFIED: tests/PrintInvoicer.Tests/ReaderContract.cs line 81]. A rename that only touches `namespace`/`using` lines leaves this string, and every robustness test then fails with "reached the user unwrapped".
**How to avoid:** sed covers it; the gate is the unit test run (485 tests) after the rename.

### Pitfall 2: Pack URI uses the assembly name
**What goes wrong:** `pack://application:,,,/PrintInvoicer;component/Assets/PrintInvoicer.ico` appears in `MainWindow.xaml` line 5 and `JobEditorWindow.xaml` line 4 [VERIFIED]. With `<AssemblyName>Billament</AssemblyName>` it must be `/Billament;component/Assets/Billament.ico`, and the `<Resource Include="Assets\PrintInvoicer.ico" />` and `<ApplicationIcon>` entries must follow the `git mv`. A mismatch fails at runtime when the window loads, not at build. The Linux build compiles the XAML (markup compile) but does not load the icon, so the Windows UI tests (`WpfHost` opens the real windows) are the check.
**How to avoid:** rename file, csproj entries and both URIs in one task; verify in the [windows] UI test run.

### Pitfall 3: `Prune` ordering with two prefixes (see Pattern 3)
Wrong 14 kept; newest backups deleted. Test it.

### Pitfall 4: Legacy constants rewritten by the bulk replace (see Anti-Patterns)

### Pitfall 5: Failed move followed by a fresh `Documents\Billament`
See Anti-Patterns. The plan must make "failure means exit before `new Database`" explicit and test the Core half (`Failed` result leaves no new folder).

### Pitfall 6: 3.7.0 started again after 4.0.0 migrated
If a user launches the old `PrintInvoicer.exe` after the move, 3.7.0 finds no `Documents\PrintInvoicer`, creates a fresh empty database there (the constructor creates the folder and seeds starter materials), and the user thinks their data vanished. Nothing in 4.0.0 can prevent this; the Read me and README rename note must say "delete or stop using PrintInvoicer.exe; your data is now in Documents\Billament". The 4.0.0 zip contains `Billament.exe`, not `PrintInvoicer.exe`, and 3.7.0's update-bar tooltip says "replace PrintInvoicer.exe with the new one" [VERIFIED: MainWindow.xaml line 14], so the Read me must explain that the exe has a new name.

### Pitfall 7: `bin/obj` leftovers keep old directories alive
`git mv src/PrintInvoicer.Core src/Billament.Core` moves tracked files only; untracked build output keeps `src/PrintInvoicer.Core/` on disk, which breaks a filesystem-level grep and can confuse the sln. Clean first (`git clean -fdX`) or `rm -rf` the old shells.

### Pitfall 8: AUDITING.md counts and search hints
AUDITING.md says "There are 27 tags" [VERIFIED: matches `git grep -c "AUDIT:" -- src` total 27 today]. New AUDIT comments change the count; update it. Add `Directory.Move` to the "To check for yourself, search `src` for" list.

### Pitfall 9: Release heading underline
`packaging/Read me.txt` line 1 `PrintInvoicer 3.7` has a `=` underline of matching length; the new heading `Billament 4.0` is shorter, so shorten the underline (cosmetic). The release workflow heading check is a warning only.

### Pitfall 10: Two repository/URL moments
`UpdateService.Repository` in 4.0.0 points at `cevans3505/Billament`. If 4.0.0 is published before the repository rename, 4.0.0 installs get a 404 (treated as "no update information") until the rename happens, and 3.7.0 installs see the update normally. Rename first, then release; see the GitHub section.

## Code Examples

### Raw two-key settings rewrite (shape)
```csharp
// AUDIT: file read/write. Opens the moved database to change the two folder settings, nothing else.
using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = dbPath, Pooling = false }.ToString());
c.Open();
using var tx = c.BeginTransaction();
foreach (string key in new[] { nameof(AppSettings.BackupFolder), nameof(AppSettings.InvoiceFolder) })
{
    // SELECT value FROM settings WHERE key = $k   ->  RemapFolder(...)  ->  UPDATE settings SET value = $v WHERE key = $k
}
tx.Commit();
```
Source pattern: unpooled connection as in `DatabaseBackup.Open` [VERIFIED: DatabaseBackup.cs lines 141-152]; parameter style as `Database.Exec` with `$k`/`$v`.

### Linux test seam for a failing move
```csharp
var failing = new FileOps(MoveDirectory: (_, _) => throw new IOException("in use"));
var result = DataFolderMigration.Run(folders, now, failing);
Assert.Equal(FolderMove.Failed, result.Documents);
Assert.True(File.Exists(Path.Combine(oldDocs, "printinvoicer.db")));
Assert.False(Directory.Exists(newDocs));
```

### Baseline build and test commands (verified in this container)
```bash
export PATH=$HOME/.dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build PrintInvoicer.sln -p:EnableWindowsTargeting=true -c Release     # becomes Billament.sln; App + UiTests compile on Linux
dotnet test tests/PrintInvoicer.Tests -c Release                              # becomes tests/Billament.Tests; 485 pass at baseline
```

## Rename Inventory

Counts are for tracked files outside `.planning/phases` (`git grep -i printinvoicer`): 99 files, about 809 occurrences. Generated by `git grep` and `git ls-files` this session.

**A. Files and folders to `git mv` (D-03)**
- `PrintInvoicer.sln` to `Billament.sln` (project names inside the sln change; the four project GUIDs do not need to change, and nothing else keys off them [VERIFIED: sln read, GUIDs only appear in the sln itself]).
- `src/PrintInvoicer.Core/` to `src/Billament.Core/`; inside: `PrintInvoicer.Core.csproj` to `Billament.Core.csproj`.
- `src/PrintInvoicer.App/` to `src/Billament.App/`; `PrintInvoicer.App.csproj` to `Billament.App.csproj`; `Assets/PrintInvoicer.ico` to `Assets/Billament.ico`.
- `tests/PrintInvoicer.Tests/` to `tests/Billament.Tests/` (includes `Fixtures/`, whose files keep their names); `PrintInvoicer.Tests.csproj` to `Billament.Tests.csproj`.
- `tests/PrintInvoicer.UiTests/` to `tests/Billament.UiTests/`; `PrintInvoicer.UiTests.csproj` to `Billament.UiTests.csproj`.
- No other tracked file or folder names contain the old name (`git ls-files | grep -i printinvoicer` lists only the above). `docs/images/*.png` names do not contain it.

**B. Namespaces and usings:** 163 lines matching `^\s*(namespace|using) PrintInvoicer\.` in `.cs` files. Three fully qualified references that are not on those lines: `tests/PrintInvoicer.UiTests/ScreenTests.cs` line 160 `PrintInvoicer.Tests.TestFiles.Fixture(name)`, line 232 `PrintInvoicer.Core.Data.DatabaseBackup.List(...)`, and `ReaderContract.cs` line 81 (string, Pitfall 1).

**C. XAML:** `App.xaml` (`x:Class="PrintInvoicer.App.App"`, `xmlns:local="clr-namespace:PrintInvoicer.App"`; file starts with a BOM, sed preserves it), `MainWindow.xaml` (`x:Class`, `clr-namespace:PrintInvoicer.App.ViewModels`, `Title="PrintInvoicer"`, pack URI, tooltip "replace PrintInvoicer.exe"), `JobEditorWindow.xaml` (`x:Class`, pack URI), `Assets/Icon.xaml` (two comment lines).

**D. csproj and project properties** [VERIFIED: files read]: App csproj: `ProjectReference ..\PrintInvoicer.Core\PrintInvoicer.Core.csproj`, `<AssemblyName>PrintInvoicer</AssemblyName>`, `<RootNamespace>PrintInvoicer.App</RootNamespace>`, `<Product>PrintInvoicer</Product>`, `<Version>3.7.0</Version>` (becomes `4.0.0`), `<ApplicationIcon>Assets\PrintInvoicer.ico</ApplicationIcon>`, a comment, `<Resource Include="Assets\PrintInvoicer.ico" />`. Core csproj has no `RootNamespace`/`AssemblyName` (they derive from the file name, so the rename of the file renames the assembly and default namespace). Tests csproj: `ProjectReference ..\..\src\PrintInvoicer.Core\...`. UiTests csproj: `ProjectReference ..\..\src\PrintInvoicer.App\...`, `Compile Include="..\PrintInvoicer.Tests\TestFiles.cs"`, `None Include="..\PrintInvoicer.Tests\Fixtures\**"`. `AssemblyInfo.cs` line 13: `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("PrintInvoicer.UiTests")]`. It is the only `InternalsVisibleTo` in the repo; Core has none, so new Core migration API must be `public` for `Billament.Tests` to call it.

**E. User-visible strings (NAME-02)** [VERIFIED: grep output above]:
- `App.xaml.cs` lines 51, 56, 69 (error messages; line 69 needs the logic change described in Anti-Patterns).
- `ErrorReport.cs`: summary comment, `Folder` (`"PrintInvoicer", "Errors"`), the saved file header `$"PrintInvoicer {version}..."`, the message text "This is a problem in PrintInvoicer", the caption `"PrintInvoicer: something went wrong"`, `IssuesPage` (derives from `UpdateService.Repository`).
- `JobEditorWindow.xaml.cs` line 73 caption `"PrintInvoicer"`.
- `MainWindow.xaml` window title and tooltip; `UpdateService.BarText` (`$"PrintInvoicer {..} is available..."`), the `User-Agent` product name `ProductInfoHeaderValue("PrintInvoicer", "1")`, state path `"PrintInvoicer", "update-check.json"`, `Repository` constant `"cevans3505/PrintInvoicer"`.
- `MainViewModel.cs` line 417 combined print job name `$"PrintInvoicer, {written.Count} invoices"` (this is the print queue name, the only PDF-related text with the app name).
- `SettingsViewModel.cs`: restore filter (line 96), confirmation text "PrintInvoicer restarts afterwards" (line 103), AUDIT comment line 118.
- `InvoicePdf.cs`: `DefaultFolder` (`"PrintInvoicer", "Invoices"`), `PreviewFolder` (`"PrintInvoicer previews"`), XML comment.
- `WindowLayout.cs`: `FilePath` default and AUDIT comment; `Database.cs`: `DefaultPath` (`"PrintInvoicer", "printinvoicer.db"`), doc comments, AUDIT comment line 28. `ShellCommands.cs` and `AppSettings.cs` doc comments (`Documents\PrintInvoicer\Invoices`).
- `BambuFileReader.cs` line 436: `private const string TooLargeKey = "PrintInvoicer.TooLarge";`. It is an in-memory `Exception.Data` key, never persisted [VERIFIED: used only on lines 436-445]; change to `"Billament.TooLarge"`.
- PDF page text does not contain the app name: the invoice PDFs show the user's business name, and the three README pictures show no app name (checked visually: `docs/images/main-jobs.png`, `job-editor.png` and `invoice-page-1.png` have no window title bar and no product text). **QuestPDF metadata:** `grep -i "metadata|Creator|Producer" src` found no QuestPDF metadata call (hits are Bambu `Metadata/` zip entry names only) [VERIFIED: grep]. Nothing to rename; recommend NOT adding metadata in this phase (new behavior, "nothing beyond the name"). Record "no metadata set" in the verification.

**F. Tests and test data:** `BackupTests` (`DbPath` `printinvoicer.db`, names `printinvoicer-2026-10-20.db` and `-06.db`, `-manual.db`, the stray `.partial`, a directory named `printinvoicer-2026-10-30.db`, `LooksLikePrintInvoicer` call): after the bulk rename these become `billament-*`, which is right for new-code tests; add separate legacy-name cases by hand. `LatestReleaseReplyTests` (two fixture URLs under `cevans3505/PrintInvoicer/releases/tag/v3.7.0`; fictional data, fine to rename), `ReleaseVersionTests` line 19 `[InlineData("PrintInvoicer")]` (a non-version string; any word works), `ShellCommandsTests` sample paths, `Fixtures/pre-snapshot.sql` comment, `SampleShop.cs` (`printinvoicer.db`), `WpfHost.TempFolder` `"PrintInvoicer UI tests"`, `ScreenTests` (`"PrintInvoicer 3.4.0 is available. You have 3.3.0."`, `"Saved printinvoicer-"`), `ReadmeImageTests` (usings, constant `PRINTINVOICER_README_IMAGES`, doc comments).

**G. Scripts and workflows:**
- `publish.ps1`: default `InstallDir` `Programs\PrintInvoicer`, `Get-Process PrintInvoicer`, sln path, `src\PrintInvoicer.App`, exe name, `PrintInvoicer.lnk`, comments.
- `make-readme-images.ps1`: header comments, `$env:PRINTINVOICER_README_IMAGES` (set and removed), `tests\PrintInvoicer.UiTests`.
- `tools/Make-Icon.ps1`: header, `$assets` path, `PrintInvoicer.ico`.
- `ci.yml`: `dotnet restore/build PrintInvoicer.sln` (x2 restore, 1 build), `tests/PrintInvoicer.Tests`, `tests/PrintInvoicer.UiTests` (x3), artifact path `tests/PrintInvoicer.UiTests/bin/Release/net10.0-windows/screens/`.
- `release.yml`: header comment, `$csproj = 'src/PrintInvoicer.App/PrintInvoicer.App.csproj'`, heading regex `"^PrintInvoicer $([regex]::Escape($majorMinor))(\.|$)"`, `dotnet test tests/PrintInvoicer.Tests`, `dotnet publish src/PrintInvoicer.App`, step names, `publish/PrintInvoicer.exe`, the zip name in four places (`$zip = "PrintInvoicer-..."` x3 plus the upload `path:` list), notes text `run PrintInvoicer.exe`, `--title "PrintInvoicer $env:VERSION"`. All repository references are dynamic (`github.repository`, `GH_REPO`), so no hard-coded owner/repo exists in the workflows.

**H. Docs:** `README.md` (32 hits: title, image alt text on line 11 "The PrintInvoicer job list...", data folders, install path, release links, `dotnet test PrintInvoicer.sln`, `dotnet run --project src\PrintInvoicer.App`, project layout bullets, release steps, license sentences), `AUDITING.md` (17 hits incl. the API URL row and `PrintInvoicer/1` User-Agent), `CLAUDE.md` (6), `.claude/CLAUDE.md` (110), `packaging/Read me.txt` (7 plus heading), `.planning/codebase/*.md` (7 files, about 300 hits), `.planning/PROJECT.md` (3), `.planning/ROADMAP.md` (8), `.planning/REQUIREMENTS.md` (4), `.planning/STATE.md`, `.planning/CONCERNS-TRIAGE.md` (1). The `.planning` top-level files describe the rename itself; their remaining hits are "history / explains the rename" and should be listed in the verification with that reason, except `PROJECT.md` `# PrintInvoicer`-style product-name lines, which should be updated.

**I. Mechanical method (recommended for the planner):** one scripted step, in this order: clean untracked `bin/obj`; `git mv` the files and folders in A; then `sed -i` with three case variants across `src tests *.sln` and csproj/xaml/cs/sql files: `s/PrintInvoicer/Billament/g`, `s/printinvoicer/billament/g`, `s/PRINTINVOICER/BILLAMENT/g`; then review `git diff --stat` and build. Docs, scripts and workflows get the same treatment in their own tasks, followed by hand edits for the sentences that need new meaning (data folder note, shortcuts, redirects). Write the legacy names (migration, legacy backup prefix, restore filter, publish.ps1 cleanup) after the sed.

## GitHub Rename and the Update Check (NAME-05, D-16)

**What GitHub documents.**
- Web: "all existing information, with the exception of project site URLs, is automatically redirected to the new name"; git operations to the old location keep working; redirects stop if the old name is reused ("redirects to the renamed repository will no longer work"). [CITED: https://docs.github.com/en/repositories/creating-and-managing-repositories/renaming-a-repository, as read through a summarizing fetch tool]
- REST API: clients should "assume that any request may result in a redirection", "Receiving an HTTP redirection is not an error, and you should follow the redirect", a `301` is permanent, `302`/`307` temporary, and the client should repeat the request to the URL in the `location` header. [CITED: https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api]
- GitHub's 2015 changelog for repository redirects says requests to the old location of a renamed or transferred repository receive "either a `301` or `307` HTTP redirect, depending on the type of request being made". It does not state the `Location` format. [CITED: https://developer.github.com/changes/2015-04-17-preview-repository-redirects]
- **Not confirmed from an authoritative source:** that `GET /repos/{old}/releases/latest` returns a 301 whose `Location` is `https://api.github.com/repositories/{id}/releases/latest` (i.e. keeps the path suffix). A third-party issue shows `Location` in the `/repositories/...` form for another endpoint, and the same pattern is widely reported. [ASSUMED: A1] I could not test it here: the container's proxy returns 403 for GitHub API calls to arbitrary repositories.

**What .NET 10 does.** `HttpClient` over `SocketsHttpHandler` defaults to `AllowAutoRedirect = true`; "The Authorization header is cleared on auto-redirects ... No other headers are cleared"; it never follows HTTPS to HTTP. [CITED: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.allowautoredirect] The 3.7.0 client is `new HttpClient()` with a `User-Agent` and an `Accept: application/vnd.github+json` default header and no custom handler [VERIFIED: UpdateService.cs `CreateClient`], so the 301/307 to `api.github.com/repositories/...` (same host, HTTPS) is followed with both headers intact.

**What 3.7.0 does with the reply** [VERIFIED: UpdateService.cs]:
- `FetchLatestAsync` does `Http.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest", HttpCompletionOption.ResponseHeadersRead, ct)`, returns null only on 404, then `response.EnsureSuccessStatusCode()`, caps the body at `LatestReleaseReply.MaxBytes`, and builds `new ReleaseInfo(v, release!.HtmlUrl ?? ReleasesPage)`. The 4.0.0 release's `html_url` will be `https://github.com/cevans3505/Billament/releases/tag/v4.0.0`.
- `Apply` shows the bar when `ReleaseVersion.IsNewer(latest.Version, Installed)`; the bar text is `PrintInvoicer 4.0.0 is available. You have 3.7.0.` (3.7.0 wording).
- `Download()` runs `if (!url.StartsWith($"https://github.com/{Repository}/", StringComparison.OrdinalIgnoreCase)) url = ReleasesPage;` with `Repository = "cevans3505/PrintInvoicer"`, so the new-name URL fails the check and `url` becomes `https://github.com/cevans3505/PrintInvoicer/releases`. The browser request to that old-name web URL is redirected by GitHub to the new repository's releases page (web redirect documented above). The user lands on the release list where 4.0.0 is on top.
- If the API redirect were not followed (for example a 301 that the handler refused), `EnsureSuccessStatusCode` throws `HttpRequestException`, which `FetchAndRememberAsync` catches silently; the bar would not appear. That is the one failure mode, and it is why the check below is mandatory.

**Conclusion:** 3.7.0 users should see the 4.0.0 update bar with no code change to 3.7.0 (which cannot be changed anyway). Nothing in the plan needs to compensate, but the plan must carry these steps (all in the manual checklist, D-21):
1. Rename the repository **before** publishing the 4.0.0 release (so 4.0.0's own `Repository` constant resolves directly and the release `html_url` is stable). Order: rename, `git remote set-url origin`, push `main`, CI, tag, draft, publish.
2. Never create a repository named `PrintInvoicer` under `cevans3505` again (ends redirects). [CITED: renaming docs]
3. Before announcing: `curl -sSIL -H "User-Agent: check" -H "Accept: application/vnd.github+json" https://api.github.com/repos/cevans3505/PrintInvoicer/releases/latest` should show a 301 or 307 followed by `200` and a `link`/`location` to the new repository. After publishing 4.0.0, run 3.7.0 and press **Check now** on the Settings tab; the status line should read `Version 4.0.0 is available. You have 3.7.0.` [VERIFIED: status text in `UpdateService.Apply`]. A draft release is invisible to `releases/latest`, so publish first.
4. If step 3 fails (the redirect is not followed), the fallback is a manual notice: the README "Moved" paragraph and release notes tell 3.7.0 users to download from `https://github.com/cevans3505/Billament/releases`; there is no way to patch 3.7.0.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build, unit tests | yes (`$HOME/.dotnet/dotnet`, not on PATH) | 10.0.403 | none needed |
| Windows targeting on Linux | compile App and UiTests | yes with `-p:EnableWindowsTargeting=true` | same SDK | none needed |
| git | `git mv`, `git grep` | yes | not recorded | none |
| PowerShell (`pwsh`) | syntax-check of `.ps1` scripts | **no** (`command -v pwsh powershell` found nothing) | n/a | text checks with grep only; real run is [windows] |
| gh CLI | repository queries | present, but GitHub access to arbitrary repos is blocked by the proxy (403) | n/a | none; redirect test is manual on the user's PC |
| Windows desktop | UI tests, README images, publish.ps1, real upgrade test | no | n/a | [windows] steps on the user's PC |

**Exact cloud compile command that works (run this session, 0 warnings, 0 errors):**
```bash
export PATH=$HOME/.dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build PrintInvoicer.sln -p:EnableWindowsTargeting=true -c Release    # after the rename: Billament.sln
# or per project: dotnet build src/PrintInvoicer.App -p:EnableWindowsTargeting=true -c Release
#                 dotnet build tests/PrintInvoicer.UiTests -p:EnableWindowsTargeting=true -c Release
dotnet test tests/PrintInvoicer.Tests -c Release     # 485 passed, 0 failed (baseline)
```
[VERIFIED: ran all of these this session] This compiles the WPF markup, so `x:Class`/`clr-namespace` mistakes fail in the cloud; it does not run anything, so pack-URI icon loading and UI behavior are still [windows] checks. The restore step needs network access to NuGet (it worked here).

**Missing dependencies with no fallback:** none for [cloud] work.
**Missing dependencies with fallback:** `pwsh` (review scripts as text; add a Windows run of `publish.ps1` to the checklist).

## publish.ps1 Design (NAME-06, research question 6)

Current script facts [VERIFIED: publish.ps1 read]: default `$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\PrintInvoicer'`, `Get-Process PrintInvoicer`, shortcut at `Join-Path ([Environment]::GetFolderPath('Programs')) 'PrintInvoicer.lnk'`, script runs under Windows PowerShell 5.1 with `$ErrorActionPreference = 'Stop'`.

Recommended changes:
- Default install dir `Programs\Billament`, exe `Billament.exe`, shortcut `Billament.lnk`, `dotnet test (Join-Path $root 'Billament.sln')`, publish `src\Billament.App`.
- Running check: `if (Get-Process -Name 'Billament', 'PrintInvoicer' -ErrorAction SilentlyContinue) { throw 'Billament (or the old PrintInvoicer) is running. Close it and run this again.' }`. (`Get-Process -Name` takes several names and, with `SilentlyContinue`, ignores names with no process. Process names have no `.exe`.) [ASSUMED: PowerShell 5.1 behavior, A5]
- **Remove old items only after the new install succeeded** (so a failed publish never leaves the user with no working program), in a clearly commented block with `# AUDIT:` tags (it deletes files; AUDITING.md "Building and releasing" must say so):
  - Old shortcut: `$oldLnk = Join-Path programs 'PrintInvoicer.lnk'`. Delete only if it exists **and** `(New-Object -ComObject WScript.Shell).CreateShortcut($oldLnk).TargetPath` ends with `\PrintInvoicer.exe` (so an unrelated user shortcut with that name is not removed). Use `Remove-Item -LiteralPath`.
  - Old install folder: only the default old location `Join-Path $env:LOCALAPPDATA 'Programs\PrintInvoicer'` (never derived from the `-InstallDir` parameter), only if `Test-Path (Join-Path $old 'PrintInvoicer.exe')`, only if it contains no `*.db` file and no `Backups`/`Invoices` folder, and only if its resolved path is not equal to the new install dir. Then `Remove-Item -LiteralPath $old -Recurse -Force`. The data folders (`Documents\PrintInvoicer`, `%LOCALAPPDATA%\PrintInvoicer`) are different paths and are never mentioned in a delete command.
  - Print what was removed (`Write-Host`).
- Add a one-line `Write-Host` reminding the developer that taskbar pins to the old exe must be re-pinned.

## Common Pitfalls (continued): user-experience traps to put in docs

- Taskbar pins and desktop shortcuts to `PrintInvoicer.exe` break; the Read me should say to re-pin.
- A second PC syncing `Documents` through OneDrive will see the folder rename; 3.7.0 on that PC would create a new empty `Documents\PrintInvoicer`. Say "update every PC together" (multi-PC sharing is out of scope per REQUIREMENTS).
- 3.7.0's update bar tooltip tells users to "replace PrintInvoicer.exe with the new one"; the zip now holds `Billament.exe`. The Read me note must say "run Billament.exe; you can delete PrintInvoicer.exe".

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `Directory.Move` onto an existing directory could behave differently per platform | Throws `IOException` on all platforms when the destination exists | .NET Core 3.0 | Safe default for D-08; still check explicitly |
| API 404 for old repository names | 301/307 redirects for renamed or transferred repositories | 2015 | 3.7.0's update check keeps working |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `GET /repos/{old}/releases/latest` after a rename returns a 301 (or 307) with `Location` `https://api.github.com/repositories/{id}/releases/latest` (path suffix kept), and then 200 | GitHub Rename | 3.7.0 users silently see no update bar; mitigated by the curl and "Check now" checks and the manual-notice fallback |
| A2 | GitHub's web redirect sends `https://github.com/cevans3505/PrintInvoicer/releases` to the new repository's releases page | GitHub Rename | The Download button lands on a 404; documented generally for "existing information" but not tested for this URL |
| A3 | `Directory.Move` on a junction or symlink renames the link itself, not the target | Data Move Design | Only matters for users who junctioned their data folder; low probability; the backup is made first either way |
| A4 | A running 3.7.0 copy (pooled SQLite connection) or a sync client with an open handle makes the directory rename fail with IOException/UnauthorizedAccessException rather than succeed | Data Move Design | If the rename succeeded while a file is open, a running 3.7.0 could keep writing to a moved file; the pre-move backup limits damage. Verify in the [windows] upgrade test by starting 3.7.0, then 4.0.0 |
| A5 | Windows PowerShell 5.1 `Get-Process -Name a, b -ErrorAction SilentlyContinue` ignores names with no process | publish.ps1 | The running check might throw or miss; verify when running the script on Windows |
| A6 | `Path.Exists` is false and `FileSystemInfo.LinkTarget` is non-null for a dangling directory symlink/junction at the new name | Data Move Design | A dangling link would be treated as free and `Directory.Move` would fail with IOException; result is still `Failed` with data intact, so the impact is a worse message |
| A7 | Native-library self-extraction leaves a stale old-name cache under `%TEMP%` | Runtime State | None (disposable) |

**Confirm with the user before they become locked:** A1 and A2 are verified by the manual checklist rather than by a decision.

## Open Questions

1. **What does 4.0.0 do when the Documents move fails?**
   - What we know: D-10 requires a plain message and old data left in place; CONTEXT leaves "stop start-up or start with old data untouched" open.
   - What's unclear: whether the app should exit or run on the old folder.
   - Recommendation: show the plain message and exit (`Shutdown(1)`) without creating any new folder. Running on the old folder would need `Database.DefaultPath` to be switchable and would leave the user on a path 3.7.0 and 4.0.0 both think they own; the typical causes (another copy running, sync, permissions) are fixed by the user in minutes and the next start retries cleanly.
2. **Should a local-pair-only failure also be fatal?**
   - Recommendation: no. The pair holds only window layout, the update-check cache and error files. Continue, save details with `ErrorReport.Save`, and mention it in the notice once.
3. **A leftover unrelated `Documents\Billament` folder** (exists, has no `billament.db`) would make D-08 skip the move silently while `Documents\PrintInvoicer\printinvoicer.db` still exists.
   - Recommendation: keep D-08 (touch nothing) but return `NewFolderExists` plus a `Notice` when the old folder holds a database and the new folder holds none, and let the app show that message. Ask the user if they want the notice in 4.0.0 or want to leave it out.
4. **Restoring a pre-4.0 backup** brings back `BackupFolder`/`InvoiceFolder` values that still point into `Documents\PrintInvoicer`. Out of NAME-04's wording; recommend noting it in the Read me only, or applying `RemapFolder` after a restore in Phase 4 (which already reworks restore, FIX-09).
5. **README pictures:** none of the three shows the app name (no title bar, no product text), so regeneration is not required by the rename. Only the alt text on README line 11 changes. Regenerate on Windows only if the UI text changes for another reason.

## Validation Architecture

> `.planning/config.json` has `workflow.nyquist_validation: false`, so this section is not required by the config; it is included because the task asked for it.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xunit 2.9.3 (+ xunit.runner.visualstudio 3.1.4), .NET 10 [VERIFIED: csproj] |
| Config file | none (project properties only) |
| Quick run command | `dotnet test tests/Billament.Tests -c Release --filter "FullyQualifiedName~DataFolderMigration|FullyQualifiedName~Backup"` |
| Full suite command (Linux) | `dotnet test tests/Billament.Tests -c Release` (485 tests at baseline) |
| Full suite command (Windows) | `dotnet test Billament.sln` |
| Cloud compile gate | `dotnet build Billament.sln -p:EnableWindowsTargeting=true -c Release` |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| NAME-01 | Solution builds under the new names; robustness suites still see `Billament.Core.Slicing.` | build + unit | cloud compile gate; `dotnet test tests/Billament.Tests` | existing tests (renamed) |
| NAME-02 | User-visible strings | unit/UI + grep | `ScreenTests` update-bar test (Windows); grep for old name in `src` | existing, adjust expected text |
| NAME-03 | Only old exists: moved, db renamed, backup present, old gone | unit | `--filter DataFolderMigration` | Wave 0 new `DataFolderMigrationTests.cs` |
| NAME-03 | Both exist: old untouched byte for byte, no backup | unit | same | Wave 0 |
| NAME-03 | Only new exists / neither exists: nothing created or changed | unit | same | Wave 0 |
| NAME-03 | Finalized `finalized_total` and `price_snapshot` identical before and after (raw SQL compare) and totals still price to 87.20 and 40.40 via `PriceSnapshot.For` using the `pre-snapshot.sql` fixture | unit | same | Wave 0 (reuse `LegacyDatabaseTests` helpers) |
| NAME-03 | Failure leaves old in place (move delegate throws; damaged db fails the backup): `Failed`, old intact, no new folder, backup file kept | unit | same | Wave 0 |
| NAME-03 | Rename-after-move fails: rolled back; both fail: result names the new folder and a second `Run` repairs it | unit | same | Wave 0 |
| NAME-03 | Runs once: second `Run` after success is `NothingToMove`; after a failed run a later `Run` succeeds | unit | same | Wave 0 |
| NAME-03 | Symlink or file occupying the new name is treated as existing | unit | same (Linux `Directory.CreateSymbolicLink`) | Wave 0 |
| NAME-03 | Old folder without a database: moved, no backup, no error | unit | same | Wave 0 |
| NAME-04 | BackupFolder and InvoiceFolder inside old are remapped; outside, blank, relative, `PrintInvoicerOld\x`, `Old\..\Other` are unchanged; trailing separator; exact old folder; case rules both ways; only those two rows change (diff all settings rows) | unit | same | Wave 0 |
| NAME-04 | Legacy backups are listed, restorable, and pruned by date across both prefixes; `pre-rename` and manual files never pruned | unit | `--filter Backup` | extend `BackupTests.cs` |
| NAME-05 | `UpdateService.Repository`, allow-check, issue page, BarText | unit/UI | `ScreenTests` update bar (Windows); `LatestReleaseReplyTests` (Linux) | existing, adjust |
| NAME-05 | 3.7.0 finds 4.0.0 after the GitHub rename | manual | curl + "Check now" in 3.7.0 | manual |
| NAME-06 | Scripts and workflows use new names | grep | `git grep -n -i printinvoicer -- publish.ps1 make-readme-images.ps1 tools .github` shows only the deliberate old-name cleanup lines | n/a |
| NAME-07 | Docs, Read me heading `Billament 4.0` and note | grep + read | grep; heading regex test (see Wave 0) | n/a |
| NAME-08 | Version 4.0.0 and final grep | grep | commands below | n/a |

Final grep (last cloud task), all three must be reviewed and every remaining hit listed with its reason:
```bash
git grep -n -i printinvoicer -- . ':!.planning/phases/01-readme-showcase' ':!.planning/phases/02-quick-win-concern-fixes'
git ls-files | grep -i printinvoicer          # expected: nothing
find . -ipath '*printinvoicer*' -not -path './.git/*'   # expected: nothing (also catches untracked bin/obj shells)
```
Expected deliberate hits: `DataFolderMigration.cs` (old folder and database names, comments), `DatabaseBackup.cs` (legacy prefix and regex), `SettingsViewModel.cs` (restore filter), the new migration and legacy-backup tests, AUDITING.md rows for the move and for `publish.ps1` cleanup, README and `packaging/Read me.txt` rename notes, `publish.ps1` old shortcut/install/process names, `.planning/ROADMAP.md`, `REQUIREMENTS.md`, `PROJECT.md`, `STATE.md`, `CONCERNS-TRIAGE.md` and `.planning/phases/03-*` (they describe the rename), and an explanatory comment in `UpdateService.cs` if the planner adds one about the 3.7.0 redirect.

### Checks that run only on Windows
- UI tests (`dotnet test tests/Billament.UiTests`): opens real windows offscreen; the only check that the new pack URI and `x:Class` load.
- `make-readme-images.ps1` (optional, see Open Question 5), `publish.ps1` (install, shortcut, old-item cleanup), starting the exe.
- **Real upgrade test:** copy a real 3.7.0 `Documents\PrintInvoicer` and `%LOCALAPPDATA%\PrintInvoicer` to a test user or at least back them up, start 4.0.0, confirm: backup `billament-...-pre-rename.db` exists in `Documents\Billament\Backups`, `billament.db` present, job list and finalized totals unchanged, custom folder settings (if any) rewritten, window layout kept. Also test the failure path by starting 3.7.0 first (data file open) and then 4.0.0: expect the plain message and no `Documents\Billament`.
- GitHub rename checks (curl, "Check now") and the release flow.

### Sampling Rate
- **Per task commit:** cloud compile gate plus the quick filtered test command.
- **Per wave merge:** `dotnet test tests/Billament.Tests -c Release` (full) and the compile gate.
- **Phase gate:** full unit suite green, compile gate green, final grep reviewed; then the [windows] checklist.

### Wave 0 Gaps
- [ ] `tests/Billament.Tests/DataFolderMigrationTests.cs` covering the cases above (temp folders under `Path.GetTempPath()`, `SqliteConnection.ClearAllPools()` in `Dispose`, a helper that builds the old data folder from `Fixtures/pre-snapshot.sql`, sample names only, test method names as sentences with underscores).
- [ ] New legacy-backup cases in `BackupTests.cs` (mixed prefixes, prune by date).
- [ ] Optional: a unit test that reads `packaging/Read me.txt` line 1 and applies the release heading rule to catch a heading that lags the version (the workflow only warns).
- Framework install: none needed.

## Recommended Plan and Wave Split

| Wave | Plan | Where | Covers | Notes |
|------|------|-------|--------|-------|
| 1 | **03-01 Mechanical rename of code** | [cloud] | NAME-01, code parts of NAME-02, NAME-05 constants, `<Version>4.0.0</Version>` (NAME-08) | clean `bin/obj`; `git mv` list A; sed three case variants over `src tests Billament.sln`; fix `ReaderContract`, pack URIs, `TooLargeKey`; gate: cloud compile + 485 tests. Legacy-looking strings written later. |
| 2 | **03-02 Core migration + backup prefix** | [cloud] | NAME-03 (Core, tests, AUDIT comments), NAME-04, AUDITING.md rows | `DataFolderMigration`, `DatabaseBackup` prefix/list/prune/regex/rename of `LooksLike...`, `DataFolderMigrationTests`, `BackupTests` additions; `Database.DefaultPath` and AUDIT comment text. Owns the AUDITING.md migration rows and the AUDIT count. |
| 2 | **03-03 Scripts and workflows** | [cloud] | NAME-06 | `publish.ps1` (with old-item cleanup), `make-readme-images.ps1`, `tools/Make-Icon.ps1`, `ci.yml`, `release.yml`; text review only (no `pwsh` here). Does not edit AUDITING.md (03-05 does). Parallel with 03-02 (disjoint files). |
| 3 | **03-04 App wiring** | [cloud] compile-checked, [windows] verified later | NAME-03 (App half), NAME-02 | `App.OnStartup` calls `DataFolderMigration.Run(DataFolders.Under(documents, local), DateTime.Now)` before `new Database`, plain failure message and `Shutdown(1)` with no new folder, Local failure non-fatal, fix the catch-all message at line 69, restore picker filter, caption and message strings, `ErrorReport`, `UpdateService` (Repository, User-Agent, state path), `MainViewModel` print name. Depends on 03-02's public API. |
| 4 | **03-05 Docs sweep and final grep** | [cloud] | NAME-07, NAME-08 | README (including alt text, a short "Renamed to Billament" section with where the pre-move backup is), AUDITING.md (rename, API row, count, `publish.ps1` deletes, `Directory.Move`), `CLAUDE.md`, `.claude/CLAUDE.md`, `packaging/Read me.txt` (heading `Billament 4.0`, shortened underline, move note, "delete PrintInvoicer.exe, re-pin"), `.planning/codebase/*.md`, `PROJECT.md`; **last task: the final case-insensitive grep and the list of deliberate hits.** |
| 5 | **03-06 Windows checklist** | [windows], manual | NAME-02, NAME-03, NAME-05, NAME-06, release | UI tests, optional README pictures, `publish.ps1` run, real upgrade test from a 3.7.0 data copy (include the "3.7.0 still running" failure case), rename the GitHub repository, then push, CI, tag `v4.0.0`, draft, notes with the `SHA-256:` line, publish, confirm 3.7.0 "Check now". Re-run the final grep after any change made here. |

## Security Domain

> `security_enforcement` is enabled, ASVS level 1, block on high (config). No new network surface and no new packages.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | none (local single-user app) |
| V3 Session Management | no | none |
| V4 Access Control | no | OS file permissions only; the migration runs as the current user |
| V5 Input Validation | yes | Settings values are treated as untrusted text: `Path.IsPathFullyQualified`, `GetFullPath` normalization, directory-boundary containment, exception-filtered fallbacks (value left unchanged) |
| V6 Cryptography | no | none (the SHA-256 release checksum is unchanged) |
| V8 Data Protection | yes | Backup before any move; no deletion; finalized snapshots untouched and test-compared byte for byte |
| V12 Files and Resources | yes | Rename only within one parent; occupied-name check includes files and symlinks; no write through an existing destination; AUDIT comments and AUDITING.md rows |
| V14 Configuration | yes | No new packages; release workflow permissions unchanged (read-only token, write only in the draft-release job) |

### Known Threat Patterns for this phase

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Path traversal through `BackupFolder`/`InvoiceFolder` values (`...\PrintInvoicer\..\..\Other`) | Tampering | Compare the normalized full path, rewrite only when strictly inside the old folder; the remainder comes from the normalized string so it cannot contain `..` |
| Sibling-prefix confusion (`PrintInvoicerOld`) | Tampering | Directory-boundary check (separator must follow the old folder) |
| Symlink or junction (or a plain file) at `Documents\Billament` before first run | Tampering / Information disclosure | Treat any occupied name (`Path.Exists` or non-null `LinkTarget`) as "new exists" and skip; never write through it; `Directory.Move` also refuses an existing destination |
| Partial move leaving two partial copies | Denial of service / Repudiation | Single-parent rename (atomic); backup first; rollback on db-rename failure; next-start repair rule; message names exact locations |
| Data loss by deletion | Denial of service | The migration has no delete call; `pre-rename` backup never matches the daily-prune regex; old folders untouched when new exist |
| Wrong database opened after a failed move | Tampering (silent data fork) | Exit on Documents failure before any `new Database`; do not create the new folder |
| `publish.ps1` removing the wrong folder | Tampering / DoS | Delete only the exact default old install path, only if it holds `PrintInvoicer.exe`, no `*.db`, and is not the new install dir; shortcut deleted only when its target is `PrintInvoicer.exe`; never reference data folders |
| Error text disclosing paths | Information disclosure | Plain message names only the user's own folders; details go to the local error file (already documented as local-only) |
| Redirect from the update check to another host | Spoofing | Unchanged: the allow-check requires `https://github.com/{Repository}/` before opening anything; the HTTP client cannot follow HTTPS to HTTP |

## Sources

### Primary (HIGH confidence)
- In-repo files read this session (paths and line numbers cited inline): `UpdateService.cs`, `LatestReleaseReply.cs`, `DatabaseBackup.cs`, `Database.cs` (lines 1-140 and `LoadSettings`/`SaveSettings`), `AppSettings.cs`, `App.xaml.cs`, `SettingsViewModel.cs`, `ErrorReport.cs`, `WindowLayout.cs`, `InvoicePdf.cs`, `publish.ps1`, `make-readme-images.ps1`, `ci.yml`, `release.yml`, all four csproj files, the sln, `AssemblyInfo.cs`, `LegacyDatabaseTests.cs`, `BackupTests.cs`, `ReaderContract.cs`, AUDITING.md, README.md, `packaging/Read me.txt`, and the three README pictures.
- Commands run: `dotnet build PrintInvoicer.sln -p:EnableWindowsTargeting=true -c Release` (0 warnings, 0 errors), `dotnet test tests/PrintInvoicer.Tests -c Release` (485 passed), `git grep`/`git ls-files` inventories.

### Secondary (MEDIUM confidence, official docs via a summarizing fetch tool)
- https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.move (IOException cases, destination-exists note, trailing spaces)
- https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.allowautoredirect (default true, only Authorization cleared, no HTTPS to HTTP)
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.filedialog.filter (semicolon-separated extensions)
- https://docs.github.com/en/repositories/creating-and-managing-repositories/renaming-a-repository (redirects, name reuse ends them)
- https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api (follow 301/302/307 redirects)
- https://developer.github.com/changes/2015-04-17-preview-repository-redirects (301 or 307 for renamed repositories)

### Tertiary (LOW confidence)
- Search-result snippets showing `Location: https://api.github.com/repositories/...` for a redirected API call (A1). Not authoritative for `releases/latest`.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH (no change; versions read from csproj)
- Architecture and migration design: HIGH on structure and order, MEDIUM on Windows-specific lock and OneDrive behavior (A3, A4 need the Windows upgrade test)
- Rename inventory: HIGH (generated from git this session)
- GitHub redirect behavior: MEDIUM (documented generally; `releases/latest` Location form untested here)
- Pitfalls: HIGH for the repo-specific ones (ReaderContract, pack URI, Prune ordering, App catch-all message), MEDIUM for user-experience ones

**Research date:** 2026-11-02
**Valid until:** 30 days for repo facts (re-run the inventory greps if other phases land first); the GitHub redirect checks must be repeated on the day of the rename.
