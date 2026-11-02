---
status: testing
phase: 02-quick-win-concern-fixes
source: [02-VERIFICATION.md]
started: 2026-11-02T00:00:00Z
updated: 2026-11-02T00:00:00Z
---

## Current Test

number: 1
name: Full test run on Windows, including screen tests
expected: |
  `dotnet test PrintInvoicer.sln` on Windows (or the CI screen-tests job on a pull request to main) passes with no failures.
awaiting: user response

## Tests

### 1. Full test run on Windows, including screen tests
expected: `dotnet test PrintInvoicer.sln` passes on Windows. It must include `ScreenTests.Update_bar_shows_only_for_a_newer_release`, `ScreenTests.Every_invoice_kind_renders_for_every_job` and the phase 1 `ReadmeImageTests` (regression gate; these only built on Linux, they were not run).
result: [pending]

### 2. Settings > Check now against live GitHub
expected: Start the built app, open Settings, click Check now. Status reads "You have the latest version (...)" or "Version X is available", not "Could not reach GitHub to check."
result: [pending]

### 3. Hostile-name PDFs
expected: Create a draft job titled `..\evil:CON` with invoice number `x\..\..\y` and one hand-added plate. Preview writes `Preview x-.-.-y -evil-CON.pdf` in `%TEMP%\PrintInvoicer previews`; Detail PDF writes `Invoice x-.-.-y -evil-CON (detail).pdf` in the invoice folder. A normal job's Preview name is unchanged. Optional (review WR-03): set the invoice folder in Settings to a path with a trailing space and check that invoices still generate.
result: [pending]

### 4. Windows Explorer check (A2), end-of-phase gate
expected: Each Settings Open button (data, backup, invoice folder) opens Explorer at that folder. `explorer.exe /select, "C:\Program Files\Windows Defender\MpCmdRun.exe"` in cmd highlights `MpCmdRun.exe` (the form `ShellCommands.ShowFile` sends). If not highlighted, apply the fallback in 02-04-SUMMARY.md. Record the result in the "Windows Explorer check (A2)" section of 02-04-SUMMARY.md, replacing PENDING.
result: [pending]

### 5. Checksum on the next real release tag
expected: On the next `v<version>` tag, the draft release has `PrintInvoicer-<version>.zip` and `PrintInvoicer-<version>.zip.sha256`, a `SHA-256:` line in the notes, the "Write the SHA-256 checksum file" step passes, and `Get-FileHash .\PrintInvoicer-<version>.zip -Algorithm SHA256` matches the notes and the file.
result: [pending]

### 6. Maintainer review of judgment items
expected: The FIX-02 test uses literal totals; docs do not claim the checksum proves who built the release; the QuestPDF text is not presented as legal advice and matches QuestPDF's page.
result: [pending]

## Summary

total: 6
passed: 0
issues: 0
pending: 6
skipped: 0
blocked: 0

## Gaps
