# Phase 3: Rename to Billament - Context

**Gathered:** 2026-11-02
**Status:** Ready for planning

<domain>
## Phase Boundary

PrintInvoicer becomes Billament everywhere: solution, projects, folders, namespaces, the exe, window titles and
dialogs, PDF text and file names, scripts, CI and release workflows, docs, the README pictures, the local data
folders and the GitHub repository. Existing users keep all their data: on first start of 4.0.0 the old data folders
move once, after a backup, to the new names. The result ships as 4.0.0. Requirements: NAME-01 to NAME-08.

Not in this phase: any change to pricing, totals or the invoice layout beyond the name; the data-safety fixes
(FIX-08 to FIX-12, now Phase 4); the slice file submission forms (now Phase 5); a new icon design; code signing.
Historical `.planning` phase files (Phases 1 and 2) are left as written.

</domain>

<decisions>
## Implementation Decisions

All decisions below were given by the user before discussion and are locked.

### Name
- **D-01:** The new name is **Billament** (bill plus filament), one word, capital B, no space. It replaces
  "PrintInvoicer" in every place a user or developer sees it. **Reversibility:** one-way. The repository name,
  data folder names and exe name are public and land on users' PCs; renaming again would need a second migration.
- **D-02:** Names of things built from it:
  - exe and assembly: `Billament.exe` (`<AssemblyName>Billament</AssemblyName>`, `<Product>Billament</Product>`)
  - solution: `Billament.sln`
  - projects, folders and root namespaces: `Billament.Core`, `Billament.App`, `Billament.Tests`, `Billament.UiTests`
    (`src/Billament.Core/Billament.Core.csproj` and so on), every `namespace PrintInvoicer.*` and `using
    PrintInvoicer.*` becomes `Billament.*`, XAML `x:Class` and `clr-namespace` values follow
  - `InternalsVisibleTo("Billament.UiTests")` in `src/Billament.App/AssemblyInfo.cs`
  - release zip: `Billament-<version>.zip` and `Billament-<version>.zip.sha256`
  - database file: `billament.db` (lowercase, like today's `printinvoicer.db`)
- **D-03:** Use `git mv` for file and folder renames so history follows the files.

### Placement and version
- **D-04:** This is Phase 3, the next phase, before Data Safety Fixes (now Phase 4) and Slice File Submissions (now
  Phase 5), so later phases are written under the new name from the start.
- **D-05:** Ships as **4.0.0**: `<Version>4.0.0</Version>` in `src/Billament.App/Billament.App.csproj`, and the
  heading of `packaging/Read me.txt` becomes `Billament 4.0`. The release workflow's heading check looks for
  `Billament <major>.<minor>`. **Reversibility:** one-way. A published 4.0.0 tag and release cannot be taken back
  without confusing the update check of everyone who saw it.

### Existing data (one-time move)
- **D-06:** On first start, if the old folders exist and the new ones do not, move them **once**:
  `Documents\PrintInvoicer` to `Documents\Billament`, and `%LOCALAPPDATA%\PrintInvoicer` to `%LOCALAPPDATA%\Billament`.
  The database file inside is renamed from `printinvoicer.db` to `billament.db`. The move runs **before**
  `new Database(...)` opens anything, because the `Database` constructor writes (`EnsureCreated`,
  `FreezeFinalizedJobs`). **Reversibility:** one-way. After the move, 3.7.0 no longer finds the data where it
  looks; going back means moving folders by hand.
- **D-07:** Make a **backup first**, before anything is moved. The backup must survive a failed move. (Where it goes
  and its name are Claude's discretion, see below; it must not be pruned as a daily backup.)
- **D-08:** **Never touch the old folders when the new ones already exist.** If `Documents\Billament` exists, the
  Documents move is skipped entirely, even if `Documents\PrintInvoicer` also exists. The same rule applies on its own
  to the `%LOCALAPPDATA%` pair. Nothing is merged, overwritten or deleted.
- **D-09:** **Safety first.** Finalized invoices never change and no data is lost. A test shows that every finalized
  job's `finalized_total` and `price_snapshot` are byte-for-byte the same after the move as before.
- **D-10:** **Failures** show a plain message (what happened, where the data is, that nothing was lost) and leave the
  old data in place. A half-done move must not leave the user with two partial copies and no clear answer about which
  is real. Technical details go to the error file through `ErrorReport`, not into the message.
- **D-11:** **Every new file and folder access** (folder checks, the backup, the move, the rename, any settings
  rewrite) gets an `// AUDIT:` comment directly above it and a row in `AUDITING.md`.
- **D-12:** **The move logic lives in Core** (no WPF, no `Environment.GetFolderPath` inside the logic: the folders
  are passed in as parameters, the way `DatabaseBackup.DailyIfDue` takes its folder and time) and is covered by unit
  tests in `tests/Billament.Tests` that run on Linux. `App.OnStartup` only resolves the real folders and calls it.
- **D-13:** **Custom folders inside the old folder.** If the user's `BackupFolder` or `InvoiceFolder` setting points
  inside `Documents\PrintInvoicer` (for example `...\Documents\PrintInvoicer\Invoices 2026`), it is rewritten to the
  same place under `Documents\Billament` after the move, so backups and invoices keep working. Settings that point
  anywhere else are left exactly as they are. Only these two text settings may change; no job, part, plate, snapshot
  or total is written by the move.

### GitHub repository
- **D-14:** The repository is renamed from `cevans3505/PrintInvoicer` to `cevans3505/Billament`. **The user renames
  it on GitHub by hand when the phase ships**; this is a manual step in the end-of-phase checklist, not something a
  plan or agent does. **Reversibility:** costly. GitHub keeps redirects from the old name only until a new
  repository takes the old name; links in 3.7.0 depend on them.
- **D-15:** `UpdateService.Repository` becomes `cevans3505/Billament`. Its allow-check (release page URL must start
  with `https://github.com/{Repository}/`), the error report's issue page (`ErrorReport.IssuesPage`), the User-Agent
  product name, the README, `packaging/Read me.txt` and all docs links point at the new name.
- **D-16:** **3.7.0 users must still find the update** through GitHub's redirect. Research must verify how
  `https://api.github.com/repos/cevans3505/PrintInvoicer/releases/latest` behaves after a rename (status code,
  `Location` header, whether .NET `HttpClient` follows it by default), and what 3.7.0's allow-check does with a
  `html_url` under the new name (expected: it falls back to `ReleasesPage` under the old name, which GitHub's web
  redirect sends on). If 3.7.0 would not find the update, the plan must say what to do about it.

### Scope that plans must reach
- **D-17:** Every reference is in scope:
  - `src`, `tests`, the `.sln`, `.csproj` file and folder names, namespaces, `AssemblyInfo` `InternalsVisibleTo`
  - window titles and UI text, message box captions, PDF text and file names (for example the
    `"PrintInvoicer, {n} invoices"` combined PDF name), the preview temp folder, the UI test temp folder
  - pack URIs to the icon (`pack://application:,,,/PrintInvoicer;component/Assets/PrintInvoicer.ico`)
  - `publish.ps1`: install folder `%LOCALAPPDATA%\Programs\Billament`, Start menu shortcut `Billament.lnk`, the
    running-process check for both names, and **removal of the old `PrintInvoicer.lnk` shortcut and the old
    `%LOCALAPPDATA%\Programs\PrintInvoicer` install folder** (the program files only, never the data folders)
  - `make-readme-images.ps1` (including the `PRINTINVOICER_README_IMAGES` variable), `tools/Make-Icon.ps1`
  - `.github/workflows/ci.yml` and `release.yml` (paths, zip and checksum names, read-me heading check, release
    title and notes)
  - README, AUDITING.md, CLAUDE.md, `.claude/CLAUDE.md`, `packaging/Read me.txt` (plus a short note that 4.0 moves
    the data from `Documents\PrintInvoicer` to `Documents\Billament` on first start)
  - the README pictures, regenerated on Windows if the window title or UI text in them shows the name
  - the error-report folder, window-layout file and update-check state file (all under `%LOCALAPPDATA%\Billament`)
  - QuestPDF document metadata, if the code sets any
  - `.planning/codebase/*.md` only where the old name would mislead a future agent (paths, project names, commands)
- **D-18:** Leave historical `.planning` phase files (`phases/01-*`, `phases/02-*`) alone.
- **D-19:** **Final grep.** A case-insensitive search for `PrintInvoicer` across the repository (outside historical
  `.planning` phase files) finds only deliberate hits: the migration code's old folder and file names (and their
  tests and AUDIT rows), history (git, changelog or release notes), and any note that explains the rename. Each
  remaining hit is listed in the phase verification with its reason.

### Where work runs
- **D-20:** The user prefers cloud work. Plans are marked **[cloud]** when they can be done and checked on Linux
  (Core code and tests, renames, docs, workflows, scripts edited as text) or **[windows]** when they need Windows
  (UI tests, the README pictures, running `publish.ps1`, a real upgrade test from a 3.7.0 data folder). App code is
  compile-checked in the cloud only as far as the Linux SDK allows; the Windows checks run on the user's PC
  afterwards.
- **D-21:** The phase ends with a **manual checklist** for the user: rename the repository on GitHub; run the upgrade
  test on Windows with a copy of a real 3.7.0 data folder; run the UI tests and `make-readme-images.ps1`; then do the
  4.0.0 release (push `main`, wait for CI, push tag `v4.0.0`, check the draft's zip and `.sha256`, write notes,
  publish), and confirm a 3.7.0 install shows the update bar.

### Claude's Discretion
- Where the pre-move backup goes and its exact name (for example a `pre-rename` manual backup made with
  `DatabaseBackup.Manual` into the old Backups folder so it moves along with the data, or a copy left beside the old
  folder). It must not be pruned and the README / Read me should say where it is.
- How the move is done (`Directory.Move` when on the same volume, otherwise a checked copy then removal, or refuse
  and leave the data in place) and how a half-done move is detected and reported, within D-08 to D-10.
- The backup file prefix after the rename (`billament-`), and how `DatabaseBackup.List`, `Prune` and the restore file
  picker keep handling backups named `printinvoicer-*.db` made before the rename (NAME-04).
- The name of `DatabaseBackup.LooksLikePrintInvoicer` (for example `LooksLikeBillament`) and other identifiers that
  contain the old name; the `BambuFileReader.TooLargeKey` string value.
- Whether the old `%TEMP%\PrintInvoicer previews` folder is ignored or cleaned (it is disposable either way).
- The order of plans and waves, as long as the code renames land before the docs that describe them and the final
  grep is last.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project rules
- `CLAUDE.md`: rules for finalized invoices, schema changes, AUDIT comments, UI tests, sample names, writing style
- `.claude/CLAUDE.md`: full conventions (naming, error handling, comments, module design)
- `AUDITING.md`: the list of every outside access; new rows for the migration go here

### Planning
- `.planning/ROADMAP.md` (Phase 3 section): goal and success criteria
- `.planning/REQUIREMENTS.md` (Rename to Billament): NAME-01 to NAME-08
- `.planning/PROJECT.md` (Key Decisions): the rename decisions

### Code the move and rename touch
- `src/PrintInvoicer.Core/Data/Database.cs`: `DefaultPath`, constructor (creates folder, writes on open), settings
  storage (`LoadSettings`, `SaveSettings`)
- `src/PrintInvoicer.Core/Data/DatabaseBackup.cs`: `Prefix`, `DailyIfDue`, `Manual`, `List`, `Prune`, `Restore`,
  `LooksLikePrintInvoicer`, `DailyName` regex
- `src/PrintInvoicer.Core/Models/AppSettings.cs`: `InvoiceFolder`, `BackupFolder`
- `src/PrintInvoicer.App/App.xaml.cs`: `OnStartup` (where the move must run, before `new Database`)
- `src/PrintInvoicer.App/UpdateService.cs`: `Repository`, `ReleasesPage`, allow-check, API URL, User-Agent, state file
- `src/PrintInvoicer.App/ErrorReport.cs`: errors folder, `IssuesPage`, captions
- `src/PrintInvoicer.App/WindowLayout.cs`: layout file
- `src/PrintInvoicer.App/Pdf/InvoicePdf.cs`: `DefaultFolder`, `PreviewFolder`
- `src/PrintInvoicer.App/ViewModels/SettingsViewModel.cs`: restore file picker filter, restart after restore
- `publish.ps1`, `make-readme-images.ps1`, `tools/Make-Icon.ps1`, `.github/workflows/ci.yml`,
  `.github/workflows/release.yml`, `packaging/Read me.txt`, `README.md`

### Tests to extend
- `tests/PrintInvoicer.Tests/BackupTests.cs`, `tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs` (fixture database
  with finalized jobs; a good base for the "totals unchanged after the move" test),
  `tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `DatabaseBackup.Manual(dbPath, folder, now, label)`: consistent SQLite-API copy to a `.partial` name first, never
  pruned; a natural fit for the pre-move backup.
- `DatabaseBackup.LooksLikePrintInvoicer(path)`: checks a file is one of our databases; can confirm the old database
  before moving it.
- `LegacyDatabaseTests` and `pre-snapshot.sql`: build a database with finalized jobs and known totals in a temp
  folder; reuse for move tests.
- `ErrorReport.Show(what, ex)`: plain message plus saved details, already used for startup failures.

### Established Patterns
- Time and folders passed as parameters so tests use temp folders and fixed dates.
- Narrow `catch ... when (ex is IOException or UnauthorizedAccessException ...)` filters; broad catch only at the
  program boundary.
- `// AUDIT:` comment directly above each outside-access line, with a matching AUDITING.md row.
- Startup failures: `ErrorReport.Show(...)` then `Shutdown(1)` in `App.OnStartup`.

### Integration Points
- `App.OnStartup`: call the move before `Db = new Database(Database.DefaultPath)`; decide whether a failed move stops
  start-up or starts with the old data untouched (D-10: old data must stay in place and the user must be told).
- `Database.DefaultPath`: becomes `Documents\Billament\billament.db`.
- Settings rows for `BackupFolder` and `InvoiceFolder` in the moved database (D-13).
- Three state files under `%LOCALAPPDATA%`: `Errors\`, `window-layout.json`, `update-check.json`.

</code_context>

<specifics>
## Specific Ideas

- Sample names only in tests and docs ("Harbor Dive Shop", "Sample Printing Co."; paths like
  `C:\Users\Sample\Documents\...`).
- Plain English, no em dashes, in all new comments, messages and docs.
- The user renames the GitHub repository by hand at ship time; until then `main` still lives at the old name, so CI
  and links keep working through the transition.

</specifics>

<deferred>
## Deferred Ideas

- A new icon design for the new name: not requested; the icon file is renamed only.
- Code signing the 4.0.0 exe: out of scope for the milestone (see REQUIREMENTS.md Out of Scope).

</deferred>

---

*Phase: 03-rename-to-billament*
*Context gathered: 2026-11-02*
