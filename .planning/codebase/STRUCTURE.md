---
last_mapped_commit: 2df24f47743d36dde2022025fa255f942c9bc7ae
last_mapped_at: 2026-11-01
---
# Codebase Structure

**Analysis Date:** 2026-11-01

## Directory Layout

```
Billament/                          # repository root
├── .github/workflows/                  # ci.yml (build and tests), release.yml (draft release)
├── packaging/                          # "Read me.txt" shipped in the release zip
├── src/
│   ├── Billament.Core/             # domain, pricing, slicing, storage (no UI)
│   │   ├── Data/                       # SQLite access, backups, seed data
│   │   ├── Models/                     # Job, Printer, Filament, Resin, settings, filters
│   │   ├── Pricing/                    # PricingCalculator, PriceBook, PriceSnapshot, matchers
│   │   ├── Slicing/                    # Bambu and ELEGOO .goo readers, JobBuilder
│   │   └── Updates/                    # ReleaseVersion (version text parsing)
│   └── Billament.App/              # WPF application (net10.0-windows)
│       ├── Assets/                     # Billament.ico, Icon.xaml (icon source, not UI)
│       ├── Pdf/                        # invoice PDF, print, plate images
│       └── ViewModels/                 # MVVM view models for each window
├── tests/
│   ├── Billament.Tests/            # xUnit unit tests (Core), runs anywhere
│   │   └── Fixtures/                   # sample Bambu .gcode.3mf files
│   └── Billament.UiTests/          # xUnit WPF screen tests, Windows only, offscreen
├── tools/                              # Make-Icon.ps1 (renders the app icon)
├── Billament.sln                   # solution: App, Core, Tests, UiTests
├── global.json                         # pins the .NET SDK (10.0.403, latestPatch roll-forward)
├── publish.ps1                         # local install: runs all tests, then publishes
├── CLAUDE.md                           # notes for AI coding agents
├── AUDITING.md                         # map of file, network, process and print access
├── README.md                           # user documentation
└── LICENSE                             # MIT
```

Build output (`bin/`, `obj/`, `TestResults/`) is created under each project and is not committed.

## Directory Purposes

**`src/Billament.Core/`:**
- Purpose: everything that does not need a window. Builds and tests on Linux as well as Windows.
- Contains: `.cs` files only. Project file references `Microsoft.Data.Sqlite` only.
- Key files: `Data/Database.cs` (about 890 lines, all SQL), `Pricing/PricingCalculator.cs`, `Pricing/PriceBook.cs`, `Pricing/PriceSnapshot.cs`, `Models/Job.cs`.

**`src/Billament.Core/Data/`:**
- Purpose: the database and its backups.
- Contains: `Database.cs` (schema in `EnsureCreated`, all `Get*`, `Save*`, `Delete*` methods), `DatabaseBackup.cs` (daily backups under `Backups`, keeps 14 daily copies by default, restore), `SeedData.cs` (starter filaments for a new database).

**`src/Billament.Core/Models/`:**
- Purpose: plain data classes and enums shared by all layers.
- Contains: `Job.cs` (`Job`, `JobPart`, `JobPlate`, `PlateFilament`, `PlateResin`), `Filament.cs`, `Resin.cs` (with `BottleUnit`), `Printer.cs` (with `PrintTechnology`), `ExtraCost.cs` (with `CostBasis`), `AppSettings.cs`, `JobFilter.cs` (with `PaidFilter`, `StageFilter`, `PrintedFilter`, `SearchColumn`).

**`src/Billament.Core/Pricing/`:**
- Purpose: money. Every price is computed here, from a `PriceBook`.
- Contains: `PricingCalculator.cs` (quote records and `Calculate`), `PriceBook.cs` (`PricingRules.Current` and `PriceBook`), `PriceSnapshot.cs`, `FilamentMatcher.cs` and `ResinMatcher.cs` (match a slicer profile name to a stored material).

**`src/Billament.Core/Slicing/`:**
- Purpose: read untrusted sliced-print files and turn them into job data.
- Contains: `SliceFileReader.cs` (entry point), `BambuFileReader.cs`, `GooFileReader.cs`, `SlicedFile.cs` (neutral records), `SliceLimits.cs` (size caps), `JobBuilder.cs`, `PngWriter.cs` (writes PNG images).

**`src/Billament.Core/Updates/`:**
- Purpose: version handling that can be tested without the network.
- Contains: `ReleaseVersion.cs`. The network code is in `src/Billament.App/UpdateService.cs`.

**`src/Billament.App/`:**
- Purpose: the WPF program. Output is `Billament.exe` (`AssemblyName` in `Billament.App.csproj`). The version is set in `<Version>` in that same project file (currently 3.6.0).
- Contains: windows (`*.xaml`, `*.xaml.cs`), app services, `App.xaml` (styles and Fluent dark theme), the icon file, and the `Pdf` and `ViewModels` folders.
- Key files: `App.xaml.cs` (startup), `MainWindow.xaml.cs`, `JobEditorWindow.xaml.cs`, `ViewModels/MainViewModel.cs`, `ViewModels/JobEditorViewModel.cs`, `Pdf/InvoicePdf.cs`, `UpdateService.cs`, `InvoiceActions.cs`, `ErrorReport.cs`, `WindowLayout.cs`.

**Root-level files in `src/Billament.App/`:**
- Windows and code-behind: `MainWindow.xaml(.cs)`, `JobEditorWindow.xaml(.cs)`, `App.xaml(.cs)`.
- Services and helpers: `InvoiceActions.cs`, `ErrorReport.cs`, `UpdateService.cs`, `WindowLayout.cs`, `Format.cs` (money and rate text), `Converters.cs` (XAML value converters: `BytesToImageConverter`, `HexToBrushConverter`), `AssemblyInfo.cs`.

**`src/Billament.App/Pdf/`:**
- Purpose: invoice output.
- Contains: `InvoicePdf.cs` (QuestPDF layout; `InvoiceKind` with `Preview`, `Final`, `Internal`; `Write` and `RenderPages`), `InvoicePrinter.cs` (Windows print dialog, 300 DPI), `PlateImages.cs` (plate thumbnails).

**`src/Billament.App/ViewModels/`:**
- Purpose: state and commands for each window.
- Contains: `MainViewModel.cs` (job list, filters, invoice commands), `JobEditorViewModel.cs` (one job), `PartViewModel.cs` (parts, plates, slots), `PrintersViewModel.cs`, `FilamentsViewModel.cs`, `SettingsViewModel.cs`.

**`src/Billament.App/Assets/`:**
- Purpose: icon resources.
- Contains: `Billament.ico` (set as `ApplicationIcon` and embedded as a resource), `Icon.xaml` (drawing source for `tools/Make-Icon.ps1`, excluded from the XAML build by `<Page Remove>` in the project file).

**`tests/Billament.Tests/`:**
- Purpose: unit tests for Core and the pure parts of the app.
- Contains: one `*Tests.cs` file per topic (e.g. `PricingTests.cs`, `PricingRulesTests.cs`, `PriceSnapshotTests.cs`, `DatabaseTests.cs`, `FinalizeTests.cs`, `BackupTests.cs`, `BambuFileReaderTests.cs`, `BambuRobustnessTests.cs`, `GooRobustnessTests.cs`, `ResinTests.cs`, `FilamentMatcherTests.cs`, `JobFilterTests.cs`, `ReleaseVersionTests.cs`), `ReaderContract.cs` (shared contract for file readers), `TestFiles.cs` (paths to fixtures and a `.goo` sample builder).
- Fixtures: `Fixtures/Cylindex_Tube_Cap.gcode.3mf` and `Fixtures/Wire_Brush_Insert.gcode.3mf`.

**`tests/Billament.UiTests/`:**
- Purpose: WPF screen tests. Run offscreen only.
- Contains: `ScreenTests.cs`, `WpfHost.cs` (runs code on a UI thread and creates windows offscreen; screenshots go to `bin/.../screens`), `SampleShop.cs` (sample shop data, using names like "Harbor Dive Shop").
- Links `TestFiles.cs` and the fixtures from the unit test project.

**`.github/workflows/`:**
- `ci.yml`: runs on pushes and pull requests to `main`. Jobs: build and unit tests, then screen tests, both on `windows-latest`.
- `release.yml`: builds the release zip and creates a draft GitHub release from a `v*` tag.

**`packaging/`:**
- Purpose: text shipped in the release zip. `Read me.txt`'s heading is updated on a major or minor version bump (see the Release steps in `README.md`).

## Key File Locations

**Entry Points:**
- `src/Billament.App/App.xaml.cs`: program start, error handlers, database open, backup.
- `src/Billament.App/MainWindow.xaml.cs`: main window and job list.
- `src/Billament.App/JobEditorWindow.xaml.cs`: job editor window.
- `src/Billament.Core/Slicing/SliceFileReader.cs`: file-type dispatch for sliced files.
- `publish.ps1`: local build, test and install script.

**Configuration:**
- `src/Billament.App/Billament.App.csproj`: app version, target framework, QuestPDF and CommunityToolkit.Mvvm references, icon.
- `src/Billament.Core/Billament.Core.csproj`: Core target and `Microsoft.Data.Sqlite` version.
- `global.json`: .NET SDK version.
- `.gitignore`: excludes build output and local data.
- Runtime settings: stored in the `settings` table of the database (`Database.LoadSettings`, `Database.SaveSettings`), not in a file.

**Core Logic:**
- `src/Billament.Core/Pricing/PricingCalculator.cs`: all pricing rules for a job.
- `src/Billament.Core/Data/Database.cs`: all SQL and schema upgrades.
- `src/Billament.Core/Slicing/JobBuilder.cs`: turns file data into parts and plates.
- `src/Billament.App/ViewModels/MainViewModel.cs`: invoice actions (finalize, preview, print, revert).
- `src/Billament.App/Pdf/InvoicePdf.cs`: invoice layout.

**Testing:**
- `tests/Billament.Tests/`: unit tests.
- `tests/Billament.UiTests/`: screen tests.
- `tests/Billament.Tests/TestFiles.cs`: fixture paths shared with the screen tests.

**Documentation:**
- `README.md`: user guide, including "How prices are calculated", "Data", and "Making a release".
- `CLAUDE.md`: project rules for AI coding agents.
- `AUDITING.md`: list of every place the app reads, writes, deletes, starts a program, prints, or touches the network.

## Naming Conventions

**Files:**
- One public type per file, named after the type: `PricingCalculator.cs` holds `PricingCalculator`. Small related records can share a file (`SlicedFile.cs` holds `SlicedFile`, `SlicedPlate`, `SlicedFilament`, `SlicedResin`).
- XAML windows and their code-behind pair: `JobEditorWindow.xaml` with `JobEditorWindow.xaml.cs`.
- Tests: `<Topic>Tests.cs` (e.g. `PriceSnapshotTests.cs`). Robustness tests for file readers: `<Format>RobustnessTests.cs`.

**Types:**
- ViewModels: `<Thing>ViewModel` (`MainViewModel`), row items for lists: `<Thing>RowViewModel` (`JobRowViewModel`, `PrinterRowViewModel`).
- Views: `<Thing>Window`.
- Enums: singular names with a `Filter` or `Unit` suffix when they describe a choice (`PaidFilter`, `BottleUnit`).

**Members:**
- Public and static members: PascalCase. Private fields: `_camelCase` (`_db`, `_connectionString`, `_vm`).
- Database methods: `Get<Things>`, `Save<Thing>`, `Delete<Thing>` (e.g. `GetFilaments`, `SaveJob`, `DeleteJob`); private overloads take a `SqliteConnection`.
- Commands: `[RelayCommand]` methods named as the verb (`NewJob`, `FinalizeInvoices`) generate `NewJobCommand`, and so on.

**Namespaces:**
- `Billament.Core.<Folder>` for Core, matching the folder (`Billament.Core.Pricing`).
- `Billament.App`, `Billament.App.ViewModels`, `Billament.App.Pdf`.
- Tests use `Billament.Tests` and `Billament.UiTests`.

**Directories:**
- Folders are PascalCase and singular or plural by content: `Models`, `Pricing`, `Slicing`, `Data`, `ViewModels`, `Pdf`, `Fixtures`.

## Where to Add New Code

**New pricing rule or fee:**
- Primary code: `src/Billament.Core/Pricing/PricingCalculator.cs`. If the change alters totals for existing jobs, add a new `PricingRules` version in `src/Billament.Core/Pricing/PriceBook.cs` and keep the old behavior for older versions.
- Settings field: `src/Billament.Core/Models/AppSettings.cs`, stored through `Database.LoadSettings` and `SaveSettings`.
- Tests: `tests/Billament.Tests/PricingTests.cs` or `PricingRulesTests.cs`. Check that old totals still match.

**New job field or table column:**
- Model: `src/Billament.Core/Models/Job.cs` (or the matching model file).
- Schema: `src/Billament.Core/Data/Database.cs`, `EnsureCreated`, using `AddColumnIfMissing` with a default that keeps existing data's meaning.
- Read and write: the `GetJobs` and `SaveJob` methods in `Database.cs`.
- Tests: `tests/Billament.Tests/DatabaseTests.cs`.

**New slicer file format:**
- Reader: a new `<Format>FileReader.cs` in `src/Billament.Core/Slicing/`, returning `SlicedFile`.
- Dispatch: add the extension check to `src/Billament.Core/Slicing/SliceFileReader.cs` and to `DialogFilter` in the same file.
- Tests: a new `<Format>RobustnessTests.cs` in `tests/Billament.Tests/`, following `ReaderContract.cs`. Values must be range-checked and failures must throw `InvalidDataException`.

**New window:**
- Window: `src/Billament.App/<Name>Window.xaml` and `.xaml.cs`.
- View model: `src/Billament.App/ViewModels/<Name>ViewModel.cs`, taking `Database` in its constructor.
- Screen test: `tests/Billament.UiTests/ScreenTests.cs`, created with `WpfHost`.

**New invoice section or PDF change:**
- Layout: `src/Billament.App/Pdf/InvoicePdf.cs`. Keep the layout separate from the totals, which come from `PricingCalculator`.

**New database operation:**
- Method: `src/Billament.Core/Data/Database.cs`, next to the related `Get*`/`Save*` methods. Use a private `SqliteConnection` helper (`Exec`, `Scalar<T>`, `Command`) and invariant-culture text for money.

**New file read or write, process start, print or network call:**
- Code: in the file where it is needed, with an `// AUDIT:` comment, and a row added to `AUDITING.md`.

**Shared helpers:**
- Core helpers (no UI): a file in the matching Core folder, or `src/Billament.Core/Data/` for storage helpers.
- App helpers (WPF or display text): `src/Billament.App/Format.cs` for money and rate text, `src/Billament.App/Converters.cs` for XAML converters.

**Utilities:**
- Version parsing: `src/Billament.Core/Updates/ReleaseVersion.cs`.

## Special Directories

**`bin/` and `obj/` (under each project, and `tests/*/bin`, `tests/*/obj`):**
- Purpose: build output.
- Generated: Yes.
- Committed: No (ignored by `.gitignore`).

**`TestResults/`:**
- Purpose: test result files written by CI (`unit-tests.trx`).
- Generated: Yes.
- Committed: No.

**`tests/Billament.Tests/Fixtures/`:**
- Purpose: sample slicer files used by tests and linked into the screen-test output.
- Generated: No.
- Committed: Yes.

**`.planning/codebase/`:**
- Purpose: these codebase maps, for planning tools.
- Generated: Yes, by the mapping command.
- Committed: Decided by the project owner; not set by this map.

---

*Structure analysis: 2026-11-01*
