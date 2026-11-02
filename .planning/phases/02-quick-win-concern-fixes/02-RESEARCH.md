# Phase 2: Quick-Win Concern Fixes - Research

**Researched:** 2026-11-02
**Domain:** C# / .NET 10 code hardening (SQLite backup, process start, HTTP read cap, file-name sanitizing), GitHub Actions release workflow, docs
**Confidence:** HIGH (all code sites read this session; two Windows-only behaviors are `[ASSUMED]` and listed)

<user_constraints>
## User Constraints (no CONTEXT.md)

The user chose to continue without discuss-phase, so there is no CONTEXT.md. The binding constraints come from
ROADMAP.md, REQUIREMENTS.md, STATE.md and CLAUDE.md:

### Locked Decisions
- Mode is `mvp`. Fix concerns scored 2.0 and above, in score order: C11 (3.00), C27 (2.50), then C06, C09, C10, C14, C22 (2.00 each).
- TRIA-01 (CONCERNS-TRIAGE.md scored every concern) is already done. Any item found already handled when its code is checked is re-scored in CONCERNS-TRIAGE.md rather than changed.
- Finalized invoices never change. Pricing changes go behind a new `PricingRules` version. Schema changes only through `AddColumnIfMissing` / `CREATE TABLE IF NOT EXISTS`.
- Any new line that touches the network, starts a program, prints, or reads/writes/deletes a file gets an `// AUDIT:` comment and a row in `AUDITING.md`.
- UI tests offscreen only. No personal data (sample names such as "Harbor Dive Shop", "Sample Printing Co."). Plain English, no em dashes. Readers throw only `InvalidDataException`. Catch with `when` filters.
- Do not move off QuestPDF and do not code-sign (both out of scope in REQUIREMENTS.md).

### Claude's Discretion
- How to split the work into plans; where new helpers live; exact cap and length numbers; fixture format for FIX-02.

### Deferred Ideas (OUT OF SCOPE)
- Everything scored below 2.0 (FIX-08..FIX-12 are Phase 3; v2 items FIX2-01..07). Freeze-once recording in `settings` (FIX-08) is Phase 3 and must not be started here.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TRIA-01 | CONCERNS-TRIAGE.md scores every concern | Done. This phase adds re-score notes (table in "Re-score findings") |
| FIX-01 | SHA-256 checksum file per release, release steps say to state it in the notes | release.yml has no checksum step today; design under "FIX-01" |
| FIX-02 | Test with a pre-snapshot fixture DB, each finalized job snapshotted exactly once, expected totals | A partial test exists; real gap described under "FIX-02" |
| FIX-03 | Explorer calls use `ArgumentList` | Exactly two call sites; builder in Core is Linux-testable |
| FIX-04 | Manual and daily backup never share a `.partial` | Names already differ; unique temp name closes the remaining gaps |
| FIX-05 | Update-check reply read with a size cap; over cap = "no update info" | Unbounded read at UpdateService.cs:166-169; cap logic can live in Core |
| FIX-06 | Invoice file/folder names sanitized and length-limited, with tests | Invoice number is not sanitized (real traversal); Core sanitizer + tests |
| FIX-07 | README and AUDITING.md state the QuestPDF Community revenue-threshold condition | Terms verified on questpdf.com; README currently says nothing |
</phase_requirements>

## Summary

All seven FIX items are still worth doing, but two are only partly open. **C27** already has a test (`Older_finalized_invoices_are_frozen_on_upgrade`, PriceSnapshotTests.cs:167-182) that finalizes a job without a snapshot on a current-schema database and reopens it. What is missing is a database whose `jobs` table really lacks `price_snapshot` and `finalized_total`, more than one finalized job, a draft that must stay unfrozen, hand-derived literal totals, and proof of "exactly once" (the snapshot text is byte-identical after later opens even though prices changed). **C09** is already handled for the exact pair named in the concern: the daily name (`printinvoicer-YYYY-MM-DD.db`, DatabaseBackup.cs:24) and the manual name (`printinvoicer-YYYY-MM-DD-HHmmss-<label>.db`, DatabaseBackup.cs:34) can never be equal, so they never share a `.partial`. The remaining gaps are two manual backups in the same second with the same label (a double click) and a blind delete of any existing `.partial` (DatabaseBackup.cs:116, which also has no `AUDIT:` tag). A unique temp name closes both and is a few lines.

The other items are real. **C14** is worse than the concern text says: `Safe()` (InvoicePdf.cs:647-651) cleans only the job title, and only with `Path.GetInvalidFileNameChars()`, which on Linux returns just NUL and `/` (verified by running it). The **invoice number is typed by the user** (JobEditorViewModel.cs:150-160, a plain text box) and is put into the file name unsanitized (InvoicePdf.cs:44-50), so `x\..\..\y` as an invoice number writes outside the invoice folder. There is also no length limit, and a too-long path throws `PathTooLongException` (an `IOException`), which `GenerateInvoices` reports as "its PDF is open in another program" (MainViewModel.cs:407-410), a misleading message. **C06** is two call sites. **C10** is one unbounded read. **C11** and **C22** are workflow and docs edits.

Almost everything can be done and tested in the cloud (Linux). The App project and the UI tests do not run on Linux, but they do **compile** on Linux with `-p:EnableWindowsTargeting=true` (verified: both build with 0 warnings, 0 errors). The baseline `dotnet test tests/PrintInvoicer.Tests` passes 372 tests on Linux today. Only three things need Windows or GitHub: running the UI tests, a real release workflow run, and a manual check that Explorer accepts the new `/select,` argument form.

**Primary recommendation:** Put every new piece of logic in `PrintInvoicer.Core` (checksum is YAML; sanitizer, capped reader, `ProcessStartInfo` builder, unique temp name) so it is tested on Linux, keep the App edits to thin wiring that keeps the existing `// AUDIT:` lines, and compile-check the App with `-p:EnableWindowsTargeting=true`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| SHA-256 checksum of release zip | CI (release.yml, `build` job) | Docs (README, AUDITING) | Hash is computed where the zip is built; humans state it in the notes |
| Legacy-database freeze test | Core tests (`tests/PrintInvoicer.Tests`) | none | `Database` and `PriceSnapshot` are Core; no UI involved |
| Explorer argument building | Core (pure builder returning `ProcessStartInfo`) | App (`Process.Start` + `AUDIT:`) | Builder is pure and Linux-testable; starting the program stays in App |
| Unique `.partial` temp name | Core (`DatabaseBackup`) | none | Backup code is Core |
| Update reply size cap | Core (bounded stream read + JSON parse) | App (`UpdateService.FetchLatestAsync` wires HTTP to it) | Network call stays in App; the untrusted-input handling is testable in Core |
| File-name sanitizing | Core (`SafeFileName`) | App (`InvoicePdf.Write` calls it) | Pure string logic; must not depend on `Path.GetInvalidFileNameChars` so tests behave the same on Linux and Windows |
| QuestPDF license statement | Docs | none | README and AUDITING.md only |

## Standard Stack

No new packages. Everything uses what the repo already has.

| Library | Version | Purpose | Source |
|---------|---------|---------|--------|
| .NET SDK | 10.0.403 | build, test (`$HOME/.dotnet/dotnet` in the cloud box) | `dotnet --version` printed `10.0.403` |
| Microsoft.Data.Sqlite | 10.0.14 | fixture DB creation in the test, backup API | `.claude/CLAUDE.md` stack list; used in existing tests |
| xunit 2.9.3 | 2.9.3 | tests (`[Fact]`, `[Theory]`) | tests/PrintInvoicer.Tests/PrintInvoicer.Tests.csproj |
| System.Text.Json, System.Net.Http, System.Diagnostics.Process | in-box | capped JSON read, HTTP, `ProcessStartInfo` | BCL; no install |
| PyYAML 6.0.3 | present on the box | parse-check the edited release.yml | `python3 -c "import yaml"` printed `6.0.3` |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hand-written sanitizer | `Path.GetInvalidFileNameChars()` alone | On Linux it returns only `0` and `47` (verified), so `\` and `:` tests would fail there and the CI-on-Windows behavior would differ. Use a fixed list. |
| `HttpClient.MaxResponseContentBufferSize` | bounded stream read | The property only caps *buffered* reads and throws an exception when exceeded (Microsoft Learn: "If the size of the response content is greater than the MaxResponseContentBufferSize property, an exception is thrown"; default 2 gigabytes). Using it makes "too big" an `HttpRequestException`, which `FetchAndRememberAsync` treats as a network failure, not as "no update info". A bounded read gives the required result and is testable. |
| Checked-in binary `.db` fixture | SQL script in `Fixtures/` run by the test | A binary file cannot be reviewed in a diff and hides what the schema is. Use a reviewable SQL text file. |

**Package Legitimacy Audit:** Not applicable. This phase installs no external packages. (Packages removed due to SLOP: none. Flagged SUS: none.)

## Re-score findings (code checked this session)

Input for the CONCERNS-TRIAGE.md update (success criterion 1). Each row says what the code showed.

| ID | Still open? | Evidence (read this session) | Proposed triage note |
|----|-------------|------------------------------|----------------------|
| C11 | Yes | `.github/workflows/release.yml` zips at the "Zip PrintInvoicer.exe and Read me.txt" step and attaches only `$zip` in `gh release create $tag $zip --draft ...`. No hash anywhere. | Keep, 3.00. |
| C27 | Partly | PriceSnapshotTests.cs:167-182 `Older_finalized_invoices_are_frozen_on_upgrade` covers one job on a current-schema DB. DatabaseTests.cs:190-226 `Older_database_gets_printed_column` shows the raw-SQL old-schema pattern but has no finalized jobs. | Keep. Note: "A basic upgrade test exists; remaining gap is a real pre-snapshot schema, several jobs, exactly-once proof and literal totals." Cost stays 2. |
| C06 | Yes, 2 sites | SettingsViewModel.cs:186 `new ProcessStartInfo("explorer.exe", $"\"{path}\"")` and ErrorReport.cs:60 `new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")`. The other `Process.Start` sites (UpdateService.cs:121, JobEditorViewModel.cs:502, SettingsViewModel.cs:118, MainViewModel.cs:424, ErrorReport.cs:59) pass the path as `FileName` with no argument string, so they already handle a quote in the path. | Keep, 2.00. Note the five already-safe sites. |
| C09 | Partly | Daily name `$"{Prefix}{now:yyyy-MM-dd}.db"` (DatabaseBackup.cs:24) never equals manual name `$"{Prefix}{now:yyyy-MM-dd-HHmmss}-{label}.db"` (DatabaseBackup.cs:34). Remaining: same-second same-label manual copies share `target + ".partial"` (line 115), and `if (File.Exists(temp)) File.Delete(temp);` (line 116) deletes whatever is there. | Note: "Daily vs manual already distinct by name. Fix remains for same-second manual copies and the blind delete; cost 1." Decision for planner: still fix (recommended, see Open Questions). |
| C10 | Yes | UpdateService.cs:166-169 `Http.GetAsync(url, ct)` then `JsonSerializer.DeserializeAsync<LatestRelease>(await response.Content.ReadAsStreamAsync(ct), ...)`. No cap. | Keep, 2.00. |
| C14 | Yes, wider than described | `Safe(job.Title)` only (InvoicePdf.cs:647-651). `job.InvoiceNumber` goes into the name raw (InvoicePdf.cs:44-50) and is user-typed (JobEditorViewModel.cs:150-160). | Keep, 2.00. Note: "Title is cleaned on Windows only; invoice number is not cleaned at all; no length limit." |
| C22 | Yes | README.md mentions QuestPDF only at line 185 ("WPF UI and QuestPDF invoices"). AUDITING.md:101 says "(Community license; includes native Skia and qpdf libraries)" with no condition. The concern text says the README already lists "Community license"; it does not. | Keep, 2.00. |

## Architecture Patterns

### Data flow for this phase's changes

```
Release path (FIX-01)
  tag v<ver> --> release.yml build job --> zip --> [NEW] Get-FileHash --> <zip>.sha256 (LF, "<hash>  <zip>")
                                   \--> upload-artifact (zip + .sha256) --> draft-release job
                                        --> gh release create <tag> <zip> <zip>.sha256 --draft (hash echoed in draft notes)
  maintainer: reads hash from checksum file, keeps it in the published notes (README step)

Legacy DB path (FIX-02, test only)
  Fixtures/pre-snapshot.sql --> raw SqliteConnection --> temp .db (no price_snapshot / finalized_total)
     --> new Database(path)  [EnsureCreated adds columns; FreezeFinalizedJobs snapshots each finalized job]
     --> assert snapshot != null, FinalizedTotal == literal
     --> change prices, new Database(path) again x2 --> snapshot text identical, totals unchanged

Open folder / file (FIX-03)
  Settings button / error dialog --> App: ShellCommands.Explorer*(path)  [Core, returns ProcessStartInfo with ArgumentList]
     --> App: // AUDIT: Process.Start(info)

Backup (FIX-04)
  Manual() / DailyIfDue() --> Copy(db, target) --> temp = unique "<target>.<guid>.partial"
     --> BackupDatabase(src, temp) --> File.Move(temp, target, overwrite) ; on failure delete own temp

Update check (FIX-05)
  UpdateService.FetchLatestAsync --> GetAsync(ResponseHeadersRead) --> [ContentLength > cap? -> null]
     --> Core: LatestReleaseReply.Read(stream, cap) --> null when over cap (= "no update info")

Invoice names (FIX-06)
  InvoicePdf.Write --> Core SafeFileName.Clean(number), Clean(title) --> Compose(kind prefix, number, title, suffix)
     --> Path.Combine(folder, name) ; final guard: result stays inside folder
```

### Recommended structure (new files)
```
src/PrintInvoicer.Core/
├── Files/SafeFileName.cs          # FIX-06: Clean(), Compose(); namespace PrintInvoicer.Core.Files
├── Files/ShellCommands.cs         # FIX-03: builds ProcessStartInfo (does not start it)
└── Updates/LatestReleaseReply.cs  # FIX-05: capped read + parse of GitHub's reply
tests/PrintInvoicer.Tests/
├── Fixtures/pre-snapshot.sql      # FIX-02: old-schema script (copied to output by the existing Fixtures/** rule)
├── LegacyDatabaseTests.cs         # FIX-02
├── SafeFileNameTests.cs           # FIX-06
├── ShellCommandsTests.cs          # FIX-03
├── LatestReleaseReplyTests.cs     # FIX-05
└── BackupTests.cs                 # FIX-04: add tests to the existing class
```
Folders are PascalCase and match the namespace (CLAUDE.md). New files are one main type per file.

### FIX-01: checksum in release.yml

Add a step in the `build` job right after "Zip PrintInvoicer.exe and Read me.txt" (the zip is `PrintInvoicer-${{ steps.version.outputs.version }}.zip`). `shell: pwsh` is the job default.

```powershell
# Source: PowerShell Get-FileHash (built in); format matches `sha256sum -c`
$zip = "PrintInvoicer-${{ steps.version.outputs.version }}.zip"
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
# LF line ending on purpose: Set-Content would write CRLF on Windows.
[IO.File]::WriteAllText("$zip.sha256", "$hash  $zip`n")
Write-Output "SHA-256 of ${zip}: $hash"
```

- Upload both files: `upload-artifact` `path:` accepts a multi-line list; the existing step has `if-no-files-found: error`, keep it.
- In `draft-release`, attach both: `gh release create $tag $zip "$zip.sha256" --draft ...`. Put the hash line into the draft `$notes` text (read it from the downloaded `.sha256` file) so the notes already contain it, and README step 4 says to keep that line when writing the final notes. Reading the file in the second job avoids passing the hash through job outputs.
- Verified on this box: GNU `sha256sum -c` accepts the `"<hash>  <name>"` form (printed `a.zip: OK`). It also printed OK for a CRLF copy, so CRLF is not fatal here, but write LF anyway.
- Docs to change: README "Making a release" step 3 (mention the checksum file) and step 4 (state the SHA-256 in the release notes); README "Install or update" gets a short "Verify a download" snippet (`Get-FileHash .\PrintInvoicer-<version>.zip -Algorithm SHA256` and compare); AUDITING.md "Building and releasing" release.yml bullet and the "Releases are not code-signed" paragraph. Say plainly that the checksum shows the download is intact and matches the notes; it does not prove who made it, because the file sits next to the zip. `AUDIT:` tag count is unchanged (YAML and docs are outside `src`).
- Cloud verification: `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/release.yml'))"`. The real run needs GitHub (next tag). pwsh is not installed here, so the PowerShell lines cannot be executed in the cloud; keep them to the three-line form above.

### FIX-02: pre-snapshot fixture test

**How `Database` handles it today** (read this session):

- Constructor: `EnsureCreated(); FreezeFinalizedJobs();` (Database.cs:31-32).
- `FreezeFinalizedJobs` (Database.cs:40-56): first clears snapshot and total on drafts, then returns early when `SELECT COUNT(*) FROM jobs WHERE finalized = 1 AND price_snapshot IS NULL` is 0. Otherwise it loads one price book with `LoadPriceBook()` and, for each finalized job with `PriceSnapshot is null`, calls `PriceSnapshot.Freeze(job, book)` and writes `price_snapshot` and `finalized_total`.
- `PriceSnapshot.Freeze` (PriceSnapshot.cs:48-53) returns at once if `job.PriceSnapshot is not null`, so a job is frozen once. The frozen total is `PricingCalculator.Calculate(job, Restore(job.PriceSnapshot, current.Settings)).Total`.
- The snapshot carries `book.RulesVersion`, which defaults to `PricingRules.Current` (= 2, PriceBook.cs:12 and :28). A job that was invoiced under version 1 rules (up to 3.4) is therefore frozen under version 2 rules. This is existing behavior. The fixture must not pin a case where v1 and v2 differ (see Pitfall 5).

**What "before snapshots existed" looks like.** `jobs` has no `price_snapshot` and no `finalized_total` column. The git history is squashed (a single commit "PrintInvoicer 3.2" holds the code), so the exact 3.1 schema cannot be recovered from git. The test code itself says "finalized the 3.1 way: no snapshot" and "Versions before 3.2 revert to draft without knowing about frozen prices". Build the fixture from the CREATE TABLE statements in `EnsureCreated` and leave out the columns the later `AddColumnIfMissing` calls add. Verbatim from Database.cs:87-93 (`jobs`):

```sql
CREATE TABLE IF NOT EXISTS jobs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL, customer TEXT NOT NULL, invoice_number TEXT, created_date TEXT NOT NULL,
    invoice_date TEXT, modeling_hours TEXT NOT NULL, post_processing INTEGER NOT NULL,
    paid INTEGER NOT NULL, paid_date TEXT, notes TEXT NOT NULL,
    printed INTEGER NOT NULL DEFAULT 0, printed_date TEXT,
    finalized INTEGER NOT NULL DEFAULT 0, finalized_date TEXT);
```

That `jobs` definition already lacks `excluded_extra_costs`, `discount_percent`, `price_snapshot` and `finalized_total`, which `AddColumnIfMissing` adds (Database.cs:134-137). It is a safe fixture shape: absent columns get their defaults on open, which is the upgrade path being guarded. Whether `discount_percent` existed before 3.2 is unknown, so the fixture does not claim to be an exact 3.1 database; say "shaped like a pre-snapshot database" in the test summary. Also write `settings`, `filaments`, `printers`, `parts`, `plates`, `plate_filaments` (use the Database.cs:81-122 definitions, keeping the `plates` columns that existed in the original CREATE: through `thumbnail BLOB`, and the filament/printer tables as written). Do **not** insert zero filaments: `EnsureCreated` seeds filaments when the table is empty (Database.cs:161-164), which would add 13 rows. Insert at least one filament so the table is non-empty and seeding does not run.

**Fixture format (recommendation):** `tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql`, executed by the test through a plain `SqliteConnection` (the `Fixtures/**` copy-to-output rule in PrintInvoicer.Tests.csproj already applies). Reason: reviewable text, no binary blob, no personal data check problem, and the test asserts the precondition itself. Use sample names ("Harbor Dive Shop", "Sample Printing Co.").

**Test design (all runs on Linux):**
1. Precondition: query `pragma_table_info('jobs')` on the raw fixture DB and assert `price_snapshot` and `finalized_total` are absent. Without this the test could silently pass against a current-schema file.
2. `new Database(path)`. For each finalized fixture job: `PriceSnapshot` not null, `FinalizedTotal` equals a **hand-derived literal** (not recomputed by calling the calculator in the assertion; derive from the formulas, then pin). Derivation inputs, from PricingCalculator.cs:181-195 and Models: material = grams x (spool price / spool grams) x (1 + waste) x (1 + markup) x copies (`Filament.CostPerGram`, `Printer.MaterialFactor`); hours = seconds / 3600; machine = hours x rate x copies; electricity = hours x watts / 1000 x ElectricityPerKwh x copies; unit = `Money(printCost / qty)`; plus slicing, modeling and post-processing fees (AppSettings defaults: ElectricityPerKwh 0.20, ModelingRatePerHour 50, SlicingFeePerPart 10, PostProcessingFee 20). Choose round numbers so unit price needs no rounding and v1 equals v2.
3. A draft job in the fixture stays `PriceSnapshot == null` and follows later price changes (shows the freeze touches only finalized jobs).
4. Exactly once: store the snapshot strings; raise filament price and a rate with `SaveFilament` / `SaveSettings`; `new Database(path)` two more times; assert each snapshot string is **identical** (`Assert.Equal` on the text) and `PricingCalculator.Calculate(job, PriceSnapshot.For(job, db.LoadPriceBook())).Total` still equals the literal.
5. Optional: count rows `WHERE finalized = 1 AND price_snapshot IS NOT NULL` equals the number of finalized fixture jobs.

Name tests as sentences with underscores (CLAUDE.md), e.g. `Pre_snapshot_database_freezes_each_finalized_job_once`. Put them in a new `LegacyDatabaseTests.cs` (do not grow `PriceSnapshotTests.cs`). Clean up with the same `Dispose` pattern: `SqliteConnection.ClearAllPools(); File.Delete(...)`.

This test changes no production code, so it cannot change a finalized total. It must stay green after Phase 3's FIX-08; it asserts nothing about *how* freezing is recorded.

### FIX-03: Process.Start / Explorer call sites

All `Process.Start` calls in `src` (grep, this session):

| File:line | Call | Argument handling | Status |
|-----------|------|-------------------|--------|
| SettingsViewModel.cs:186 | `new ProcessStartInfo("explorer.exe", $"\"{path}\"")` | string concatenation | **Fix** |
| ErrorReport.cs:60 | `new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")` | string concatenation | **Fix** |
| ErrorReport.cs:59 | `new ProcessStartInfo(IssuesPage)` | URL as FileName | already safe |
| UpdateService.cs:121 | `new ProcessStartInfo(url)` | URL as FileName | already safe |
| JobEditorViewModel.cs:502 | `new ProcessStartInfo(path)` | path as FileName | already safe |
| MainViewModel.cs:424 | `new ProcessStartInfo(w.Path)` | path as FileName | already safe |
| SettingsViewModel.cs:118 | `new ProcessStartInfo(exe)` | exe as FileName | already safe (FIX-09 territory) |

**`ArgumentList` works with `UseShellExecute = true`.** The .NET source builds the string with `startInfo.BuildArguments()` and passes it as `lpParameters` to ShellExecuteEx (SafeProcessHandle.Windows.cs); `BuildArguments` uses the argument list when it has items, else `Arguments` (ProcessStartInfo.cs). Microsoft Learn: "Strings added to the list don't need to be previously escaped", and `ArgumentList` and `Arguments` cannot both be used. [CITED: learn.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.argumentlist]

**Verified on Linux this session:** a `ProcessStartInfo("explorer.exe") { UseShellExecute = true }` with `ArgumentList.Add("/select,")` and `ArgumentList.Add("C:\a \"b\c.txt")` reports `ArgumentList.Count == 2` and `Arguments.Length == 0`. So a Linux test can assert the exact list.

**Recommended shape (Core, pure):**

```csharp
// Source: pattern from ProcessStartInfo.ArgumentList docs; builds only, never starts.
public static ProcessStartInfo ShowFolder(string path)
{
    var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
    info.ArgumentList.Add(path);
    return info;
}

public static ProcessStartInfo ShowFile(string path)
{
    var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
    info.ArgumentList.Add("/select,");
    info.ArgumentList.Add(path);
    return info;
}
```

The two App call sites become `Process.Start(ShellCommands.ShowFolder(path))` and `Process.Start(ShellCommands.ShowFile(path))`, each keeping its existing `// AUDIT: opens a program.` comment (SettingsViewModel.cs:181 and ErrorReport.cs:57-58 comment blocks stay). Building a `ProcessStartInfo` does not start anything, so the builder needs no `AUDIT:` tag; the tag count stays 26. If the planner prefers a tag on the builder for traceability, add one and bump the count in AUDITING.md.

Tests (Linux, `ShellCommandsTests.cs`): path with a quote, a space, a trailing backslash, and a leading `/` each appear as exactly one `ArgumentList` item equal to the input; `Arguments` is empty; `FileName` is `explorer.exe`; `ShowFile` has `/select,` first. Sample path: `C:\Users\Sample\Harbor Dive Shop "draft"\notes.txt` (no real names).

**Windows manual check (end of phase):** `explorer.exe /select, "C:\path with spaces\file.txt"` (two arguments, space after the comma) is the documented form people use, but this session could not run Explorer. Verify once on Windows by forcing the error dialog or calling the builder from a scratch test, and confirm the file is highlighted. If it does not select, fall back to a single argument `"/select," + path` (ArgumentList then quotes the whole argument). `[ASSUMED]` A2.

Note on "a path containing a quote": `"` is not a legal character in a Windows file or folder name, so such a path can only come from a typed Settings folder text box. The point of the fix is that the path stays a single argument and cannot add switches, not that Explorer can open an impossible folder.

### FIX-04: backup `.partial` handling

Current code (DatabaseBackup.cs:112-122): `string temp = target + ".partial"; if (File.Exists(temp)) File.Delete(temp);` then `BackupDatabase` into `temp`, then `File.Move(temp, target, overwrite: true)` (tagged `AUDIT: file write`). The delete at line 116 has **no** `AUDIT:` tag although AUDITING.md already describes it ("a leftover half-written `.partial` copy deleted before retrying").

Callers: `App.BackUpTodayAsync` runs `DailyIfDue` on `Task.Run` (App.xaml.cs:24-37); `SettingsViewModel.BackUpNow` calls `Manual` on the UI thread (SettingsViewModel.cs:63-76); `Restore` calls `Manual(..., "before-restore")` (DatabaseBackup.cs:69).

**Recommended change:** in `Copy`, name the temp file uniquely, e.g. `target + "." + Guid.NewGuid().ToString("N") + ".partial"`, and delete only *your own* temp on failure:

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
    // AUDIT: file delete. Removes this copy's own half-written .partial file after a failure.
    try { File.Delete(temp); } catch (Exception d) when (d is IOException or UnauthorizedAccessException) { }
    throw;
}
```

- `List` uses the pattern `printinvoicer-*.db` (DatabaseBackup.cs:42) and `DailyName` is anchored on `.db$`, so `...db.<guid>.partial` is never listed or pruned as a backup.
- The existing `One_daily_backup_a_day` test already asserts `Assert.Empty(Directory.GetFiles(Folder, "*.partial"))`.
- Cleanup of a `.partial` left by a killed process is not handled (it was, accidentally, before, because the next run of the same name deleted it). Accept this, or sweep `*.partial` files older than a day in `Prune`; state the choice in the plan. Do not widen scope beyond that.
- Close the AUDIT gap: the new delete line gets a tag, AUDITING.md's Backups row is reworded ("each copy is written to its own uniquely named `.partial` file and moved into place; a copy that fails deletes its own `.partial`"), and the tag count in AUDITING.md ("There are 26 tags") becomes the new `git grep -c` total (27 if one tag is added).

**Tests (Linux, add to `BackupTests.cs`, which already has `DbWithJob`, `Folder`, `DbPath`):**
- Two backups to the *same* target (same `now` and label, called twice in a row) both succeed, leave no `*.partial` file, and the result opens as a valid database. This is the real double-click case.
- A daily and a manual backup run concurrently (`Parallel.Invoke`, repeat ~20 times) both succeed and leave no `.partial`. Do **not** make several threads write the *same* target concurrently: `File.Move(..., overwrite: true)` onto a target being replaced at the same instant can fail on Windows, and CI runs these tests on Windows too (flaky risk).
- Expose a small public helper if a direct unique-name assertion is wanted (`TempPathFor(target)`); there is no `InternalsVisibleTo` from Core to the tests and CLAUDE.md says not to add entries without a reason.

### FIX-05: capped update-check read

Current code (UpdateService.cs:162-171): `Http.GetAsync($".../releases/latest", ct)` (default `HttpCompletionOption.ResponseContentRead`, so the whole body is buffered, up to the 2 GB default), `EnsureSuccessStatusCode`, then `JsonSerializer.DeserializeAsync<LatestRelease>(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct)`. `HttpClient` is created in `CreateClient` (lines 153-159) with no `AutomaticDecompression`, so no compressed-body expansion applies.

**Recommended:**
1. In App: `Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)`. If `response.Content.Headers.ContentLength > cap`, return `null` without reading.
2. Read through a Core helper that never reads more than `cap + 1` bytes: reads into a `MemoryStream` in chunks and returns "too big" when the count passes `cap`. Then parse with `JsonSerializer.Deserialize<LatestRelease>`.
3. Over the cap means `null` from `FetchLatestAsync`, which `FetchAndRememberAsync` already treats as "no update info" (same path as the 404 branch at line 167). A malformed body keeps today's behavior (`JsonException` is caught at line 134 and falls back to the last known answer).

```csharp
// Core. Source: standard bounded-read pattern (read at most cap + 1 bytes).
public const int MaxBytes = 256 * 1024;

public static async Task<(string? Tag, string? Url)?> ReadAsync(Stream body, CancellationToken ct, int maxBytes = MaxBytes)
{
    using var buffer = new MemoryStream();
    var chunk = new byte[8192];
    int n;
    while ((n = await body.ReadAsync(chunk, ct)) > 0)
    {
        if (buffer.Length + n > maxBytes) return null;   // over the cap: no update info
        buffer.Write(chunk, 0, n);
    }
    buffer.Position = 0;
    // parse tag_name and html_url from the JSON; a malformed body throws JsonException as before
}
```

(`tag_name` and `html_url` are the two JSON property names already used in `LatestRelease`, UpdateService.cs:146-147.)

The cap is 256 KB, as the triage and CONCERNS.md propose. `[ASSUMED]` A3: a normal reply for this repository is a few KB, but GitHub allows long release notes, so a very long note could exceed 256 KB; the failure mode is safe (no update bar), and the constant is easy to raise.

**Testability.** `UpdateService` is in the Windows-only App project. The existing seam is the `fetch` delegate on the constructor (`Func<CancellationToken, Task<ReleaseInfo?>>`, UpdateService.cs:38), used by ScreenTests.cs:192; it sits *above* the HTTP read, so it cannot test the cap. Putting the bounded read in Core lets `LatestReleaseReplyTests.cs` run on Linux with `MemoryStream` and a custom stream that yields endless bytes (assert it stops and returns null after about `cap` bytes, never reading to the end). Tests: reply exactly at the cap parses; one byte over returns null; endless stream returns null without hanging; valid sample JSON gives tag and URL; malformed JSON throws `JsonException`; missing fields give null parts. The App wiring is 5 lines and compile-checked on Linux; optionally add a UiTest only if a fake `HttpMessageHandler` is introduced, which is not required.

AUDITING.md Network section: add "The reply is read through a 256 KB limit; a longer reply is ignored." The existing `// AUDIT: network` comment stays above the `GetAsync` line.

### FIX-06: invoice file names

Where names are built: only `InvoicePdf.Write` (InvoicePdf.cs:37-57). `MainViewModel.GenerateInvoices` (MainViewModel.cs:386-438) and `JobEditorViewModel` (lines 440, 465, 497) call `Write` and use the returned path (`Path.GetFileNameWithoutExtension(written[0].Path)` as the print job name). There is no other site: the folder is `PreviewFolder`, `DefaultFolder`, or the Settings `InvoiceFolder`; none is built from job data. `InvoiceFolder` is a full path the user chose on purpose, so it must **not** be sanitized (that would break `D:\Invoices`); handle a bad folder with the existing error path instead.

Verbatim current naming (InvoicePdf.cs:44-51):

```csharp
string number = job.InvoiceNumber ?? $"draft {job.CreatedDate:yyyy-MM-dd}";
string name = kind switch
{
    InvoiceKind.Preview => $"Preview {number} {Safe(job.Title)}.pdf",
    InvoiceKind.Internal => $"Invoice {number} {Safe(job.Title)} (detail).pdf",
    _ => $"Invoice {number} {Safe(job.Title)}.pdf",
};
string path = Path.Combine(folder, name);
```

and `Safe` (InvoicePdf.cs:647-651): `Path.GetInvalidFileNameChars()` replaced with `-`, then `.Trim()`.

Why this is not enough (reasoned from the code, not run on Windows):
- `number` is never cleaned. `x\..\..\y` as an invoice number: .NET file APIs normalize `..` before the OS call, so the PDF lands outside the invoice folder. The invoice number can also come from the Settings prefix (`AppSettings.InvoicePrefix`, `FormatInvoiceNumber`, AppSettings.cs:46-49).
- No length limit; a too-long path throws `PathTooLongException` (an `IOException`), shown to the user as "open in another program" (MainViewModel.cs:407-410).
- On Linux (and in any Linux test) `Path.GetInvalidFileNameChars()` is `{0, 47}` only, so a test for `\` and `:` cannot pass using it.
- Trailing dots and spaces are silently stripped by Windows, and device names (`CON`, `NUL`, ...) are reserved. Today the fixed `Invoice ` / `Preview ` prefix hides the device-name risk, but the helper must still handle it because the requirement tests it and future callers may not have a prefix.

**Recommended API (Core, `PrintInvoicer.Core.Files.SafeFileName`):**
- `Clean(string? text, int maxLength)`: replace the fixed set `< > : " / \ | ? *` and all control characters (below 0x20, and 0x7F) with `-`; collapse `..` runs; trim spaces and dots from both ends; if the result is empty return a fallback such as `untitled`; if the leading segment (before the first `.`) is a reserved device name (case-insensitive: CON, PRN, AUX, NUL, COM0-COM9, LPT0-LPT9, plus CONIN$ and CONOUT$) prefix it with `_`; truncate to `maxLength` without splitting a surrogate pair, then trim trailing spaces and dots again.
- `Compose(string prefix, string? number, string? title, string suffix)` (or similar): cleans `number` and `title` separately (shorter limit for each), joins them, then guarantees the whole file name is at most about 120 characters including the extension and never ends with a space or dot, and re-checks the reserved-name rule on the stem. `InvoicePdf.Write` calls this and keeps `Path.Combine(folder, name)`.
- Defense in depth in `Write`: after `Path.Combine`, assert `Path.GetFullPath(path)` starts with `Path.GetFullPath(folder)` plus a separator, else throw `InvalidDataException`/use the safe fallback name. Keep it cheap.
- Keep `InvoicePdf.Safe` removed or delegating to `Clean`; do not leave two sanitizers.

Numbers (60 for title, 120 for the whole name): `[ASSUMED]` A1. NTFS allows 255 characters per name component and classic MAX_PATH is 260 for the full path, and the invoice folder is typically about 50 characters, so 120 for the name leaves room. These are discretionary; the planner may pick other values but must state them in the plan and test them.

Tests (Linux, `SafeFileNameTests.cs`, `[Theory]` with `InlineData`): `..`, `...`, `..\..\evil`, `a/b`, `C:\x`, `report:2026`, `a<b>c`, `con`, `CON.txt`, `Com1`, `lpt9 `, `NUL`, `aux.pdf`, trailing `.` and spaces, empty and whitespace, a control character, 1000-character input (length <= limit), a long title ending in an emoji at the cut point (no lone surrogate). Property checks for every case: result contains none of the forbidden characters, equals itself after `Path.GetFileName` with both separators checked by hand, is not empty, and is not a reserved name. A Windows-only UiTest is optional: `InvoicePdf.Write` with a hostile title and number returns a path whose directory equals the invoice folder (extend near ScreenTests.cs:180, which asserts `Assert.StartsWith(jobBook.Settings.InvoiceFolder, InvoicePdf.Write(...))`; that assertion must still pass).

AUDITING.md: the "Files written" row for `Documents\PrintInvoicer\Invoices\` gains "File names are cleaned and length-limited (`SafeFileName`)". The `AUDIT: file write` comment on `GeneratePdf(path)` stays.

### FIX-07: QuestPDF Community license text

Verified on the official pages this session (fetched summaries; the PDF "License Terms" was not opened):
- "QuestPDF is free for individuals and businesses with annual gross revenue under USD 1,000,000"; organisations: "annual gross revenue under USD 1,000,000 in its most recently completed fiscal year", measured "on a consolidated basis across entities under common control"; individuals use their own annual gross revenue from all sources. [CITED: questpdf.com/license/community.html, questpdf.com/license/guide.html]
- Also listed as eligible: charitable organisations, academic institutions, open-source projects. "Public-sector entities and publicly traded companies are not eligible, regardless of revenue." [CITED: questpdf.com/license/community.html]
- If you stop qualifying you must buy the appropriate paid license or stop using the software, within a 90-day transition period. For software built for a third party, the license needed is decided by the entity that ultimately owns, controls, commercially uses, or benefits from it. Distributing it inside your application counts as production use. [CITED: questpdf.com/license/community.html, questpdf.com/license/guide.html]
- The app sets the license at startup: App.xaml.cs:45 `QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;`.

Write in plain English, no em dashes. Suggested content: README gets a short "QuestPDF license" paragraph (put it in the existing `## License` section at the end; README.md:239-241 currently says only "MIT. See [LICENSE](LICENSE)."): the MIT license covers this project's own code; the invoice layout uses QuestPDF under its Community license, which applies only if the person or organisation using PrintInvoicer has less than USD 1,000,000 annual gross revenue (and is not a public-sector entity or a publicly traded company); above that, they need a paid QuestPDF license and are responsible for getting one. Link the QuestPDF license page. AUDITING.md: expand the QuestPDF row (line 101) with the same condition and a pointer to the line that sets `LicenseType.Community`. Because the terms are on a web page that can change, say "as stated by QuestPDF when this was written; check their page" and give the date. `[ASSUMED]` A4: the web terms match what applies to QuestPDF 2026.9.1; the NuGet package's own license text was not opened.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Escaping arguments for Explorer | quote-doubling code | `ProcessStartInfo.ArgumentList` | The runtime escapes correctly; hand quoting is the bug being fixed |
| Hashing the zip | custom hashing in a script | PowerShell `Get-FileHash -Algorithm SHA256` | Built in, on every Windows runner |
| JSON parsing of the reply | string search | `System.Text.Json` | Already used by `UpdateService` |
| Unique temp names | timestamp or counter scheme | `Guid.NewGuid().ToString("N")` | No coordination between threads or processes |
| SQLite backup | file copy | `SqliteConnection.BackupDatabase` (already used) | Consistent while the app has the DB open |
| Invalid-character list | `Path.GetInvalidFileNameChars()` alone | fixed list in `SafeFileName` | OS-dependent (Linux returns 2 chars) |

**Key insight:** every one of these is a "looks easy, differs by OS or by input" problem. Keep the logic in Core with explicit tables, test it on Linux, and keep App code to wiring.

## Common Pitfalls

### Pitfall 1: Fixture test that silently passes on a current schema
**What goes wrong:** the test creates a current-schema DB (like `Older_finalized_invoices_are_frozen_on_upgrade`) and proves nothing about the upgrade path.
**How to avoid:** assert `price_snapshot` and `finalized_total` are absent via `pragma_table_info('jobs')` before opening with `Database`.

### Pitfall 2: Seed data appears in the fixture
**What goes wrong:** an empty `filaments` table makes `EnsureCreated` insert 13 filaments (Database.cs:161-164, and `Seeds_filaments_once` expects 13), changing the price book.
**How to avoid:** insert at least one filament in the fixture.

### Pitfall 3: "Exactly once" proven only by total
**What goes wrong:** the total matches because prices did not change between opens.
**How to avoid:** change prices, reopen, and compare the snapshot **text** to the first one.

### Pitfall 4: Test that depends on today's date
**What goes wrong:** `EnsureCreated` writes `NextInvoiceYear` from `DateTime.Today` when a `NextInvoiceNumber` setting exists (Database.cs:157-159). Fixture settings with that key make the test depend on the run date.
**How to avoid:** leave `NextInvoiceNumber` out of the fixture settings, and use fixed dates in `created_date` / `finalized_date` / `invoice_date` (`yyyy-MM-dd`).

### Pitfall 5: Pinning a rule-version quirk
**What goes wrong:** freezing a legacy job uses the current rules (version 2), not version 1 under which it may have been invoiced. A fixture where v1 and v2 give different cents would pin that behavior as "expected".
**How to avoid:** choose values where rounding does not matter, and note the risk in the plan for Phase 3 (FIX-08) and the v2 backlog. Do not change `FreezeFinalizedJobs` in this phase.

### Pitfall 6: Flaky concurrent backup test on Windows
**What goes wrong:** several threads moving different temps onto the same target can fail on Windows.
**How to avoid:** test same-target copies in sequence, and concurrency only across different targets.

### Pitfall 7: Misleading error on a bad path
**What goes wrong:** `PathTooLongException` and `DirectoryNotFoundException` are `IOException`, so `GenerateInvoices` tells the user "its PDF is open in another program".
**How to avoid:** length-limit and sanitize so those cannot come from job data. Changing the message text is outside this phase.

### Pitfall 8: Em dashes and real names in new docs, tests and comments
**What goes wrong:** violates CLAUDE.md. Use colons, commas or new sentences; use only sample names.
**How to avoid:** `grep -n $'\xe2\x80\x94\|\xe2\x80\x93'` on every edited file before finishing.

### Pitfall 9: Wrong exception types in new Core helpers
`LatestReleaseReply` parses untrusted network data: range-check, cap, and let only the documented `JsonException` out (the caller already filters it). `SafeFileName` should never throw for any string input, including null.

## Code Examples

The fixture skeleton uses only table and column names quoted above from Database.cs:81-122.

```csharp
// FIX-02 precondition and exactly-once check (names from Database.cs and PriceSnapshot.cs read this session)
using var c = new SqliteConnection($"Data Source={_path}");
c.Open();
// run Fixtures/pre-snapshot.sql here, then:
// SELECT COUNT(*) FROM pragma_table_info('jobs') WHERE name IN ('price_snapshot', 'finalized_total')  -> must be 0

var db = new Database(_path);                                   // freezes each finalized job
var first = db.GetJobs().Where(j => j.Finalized).ToDictionary(j => j.Id, j => j.PriceSnapshot);
// ... change a filament price with db.SaveFilament, a rate with db.SaveSettings ...
_ = new Database(_path);
var again = new Database(_path).GetJobs().Where(j => j.Finalized).ToDictionary(j => j.Id, j => j.PriceSnapshot);
Assert.Equal(first, again);                                     // snapshot text unchanged: frozen exactly once
```

Release verification line for the README (PowerShell, built in): `Get-FileHash .\PrintInvoicer-<version>.zip -Algorithm SHA256`; compare with the hash in the release notes or in `PrintInvoicer-<version>.zip.sha256`.

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| `Arguments` string with hand quotes | `ArgumentList` (since .NET Core 2.1) | runtime escapes; works with `UseShellExecute` on Windows |
| Buffer the whole HTTP body | `ResponseHeadersRead` + bounded stream read | memory cap and explicit "too big" result |

**Deprecated/outdated:** none relevant.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Title limit 60 characters and whole file name limit 120 are safe on Windows (NTFS 255-character component, MAX_PATH 260 for the full path) | FIX-06 | Names too long or needlessly short; planner can change numbers |
| A2 | `explorer.exe /select,` followed by the path as a second argument (`/select, "path"`) highlights the file | FIX-03 | Explorer opens the wrong place; use the single-argument form `"/select," + path`. Needs a manual check on Windows |
| A3 | 256 KB is enough for GitHub's latest-release reply for this repository | FIX-05 | A very long release note would hide the update bar; raise the constant |
| A4 | QuestPDF's web license terms are the ones that apply to package 2026.9.1 | FIX-07 | Doc wording slightly off; word it as "check their page" with a date |
| A5 | The pre-snapshot schema can be approximated by the original CREATE TABLE statements minus later `AddColumnIfMissing` columns (git history is squashed, so the real 3.1 schema cannot be recovered) | FIX-02 | Fixture is not an exact 3.1 file; the test still guards the upgrade path, so word it as "shaped like" |
| A6 | Leaving an orphaned `.partial` after a killed process is acceptable | FIX-04 | A stray file in the backup folder; optional sweep in `Prune` |

## Open Questions (RESOLVED)

All three were resolved during planning (2026-11-02) by following the recommendation; each line names the plan task
that applies it.

1. **C09: re-score as handled, or fix anyway?** RESOLVED: fix anyway, and record the partial handling.
   - What we know: the daily and manual names always differ; the remaining collision is two manual copies in the same second with the same label, plus the unlabeled blind delete.
   - What's unclear: whether the user wants the requirement kept given it is mostly handled.
   - Recommendation: still fix. It is about 10 lines, closes the double-click case, adds the missing `AUDIT:` tag, and FIX-04 is a listed requirement. Record the "already distinct" finding in CONCERNS-TRIAGE.md.
   - Resolution: plan 02-03 Task 3 makes the fix (a `.partial` file of its own per copy, the tagged own-file delete, tag count 27); plan 02-01 Task 3 writes the C09 "Partly" row in CONCERNS-TRIAGE.md saying the daily and manual names were already distinct. The decision is recorded in 02-03 ("Decision recorded").
2. **FIX-05: what should "no update info" do to the remembered state?** RESOLVED: an over-cap reply returns `null`, exactly like a 404.
   - What we know: returning `null` saves `LatestTag = null` with the check time (UpdateService.cs:139), same as a 404, and a manual check then says "You have the latest version".
   - What's unclear: whether an over-cap reply should instead keep the previous answer (like a network failure).
   - Recommendation: return `null` as the requirement says ("no update info"); it is the simplest and consistent with 404.
   - Resolution: plan 02-03 Task 1 (`LatestReleaseReply.ReadAsync` returns null past `MaxBytes`, and `FetchLatestAsync` returns null when Content-Length is over the cap), pinned by the 02-03 backstop truth "An over-cap reply takes the same path as a 404" and the boundary facts in 02-03 Tasks 1 and 2.
3. **Legacy freeze uses rules version 2.** RESOLVED: not changed in this phase; flagged for Phase 3.
   - Out of scope here, but flag for Phase 3 / FIX-08 that a v1-era invoice is frozen at v2 (Pitfall 5).
   - Resolution: plan 02-01 Task 3 writes the "Note for C01 (Phase 3, FIX-08)" paragraph in CONCERNS-TRIAGE.md, and plan 02-01 Tasks 1 and 2 use fixture values where rules versions 1 and 2 give the same cents, so the new test pins no rules quirk. `FreezeFinalizedJobs` is not touched (02-01 gate on `src/PrintInvoicer.Core/Data/Database.cs`).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build, Core tests | yes | 10.0.403 at `$HOME/.dotnet/dotnet` (not on PATH) | none needed |
| Core + unit tests on Linux | FIX-02/03/04/05/06 | yes | 372 pass today | none |
| App project compile on Linux | wiring checks | yes with `-p:EnableWindowsTargeting=true` | builds with 0 warnings | none |
| UiTests compile on Linux | wiring checks | yes with the same flag | builds | cannot run; Windows CI runs them |
| UiTests run, WPF windows | screen tests | no (Windows desktop) | none | GitHub CI `screen-tests` job on windows-latest |
| PowerShell (pwsh) | run release.yml snippet | no | none | review by reading; first real tag run proves it |
| PyYAML | YAML parse check | yes | 6.0.3 | none |
| `sha256sum` | check checksum file format | yes | coreutils | none |
| actionlint / yamllint | workflow lint | no | none | PyYAML parse only |
| Network to questpdf.com, learn.microsoft.com | docs facts | yes (via proxy) | n/a | n/a |

**Missing dependencies with no fallback:** none. **With fallback:** pwsh and the UI test run (rely on Windows CI and the end-of-phase manual check).

## Validation Architecture

`workflow.nyquist_validation` is `false` in `.planning/config.json`, so this section is not required; it is included because the orchestrator asked for concrete commands.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xunit 2.9.3 on net10.0 |
| Config file | tests/PrintInvoicer.Tests/PrintInvoicer.Tests.csproj (Fixtures copied to output) |
| Quick run command | `$HOME/.dotnet/dotnet test tests/PrintInvoicer.Tests --nologo --filter "FullyQualifiedName~LegacyDatabaseTests"` (swap the class name per item) |
| Full suite command | `$HOME/.dotnet/dotnet test tests/PrintInvoicer.Tests --nologo` (372 pass before this phase) |
| App compile check (Linux) | `$HOME/.dotnet/dotnet build src/PrintInvoicer.App -p:EnableWindowsTargeting=true --nologo -v q` |
| UiTests compile check (Linux) | `$HOME/.dotnet/dotnet build tests/PrintInvoicer.UiTests -p:EnableWindowsTargeting=true --nologo -v q` |
| Windows full run | `dotnet test PrintInvoicer.sln` (CI job `screen-tests` runs the UI tests) |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TRIA-01 | re-score notes present | doc check | `grep -n "C09\|C27\|C14" .planning/CONCERNS-TRIAGE.md` | edit existing |
| FIX-01 | workflow parses; checksum step and `.sha256` attached | static | `python3 -c "import yaml;yaml.safe_load(open('.github/workflows/release.yml'))"` then `grep -n "sha256\|Get-FileHash" .github/workflows/release.yml README.md AUDITING.md` | edit existing |
| FIX-02 | each finalized job snapshotted once; literal totals | unit | `dotnet test tests/PrintInvoicer.Tests --filter "FullyQualifiedName~LegacyDatabaseTests"` | Wave 0 (new) |
| FIX-03 | path with quote is one argument | unit | `--filter "FullyQualifiedName~ShellCommandsTests"` | Wave 0 (new) |
| FIX-04 | unique temp, no `.partial` left, same-second manual copies work | unit | `--filter "FullyQualifiedName~BackupTests"` | edit existing |
| FIX-05 | over-cap reply gives null; at cap parses | unit | `--filter "FullyQualifiedName~LatestReleaseReplyTests"` | Wave 0 (new) |
| FIX-06 | `..`, `\`, `:`, `CON` cleaned and length-limited | unit | `--filter "FullyQualifiedName~SafeFileNameTests"` | Wave 0 (new) |
| FIX-07 | README and AUDITING.md state USD 1,000,000 condition | doc check | `grep -n "1,000,000" README.md AUDITING.md` | edit existing |
| all | no em dashes or real names introduced | static | `git diff -U0 \| grep -n $'\xe2\x80\x94\|\xe2\x80\x93'` should print nothing | n/a |
| all | AUDIT tag count matches AUDITING.md | static | `git grep -c "AUDIT:" -- src` summed equals the number in AUDITING.md ("There are N tags") | n/a |

### Sampling Rate
- **Per task commit:** the single class filter for that item, plus the App compile check when App files changed.
- **Per wave merge:** full Linux unit suite plus both compile checks.
- **Phase gate:** full Linux suite green; Windows CI (build, unit, screen tests) green; manual Windows checks below.

### Wave 0 Gaps
- [ ] `tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql` and `LegacyDatabaseTests.cs` (FIX-02)
- [ ] `ShellCommandsTests.cs`, `LatestReleaseReplyTests.cs`, `SafeFileNameTests.cs`
- [ ] No framework install needed.

### Manual (Windows or GitHub) checks, for end-of-phase human verification
1. Open Settings, click the Open data folder button; Explorer opens the folder.
2. Trigger the error dialog path (or call the builder in a scratch run) and confirm Explorer highlights the saved error file (A2).
3. Create a draft job whose title is `..\evil:CON` and invoice number `x\..\..\y`; Preview and Detail PDF open from the previews folder with safe names.
4. Push the next version tag and confirm the draft release has the zip, the `.sha256` file, and the hash in the notes; compare with `Get-FileHash`.
5. CI `screen-tests` job passes (UpdateService bar test uses the `fetch` seam and must be unaffected).

## Security Domain

`security_enforcement` is on (ASVS level 1, block on high).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | single-user desktop app |
| V3 Session Management | no | none |
| V4 Access Control | no | local files only |
| V5 Input Validation | yes | `SafeFileName` (invoice names), 256 KB cap on the update reply, `ArgumentList` for process arguments |
| V6 Cryptography | partly | SHA-256 via `Get-FileHash` for release integrity; no custom crypto |
| V12 Files and Resources | yes | path stays inside the invoice folder; unique temp files for backups |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Path traversal through user-typed invoice number or title | Tampering | `SafeFileName.Clean` plus a final "inside the folder" check |
| Reserved device name as file name | Denial of service | reserved-name rule in `SafeFileName` |
| Argument injection into `explorer.exe` | Tampering / Elevation | `ArgumentList` keeps the path as one argument |
| Oversized or hostile API reply | Denial of service | bounded read, `ResponseHeadersRead`, cap, 10 s timeout already present |
| Backup temp file collision or clobber | Tampering | GUID temp name, delete only own temp |
| Tampered release download | Spoofing | SHA-256 in the notes (integrity only, not origin; stated in docs; code signing out of scope) |

## Plan split recommendation

Score order is C11, C27, then the 2.0 group. The `AUDITING.md` and `README.md` files are touched by several items, so plans that edit them must not run in the same wave (plan-checker flags file overlap). Suggested split:

| Plan | Items | Where it can run | Files touched | Verification |
|------|-------|------------------|---------------|--------------|
| 02-01 | FIX-02 (tracer, test only) + TRIA-01 re-score notes for all seven items | Cloud (Linux) | `tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql`, `LegacyDatabaseTests.cs`, `.planning/CONCERNS-TRIAGE.md` | Core tests |
| 02-02 | FIX-01 (release.yml checksum, README steps, AUDITING.md) + FIX-07 (README and AUDITING.md license text) | Cloud (YAML and docs); real run needs GitHub | `.github/workflows/release.yml`, `README.md`, `AUDITING.md` | PyYAML parse, grep, next tag run |
| 02-03 | FIX-04 (backup temp) + FIX-05 (capped read, `UpdateService` wiring) | Cloud (Core + tests); App wiring compile-checked on Linux | `DatabaseBackup.cs`, `BackupTests.cs`, `Updates/LatestReleaseReply.cs`, its tests, `UpdateService.cs`, `AUDITING.md` | Core tests, App compile check |
| 02-04 | FIX-03 (`ShellCommands`, 2 call sites) + FIX-06 (`SafeFileName`, `InvoicePdf.Write`) | Cloud (Core + tests); App wiring compile-checked on Linux | `Files/ShellCommands.cs`, `Files/SafeFileName.cs`, tests, `SettingsViewModel.cs`, `ErrorReport.cs`, `InvoicePdf.cs`, `AUDITING.md` | Core tests, App + UiTests compile check; Windows manual checks |

Waves: 02-01 and 02-02 share no files and can run in parallel (Wave 1). 02-03 and 02-04 both edit `AUDITING.md` and run after 02-02: put them in Wave 2 and Wave 3, or have each leave `AUDITING.md` to one final docs task. If the planner wants a smaller plan count, merge 02-03 and 02-04 only if the AUDITING edits are kept to separate sections and executed sequentially in one plan.

All four plans are cloud-runnable for their code and tests. Windows is needed only for: running UI tests (CI does it), the real release workflow (next tag), and manual check A2. Because `human_verify_mode` is `end-of-phase`, collect those three checks into one end-of-phase checklist rather than blocking each plan.

## Project Constraints (from CLAUDE.md)

- Finalized invoices never change; any totals change goes behind a new `PricingRules` version and old totals must still match. This phase changes no pricing code.
- Schema changes only via `AddColumnIfMissing` / `CREATE TABLE IF NOT EXISTS` in `Database.EnsureCreated`. This phase makes none. The fixture SQL builds an *old* schema in a temp file only.
- Outside access (network, process start, print, file read/write/delete) gets an `// AUDIT:` comment directly above it and a row in `AUDITING.md`. Update the tag count ("There are 26 tags", verified 26 by `git grep -n "AUDIT:" -- src`).
- Comments: purpose comments on classes, methods and non-obvious blocks; not on plain lines. Plain English, no em dashes or en dashes.
- Core must not reference WPF or App. Pricing rules stay in `PrintInvoicer.Core.Pricing`.
- File readers (and network readers) treat input as untrusted: range-check, cap sizes, throw `InvalidDataException` with a readable message, never other types.
- Catch with `when` filters; broad catch only at the outermost boundary.
- Test names are sentences with underscores, never `ShouldX`/`Given_When_Then`. Money is `decimal`.
- UI tests offscreen only via `WpfHost`; never show, focus or trigger a dialog.
- Sample names only ("Harbor Dive Shop", "Sample Printing Co."); no personal data.
- Private fields `_camelCase`, constants PascalCase, file-scoped namespaces, one main type per file, Allman braces, four spaces.
- `.claude/CLAUDE.md` GSD enforcement: edits go through a GSD workflow (this research writes only the RESEARCH.md; the orchestrator commits).

## Sources

### Primary (HIGH confidence)
- Repository files read this session: `Database.cs` (1-260, 636-700), `DatabaseBackup.cs`, `PriceSnapshot.cs`, `PriceBook.cs`, `PricingCalculator.cs`, `UpdateService.cs`, `ErrorReport.cs`, `SettingsViewModel.cs` (60-210), `InvoicePdf.cs` (1-90, 625-652), `MainViewModel.cs` (380-446), `JobEditorViewModel.cs` (145-165, 480-534), `PriceSnapshotTests.cs`, `DatabaseTests.cs` (1-30, 185-230), `BackupTests.cs`, `release.yml`, `ci.yml`, `README.md` (headings, 20-82, 205-241), `AUDITING.md`, `AppSettings.cs`, `App.xaml.cs`
- Commands run: `dotnet test tests/PrintInvoicer.Tests` (372 passed), `dotnet build` of App and UiTests with `-p:EnableWindowsTargeting=true` (succeeded), a scratch console run (invalid filename chars `0,47` on Linux; `ArgumentList` count 2 with empty `Arguments`), `sha256sum -c` format check, `git grep` of `AUDIT:` (26)
- [CITED: learn.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.argumentlist] - ArgumentList escaping and exclusivity with Arguments
- [CITED: learn.microsoft.com/dotnet/api/system.net.http.httpclient.maxresponsecontentbuffersize] - buffered cap, exception on exceed, 2 GB default
- [CITED: github.com/dotnet/runtime SafeProcessHandle.Windows.cs, ProcessStartInfo.cs (via raw fetch)] - ShellExecute path uses `BuildArguments()`

### Secondary (MEDIUM confidence)
- [CITED: questpdf.com/license/community.html and questpdf.com/license/guide.html] - fetched through a summarizing tool, not read as raw text; the PDF terms were not opened

### Tertiary (LOW confidence)
- Windows file-name limits and the Explorer `/select,` argument form are from general knowledge (A1, A2)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH, no new packages
- Architecture: HIGH, all call sites read
- Pitfalls: HIGH for Core items; MEDIUM for Windows behavior (A1, A2)

**Research date:** 2026-11-02
**Valid until:** 30 days (stable code; QuestPDF terms and GitHub behavior could change)
