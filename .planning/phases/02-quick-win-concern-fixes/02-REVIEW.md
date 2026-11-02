---
phase: 02-quick-win-concern-fixes
reviewed: 2026-11-01T00:00:00Z
depth: standard
files_reviewed: 15
files_reviewed_list:
  - .github/workflows/release.yml
  - src/PrintInvoicer.App/ErrorReport.cs
  - src/PrintInvoicer.App/Pdf/InvoicePdf.cs
  - src/PrintInvoicer.App/UpdateService.cs
  - src/PrintInvoicer.App/ViewModels/SettingsViewModel.cs
  - src/PrintInvoicer.Core/Data/DatabaseBackup.cs
  - src/PrintInvoicer.Core/Files/SafeFileName.cs
  - src/PrintInvoicer.Core/Files/ShellCommands.cs
  - src/PrintInvoicer.Core/Updates/LatestReleaseReply.cs
  - tests/PrintInvoicer.Tests/BackupTests.cs
  - tests/PrintInvoicer.Tests/LatestReleaseReplyTests.cs
  - tests/PrintInvoicer.Tests/LegacyDatabaseTests.cs
  - tests/PrintInvoicer.Tests/SafeFileNameTests.cs
  - tests/PrintInvoicer.Tests/ShellCommandsTests.cs
  - tests/PrintInvoicer.UiTests/ScreenTests.cs
findings:
  critical: 0
  warning: 2
  info: 5
  total: 7
status: issues_found
---

# Phase 2: Code Review Report

**Reviewed:** 2026-11-01
**Depth:** standard
**Files Reviewed:** 15
**Status:** issues_found

## Summary

Re-review of the phase 2 changes since 2b5965a, after the WR-01 and WR-03 fixes (dee2ad2 and 5139a31).

- WR-01 is fixed. `CleanNumber` cuts a long number from the front, so the sequence digits survive, and a number that fits
  gives the same name part as before. The only gap left is the reserved-name edge case you already know about (now IN-06).
- WR-03 is fixed. `NormalizeFolder` is applied before `CreateDirectory` and inside `IsDirectlyInside`, so the guard no
  longer disagrees with Windows for a folder typed with a trailing space or dot. Confirmed on your PC.
- IN-05 stays dropped (Explorer highlights the file for the two-argument `/select,` form).
- Still open from the last review: WR-02, IN-01, IN-02, IN-03, IN-04. Their IDs are unchanged.
- One new warning (WR-04) and one new info (IN-06). I checked the Windows behavior for WR-04 on this PC rather than
  guessing.

The backup `.partial` change, the 256 KB reply cap, the `ArgumentList` change and the release workflow checksum steps
look sound. No critical issues.

## Warnings

### WR-02: Titles over 60 characters (and a few other titles) now produce a different PDF name than before, so a regenerated invoice sits beside the old file

**File:** `src/PrintInvoicer.Core/Files/SafeFileName.cs:12,42-64,70-94`, `src/PrintInvoicer.App/Pdf/InvoicePdf.cs:48-53`
**Issue:** Unchanged by the fixes. The old `Safe(title)` only replaced invalid characters and trimmed whitespace. `Clean`
also cuts to 60 chars, collapses runs of dots, trims trailing dots, prefixes reserved names with an underscore, turns an
empty title into "untitled", and no longer trims a leading no-break space. The invoice number is also affected: since
the WR-01 fix a number over 30 chars is cut from the front, where before it was never shortened. `InvoicePdf.Write`
documents that it replaces "any earlier copy of the same kind for this job". For an already finalized invoice whose
title is over 60 chars (nothing limits the title in the UI), or whose number is over 30 chars, regenerating the PDF now
creates a second file with a new name and leaves the old one in the folder. The user can then send the stale copy,
and the two files can differ if business details changed. This is a visible change for finalized invoices.
**Fix:** Either accept it and say so (README or AUDITING note, plus a release note), or avoid it for existing data by not
truncating titles that are only long: raise `MaxTitleLength` so number, prefix and suffix still fit in `MaxNameLength`,
so only names that are unsafe change. At minimum keep a test that pins the 61-char title name (it exists:
`A_title_over_60_chars_gets_a_new_name`) and record the decision in the disposition file.

### WR-04: The folder cleanup is applied only to PDF writes; "Open invoice folder" and backups still use the raw typed folder, and a bad folder reaches the "problem in PrintInvoicer" dialog

**File:** `src/PrintInvoicer.App/ViewModels/SettingsViewModel.cs:174-180`, `src/PrintInvoicer.Core/Files/SafeFileName.cs:113-138`, `src/PrintInvoicer.App/Pdf/InvoicePdf.cs:43-45`
**Issue:** Checked on Windows (this PC, .NET `Directory.CreateDirectory`):
- A folder with a space (or dot) at the end of a middle segment, such as `C:\...\a \b`, is NOT cleaned up by Windows. It
  creates `a` and then fails with "Could not find a part of the path ... `a \b`" (DirectoryNotFoundException, an
  IOException). `NormalizeFolder` strips every segment, so `InvoicePdf.Write` works for this folder, but the doc
  comment ("the folder as Windows will really create and use it") and the test name
  `Every_folder_segment_loses_its_trailing_spaces_and_dots` describe Windows rules that are not quite true; the code is
  more forgiving than Windows. Only the last segment is stripped by Windows, which is the case you confirmed.
- `OpenInvoiceFolder` calls `Directory.CreateDirectory(InvoiceFolder)` on the raw setting with no try/catch. With the
  middle-segment case above it throws IOException, and with an illegal character (`|`, `?`) it throws ArgumentException,
  and with a stray colon NotSupportedException (all three observed here). `OpenBackupFolder` next to it catches
  `IOException or UnauthorizedAccessException`, so it also misses ArgumentException and NotSupportedException. These
  reach the global handler, which tells the user "This is a problem in PrintInvoicer, not something you did wrong"
  for a typo in their own folder setting. The Open button and the PDF write also disagree about which folder a setting
  means.
- `InvoicePdf.Write` has the same exposure for ArgumentException and NotSupportedException:
  `MainViewModel.GenerateInvoices` catches `IOException` only, and shows "its PDF is open in another program" for any
  IOException, including this DirectoryNotFoundException.
**Fix:** Treat a folder problem as an expected, recoverable error. In `OpenInvoiceFolder` normalize and guard like
`OpenBackupFolder`, with a wider filter:
```csharp
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
{ SavedText = $"Cannot open the invoice folder: {ex.Message}"; return; }
```
and call `SafeFileName.NormalizeFolder` on the folder in both places (or in one `InvoiceFolder` helper) so the button and
the PDF use the same folder. Correct the `NormalizeFolder` comment to say it also repairs middle segments that Windows
would reject. Optionally have `GenerateInvoices` report a folder problem with its own sentence instead of the
"PDF is open" text.

## Info

### IN-01: Interrupted backups now leave a uniquely named `.partial` file that nothing ever removes

**File:** `src/PrintInvoicer.Core/Data/DatabaseBackup.cs:113-138`, `AUDITING.md` (Backups row)
**Issue:** Unchanged. The old code deleted a stale `target.partial` before retrying. With a GUID in the name, a copy cut
off by a power cut or process kill (the catch only runs on exceptions) leaves a full-size database copy that no later
run replaces or prunes. `List` and `Prune` only match `*.db`. AUDITING.md documents this honestly, but each interrupted
run costs one database-sized file for good. Copies left by the old fixed `.partial` name from earlier versions are also
never cleaned.
**Fix:** In `Prune`, also delete `printinvoicer-*.db.*.partial` files older than a day (add an AUDIT tag and update the
AUDITING.md row), or keep the documented trade-off.

### IN-02: Backup concurrency tests do not exercise two copies writing the same target

**File:** `tests/PrintInvoicer.Tests/BackupTests.cs:130-172`
**Issue:** Unchanged. `Two_manual_backups_in_the_same_second_both_succeed` runs the two calls one after the other, and
`Daily_and_manual_backups_running_at_once_both_finish` uses different target files each round, as its comment admits.
`A_partial_file_from_another_copy_is_left_alone` plants `printinvoicer-2026-10-30.db.partial`, a name the new code can
never produce (it always adds a GUID), so it would pass against the old shared-name bug only if the old name matched,
and it checks nothing about the new behavior. The test names promise more than they check.
**Fix:** Run two `Manual` calls with the same `now` inside `Parallel.Invoke`, repeated several times, and assert no
`.partial` files remain and the target opens as a valid database. Plant the stray file with a GUID-style name. Or rename
the tests to say what they do.

### IN-03: An oversized update reply clears the remembered update, unlike a network failure

**File:** `src/PrintInvoicer.App/UpdateService.cs:123-140,157-169`
**Issue:** Unchanged. An oversized reply (by `Content-Length` or by the streaming cap) makes `FetchLatestAsync` return
null, and `FetchAndRememberAsync` then saves a state with no release. A network failure falls back to the last known
release, but a too-large reply (for example a proxy error page returned with status 200) wipes it, so the update bar
disappears until the next successful check. The doc comment says this is by design ("like a 404").
**Fix:** Optional. Treat an oversized reply as a failed fetch (throw `InvalidDataException` or `IOException` from
`FetchLatestAsync`, and add `InvalidDataException` to the catch filter) so the last known answer is kept.

### IN-04: Bidirectional control characters are kept in file names

**File:** `src/PrintInvoicer.Core/Files/SafeFileName.cs:140-163`
**Issue:** Unchanged. `ReplaceUnsafe` replaces `char.IsControl` characters, but U+202E (right-to-left override) and other
format characters (category Cf) pass through. A title containing U+202E makes Explorer show the end of the name reversed,
including the `.pdf` suffix. The real extension is still `.pdf` and the title is the user's own text, so the risk is
low (cosmetic, or a pasted title from a customer).
**Fix:** Also replace characters in `UnicodeCategory.Format` (U+200B to U+200F, U+202A to U+202E, U+2066 to U+2069) with a
dash, and add a test.

### IN-06: A long number whose last 30 characters start with a reserved device name and a dot can still lose its last digit

**File:** `src/PrintInvoicer.Core/Files/SafeFileName.cs:86-94` (with `Clean` at `:59-62`)
**Issue:** This is the edge case already known from the WR-01 fix. `CleanNumber` cuts the front to 30 chars, then calls
`Clean(..., 30)` again. If those 30 chars start with a device name followed by a dot (for example `CON.` plus 26
chars), `Clean` adds an underscore and `Cut("_" + ..., 30)` removes the last char, which is a sequence digit. Two
invoices differing only in the last digit can then share a name. It needs a prefix with a dot-separated device name
exactly 30 chars from the end, so it is very unlikely, but it is the same failure as WR-01.
**Fix:** Cut one char less when the cut text would get an underscore, so nothing is lost from the end:
```csharp
string cut = CutFront(whole, MaxNumberLength);
string tail = Clean(cut, MaxNumberLength);
if (tail != cut) tail = Clean(CutFront(whole, MaxNumberLength - 1), MaxNumberLength);
return tail;
```
and add a test with two numbers that differ only in the last digit and whose last 30 chars start with `CON.`.

---

_Reviewed: 2026-11-01_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
