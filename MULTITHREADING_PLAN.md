# Multithreading Implementation Plan — LinkReplacer-DNATool

Target: `net472` WinForms (`LinkReplacer.csproj:6`), iTextSharp 5.5.13.2.
Scope: make batch PDF processing multi-threaded, keep UI responsive, add cancellation.

## 1. Current state

* `Main.cs:61 DNATool()`, `Main.cs:120 LinkReplacer()`, `Main.cs:247 LinkGrabber()` each process **one file**.
* `Form1.cs:253,307,363` (`ButtonLinkReplacer_Click`, `ButtonDNATool_Click`, `ButtonLinkGrabber_Click`) loop `for (i < fileList.Count)` **sequentially on the UI thread**. This causes `TODO.md:3` freeze.
* `DNATool` / `LinkGrabber` take a shared `StreamWriter writer` — **not thread-safe**.
* `LinkReplacer` is already per-file independent (own `PdfReader`/`PdfStamper`/output path, GUID temp file at `Main.cs:139`) — **trivially parallelizable**.
* `ProgressBarDrawString` (`Form1.cs:15`) uses `CreateGraphics()` and `textLog.AppendText` — **UI-thread only**.
* `Main.cs:14 UnmapSharePoint()`, `Main.cs:25 ReadSharePoint()`, `Main.cs:44 ReadFolder()` are single I/O operations — **keep sequential**.

Constraint: `net472` has no `Parallel.ForEachAsync` (.NET 6+). Use `Task.Run(() => Parallel.ForEach(...))` + `async/await`. `LangVersion:latest` allows modern syntax.

Decision: **parallelize across files only**. Do NOT parallelize pages within one PDF — a single `PdfReader` instance is not safe for concurrent page access.

## 2. Design

### 2.1 Threading model

* UI thread: owns all controls, owns `IProgress<T>` callbacks.
* Worker pool: `Parallel.ForEach` with `MaxDegreeOfParallelism = Environment.ProcessorCount` (or `ProcessorCount - 1` to keep UI smooth; make it a constant, optionally a NumericUpDown later).
* Pattern per button handler:
  ```csharp
  private CancellationTokenSource _cts;
  private async void ButtonX_Click(...) {
    var snapshot = fileList.ToList(); // avoid mutation during run
    _cts = new CancellationTokenSource();
    SetBusy(true);
    var progress = new Progress<(int done, int total, string log)>(t => {
      ProgressBarNoAnimation(...); ProgressBarDrawString(...);
      textLog.AppendText(t.log); textLog.ScrollToCaret();
    });
    try {
      await Task.Run(() => ProcessParallel(snapshot, progress, _cts.Token));
    } catch (OperationCanceledException) { textLog.AppendText("Cancelled.\n"); }
    finally { SetBusy(false); Main.UnmapSharePoint(); }
  }
  ```
* `ParallelOptions { MaxDegreeOfParallelism, CancellationToken }`. Call `token.ThrowIfCancellationRequested()` per iteration; handle `OperationCanceledException`.
* Ordered results: pre-size arrays `string[] logs = new string[n]`, `List<string>[] rows = new List<string>[n]` indexed by `i` so CSV output order matches input order. Use `Interlocked.Increment(ref done)` for progress.

### 2.2 `StreamWriter` fix (required before any parallelism)

Change producers to pure functions returning data; single-threaded writer at the end.

* `Main.DNATool`:
  ```csharp
  // old: string DNATool(path, fileName, siteAddress, StreamWriter writer)
  // new: DnaResult DNATool(path, fileName, siteAddress)
  //   where DnaResult { string Log; List<string> Rows; }
  ```
  Build `List<string> rows` locally, `rows.Add($"\"{assetNum}\",,\"{site}/{file}\",,\"{file}\"")`, return. No shared state.
* `Main.LinkGrabber`: same — return `(string Log, List<string> Rows)`.
* `Main.LinkReplacer`: no signature change. Returns per-file log string as today.
* CSV write phase (UI thread or one `Task.Run` continuation after `Parallel.ForEach` completes):
  ```csharp
  using (var writer = new StreamWriter(..., false, Encoding.UTF8))
    for (i...) foreach (var r in rows[i]) writer.WriteLine(r);
  ```
  This preserves deterministic order and avoids `lock(writer)` contention.

### 2.3 UI responsiveness + cancellation

* Add `buttonCancel` to `Form1.Designer.cs` (next to `progressBar1:248`, same row as `textLog:426`). Wired to `_cts?.Cancel()`.
* `SetBusy(bool)`: disables `buttonLinkReplacer`, `buttonDNATool`, `buttonLinkGrabber`, `buttonFiles`, `buttonFolder`, `comboSite`; enables `buttonCancel` when busy.
* Batch log appends: to avoid 10k `IProgress` marshals, report per-file (fine for <5k files). For very large batches, throttle to every N files or 100 ms.
* Fix `ProgressBarDrawString`: keep on UI thread only; guard divide-by-zero when `count == 0`.
* Make SharePoint/folder discovery non-blocking (Phase 2): `ComboSite_SelectedIndexChanged` (`Form1.cs:97`), `RefreshFileList` (`Form1.cs:129`) → `async void` + `await Task.Run(() => Main.ReadSharePoint(...))`. Not strictly required for multithreading but same freeze class.

## 3. Step-by-step changes

### Phase 0 — safety, no behavior change
1. Extract per-file bodies into helpers returning strings (so parallel loop is a move, not a rewrite).
2. Pre-create output dirs before the loop (`Directory.CreateDirectory` once); keep `GetOutputFilePath` (`Form1.cs:180`) read-only during parallel phase. Pre-compute `fileOutputPath[]` array for LinkReplacer.
3. Snapshot `fileList`, `find/replace/mode`, `siteAddress` into locals before `Task.Run` (avoid closure over mutable form fields).

### Phase 1 — `LinkReplacer` (easiest, no CSV)
1. `Form1.ButtonLinkReplacer_Click` → `async void`, `await Task.Run(() => Parallel.ForEach(...))`.
2. Body: compute `fileName` via `Path.GetFileName` (replace `Substring(LastIndexOf("\\"))` at `Form1.cs:256,310,365` — handles `/` too), call `Main.LinkReplacer`, store `logs[i]`, `progress.Report`.
3. Keep `Main.UnmapSharePoint()` after `await` on UI thread.

### Phase 2 — `DNATool` + `LinkGrabber` (needs signature change)
1. Refactor `Main.cs:61,247` to return results as in §2.2. Update the two call sites.
2. Parallel loop fills `logs[i]` / `rows[i]`.
3. After `await`, write CSV sequentially on UI thread (or `await Task.Run` without parallelism). Keep `FileMode.Create`, `Encoding.UTF8` as today (`Form1.cs:299,354`).
4. Preserve current log semantics: `DNATool` warnings, `LinkGrabber` `" - N link(s) found"` + `"\n"`.

### Phase 3 — Cancel + busy state + progress polish
1. Add `CancellationTokenSource _cts`, `buttonCancel`, `SetBusy`.
2. Pass token into `ParallelOptions`; per-iteration check; CSV write checks token between chunks.
3. On cancel: stop scheduling new files, let in-flight PDFs finish, delete their partial outputs (existing `Main.cs:221,242` cleanup already does this), report `"Cancelled at k/n"`.
4. Handle `AggregateException` → flatten to per-file error strings; never crash the batch on one bad PDF (preserve current `catch → "ERROR: Cannot read file"` behavior).

### Phase 4 — Async discovery (optional but recommended)
1. `ComboSite_SelectedIndexChanged`, `TextFolder_LostFocus`, `CheckRecursive_CheckedChanged` → async wrappers with a small “Loading…” log line and disabled combo during `ReadSharePoint`/`ReadFolder`.

## 4. Code sketch (LinkReplacer loop)

```csharp
var files = fileList.ToList();
var outputs = files.Select(GetOutputFilePath).ToList();
var logs = new string[files.Count];
int done = 0;
var opts = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = token };
await Task.Run(() => Parallel.For(0, files.Count, opts, i => {
  opts.CancellationToken.ThrowIfCancellationRequested();
  string name = Path.GetFileName(files[i]);
  string res;
  try { res = Main.LinkReplacer(files[i], outputs[i], find, replace, mode); }
  catch (Exception ex) { res = $" - ERROR: Cannot read file {name} ({ex.Message})"; }
  logs[i] = name + res + "\n";
  int d = Interlocked.Increment(ref done);
  progress.Report((d, files.Count, logs[i]));
}), token);
```

DNATool/LinkGrabber add `List<string>[] csv = new List<string>[n]` alongside `logs`.

## 5. Testing

1. Build `net472` on Windows, run from `bin/` so `Sites.csv` resolves (`Form1.cs:40 GetSitesCsvPath`).
2. Correctness: batch of ~20 mixed PDFs (with/without annots, 1 corrupt file) — diff outputs + CSV row multisets vs. sequential build. Expect identical sets; order identical due to indexed write-back.
3. Concurrency: 200-file folder, recursive on/off; verify CPU saturates (~ProcessorCount workers), no `InvalidOperationException: Cross-thread operation`, no interleaved CSV lines, no leftover `.tmp-*`.
4. UI: resize/bring-to-front during run (regression for `TODO.md:3`); Cancel mid-run leaves valid completed outputs + clean `B:` unmap.
5. In-place mode (output == input): run twice; second run finds 0 occurrences, no truncation.

## 6. Risks / notes

* iTextSharp `PdfReader`/`PdfStamper` per-thread is fine; do not share instances or static caches across threads.
* `B:` drive mapping is global process state — all SharePoint I/O must complete before workers start; do not parallelize mapping itself.
* `lock(writer)` alternative rejected: correct but nondeterministic order + contention; indexed collect-then-write is preferred.
* If batches are dominated by many tiny PDFs, cap parallelism to avoid disk contention; if by few huge PDFs, file-level parallelism gives limited speedup (accepted — page-level is unsafe).
* Future: upgrade to `net8.0-windows` to get `Parallel.ForEachAsync` + `await` inside loop; not required for this plan.

## 7. Order of work

1. Phase 0 + Phase 1 (LinkReplacer parallel, UI async shell, no signature breaks).
2. Phase 2 (DNATool/LinkGrabber refactor + ordered CSV write).
3. Phase 3 (Cancel button + `SetBusy` + `AggregateException` polish).
4. Phase 4 (async discovery) if time permits.
