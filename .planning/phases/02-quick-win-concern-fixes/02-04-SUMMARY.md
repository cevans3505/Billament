---
phase: 02-quick-win-concern-fixes
plan: 04
subsystem: files-and-shell
tags: [file-names, windows-reserved-names, explorer, argumentlist, audit]
requires:
  - phase: 02-03
    provides: AUDITING.md tag count of 27, which this plan keeps
provides:
  - SafeFileName (Core): Clean, Compose and IsDirectlyInside for file names built from user data
  - InvoicePdf.Write names all three PDF kinds through SafeFileName and refuses a path outside its folder
  - ShellCommands (Core): ShowFolder and ShowFile build Explorer requests with ArgumentList
  - Settings Open buttons and the error report start Explorer through ShellCommands
affects: [phase 2 close-out (Windows Explorer check A2 passed on 2026-11-02)]
status: complete
requirements-completed: [FIX-06, FIX-03]
actuals:
  tokens: 14000
  tasks: 3
  commits: 3
plan_head_before: a4c51c0759b86790d145696d8356d4c4d0b6143c
plan_head_after: 7622c4ded5fb005b848db30896ccf097bb306f14
tech-stack:
  added: []
  patterns: ["fixed forbidden-character list instead of the OS list, so tests on Linux give the Windows answer", "Core builds a ProcessStartInfo and never starts it; the App starts it under the existing AUDIT comment"]
key-files:
  created:
    - src/PrintInvoicer.Core/Files/SafeFileName.cs
    - src/PrintInvoicer.Core/Files/ShellCommands.cs
    - tests/PrintInvoicer.Tests/SafeFileNameTests.cs
    - tests/PrintInvoicer.Tests/ShellCommandsTests.cs
  modified:
    - src/PrintInvoicer.App/Pdf/InvoicePdf.cs
    - src/PrintInvoicer.App/ViewModels/SettingsViewModel.cs
    - src/PrintInvoicer.App/ErrorReport.cs
    - AUDITING.md
    - .planning/REQUIREMENTS.md
key-decisions:
  - "Title limit 60, invoice number limit 30, whole name limit 120 UTF-16 chars (user decision, kept)"
  - "Rules-version-2 issue left for Phase 3 (FIX-08), as the user decided"
  - "FIX-03 wording in REQUIREMENTS.md now mentions refusing a path with a double quote, as the user asked; the code passes such a path through as one argument, and refusal exists only in the A2 fallback (see Deviations)"
coverage:
  - id: D1
    description: "Clean turns text with .., \\, /, :, <, >, quote, |, ?, *, control characters, broken surrogates, dot runs and reserved device names into a safe, length-limited name; empty text becomes untitled; a cut never splits a surrogate pair"
    requirement: FIX-06
    verification:
      - kind: test
        ref: "tests/PrintInvoicer.Tests/SafeFileNameTests.cs (70 cases, all pass)"
        status: pass
    human_judgment: false
  - id: D2
    description: "Names inside the 'Which invoice names change' boundary are unchanged and the listed outside cases get their pinned new names"
    requirement: FIX-06
    verification:
      - kind: test
        ref: "SafeFileNameTests Normal_invoice_names_are_unchanged, Titles_inside_the_boundary_keep_their_old_name, Titles_outside_the_boundary_get_a_new_name, A_title_over_60_chars_gets_a_new_name"
        status: pass
    human_judgment: false
  - id: D3
    description: "InvoicePdf.Write builds Preview, Invoice and detail names with Compose and throws InvalidOperationException for a path not directly in its folder; the invoice folder from Settings is used as given"
    requirement: FIX-06
    verification:
      - kind: other
        ref: "App and UiTests build on Linux with -p:EnableWindowsTargeting=true, 0 warnings 0 errors; IsDirectlyInside covered by Only_files_directly_in_the_folder_count_as_inside; acceptance greps pass"
        status: pass
      - kind: manual_procedural
        ref: "Windows: ScreenTests.Every_invoice_kind_renders_for_every_job and the hostile-title draft (see Pending Human Checks)"
        status: unknown
    human_judgment: true
  - id: D4
    description: "ShowFolder and ShowFile return explorer.exe with UseShellExecute, empty Arguments and each path as its own ArgumentList entry (quote, space, trailing backslash, switch-like text); both call sites use them under their existing AUDIT comments; 27 AUDIT tags remain"
    requirement: FIX-03
    verification:
      - kind: test
        ref: "tests/PrintInvoicer.Tests/ShellCommandsTests.cs (8 cases, all pass)"
        status: pass
      - kind: other
        ref: "git grep -c 'AUDIT:' -- src totals 27; AUDITING.md says 27 tags and has the ArgumentList sentence"
        status: pass
    human_judgment: false
  - id: D5
    description: "Explorer highlights the file for '/select,' followed by the path as a separate argument (assumption A2)"
    requirement: FIX-03
    verification:
      - kind: manual_procedural
        ref: "Windows Explorer check (A2) below"
        status: pass
    human_judgment: true
duration: 15min
completed: 2026-11-02
---

# Phase 2 Plan 04: Safe invoice file names, and Explorer paths passed as single arguments Summary

`SafeFileName` now builds every invoice, detail-sheet and preview PDF name from a cleaned invoice number (30 chars) and title (60 chars, 120 for the whole name) and `InvoicePdf.Write` refuses any path outside its folder, while `ShellCommands` makes the Settings Open buttons and the error report hand Explorer each path as its own `ArgumentList` entry.

## Accomplishments

- New Core type `PrintInvoicer.Core.Files.SafeFileName`: `Clean(text, maxLength)`, `Compose(prefix, number, title, suffix)`, `IsDirectlyInside(folder, path)`, constants `MaxTitleLength = 60`, `MaxNumberLength = 30`, `MaxNameLength = 120`, `Fallback = "untitled"`. It uses a fixed forbidden list (not the OS list), turns control characters (including U+007F to U+009F) and unpaired surrogates into `-`, collapses dot runs, trims spaces and dots, prefixes reserved device names (including the superscript COM and LPT forms, CONIN$ and CONOUT$) with `_`, and cuts without splitting a surrogate pair. Source is ASCII (superscripts written as escapes).
- `InvoicePdf.Write`: three `SafeFileName.Compose` calls, the `IsDirectlyInside` guard right after the path is built (throws `InvalidOperationException`), the old `Safe` helper removed. The `number` line, the folder choice and the `// AUDIT: file write.` comment are unchanged.
- New Core type `PrintInvoicer.Core.Files.ShellCommands` (`ShowFolder`, `ShowFile`). `SettingsViewModel.OpenFolder` and `ErrorReport.Show` use it; their `// AUDIT: opens a program.` comments are untouched. The five other `Process.Start` calls were not changed.
- AUDITING.md: Invoices and previews rows mention `SafeFileName`; the Programs started section explains `ArgumentList`; tag count still 27.
- REQUIREMENTS.md: FIX-03 reworded as the user asked.

## Task Commits

| Task | Name | Commit |
|------|------|--------|
| 1 | End-to-end safe invoice names (SafeFileName, InvoicePdf wiring, 32 tests) | 2db6579 |
| 2 | Inside-the-folder guard, hostile-name matrix (70 tests total), AUDITING.md rows | 0d063b4 |
| 3 | ShellCommands, call sites, 8 tests, AUDITING.md note | 7622c4d |

## Verification

- `SafeFileNameTests`: 70 passed (32 after Task 1). `ShellCommandsTests`: 8 passed.
- Full Linux suite `dotnet test tests/PrintInvoicer.Tests`: 464 passed, 0 failed (was 386 before this plan; the plan needed more than 372).
- `dotnet build src/PrintInvoicer.App -p:EnableWindowsTargeting=true` and `dotnet build tests/PrintInvoicer.UiTests -p:EnableWindowsTargeting=true`: 0 warnings, 0 errors.
- Acceptance greps pass: three `Compose(` calls, no `Safe(` helper, no `GetInvalidFileNameChars` in InvoicePdf.cs or SafeFileName.cs, `number` line kept, AUDIT comment directly above `GeneratePdf(path)`, ASCII sources, no em or en dashes, no hand-built `explorer.exe` argument strings, `git grep -c 'AUDIT:' -- src` totals 27, AUDITING.md still says 27 tags.
- UI tests were not run (Windows only).

## Which invoice names change

Before this phase `InvoicePdf.Write` named a PDF `{prefix} {number} {Safe(title)}{suffix}`. In the Windows app, `Safe` turned `" < > | : * ? \ /` and the characters U+0000 to U+001F into `-`, then trimmed whitespace from both ends; the invoice number went in as typed. Now both parts go through `SafeFileName.Clean` (title limit 60, number limit 30). A PDF keeps exactly its old name when, and only when, both parts keep their old text.

**Title.** The title keeps its old text if all six hold, and gets a new one otherwise:

1. It has no character from U+007F to U+009F and no unpaired surrogate (kept before; each now becomes `-`).
2. It has no run of two or more dots (now one dot).
3. After the forbidden and control characters become `-`, and ignoring plain spaces at either end, it neither starts nor ends with a dot or with another whitespace character such as a no-break space (the old code trimmed every whitespace character and no dot; the new code trims plain spaces and dots).
4. It is not empty or whitespace only (that used to give `Invoice 2026-001 .pdf`; now `Invoice 2026-001 untitled.pdf`).
5. Trimmed, it is at most 60 UTF-16 chars (a longer title is now cut, never splitting a character).
6. Its part before the first dot, with trailing spaces removed, is not a Windows device name (CON, PRN, AUX, NUL, COM0 to COM9, LPT0 to LPT9, the superscript COM and LPT forms, CONIN$, CONOUT$, in any case); such a title now starts with `_`.

**Invoice number.** The number keeps its old text if it already equals `SafeFileName.Clean(number, 30)`: at most 30 UTF-16 chars, not empty, none of `< > : " / \ | ? *`, no control character or unpaired surrogate, no run of two or more dots, no space or dot at either end, and its part before the first dot is not a device name. Draft numbers (`draft yyyy-MM-dd`) and numbers the counter makes with the default prefix (`{yyyy}-` and three digits, such as `2026-001`) always keep their text. A number holding `/`, `\`, `:` or another forbidden character never gave a normal PDF in the invoice folder before, so no working name is lost there.

**Effect and decision.** File names do not feed pricing: `PricingCalculator`, `PriceSnapshot` and the stored `FinalizedTotal` never read them, so no finalized total changes. The only effect of a changed name is that regenerating that invoice writes the PDF under the new name and leaves the earlier PDF beside it. Nothing is deleted or renamed, because that would be new file access outside FIX-06. This is accepted for the cases above, which are unusual titles and typed numbers; a title over 60 chars is the likeliest real one. The tests pin both sides of the boundary. No correction to the boundary was needed while implementing Task 1: every planned expectation passed on first run (the leading no-break space case is written `\u00A0Tube caps` in the test to keep the source ASCII).

## Folder names

FIX-06 says "file and folder names". `InvoicePdf.Write` picks its folder in one expression: `PreviewFolder` for a preview, otherwise `DefaultFolder` (`Documents\PrintInvoicer\Invoices`) when the Settings `InvoiceFolder` is blank, else that Settings folder. None of the three reads the job (title, customer, number or date), and no subfolder is made per job, so there is no folder name built from job data to clean, and FIX-06's folder part is out of scope by construction. The Settings folder is a full path the user chose (such as `D:\Invoices`) and is used as given; the `IsDirectlyInside` guard makes sure the cleaned file name cannot climb out of whichever folder was picked. If a later change builds a folder from job data, it must go through `SafeFileName.Clean` too.

## Windows Explorer check (A2)

File highlighted: two-argument form kept.

Checked on Windows 11 on 2026-11-02 by starting the same `ProcessStartInfo` that `ShellCommands` builds. `/select,` and `C:\Program Files\Windows Defender\MpCmdRun.exe` as separate `ArgumentList` entries opened Windows Defender with `MpCmdRun.exe` highlighted. `ShowFolder` with `C:\Program Files\Common Files` opened that folder. No fallback was applied. Details are in 02-UAT.md, test 4.

## Deviations from Plan

None to the plan's code or tests. Notes:

- **Lone surrogates in tests (Rule 3, test technique):** C# attribute strings cannot carry lone surrogates (the compiler stores U+FFFD), so the first version of two InlineData cases failed for a reason unrelated to `Clean`. Those cases moved to two `[Fact]`s that build the string from `(char)0xD800` and `(char)0xDC00`. The `Hostile_text_comes_out_safe` theory no longer holds a lone-surrogate row; `Broken_surrogates_come_out_safe` covers it. No change to `Clean`.
- **Task 1 commit amended once:** the first heredoc wrote the superscript digits as raw characters instead of `\u00B9` escapes, which broke the "ASCII source" criterion. I fixed the file and amended my own unpushed commit before moving on (2db6579 is the amended hash).
- **Test count above the plan's floor:** `SafeFileNameTests` has 70 cases (the plan asked for at least 50 after Task 2).
- **FIX-03 wording versus the code (needs your eye):** the user asked for REQUIREMENTS.md to say a path containing a double quote is refused. The delivered code does not refuse such a path: with `ArgumentList` a path with a quote stays one argument (tests prove it). Refusal exists only in the plan's A2 fallback (`ShowFile` throws `ArgumentException` for a quote, then builds a quoted `Arguments` string). I worded FIX-03 as "...extra Explorer switches; if Explorer needs one quoted string instead, a path containing a double quote is refused" so it is true either way. Reword if you want something else.
- Duration is an estimate; the start time was not recorded.

## Known Stubs

None.

## Threat Flags

None. No new file, network or process access: `SafeFileName` touches no file, `ShellCommands` only builds a `ProcessStartInfo`, and the two start sites keep their existing `// AUDIT:` comments.

## Pending Human Checks

All WINDOWS-ONLY. None were run and none are marked passed.

1. **Screen tests.** On Windows run `dotnet test PrintInvoicer.sln`, or open a pull request to `main` so the CI screen-tests job runs. `ScreenTests.Every_invoice_kind_renders_for_every_job` must pass (its `InvoicePdf.Write` call now goes through `SafeFileName` and must return a path inside the invoice folder).
2. **Hostile-name PDFs.** In the built app create a draft job titled `..\evil:CON` with invoice number `x\..\..\y` (add one plate by hand). Click Preview, then Detail PDF. Expected: both PDFs open; files sit directly in `%TEMP%\PrintInvoicer previews` and in the invoice folder with names like `Preview x-.-.-y -evil-CON.pdf` and `Invoice x-.-.-y -evil-CON (detail).pdf` (check the exact spelling against the cleaned text). Also open an existing normal job and click Preview: its file name is the usual one.
3. **Explorer check (A2), END-OF-PHASE GATE.** (a) In the built app open Settings and click each Open button (data folder, backup folder, invoice folder): Explorer opens at each folder. (b) In cmd run `explorer.exe /select, "C:\Program Files\Windows Defender\MpCmdRun.exe"`: Explorer opens that folder with `MpCmdRun.exe` highlighted. Record the result under "Windows Explorer check (A2)" above. If (b) opens a different place, apply the fallback: change only `ShellCommands.ShowFile` to set `Arguments` to `/select,` followed by the double-quoted path (after throwing `ArgumentException` for a path containing a double quote), update its theory and the AUDITING.md sentence, check `explorer.exe /select,"C:\Program Files\Windows Defender\MpCmdRun.exe"` highlights the file, and record "not highlighted: fallback applied" with the commit.

## Self-Check: PASSED

- FOUND: src/PrintInvoicer.Core/Files/SafeFileName.cs, src/PrintInvoicer.Core/Files/ShellCommands.cs, tests/PrintInvoicer.Tests/SafeFileNameTests.cs, tests/PrintInvoicer.Tests/ShellCommandsTests.cs
- FOUND: commits 2db6579, 0d063b4, 7622c4d are ancestors of HEAD
