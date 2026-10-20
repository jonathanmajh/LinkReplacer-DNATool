# AGENTS.md

## Project

Single-project WinForms desktop tool (C# / .NET Framework 4.7.2, SDK-style csproj) that batch-scans and edits PDF link annotations using iTextSharp v5 (package `iTextSharp` 5.5.13.2 — not iText7). Windows-only: the SharePoint source maps a site to the `B:` drive via `net use`, so SharePoint-mode behavior can only be exercised on the user's network.

## Build & verify

- `dotnet build LinkReplacer.csproj --configuration Release` — that's the whole verification story. There is no test project, linter, or typecheck; do not invent test commands.
- Build output lands in `bin/Release/net472/` (TFM subfolder), not `bin/Release/` directly.
- `Sites.csv` must sit next to the exe at runtime (loaded from `AppDomain.CurrentDomain.BaseDirectory`); the build copies it there automatically.
- Releases: `.github/workflows/publish.yml` builds Release, zips `bin/Release/net472/*`, and attaches the zip to a GitHub Release when a `v*` tag is pushed (manual `workflow_dispatch` otherwise).
- Default branch is `master`.

## Architecture

- `Program.cs` → `Application.Run(new Form1())`. `Form1.cs` is the UI plus all batch orchestration.
- `Main.cs` defines a class named `Main` holding every PDF operation (`DNATool`, `LinkReplacer`, `LinkGrabber`, `FlattenCheck`, `FlattenPDF`). Despite the name it is NOT the entry point — `Program.Main` is.
- Keep the existing split: `Main.cs` does PDF parsing but never touches UI controls; `Form1.cs` orchestrates but never parses PDFs.

## PDF-processing invariants

- Only link/widget annotations whose `/A` action has `/S = /URI` are read or rewritten. Internal page links (`/Dest`, no `/A`) must stay skipped — replicate this filter in any new scan code.
- In-place edits (output path == input path) write to a `.tmp-<guid>` file first and copy over the original only after success; on error the temp file is deleted and the original is never touched.
- Files with zero changes are not written at all (LinkReplacer writes only when replacements > 0; FlattenPDF skips already-flat files).
- `PdfReader`, `PdfStamper`, and streams must be closed and disposed per file — past releases fixed memory/GDI leaks from missed disposal.
- iTextSharp loads whole PDFs into the LOH. `ReleaseBatchMemory()` (LOH compaction + full GC) runs once per batch — call it in the `finally` of any new batch operation.

## Batch/UI conventions (Form1.cs)

- Every batch operation follows the same shape: snapshot `fileList`, sort `OrderLargestFirst` (file sizes vary hugely; fixed chunks idle threads), `Partitioner.Create(..., NoBuffering)`, `Parallel.ForEach` inside `Task.Run` with `MaxDegreeOfParallelism = GetMaxThreads()` (1–64 from the UI control), and per-file exceptions caught and turned into a log line, not thrown.
- Results go into arrays indexed by original file position, then are written single-threaded in order after the loop, so CSV/output order is deterministic regardless of parallel completion order.
- UI updates from worker threads only via `Progress<BatchProgress>` — never touch controls directly on worker threads.
- `SetBusy(true/false)` gates all buttons during runs; cancellation goes through the shared `CancellationTokenSource` `_cts` and the Cancel button.

## Style constraints

- `Nullable` and `ImplicitUsings` are disabled in the csproj. Write old-style code: explicit `using` statements, no nullability annotations.

## Runtime data

- `Sites.csv` lines are `Name,SiteID,SiteAddress` (no header). It feeds both the SharePoint and DNATool site dropdowns; DNATool writes its output to `<SiteID>.csv`.
- DNATool extracts the asset number as 5 characters at `IndexOf("assetnum") + 9` — a fragile positional slice; don't "clean it up" casually.

## Docs

- `PROGRAM_FUNCTIONALITY.md` documents end-user behavior; `TODO.md` is the open-issues list; `LinkReplacer DNATool SOP.docx` is the user-facing SOP.
