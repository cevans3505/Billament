---
phase: 02-quick-win-concern-fixes
reviewed: 2026-11-02T00:00:00Z
depth: standard
files_reviewed: 18
files_reviewed_list:
  - .github/workflows/release.yml
  - AUDITING.md
  - CLAUDE.md
  - README.md
  - src/PrintInvoicer.App/ErrorReport.cs
  - src/PrintInvoicer.App/Pdf/InvoicePdf.cs
  - src/PrintInvoicer.App/UpdateService.cs
  - src/PrintInvoicer.App/ViewModels/SettingsViewModel.cs
  - src/PrintInvoicer.Core/Data/DatabaseBackup.cs
  - src/PrintInvoicer.Core/Files/SafeFileName.cs
  - src/PrintInvoicer.Core/Files/ShellCommands.cs
  - src/PrintInvoicer.Core/Updates/LatestReleaseReply.cs
  - tests/PrintInvoicer.Tests/BackupTests.cs
  - tests/PrintInvoicer.Tests/Fixtures/pre-snapshot.sql
  - tests/PrintInvoicer.Tests/LatestReleaseReplyTests.cs
  - tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs
  - tests/PrintInvoicer.Tests/SafeFileNameTests.cs
  - tests/PrintInvoicer.Tests/ShellCommandsTests.cs
findings:
  critical: 0
  warning: 3
  info: 5
  total: 8
status: issues_found
---

# Phase 2: Code Review Report

**Reviewed:** 2026-11-02
**Depth:** standard
**Files Reviewed:** 18
**Status:** issues_found

## Summary

Reviewed the phase diff (`git diff 2b5965a`) for the SafeFileName, ShellCommands, LatestReleaseReply and DatabaseBackup
changes, their App call sites, the release workflow checksum step, the docs, and the new tests. The Core unit tests
pass on Linux (464 passed). The AUDIT tag count (27) matches AUDITING.md. No em or en dashes were added. The
ArgumentList, 256 KB cap and unique-partial-file changes are sound in the main paths. The main concern is that
`SafeFileName` changes the names of invoice PDFs in ways that can make two different invoices share one file.

## Warnings

### WR-01: Long invoice-number prefixes lose the sequence number, so different invoices get the same PDF name

**File:** `src/PrintInvoicer.Core/Files/SafeFileName.cs:70-80` (cut at `:138-150`), called from `src/PrintInvoicer.App/Pdf/InvoicePdf.cs:48-50`
**Issue:** `Compose` runs the invoice number through `Clean(number, MaxNumberLength = 30)`, and `Cut` keeps the head and drops the tail. The number is `InvoicePrefix` plus a 3-digit sequence (`AppSettings.FormatInvoiceNumber`), and `InvoicePrefix` is a free text box in Settings with no length limit (`MainWindow.xaml:402`). With a prefix of 28 or more chars (for example "Harbor Dive Shop Invoice No "), the sequence digits are cut off, so invoices 001, 002 and 003 all become the same name part. Two invoices with the same job title (common for repeat orders) then write to the same file, and the later one silently replaces the earlier PDF in the invoice folder. Before this phase the number was never shortened, so this is a regression. It also works against the project rule that a finalized invoice never changes. I confirmed `Clean` keeps only the first 30 chars; the sequence is the last part of the number.
**Fix:** Keep the tail of the number, not the head, or do not cut it at all and give the whole name a higher budget. For example, in `Compose` use a tail-preserving cut for the number:
```csharp
static string CleanNumber(string? n) {
    string c = Clean(n, 1000);                       // clean first
    return c.Length <= MaxNumberLength ? c : Clean(c[^MaxNumberLength..], MaxNumberLength);
}
```
Also add a test: two numbers that share a 30-char prefix must give different names.

### WR-02: Titles over 60 characters (and a few other titles) now produce a different PDF name than before, so a regenerated invoice sits beside the old file

**File:** `src/PrintInvoicer.Core/Files/SafeFileName.cs:12,42-64`, `src/PrintInvoicer.App/Pdf/InvoicePdf.cs:48-50`
**Issue:** The old `Safe(title)` only replaced invalid characters and trimmed spaces. `Clean` also cuts to 60 chars, collapses runs of dots, trims trailing dots, prefixes reserved names with an underscore, and turns an empty title into "untitled" (the old name was "Invoice 2026-001 .pdf"). `InvoicePdf.Write` documents that it replaces "any earlier copy of the same kind for this job". For an already finalized invoice whose title is longer than 60 chars (nothing limits the title in the UI), re-generating the PDF now creates a second file with a new name and leaves the old one in the folder. The user can then send the stale copy, and the two files can differ if prices or business details changed. This is a visible change for already finalized invoices.
**Fix:** Either accept and document the rename (README or AUDITING note, plus a release note), or avoid it for existing data by not truncating titles that are only long (raise `MaxTitleLength` to fit within `MaxNameLength` after number and prefix, so only names that are unsafe change). At minimum add a test that pins the new name for a 61-char title so the change is deliberate.

### WR-03: `IsDirectlyInside` compares two differently normalized paths and may throw for a valid folder with a trailing space or dot on Windows

**File:** `src/PrintInvoicer.Core/Files/SafeFileName.cs:87-95`, used at `src/PrintInvoicer.App/Pdf/InvoicePdf.cs:55`
**Issue:** The check compares `GetDirectoryName(GetFullPath(path))` with `TrimEndingDirectorySeparator(GetFullPath(folder))`. On Windows, `GetFullPath` strips trailing spaces and dots from the last path segment, but a trailing space or dot in the middle of `path` (the folder part of `Path.Combine(folder, name)`) is kept. A pasted Settings value such as `C:\Invoices ` (trailing space; `InvoiceFolder` is not trimmed) would give `C:\Invoices ` on one side and `C:\Invoices` on the other, so the guard returns false and `Write` throws `InvalidOperationException`. That exception is not caught in `MainViewModel.GenerateInvoices` (it catches `IOException` only) or in `JobEditorViewModel`, so it would reach the global handler as an unexpected error and block every invoice. I could not run this on Windows (Linux does not normalize), so this is a risk, not a confirmed failure; the tests cannot see it for the same reason.
**Fix:** Do not compare directories. The file name is the only part the code builds, so check that part directly:
```csharp
public static bool IsDirectlyInside(string folder, string path) =>
    string.Equals(Path.GetFileName(path), path.Length > folder.Length ? path[folder.Length..].TrimStart('\\', '/') : "", StringComparison.Ordinal);
```
or simpler, validate the composed `name` (`Path.GetFileName(name) == name` and no separator characters) before combining, and keep the folder as the user typed it.

## Info

### IN-01: Interrupted backups now leave a uniquely named `.partial` file that nothing ever removes

**File:** `src/PrintInvoicer.Core/Data/DatabaseBackup.cs:113-138`, `AUDITING.md` (Backups row)
**Issue:** The old code deleted a stale `target.partial` before retrying. With a GUID in the name, a copy cut off by a power cut or process kill (the catch only runs on exceptions) leaves a full-size database copy that no later run replaces or prunes. `List` and `Prune` only match `*.db`. AUDITING.md documents this honestly, but each interrupted run costs one database-sized file for good.
**Fix:** In `Prune`, also delete `printinvoicer-*.db.*.partial` files older than a day (add an AUDIT tag and update the AUDITING.md row), or leave as is and keep the documented trade-off.

### IN-02: Backup concurrency tests do not exercise two copies writing the same target

**File:** `tests/PrintInvoicer.Tests/BackupTests.cs:130-158`
**Issue:** `Two_manual_backups_in_the_same_second_both_succeed` runs the two calls one after the other, and `Daily_and_manual_backups_running_at_once_both_finish` uses different target files each round, as its comment admits. Neither would fail against the old shared `.partial` name in the way the phase intended (two copies sharing one temporary file). The names promise more than the tests check.
**Fix:** Run two `Manual` calls with the same `now` inside `Parallel.Invoke`, repeated several times, and assert no `.partial` files remain and the target opens as a valid database. Or rename the tests to say what they do.

### IN-03: An oversized update reply clears the remembered update, unlike a network failure

**File:** `src/PrintInvoicer.App/UpdateService.cs:158-168` and `:126-142`
**Issue:** An oversized reply makes `FetchLatestAsync` return null, and `FetchAndRememberAsync` then saves a state with no release. A network failure falls back to the last known release, but a too-large reply (for example a proxy error page returned with status 200) wipes it, so the update bar disappears until the next successful check. The doc comment says this is by design ("like a 404").
**Fix:** Optional. Treat an oversized reply as a failed fetch (throw `InvalidDataException` or `IOException` from `FetchLatestAsync`, which the catch handles) so the last known answer is kept.

### IN-04: Bidirectional control characters are kept in file names

**File:** `src/PrintInvoicer.Core/Files/SafeFileName.cs:99-120`
**Issue:** `ReplaceUnsafe` replaces `char.IsControl` characters, but U+202E (right-to-left override) and other format characters (category Cf) pass through. A title containing U+202E makes Explorer show the end of the name reversed, including the `.pdf` suffix. The real extension is still `.pdf`, and the title is the user's own text, so the risk is low (cosmetic, or a pasted title from a customer).
**Fix:** Also replace characters in `UnicodeCategory.Format` (U+200B to U+200F, U+202A to U+202E, U+2066 to U+2069) with a dash, and add a test.

### IN-05: The Windows behavior of the Explorer arguments has not been checked

**File:** `src/PrintInvoicer.Core/Files/ShellCommands.cs:21-27`, `tests/PrintInvoicer.Tests/ShellCommandsTests.cs`
**Issue:** The tests prove the shape of the request (two list items, empty `Arguments`) on Linux only. 02-RESEARCH.md marks the `explorer.exe /select, "path"` split into two arguments as assumption A2 and asks for a manual Windows check. I could not find a record that the check was done. If Explorer does not select the file when `/select,` and the path are separate arguments, the error dialog's "show the details file" step would open the wrong folder.
**Fix:** Do the one-off Windows check described in 02-RESEARCH.md and note the result in the phase summary. If it fails, pass one argument: `"/select," + path`.

---

_Reviewed: 2026-11-02_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
