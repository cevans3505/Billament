# Phase 3: Rename to Billament - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in 03-CONTEXT.md; this log preserves how they were reached.

**Date:** 2026-11-02
**Phase:** 03-rename-to-billament
**Mode:** answers given up front by the user (no interactive questions were needed)

---

## Name

| Option | Selected |
|--------|----------|
| Billament, one word (Billament.exe, Billament.* projects) | Yes |

**User's choice:** Billament (bill plus filament). One word makes a clean exe and folder name.

## Placement

| Option | Selected |
|--------|----------|
| Next phase, before Data Safety Fixes | Yes |
| After the current milestone | |

**User's choice:** Next phase, so later phases use the new name from the start. Asked for an integer Phase 3 with
the later phases renumbered (done: Data Safety Fixes is now 4, Slice File Submissions 5).

## Existing data

| Option | Selected |
|--------|----------|
| Move old folders once on first start, after a backup | Yes |
| Keep using the old folders | |
| Copy and leave the old folders | |

**User's choice:** Move once after a backup; never touch the old folders if the new ones exist; database renamed to
billament.db; failures show a plain message and leave the old data; logic in Core with Linux tests; consider custom
BackupFolder / InvoiceFolder settings inside the old folder.

## GitHub repository

| Option | Selected |
|--------|----------|
| Rename to cevans3505/Billament (by hand at ship time) | Yes |
| Keep the old repository name | |

**User's choice:** Rename. Research must verify the API redirect for `/repos/{owner}/{repo}/releases/latest` so 3.7.0
users still find the update.

## Version

| Option | Selected |
|--------|----------|
| 4.0.0, Read me heading "Billament 4.0" | Yes |

## Claude's Discretion

- Pre-move backup location and name, move mechanics, legacy backup prefix handling, identifier names containing the
  old name, temp preview folder cleanup, plan and wave order.

## Deferred Ideas

- New icon design; code signing.
