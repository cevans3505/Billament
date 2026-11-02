---
status: complete
phase: 02-quick-win-concern-fixes
source: [02-VERIFICATION.md]
started: 2026-11-02T00:00:00Z
updated: 2026-11-02T02:47:50.372Z
---

## Current Test

[testing complete]

## Tests

### 1. Full test run on Windows, including screen tests
expected: `dotnet test PrintInvoicer.sln` passes on Windows. It must include `ScreenTests.Update_bar_shows_only_for_a_newer_release`, `ScreenTests.Every_invoice_kind_renders_for_every_job` and the phase 1 `ReadmeImageTests` (regression gate; these only built on Linux, they were not run).
result: pass. Windows 11, branch head 914c304: PrintInvoicer.Tests 464 passed, PrintInvoicer.UiTests 10 passed, 0 failed. The run listed all three named tests (`Readme_images_are_rebuilt_from_the_sample_shop` among them).

### 2. Settings > Check now against live GitHub
expected: Start the built app, open Settings, click Check now. Status reads "You have the latest version (...)" or "Version X is available", not "Could not reach GitHub to check."
result: pass. Run as an offscreen check that executes the real `UpdateService.CheckNowCommand` (the command the Settings button binds to) against live GitHub, with a temporary state file. Status: "You have the latest version (3.6.0). Checked Nov 1, 9:24 PM." The real GitHub reply was read through the 256 KB cap.

### 3. Hostile-name PDFs
expected: Create a draft job titled `..\evil:CON` with invoice number `x\..\..\y` and one hand-added plate. Preview writes `Preview x-.-.-y -evil-CON.pdf` in `%TEMP%\PrintInvoicer previews`; Detail PDF writes `Invoice x-.-.-y -evil-CON (detail).pdf` in the invoice folder. A normal job's Preview name is unchanged. Optional (review WR-03): set the invoice folder in Settings to a path with a trailing space and check that invoices still generate.
result: pass for the main check; the optional WR-03 check failed (see Gaps). Run offscreen on a sample-shop database through `InvoicePdf.Write`: the preview was `%TEMP%\PrintInvoicer previews\Preview x-.-.-y -evil-CON.pdf` and the detail PDF was `<invoice folder>\Invoice x-.-.-y -evil-CON (detail).pdf`, both as expected. A normal job's preview stayed `Preview 2026-001 Wire Brush Inserts.pdf`, the same as the old formula.

### 4. Windows Explorer check (A2), end-of-phase gate
expected: Each Settings Open button (data, backup, invoice folder) opens Explorer at that folder. `explorer.exe /select, "C:\Program Files\Windows Defender\MpCmdRun.exe"` in cmd highlights `MpCmdRun.exe` (the form `ShellCommands.ShowFile` sends). If not highlighted, apply the fallback in 02-04-SUMMARY.md. Record the result in the "Windows Explorer check (A2)" section of 02-04-SUMMARY.md, replacing PENDING.
result: pass. The same `ProcessStartInfo` that `ShellCommands` builds (`ArgumentList` entries, `UseShellExecute`) was started on Windows 11 and the result checked on screen. `/select,` plus `C:\Program Files\Windows Defender\MpCmdRun.exe` opened Windows Defender with `MpCmdRun.exe` highlighted. The folder form with `C:\Program Files\Common Files` (a path with spaces) opened that folder. The two-argument form is kept; no fallback needed.

### 5. Checksum on the next real release tag
expected: On the next `v<version>` tag, the draft release has `PrintInvoicer-<version>.zip` and `PrintInvoicer-<version>.zip.sha256`, a `SHA-256:` line in the notes, the "Write the SHA-256 checksum file" step passes, and `Get-FileHash .\PrintInvoicer-<version>.zip -Algorithm SHA256` matches the notes and the file.
result: pass (2026-11-02, release run 41380345274 for v3.7.0). The draft release has PrintInvoicer-3.7.0.zip and PrintInvoicer-3.7.0.zip.sha256, the notes end with `SHA-256: d86f3935...b296dac71`, the "Write the SHA-256 checksum file" step passed, and Get-FileHash on the downloaded zip printed the same hash as the notes and the file. The exe inside reports 3.7.0.

### 6. Maintainer review of judgment items
expected: The FIX-02 test uses literal totals; docs do not claim the checksum proves who built the release; the QuestPDF text is not presented as legal advice and matches QuestPDF's page.
result: pass (2026-11-02). The maintainer approved the README checksum caveat and the QuestPDF license text as written. `LegacyDatabaseTests.cs` asserts literal totals (87.20m, 40.40m, 12.70m, 16.05m), not computed ones.

## Summary

total: 6
passed: 6
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

- WR-03 confirmed on Windows. With the invoice folder set to a path ending in a space, `InvoicePdf.Write` for a Final invoice throws `InvalidOperationException: The PDF name "Invoice 2026-001 Wire Brush Inserts.pdf" would not be saved in ...\InvoicesTrailing .` Windows drops the trailing space when the folder is created, so `SafeFileName.IsDirectlyInside` rejects every invoice. The exception is not caught where invoices are made, so you would see the error report instead of a PDF. Needs a fix (trim or normalize the folder before the check) before the phase closes. Fixed in 5139a31 (folder normalized the way Windows does); re-run this check on Windows.
  - Resolved on Windows (2026-11-02, branch head d60114c): `dotnet test PrintInvoicer.sln` passed 485 Core and 11 UI tests, including `An_invoice_folder_with_a_trailing_space_still_saves`. Starting `ShellCommands.ShowFolder` with a folder typed as `...\Harbor Dive Shop invoices ` (trailing space) after `CreateDirectory` opened the trimmed folder in Explorer, so the Settings Open button also works for such a folder.

## Deferred Follow-Ups

- test: 5
  idea: "Check the SHA-256 file, the SHA-256 line in the notes and Get-FileHash on the 3.7.0 draft release"
  deferred_at: 2026-11-02
  resolved: "Checked on the v3.7.0 draft release, 2026-11-02 (test 5 now passes)"
