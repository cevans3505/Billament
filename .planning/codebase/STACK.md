---
last_mapped_commit: 2df24f47743d36dde2022025fa255f942c9bc7ae
last_mapped_at: 2026-11-01
---
# Technology Stack

**Analysis Date:** 2026-11-01

## Languages

**Primary:**
- C# (language version from the .NET 10 SDK, nullable reference types and implicit usings enabled in every project) - all code in `src/Billament.Core/`, `src/Billament.App/`, `tests/Billament.Tests/`, `tests/Billament.UiTests/`
- XAML - WPF windows and resources: `src/Billament.App/App.xaml`, `src/Billament.App/MainWindow.xaml`, `src/Billament.App/JobEditorWindow.xaml`, `src/Billament.App/Assets/Icon.xaml` (drawing source for the icon, not app UI)

**Secondary:**
- PowerShell - build, install and icon tooling: `publish.ps1`, `tools/Make-Icon.ps1`, and the inline scripts in `.github/workflows/release.yml`
- YAML - GitHub Actions workflows: `.github/workflows/ci.yml`, `.github/workflows/release.yml`
- Markdown and plain text - user docs and packaging: `README.md`, `AUDITING.md`, `CLAUDE.md`, `packaging/Read me.txt` (shipped inside the release zip)

## Runtime

**Environment:**
- .NET 10 (`net10.0` for the Core library and tests, `net10.0-windows` for the WPF app and the UI tests)
- Windows only for the app and UI tests. The Core library and unit tests have no Windows-only API and run on Linux (`dotnet test tests/Billament.Tests`).
- SDK pinned in `global.json`: `10.0.403`, with `"rollForward": "latestPatch"`.
- Release target: `win-x64`, self-contained, single-file executable (`PublishSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true`, `EnableCompressionInSingleFile=true` in `src/Billament.App/Billament.App.csproj`). Users do not need a .NET install.

**Package Manager:**
- NuGet via `dotnet restore` (driven by the `.csproj` files; no central package management file was found)
- Lockfile: missing. No `packages.lock.json` or `Directory.Packages.props` is tracked in the repository, so package versions are pinned only by the explicit `Version=` attributes in each `.csproj`.
- Solution file: `Billament.sln`

## Frameworks

**Core:**
- WPF (Windows Presentation Foundation) - the desktop UI, `src/Billament.App/`. Enabled with `<UseWPF>true</UseWPF>`. Dark theme set with `ThemeMode="Dark"` and the `PresentationFramework.Fluent` dark dictionary in `src/Billament.App/App.xaml`. The `WPF0001` warning (experimental `ThemeMode`) is suppressed in the app and UI test projects.
- MVVM via CommunityToolkit.Mvvm 8.4.2 - view models in `src/Billament.App/ViewModels/` use `ObservableObject`, `RelayCommand` and source generators (`[ObservableProperty]`, `[RelayCommand]`).
- QuestPDF 2026.9.1 - PDF layout and rendering for invoices and detail sheets, `src/Billament.App/Pdf/InvoicePdf.cs`. The license is set to Community at startup in `src/Billament.App/App.xaml.cs` (`QuestPDF.Settings.License = LicenseType.Community`).
- Microsoft.Data.Sqlite 10.0.14 - SQLite access, `src/Billament.Core/Data/Database.cs` and `src/Billament.Core/Data/DatabaseBackup.cs`. Hand-written SQL, no ORM. The database is a plain file; the native SQLite engine comes in through SQLitePCLRaw.
- Standard .NET libraries used directly: `System.IO.Compression` (opens `.gcode.3mf` zip archives in `src/Billament.Core/Slicing/BambuFileReader.cs`), `System.Xml` (`XmlReader` with DTDs prohibited and `XmlResolver = null`), `System.Text.Json` (update check state and GitHub API reply, `src/Billament.App/UpdateService.cs`), `System.Net.Http.HttpClient` (single GitHub request, `UpdateService.cs`), and a hand-written PNG encoder in `src/Billament.Core/Slicing/PngWriter.cs`.
- Printing uses WPF's own `System.Windows.Controls.PrintDialog` (`src/Billament.App/Pdf/InvoicePrinter.cs`). No Windows Forms dependency is referenced.

**Testing:**
- xUnit 2.9.3 with `xunit.runner.visualstudio` 3.1.4 and `Microsoft.NET.Test.Sdk` 17.14.1 - both test projects, `tests/Billament.Tests/Billament.Tests.csproj` and `tests/Billament.UiTests/Billament.UiTests.csproj`. `Xunit` is globally imported in both.
- coverlet.collector 6.0.4 - code coverage collector, referenced by `tests/Billament.Tests/Billament.Tests.csproj` only.
- UI tests (`tests/Billament.UiTests/`) open the real WPF windows offscreen through `WpfHost.cs`. They need an interactive Windows desktop session.
- Test fixtures: sample sliced files in `tests/Billament.Tests/Fixtures/` (`Cylindex_Tube_Cap.gcode.3mf`, `Wire_Brush_Insert.gcode.3mf`), copied to output by the test `.csproj` files.

**Build/Dev:**
- .NET SDK 10.0.403 (`global.json`) - build, test, publish.
- `dotnet publish` with the release switches above; the same arguments appear in `publish.ps1` and `.github/workflows/release.yml`.
- `publish.ps1` - runs all tests, publishes, installs to `%LOCALAPPDATA%\Programs\Billament`, creates a Start menu shortcut. Runs in Windows PowerShell 5.1.
- `tools/Make-Icon.ps1` - renders `src/Billament.App/Assets/Icon.xaml` into `src/Billament.App/Assets/Billament.ico`. Developer tool only.
- GitHub Actions (`actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/upload-artifact@v7`, `actions/download-artifact@v8`), running on `windows-latest`.

## Key Dependencies

**Critical:**
- `QuestPDF` 2026.9.1 (App) - the whole invoice and detail-sheet layout. Community license. Ships native Skia and qpdf libraries (`QuestPdfSkia.dll`, `qpdf.dll`) that are bundled into the single-file exe.
- `Microsoft.Data.Sqlite` 10.0.14 (Core) - the only persistence layer. Ships the native `e_sqlite3.dll` via SQLitePCLRaw.
- `CommunityToolkit.Mvvm` 8.4.2 (App) - MVVM base classes and source generators. No runtime I/O.

**Infrastructure:**
- `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `coverlet.collector` 6.0.4 - test-only; not shipped in the release.
- Project references: `Billament.App` references `Billament.Core`. Both test projects reference the projects they test. `Billament.UiTests` also links `tests/Billament.Tests/TestFiles.cs` and the fixtures.

**Versioning:**
- The app version is `<Version>3.6.0</Version>` in `src/Billament.App/Billament.App.csproj`. The release workflow requires the git tag to be exactly `v<Version>`. `packaging/Read me.txt` carries a `Billament 3.6` heading that is checked (with a warning only) against the major and minor version.
- Version comparison helpers: `src/Billament.Core/Updates/ReleaseVersion.cs`.

## Configuration

**Environment:**
- No environment variables are read by the application code. Data location comes from Windows special folders: `Documents\Billament\billament.db` (`Database.DefaultPath`) and `%LOCALAPPDATA%\Billament\` for small state files.
- User settings are stored in the SQLite `settings` table (key/value text, `src/Billament.Core/Data/Database.cs`) and mapped to `src/Billament.Core/Models/AppSettings.cs`. Notable keys include `CheckForUpdates` (default `true`) and `BackupFolder` (empty means `Backups` next to the database).
- Build-time configuration lives only in the `.csproj` files and `global.json`. No `appsettings.json`, `app.config` or user secrets file is tracked.
- Environment files: none tracked. The `.gitignore` covers build output; no `.env` file is present in the repository.
- Slice-file size and count limits: `src/Billament.Core/Slicing/SliceLimits.cs`.

**Build:**
- `global.json` - pins the SDK.
- `Billament.sln` - solution with the two `src` projects and two `tests` projects.
- `src/Billament.Core/Billament.Core.csproj` - net10.0, Microsoft.Data.Sqlite.
- `src/Billament.App/Billament.App.csproj` - net10.0-windows WinExe, WPF, QuestPDF, CommunityToolkit.Mvvm, icon and version.
- `tests/Billament.Tests/Billament.Tests.csproj` and `tests/Billament.UiTests/Billament.UiTests.csproj` - test projects.
- `.github/workflows/ci.yml` - restore, build (Release), unit tests on `windows-latest`, and a separate screen-test job for the UI tests.
- `.github/workflows/release.yml` - on a `v*` tag: checks the tag against `<Version>`, runs unit tests, publishes the single-file exe, zips it with `packaging/Read me.txt`, and creates a draft GitHub release.

## Platform Requirements

**Development:**
- Windows 10 or 11 with the .NET 10 SDK (10.0.403 or a later patch).
- Windows PowerShell 5.1 or PowerShell 7 for `publish.ps1`. The app is closed before publishing (the script refuses to run while `Billament` is running).
- Core library and `tests/Billament.Tests` can build and test on Linux or macOS. The App and UI tests need Windows and WPF.
- An interactive desktop session for `tests/Billament.UiTests`.

**Production:**
- Windows 10 or 11, 64-bit (`win-x64`). The exe is self-contained, so no .NET runtime install is needed.
- Distributed as `Billament-<version>.zip` (exe plus `Read me.txt`) on GitHub Releases. The exe is not code-signed; the read-me warns that Windows may show an unknown-publisher prompt.
- Local data: `Documents\Billament\` (database, backups, invoices) and `%LOCALAPPDATA%\Billament\` (update check state, window layout, error details). See `AUDITING.md` for the full file list.
- Printing uses the Windows print dialog through WPF `PrintDialog` (`src/Billament.App/Pdf/InvoicePrinter.cs`).

---

*Stack analysis: 2026-11-01*
