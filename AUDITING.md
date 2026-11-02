# Auditing Billament

A guide for anyone checking this code before trusting it. It lists every place the program reaches outside itself: the
network, other programs, and files. Those are the places where software can do harm, so they are the places worth
checking first.

This guide and the code comments describe what the code is **meant** to do. Don't take them on trust: they are a map,
not proof. Read the code they point to.

## Start here

Every line that touches the network, starts another program, prints, or writes, reads or deletes a file is marked with
a comment starting `AUDIT:`. To list them all:

```powershell
git grep -n "AUDIT:" -- src
```

or search the `src` folder for `AUDIT:` in any editor. There are 35 tags, and the tables below summarize them. If you find
code that does any of these things **without** an `AUDIT:` tag, that's a gap in this guide; please report it.

## Network

| What | Where | When |
| --- | --- | --- |
| `GET https://api.github.com/repos/cevans3505/Billament/releases/latest` | `src/Billament.App/UpdateService.cs`, `FetchLatestAsync` | At startup, at most once a day, unless turned off in Settings. Also when the user clicks **Check now**. |

That is the only network request in the program. It sends no information about the user or their data; only the
standard request headers (a `User-Agent` of `Billament/1`). From the reply it reads only the release's tag and page
address. It reads at most 256 KB of the reply (`LatestReleaseReply.MaxBytes`); a longer reply is ignored and treated as
no update information. There is no telemetry, analytics or crash upload. Error details are saved locally and never sent
(see below).

## Programs started

All of these use Windows' "open this with its default program" (`Process.Start` with `UseShellExecute`). Each one happens
only in response to a click. Explorer is given the folder or file as a separate argument
(`ProcessStartInfo.ArgumentList`, built by `ShellCommands` in Core), never pasted into a command line, so a path with a
quote or a space stays one argument.

| What opens | Where | Triggered by |
| --- | --- | --- |
| A PDF just created, in the default PDF viewer | `JobEditorViewModel.OpenPdf`, `MainViewModel.GenerateInvoices` | Preview, Finalize, Detail PDF, Open internal |
| A GitHub release page in the browser | `UpdateService.Download` (refuses any address outside this repository) | **Download** on the update bar |
| The GitHub "new issue" page, and Explorer showing the error file | `ErrorReport.Show` | **Yes** in the error dialog |
| A folder in Explorer | `SettingsViewModel.OpenFolder`, `OpenBackupFolder` | The **Open** buttons in Settings |
| Billament itself, to restart | `SettingsViewModel.RestoreBackup` | After the user confirms **Restore a backup** |
| The Windows print dialog, then the chosen printer | `InvoicePrinter.Print` | **Print invoices** / **Print internal** |

## Files written or deleted

| Location | What | Where in the code |
| --- | --- | --- |
| `Documents\Billament\billament.db` | All jobs, materials, printers and settings (SQLite) | `Database` (`Open`, used by every read and write) |
| `Documents\Billament\Backups\` (or the folder set in Settings) | Daily and manual copies of the database; oldest daily copies deleted to keep 14; each copy is first written to its own uniquely named `.partial` file and then moved into place; a copy that fails deletes its own `.partial` file and no other (one left by a copy that was cut off, for example by a power cut, stays until you delete it) | `DatabaseBackup` |
| `Documents\Billament\Invoices\` (or the folder set in Settings) | Invoice and detail-sheet PDFs. Their names come from the invoice number and job title, cleaned and length-limited by `SafeFileName`, so a PDF is always saved directly in this folder | `InvoicePdf.Write` |
| `%TEMP%\Billament previews\` | Preview PDFs (names cleaned the same way) | `InvoicePdf.Write` |
| `%LOCALAPPDATA%\Billament\update-check.json` | When GitHub was last asked, and the newest release it reported | `UpdateService.SaveState` |
| `%LOCALAPPDATA%\Billament\window-layout.json` | Window positions and sizes | `WindowLayout.Save` |
| `%LOCALAPPDATA%\Billament\Errors\` | Details of unexpected errors (app version, Windows version, time, the error) | `ErrorReport.Save` |
| `Documents\PrintInvoicer\Backups\billament-<date>-<time>-pre-rename.db` | Once, on the first start of 4.0: a safety copy of the old database, made before anything is moved. It moves with the folder to `Documents\Billament\Backups` and is never pruned | `DataFolderMigration.Run` (through `DatabaseBackup.Manual`) |
| `Documents\PrintInvoicer` renamed to `Documents\Billament`, and `%LOCALAPPDATA%\PrintInvoicer` renamed to `%LOCALAPPDATA%\Billament` | Once, on the first start of 4.0: each old folder is renamed in place (`Directory.Move`), only when no folder, file or link has the new name yet; each pair is decided on its own. Nothing is copied, merged, overwritten or deleted. If the database rename then fails, the Documents folder is renamed back | `DataFolderMigration.Run` |
| `printinvoicer.db` renamed to `billament.db` inside `Documents\Billament` | Right after the folder moves (`File.Move`, never over an existing file); a later start finishes this rename if an earlier one could not | `DataFolderMigration.Run` |

**Restoring a backup** (Settings, after a confirmation) replaces the database's contents with the chosen backup. The
current data is backed up first, and a damaged backup is refused.

**The one-time move in 4.0.** The app was called PrintInvoicer before 4.0. On the first start of 4.0 it copies the old
database, then renames the old data folders to the new names as listed above. If the Documents folder cannot be moved,
the app says where your data is and closes without creating the new folder, so the next start can try again.

Nothing is written anywhere else. The program never modifies the sliced files it reads.

Developer tools that run only on a developer's PC and write files there, such as `make-readme-images.ps1` for the
README pictures, are listed under [Building and releasing](#building-and-releasing).

## Files read

| What | Where in the code |
| --- | --- |
| Sliced files the user chooses or drags in (`.gcode.3mf`, `.gcode`, `.goo`), opened read-only | `BambuFileReader`, `GooFileReader` |
| The original sliced file again, read-only, to get a sharper plate picture for an invoice | `BambuFileReader.ReadPlateImage` |
| Its own database, backups (when restoring), and the small JSON files above | `Database`, `DatabaseBackup`, `UpdateService`, `WindowLayout` |
| Whether the old and new data folders and database files exist (names only, no contents), before the one-time move | `DataFolderMigration.Run` |

The file readers treat every file as untrusted. They never run anything from a file. The XML parser has DTDs and
external references switched off, every size is capped, and every number is range-checked (limits in
`src/Billament.Core/Slicing/SliceLimits.cs`). Hundreds of tests in `tests/Billament.Tests/*RobustnessTests.cs`
feed the readers damaged files.

## What it never does

A search of this project's code finds none of the following:

- **Registry access.**
- **Native code or P/Invoke in this project's code.** The packages below do ship native libraries: the SQLite engine
  (`e_sqlite3.dll`) and QuestPDF's PDF renderer (`QuestPdfSkia.dll`, `qpdf.dll`).
- **Loading code at run time.** No plugins, scripts or downloaded code.
- **Elevation.** It never asks for administrator rights.
- **Self-updating.** Nothing is downloaded or installed automatically; the update bar only links to the release page.
- **Credentials.** No passwords, accounts, tokens or keys.
- **Clipboard access.**
- **Background services or startup entries.**

To check for yourself, search `src` for: `Registry`, `DllImport`, `LibraryImport`, `Assembly.Load`, `HttpClient`,
`WebClient`, `Socket`, `Process.Start`, `File.Delete`, `Directory.Move`, `File.Move`, `Clipboard`.

## Third-party packages in the program

| Package | Version | Used for |
| --- | --- | --- |
| Microsoft.Data.Sqlite | 10.0.14 | The SQLite database (includes the native SQLite engine via SQLitePCLRaw) |
| QuestPDF | 2026.9.1 | Laying out and rendering the PDF invoices (Community license, set by `QuestPDF.Settings.License` in `App.xaml.cs`: for individuals and businesses only with annual gross revenue under USD 1,000,000, and not for public-sector entities or publicly traded companies; see the License section of the README; includes native Skia and qpdf libraries) |
| CommunityToolkit.Mvvm | 8.4.2 | Wiring the windows to their logic (source generators; no I/O of its own) |

Plus .NET 10 and WPF from Microsoft. The test projects use xUnit; tests are not part of the released program.

## Building and releasing

These run on a developer's PC or on GitHub, never on users' machines:

- **`publish.ps1`** runs the tests and builds the program. It then installs it to `%LOCALAPPDATA%\Programs\Billament`
  with a Start menu shortcut, and refreshes Windows' icon cache (`ie4uinit.exe -show`).
- **`make-readme-images.ps1`** rebuilds the three README pictures. It starts `dotnet test` for one screen test
  (`ReadmeImageTests`), which runs offscreen, creates the folders if needed and writes PNG files to `docs/images` in the
  repository and to the test's own `bin` folder. The script then reads each picture's last-write time to check it was
  rebuilt. A normal test run, CI and `publish.ps1` write to the test's `bin` folder and to a temporary folder under
  `%TEMP%` that is deleted afterwards, but never to `docs/images`. It is a developer tool and is not in the release zip.
  The script's program start and file read are tagged `# AUDIT:`. The `// AUDIT:` tags under `tests` are in
  `WpfHost.cs` (screenshots) and `ReadmeImageTests.cs` (README pictures), outside the `src` count above.
- **`.github/workflows/ci.yml`** builds and tests every change on a GitHub Windows machine, with a read-only token.
- **`.github/workflows/release.yml`**, on a version tag, builds the program the same way, writes the zip's SHA-256
  checksum to `Billament-<version>.zip.sha256`, and attaches the zip and that file to a **draft** release whose
  notes include the checksum. Only that last step has write access, and a person publishes the release by hand.

Releases are not code-signed. Each release gives the zip's SHA-256 checksum in its notes and in its `.sha256` file;
`Get-FileHash .\Billament-<version>.zip -Algorithm SHA256` shows whether a download is complete and unchanged from
what the release describes. It does not prove who built it, because the checksum is published in the same place as the
zip. To be sure a download matches this source, build it yourself from the tag with `publish.ps1` or the
`dotnet publish` command in the release workflow.

## A suggested order for a review

1. Search for `AUDIT:` and read each tagged line and its method.
2. Read `UpdateService.cs` (the only network code) and `ErrorReport.cs`.
3. Read the file readers in `src/Billament.Core/Slicing/`, the code that handles files from outside.
4. Skim `Database.cs` and `DatabaseBackup.cs`: all SQL uses parameters for values.
5. The rest is pricing (`Pricing/`), data classes (`Models/`), windows (`*.xaml`, `ViewModels/`) and PDF layout
   (`Pdf/`). These do calculation and display, and touch nothing outside the program.

Found something? Please [open an issue](https://github.com/cevans3505/Billament/issues).
