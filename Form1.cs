using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinkReplacer {
    public partial class Form1 : Form
    {
        List<string> idList = new List<string>();
        List<string> addressList = new List<string>();
        List<string> fileList = new List<string>();
        private CancellationTokenSource _cts;
        private ToolTip _threadTip;

        private sealed class BatchProgress
        {
            public int Done;
            public int Total;
            public string LogText = "";
        }

        private sealed class WorkItem
        {
            public int Index;
            public string Path = "";
            public long Size;
        }

        // Largest-first + one-at-a-time work stealing. Files vary hugely in size,
        // so fixed range chunks leave one thread stuck on the big PDFs while the
        // rest sit idle. Sorting big-first and letting idle threads pull the next
        // file keeps the tail short. Results stay indexed by original position so
        // CSV/output order is unchanged.
        private static List<WorkItem> OrderLargestFirst(List<string> files)
        {
            var items = new List<WorkItem>(files.Count);
            for (int i = 0; i < files.Count; i++)
            {
                long size = 0;
                try { size = new FileInfo(files[i]).Length; } catch { size = 0; }
                items.Add(new WorkItem { Index = i, Path = files[i], Size = size });
            }
            items.Sort((a, b) => b.Size.CompareTo(a.Size));
            return items;
        }

        private void ProgressBarDrawString(string value) //Draws the percentage on the progressbar
        {
            progressBar1.Refresh();
            using (var g = progressBar1.CreateGraphics())
            using (var font = new Font(FontFamily.GenericSansSerif, (float)7.8, FontStyle.Regular))
            {
                g.DrawString(value, font, Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 7));
            }
        }

        // iTextSharp loads whole PDFs into large byte[]s (LOH). The GC does not
        // promptly return that to the OS after a big batch, so working set can sit
        // at many GB after "Done". Compact the LOH and force a full collection
        // once per batch; per-file readers are already closed/disposed by then.
        private static void ReleaseBatchMemory()
        {
            try
            {
                System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            }
            catch { }
            try
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            }
            catch { }
        }

        private void ProgressBarNoAnimation(int value) //Prevent the progressbar from lagging behind, disables the animation by going backwards
        {
            if (value < 0)
                value = 0;
            if (value > progressBar1.Maximum)
                value = progressBar1.Maximum;
            if (value == progressBar1.Maximum)
            {
                progressBar1.Maximum = value + 1;
                progressBar1.Value = value + 1;
                progressBar1.Maximum = value;
            } else
            {
                progressBar1.Value = value + 1;
            }
            progressBar1.Value = value;
        }

        private void SetBusy(bool busy)
        {
            buttonLinkReplacer.Enabled = !busy;
            buttonDNATool.Enabled = !busy;
            buttonLinkGrabber.Enabled = !busy;
            buttonFlattenCheck.Enabled = !busy;
            buttonFlatten.Enabled = !busy;
            buttonFiles.Enabled = !busy;
            buttonFolder.Enabled = !busy;
            buttonOutputFolder.Enabled = !busy;
            comboSite.Enabled = !busy;
            comboDNATool.Enabled = !busy;
            if (numericThreads != null)
                numericThreads.Enabled = !busy;
            if (buttonCancel != null)
                buttonCancel.Enabled = busy;
        }

        private int GetMaxThreads()
        {
            try
            {
                if (numericThreads != null)
                    return Math.Max(1, Math.Min(64, (int)numericThreads.Value));
            }
            catch { }
            return Environment.ProcessorCount;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            try { _cts?.Cancel(); } catch { }
        }

        private static string ProgressPercent(int done, int total)
        {
            if (total <= 0)
                return "0%";
            return (done * 100 / total) + "%";
        }

        public Form1()
        {
            InitializeComponent();
        }

        private string GetSitesCsvPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sites.csv");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string sitesCsvPath = GetSitesCsvPath();
            if (!File.Exists(sitesCsvPath))
            {
                MessageBox.Show($"Required file not found:\n{sitesCsvPath}", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var reader = new StreamReader(sitesCsvPath))
            {
                while (!reader.EndOfStream) //Initializes the dropdown list of sites for SharePoint and DNATool
                {
                    var line = reader.ReadLine().Split(','); //Name,SiteID,SiteAddress

                    idList.Add(line[1]);
                    addressList.Add(line[2]);
                    comboSite.Items.Add(line[1] + ": " + line[0]);
                    comboDNATool.Items.Add(line[1] + ": " + line[0]);
                }
            }
            //Initialization
            openFileDialog1.FileName = String.Empty;
            folderBrowserDialog1.SelectedPath = Directory.GetCurrentDirectory();
            textFolder.Text = Directory.GetCurrentDirectory();
            textOutputFolder.Text = Directory.GetCurrentDirectory() + @"\Output";
            radioSharePoint.Checked = true;
            radioFindReplace.Checked = true;
            try
            {
                if (numericThreads != null)
                {
                    numericThreads.Minimum = 1;
                    numericThreads.Maximum = 64;
                    numericThreads.Value = Math.Max(1, Math.Min(64, Environment.ProcessorCount));
                }
            }
            catch { }
            try
            {
                _threadTip = new ToolTip
                {
                    AutoPopDelay = 15000,
                    InitialDelay = 400,
                    ReshowDelay = 100,
                    ShowAlways = true
                };
                const string threadTip = "How many PDF files to process at once.\n\nEach thread holds a full PDF in memory, so memory use grows with this number. Lower to 2-4 for large PDFs if memory gets high; raise toward CPU count for many small PDFs.";
                if (numericThreads != null)
                    _threadTip.SetToolTip(numericThreads, threadTip);
                if (labelThreads != null)
                    _threadTip.SetToolTip(labelThreads, threadTip);
            }
            catch { }
            Main.UnmapSharePoint();
        }

        private void RadioSharePoint_CheckedChanged(object sender, EventArgs e) //Enables the SharePoint panel
        {
            panelFiles.Enabled = false;
            panelFolder.Enabled = false;
            panelSharepoint.Enabled = true;
        }

        private void RadioFiles_CheckedChanged(object sender, EventArgs e) //Enables the Files panel
        {
            panelSharepoint.Enabled = false;
            panelFolder.Enabled = false;
            panelFiles.Enabled = true;
        }

        private void RadioFolder_CheckedChanged(object sender, EventArgs e) //Enables the Folder panel
        {
            panelSharepoint.Enabled = false;
            panelFiles.Enabled = false;
            panelFolder.Enabled = true;
        }

        private void ComboSite_SelectedIndexChanged(object sender, EventArgs e) //Getting files from SharePoint
        {
            textLog.AppendText("Connecting to Sharepoint\n:");
            textLog.AppendText(addressList[comboSite.SelectedIndex] + "\n");
            fileList = Main.ReadSharePoint(addressList[comboSite.SelectedIndex], checkRecursive.Checked);
            labelNumOfFilesLocal.Text = "Number of files: " + fileList.Count; //Updates number of files
            comboDNATool.SelectedIndex = comboSite.SelectedIndex; //Matches DNATool combobox selection
        }

        private void ButtonFile_Click(object sender, EventArgs e) //Selecting individual files
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileList = Main.ReadFiles(openFileDialog1.FileNames);
                labelNumOfFilesLocal.Text = "Number of files: " + fileList.Count; //Updates number of files
            }
        }

        private void ButtonFolder_Click(object sender, EventArgs e) //Selecting folder
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textFolder.Text = folderBrowserDialog1.SelectedPath;
                TextFolder_LostFocus(sender, e); //Update folder
            }
        }

        private void TextFolder_LostFocus(object sender, EventArgs e) //Grabs all files from selected folder
        {
            RefreshFileList();
        }

        private void RefreshFileList(bool verbose = true)
        {
            if (radioFolder.Checked)
            {
                fileList = Main.ReadFolder(textFolder.Text, checkRecursive.Checked);
                labelNumOfFilesLocal.Text = "Number of files: " + fileList.Count; //Updates number of files
            }
            else if (radioSharePoint.Checked)
            {
                if (comboSite.SelectedIndex >= 0)
                {
                    if (verbose)
                    {
                        textLog.AppendText("Connecting to Sharepoint\n:");
                        textLog.AppendText(addressList[comboSite.SelectedIndex] + "\n");
                    }
                    fileList = Main.ReadSharePoint(addressList[comboSite.SelectedIndex], checkRecursive.Checked);
                    labelNumOfFilesLocal.Text = "Number of files: " + fileList.Count; //Updates number of files
                    comboDNATool.SelectedIndex = comboSite.SelectedIndex; //Matches DNATool combobox selection
                }
            }
            // radioFiles: keep the explicitly chosen files; nothing to refresh.
        }

        private void CheckRecursive_CheckedChanged(object sender, EventArgs e)
        {
            if (radioFolder.Checked && !string.IsNullOrWhiteSpace(textFolder.Text))
            {
                TextFolder_LostFocus(sender, e);
            }
            else if (radioSharePoint.Checked && comboSite.SelectedIndex >= 0)
            {
                ComboSite_SelectedIndexChanged(sender, e);
            }
        }

        private string GetSelectedSourceRoot()
        {
            if (radioSharePoint.Checked)
            {
                return @"B:\";
            }

            if (radioFolder.Checked)
            {
                return textFolder.Text;
            }

            return string.Empty;
        }

        private string GetOutputFilePath(string filePath)
        {
            Directory.CreateDirectory(textOutputFolder.Text);

            if (checkRecursive.Checked)
            {
                string sourceRoot = GetSelectedSourceRoot();
                if (!string.IsNullOrWhiteSpace(sourceRoot))
                {
                    string fullSourceRoot = Path.GetFullPath(sourceRoot);
                    string fullFilePath = Path.GetFullPath(filePath);

                    if (fullFilePath.StartsWith(fullSourceRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        string relativePath = fullFilePath.Substring(fullSourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string relativeDirectory = Path.GetDirectoryName(relativePath);

                        if (!string.IsNullOrWhiteSpace(relativeDirectory))
                        {
                            string outputDirectory = Path.Combine(textOutputFolder.Text, relativeDirectory);
                            Directory.CreateDirectory(outputDirectory);
                            return Path.Combine(outputDirectory, Path.GetFileName(filePath));
                        }
                    }
                }
            }

            return Path.Combine(textOutputFolder.Text, Path.GetFileName(filePath));
        }

        private void ButtonOutputFolder_Click(object sender, EventArgs e) //Displays the Folder Browser Dialog
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textOutputFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private async void ButtonLinkReplacer_Click(object sender, EventArgs e) //Finds and replaces matching strings in PDF hyperlinks
        {
            RefreshFileList(verbose: false);
            if (fileList.Count == 0)
            {
                MessageBox.Show("No files found!", "ERROR");
                return;
            } else if (textFindString.Text.Equals("") && radioFindReplace.Checked)
            {
                MessageBox.Show("Please input a string to find!", "ERROR");
                return;
            }

            string find = textFindString.Text;
            string replace = textReplaceString.Text;
            int mode = 0;
            Directory.CreateDirectory(textOutputFolder.Text); //Output folder for the new PDFs

            if (radioFindReplace.Checked)
            {
                mode = 0; //Find-replace all
            } else if (radioAddStart.Checked)
            {
                mode = 1; //Add to start of strings that contain the find string
            } else if (radioAddEnd.Checked)
            {
                mode = 2; //Add to end of strings that contain the find string
            }

            List<string> files = fileList.ToList(); //Snapshot so the list can't mutate mid-run
            List<string> outputs = files.Select(GetOutputFilePath).ToList(); //Pre-create output dirs on UI thread

            //Initializes progress bar and log
            progressBar1.Value = 0;
            progressBar1.Maximum = Math.Max(1, files.Count);
            ProgressBarDrawString("0%");
            textLog.Clear();

            try { _cts?.Dispose(); } catch { }
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            SetBusy(true);

            System.IProgress<BatchProgress> progress = new System.Progress<BatchProgress>(u =>
            {
                ProgressBarNoAnimation(u.Done);
                ProgressBarDrawString(ProgressPercent(u.Done, u.Total));
                if (!string.IsNullOrEmpty(u.LogText))
                    textLog.AppendText(u.LogText);
                textLog.ScrollToCaret();
            });

            bool cancelled = false;
            try
            {
                int done = 0;
                var options = new ParallelOptions { MaxDegreeOfParallelism = GetMaxThreads(), CancellationToken = token };
                var ordered = OrderLargestFirst(files);
                var partitioner = Partitioner.Create(ordered, EnumerablePartitionerOptions.NoBuffering);
                await Task.Run(() => Parallel.ForEach(partitioner, options, item =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    int i = item.Index;
                    string name = Path.GetFileName(item.Path);
                    string line;
                    try
                    {
                        line = name + Main.LinkReplacer(item.Path, outputs[i], find, replace, mode) + "\n"; //Replaces links in current PDF and outputs count
                    }
                    catch (Exception ex)
                    {
                        line = " - ERROR: Cannot read file " + name + " (" + ex.Message + ")\n";
                    }
                    int d = Interlocked.Increment(ref done);
                    progress.Report(new BatchProgress { Done = d, Total = files.Count, LogText = line });
                }), token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                textLog.AppendText("ERROR: " + ex.Message + "\n");
            }
            finally
            {
                SetBusy(false);
                try { _cts?.Dispose(); } catch { }
                _cts = null;
                files = null;
                outputs = null;
                ReleaseBatchMemory();
            }

            if (cancelled)
                textLog.AppendText("Cancelled.\n");
            else
                textLog.AppendText("Done! :)");
            textLog.ScrollToCaret();
            ProgressBarDrawString(cancelled ? ProgressPercent(progressBar1.Value, progressBar1.Maximum) : "100% Done! :)");

            Main.UnmapSharePoint(); //Unmaps B: drive in case files were grabbed from SharePoint
        }

        private async void ButtonDNATool_Click(object sender, EventArgs e) //Generates a CSV file of all the assets in the PDFs
        {
            RefreshFileList(verbose: false);
            int index = comboDNATool.SelectedIndex;
            if (fileList.Count == 0)
            {
                MessageBox.Show("No files found!", "ERROR");
                return;
            } else if (index == -1)
            {
                MessageBox.Show("Please choose a site!", "ERROR");
                return;
            }

            string siteAddress = addressList[index];
            string csvPath = Path.Combine(textOutputFolder.Text, idList[index] + ".csv");
            Directory.CreateDirectory(textOutputFolder.Text); //Output folder for CSV

            List<string> files = fileList.ToList(); //Snapshot so the list can't mutate mid-run

            //Initializes progress bar and log
            progressBar1.Value = 0;
            progressBar1.Maximum = Math.Max(1, files.Count);
            ProgressBarDrawString("0%");
            textLog.Clear();

            try { _cts?.Dispose(); } catch { }
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            SetBusy(true);

            System.IProgress<BatchProgress> progress = new System.Progress<BatchProgress>(u =>
            {
                ProgressBarNoAnimation(u.Done);
                ProgressBarDrawString(ProgressPercent(u.Done, u.Total));
                if (!string.IsNullOrEmpty(u.LogText))
                    textLog.AppendText(u.LogText);
                textLog.ScrollToCaret();
            });

            string[] logs = new string[files.Count];
            List<string>[] csvRows = new List<string>[files.Count];
            bool cancelled = false;
            try
            {
                int done = 0;
                var options = new ParallelOptions { MaxDegreeOfParallelism = GetMaxThreads(), CancellationToken = token };
                var ordered = OrderLargestFirst(files);
                var partitioner = Partitioner.Create(ordered, EnumerablePartitionerOptions.NoBuffering);
                await Task.Run(() => Parallel.ForEach(partitioner, options, item =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    int i = item.Index;
                    string fileName = Path.GetFileName(item.Path);
                    try
                    {
                        Main.DnaToolResult r = Main.DNATool(item.Path, fileName, siteAddress); //Scans PDF and returns assets
                        logs[i] = fileName + "\n" + r.Log;
                        csvRows[i] = r.Rows;
                    }
                    catch (Exception ex)
                    {
                        logs[i] = "ERROR: Cannot read file " + fileName + " (" + ex.Message + ")\n";
                        csvRows[i] = new List<string>();
                    }
                    int d = Interlocked.Increment(ref done);
                    progress.Report(new BatchProgress { Done = d, Total = files.Count, LogText = logs[i] });
                }), token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                textLog.AppendText("ERROR: " + ex.Message + "\n");
                cancelled = true;
            }

            //Single-threaded ordered CSV write so lines never interleave.
            try
            {
                using (StreamWriter writer = new StreamWriter(new FileStream(csvPath, FileMode.Create), Encoding.UTF8))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        List<string> rows = csvRows[i];
                        if (rows == null)
                            continue;
                        foreach (string row in rows)
                            writer.WriteLine(row);
                    }
                }
            }
            catch (Exception ex)
            {
                textLog.AppendText("ERROR: Cannot write CSV (" + ex.Message + ")\n");
            }
            finally
            {
                SetBusy(false);
                try { _cts?.Dispose(); } catch { }
                _cts = null;
                try
                {
                    if (logs != null) Array.Clear(logs, 0, logs.Length);
                    if (csvRows != null) Array.Clear(csvRows, 0, csvRows.Length);
                }
                catch { }
                logs = null;
                csvRows = null;
                files = null;
                ReleaseBatchMemory();
            }

            if (cancelled)
                textLog.AppendText("Cancelled (partial CSV kept).\n");
            else
                textLog.AppendText("Done! :)");
            textLog.ScrollToCaret();
            ProgressBarDrawString(cancelled ? ProgressPercent(progressBar1.Value, progressBar1.Maximum) : "100% Done! :)");

            Main.UnmapSharePoint(); // Unmaps B: drive in case files were grabbed from SharePoint
        }

        private async void ButtonLinkGrabber_Click(object sender, EventArgs e) //Grabs every hyperlink from the selected PDFs
        {
            RefreshFileList(verbose: false);
            int index = comboDNATool.SelectedIndex;
            if (fileList.Count == 0)
            {
                MessageBox.Show("No files found!", "ERROR");
                return;
            }
            else if (index == -1)
            {
                MessageBox.Show("Please choose a site!", "ERROR");
                return;
            }

            string csvPath = Path.Combine(textOutputFolder.Text, idList[index] + " - Links.csv");
            Directory.CreateDirectory(textOutputFolder.Text); //Output folder for CSV

            List<string> files = fileList.ToList(); //Snapshot so the list can't mutate mid-run

            //Initializes progress bar and log
            progressBar1.Value = 0;
            progressBar1.Maximum = Math.Max(1, files.Count);
            ProgressBarDrawString("0%");
            textLog.Clear();

            try { _cts?.Dispose(); } catch { }
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            SetBusy(true);

            System.IProgress<BatchProgress> progress = new System.Progress<BatchProgress>(u =>
            {
                ProgressBarNoAnimation(u.Done);
                ProgressBarDrawString(ProgressPercent(u.Done, u.Total));
                if (!string.IsNullOrEmpty(u.LogText))
                    textLog.AppendText(u.LogText);
                textLog.ScrollToCaret();
            });

            string[] logs = new string[files.Count];
            List<string>[] csvRows = new List<string>[files.Count];
            bool cancelled = false;
            try
            {
                int done = 0;
                var options = new ParallelOptions { MaxDegreeOfParallelism = GetMaxThreads(), CancellationToken = token };
                var ordered = OrderLargestFirst(files);
                var partitioner = Partitioner.Create(ordered, EnumerablePartitionerOptions.NoBuffering);
                await Task.Run(() => Parallel.ForEach(partitioner, options, item =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    int i = item.Index;
                    string fileName = Path.GetFileName(item.Path);
                    try
                    {
                        Main.LinkGrabberResult r = Main.LinkGrabber(item.Path, fileName); //Scans PDF and returns links
                        logs[i] = fileName + r.Log;
                        csvRows[i] = r.Rows;
                    }
                    catch (Exception ex)
                    {
                        logs[i] = fileName + " - ERROR: Cannot read file " + fileName + " (" + ex.Message + ")\n";
                        csvRows[i] = new List<string>();
                    }
                    int d = Interlocked.Increment(ref done);
                    progress.Report(new BatchProgress { Done = d, Total = files.Count, LogText = logs[i] });
                }), token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                textLog.AppendText("ERROR: " + ex.Message + "\n");
                cancelled = true;
            }

            //Single-threaded ordered CSV write so lines never interleave.
            try
            {
                using (StreamWriter writer = new StreamWriter(new FileStream(csvPath, FileMode.Create), Encoding.UTF8))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        List<string> rows = csvRows[i];
                        if (rows == null)
                            continue;
                        foreach (string row in rows)
                            writer.WriteLine(row);
                    }
                }
            }
            catch (Exception ex)
            {
                textLog.AppendText("ERROR: Cannot write CSV (" + ex.Message + ")\n");
            }
            finally
            {
                SetBusy(false);
                try { _cts?.Dispose(); } catch { }
                _cts = null;
                try
                {
                    if (logs != null) Array.Clear(logs, 0, logs.Length);
                    if (csvRows != null) Array.Clear(csvRows, 0, csvRows.Length);
                }
                catch { }
                logs = null;
                csvRows = null;
                files = null;
                ReleaseBatchMemory();
            }

            if (cancelled)
                textLog.AppendText("Cancelled (partial CSV kept).\n");
            else
                textLog.AppendText("Done! :)");
            textLog.ScrollToCaret();
            ProgressBarDrawString(cancelled ? ProgressPercent(progressBar1.Value, progressBar1.Maximum) : "100% Done! :)");

            Main.UnmapSharePoint(); // Unmaps B: drive in case files were grabbed from SharePoint
        }

        private async void ButtonFlattenCheck_Click(object sender, EventArgs e) //Exports a CSV listing all PDFs that still need flattening
        {
            RefreshFileList(verbose: false);
            if (fileList.Count == 0)
            {
                MessageBox.Show("No files found!", "ERROR");
                return;
            }

            string csvPath = Path.Combine(textOutputFolder.Text, "Unflattened PDFs.csv");
            Directory.CreateDirectory(textOutputFolder.Text); //Output folder for CSV

            List<string> files = fileList.ToList(); //Snapshot so the list can't mutate mid-run

            //Initializes progress bar and log
            progressBar1.Value = 0;
            progressBar1.Maximum = Math.Max(1, files.Count);
            ProgressBarDrawString("0%");
            textLog.Clear();

            try { _cts?.Dispose(); } catch { }
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            SetBusy(true);

            System.IProgress<BatchProgress> progress = new System.Progress<BatchProgress>(u =>
            {
                ProgressBarNoAnimation(u.Done);
                ProgressBarDrawString(ProgressPercent(u.Done, u.Total));
                if (!string.IsNullOrEmpty(u.LogText))
                    textLog.AppendText(u.LogText);
                textLog.ScrollToCaret();
            });

            string[] logs = new string[files.Count];
            List<string>[] csvRows = new List<string>[files.Count];
            bool cancelled = false;
            try
            {
                int done = 0;
                var options = new ParallelOptions { MaxDegreeOfParallelism = GetMaxThreads(), CancellationToken = token };
                var ordered = OrderLargestFirst(files);
                var partitioner = Partitioner.Create(ordered, EnumerablePartitionerOptions.NoBuffering);
                await Task.Run(() => Parallel.ForEach(partitioner, options, item =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    int i = item.Index;
                    string fileName = Path.GetFileName(item.Path);
                    try
                    {
                        Main.FlattenCheckResult r = Main.FlattenCheck(item.Path, fileName); //Scans PDF and returns what would need flattening
                        logs[i] = fileName + r.Log;
                        csvRows[i] = r.Rows;
                    }
                    catch (Exception ex)
                    {
                        logs[i] = fileName + " - ERROR: Cannot read file " + fileName + " (" + ex.Message + ")\n";
                        csvRows[i] = new List<string>();
                    }
                    int d = Interlocked.Increment(ref done);
                    progress.Report(new BatchProgress { Done = d, Total = files.Count, LogText = logs[i] });
                }), token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                textLog.AppendText("ERROR: " + ex.Message + "\n");
                cancelled = true;
            }

            //Single-threaded ordered CSV write so lines never interleave.
            try
            {
                using (StreamWriter writer = new StreamWriter(new FileStream(csvPath, FileMode.Create), Encoding.UTF8))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        List<string> rows = csvRows[i];
                        if (rows == null)
                            continue;
                        foreach (string row in rows)
                            writer.WriteLine(row);
                    }
                }
            }
            catch (Exception ex)
            {
                textLog.AppendText("ERROR: Cannot write CSV (" + ex.Message + ")\n");
            }
            finally
            {
                SetBusy(false);
                try { _cts?.Dispose(); } catch { }
                _cts = null;
                try
                {
                    if (logs != null) Array.Clear(logs, 0, logs.Length);
                    if (csvRows != null) Array.Clear(csvRows, 0, csvRows.Length);
                }
                catch { }
                logs = null;
                csvRows = null;
                files = null;
                ReleaseBatchMemory();
            }

            if (cancelled)
                textLog.AppendText("Cancelled (CSV kept).\n");
            else
                textLog.AppendText("Done! :)");
            textLog.ScrollToCaret();
            ProgressBarDrawString(cancelled ? ProgressPercent(progressBar1.Value, progressBar1.Maximum) : "100% Done! :)");

            Main.UnmapSharePoint(); // Unmaps B: drive in case files were grabbed from SharePoint
        }

        private async void ButtonFlatten_Click(object sender, EventArgs e) //Flattens markup annotations and form fields into the page, links stay clickable
        {
            RefreshFileList(verbose: false);
            if (fileList.Count == 0)
            {
                MessageBox.Show("No files found!", "ERROR");
                return;
            }

            Directory.CreateDirectory(textOutputFolder.Text); //Output folder for the flattened PDFs

            List<string> files = fileList.ToList(); //Snapshot so the list can't mutate mid-run
            List<string> outputs = files.Select(GetOutputFilePath).ToList(); //Pre-create output dirs on UI thread

            //Initializes progress bar and log
            progressBar1.Value = 0;
            progressBar1.Maximum = Math.Max(1, files.Count);
            ProgressBarDrawString("0%");
            textLog.Clear();

            try { _cts?.Dispose(); } catch { }
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            SetBusy(true);

            System.IProgress<BatchProgress> progress = new System.Progress<BatchProgress>(u =>
            {
                ProgressBarNoAnimation(u.Done);
                ProgressBarDrawString(ProgressPercent(u.Done, u.Total));
                if (!string.IsNullOrEmpty(u.LogText))
                    textLog.AppendText(u.LogText);
                textLog.ScrollToCaret();
            });

            bool cancelled = false;
            try
            {
                int done = 0;
                var options = new ParallelOptions { MaxDegreeOfParallelism = GetMaxThreads(), CancellationToken = token };
                var ordered = OrderLargestFirst(files);
                var partitioner = Partitioner.Create(ordered, EnumerablePartitionerOptions.NoBuffering);
                await Task.Run(() => Parallel.ForEach(partitioner, options, item =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    int i = item.Index;
                    string name = Path.GetFileName(item.Path);
                    string line;
                    try
                    {
                        line = name + Main.FlattenPDF(item.Path, outputs[i]) + "\n"; //Flattens the current PDF and returns the log line
                    }
                    catch (Exception ex)
                    {
                        line = " - ERROR: Cannot flatten file " + name + " (" + ex.Message + ")\n";
                    }
                    int d = Interlocked.Increment(ref done);
                    progress.Report(new BatchProgress { Done = d, Total = files.Count, LogText = line });
                }), token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                textLog.AppendText("ERROR: " + ex.Message + "\n");
            }
            finally
            {
                SetBusy(false);
                try { _cts?.Dispose(); } catch { }
                _cts = null;
                files = null;
                outputs = null;
                ReleaseBatchMemory();
            }

            if (cancelled)
                textLog.AppendText("Cancelled.\n");
            else
                textLog.AppendText("Done! :)");
            textLog.ScrollToCaret();
            ProgressBarDrawString(cancelled ? ProgressPercent(progressBar1.Value, progressBar1.Maximum) : "100% Done! :)");

            Main.UnmapSharePoint(); // Unmaps B: drive in case files were grabbed from SharePoint
        }
    }
}
