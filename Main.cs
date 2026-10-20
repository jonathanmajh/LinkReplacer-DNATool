//*IMPORTANT* Requires iTextSharp v5.5.13 - available in NuGet
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using iTextSharp.text.pdf;

namespace LinkReplacer
{
    public class Main
    {
        public sealed class DnaToolResult
        {
            public string Log = "";
            public List<string> Rows = new List<string>();
        }

        public sealed class LinkGrabberResult
        {
            public string Log = "";
            public List<string> Rows = new List<string>();
        }

        public sealed class FlattenCheckResult
        {
            public string Log = "";
            public List<string> Rows = new List<string>();
            public bool IsFlattened = true; //True when nothing needs flattening
            public int NonLinkAnnotations = 0; //Markup annotations that flattening would bake into the page
            public int TotalAnnotations = 0; //All annotations found (links included, popups included)
            public string Reasons = ""; //e.g. "FreeText=16; Square=33; form fields=3"
        }
        public static void UnmapSharePoint() //Unmaps B: drive
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo("cmd.exe");
            string unmap = "/c net use B: /delete /y"; //COMMAND: net use B: /delete /y
            cmdStartInfo.Arguments = unmap; //Unmaps B: drive and waits for command to finish
            cmdStartInfo.CreateNoWindow = true;
            cmdStartInfo.UseShellExecute = false;
            Process cmd = Process.Start(cmdStartInfo); //Runs the command with given arguments
            cmd.WaitForExit(); //Waits for command to finish
        }

        public static List<string> ReadSharePoint(string address, bool recursive = false) //Map chosen SharePoint site to B: drive and gets PDF filepaths
        {
            UnmapSharePoint(); //Unmap B: drive just in case it was already mapped previously
            string map = @"/c net use B: ""\\" + address + "\""; //COMMAND: net use B: "\\address"
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo("cmd.exe");
            cmdStartInfo.Arguments = map;
            cmdStartInfo.CreateNoWindow = true; //Does not show the window
            cmdStartInfo.UseShellExecute = false;
            Process cmd = Process.Start(cmdStartInfo); //Runs the command with given arguments
            cmd.WaitForExit(); //Waits for command to finish

            return ReadFolder(@"B:\", recursive); //Can treat B: as normal folder
        }

        public static List<string> ReadFiles(string[] fileNames) //Individually selected files (no real reason to have a function but is consistent)
        {
            return fileNames.OfType<string>().ToList(); //Converts string array to list of strings
        }

        public static List<string> ReadFolder(string folder, bool recursive = false) //Gets filepaths of all PDFs in the folder
        {
            List<string> fileList = new List<string>();
            if (Directory.Exists(folder) == false)
            {
                MessageBox.Show("Folder does not exist!", "ERROR");
                return fileList;
            }

            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (string file in Directory.GetFiles(folder, "*.pdf", searchOption)) //All PDFs in the folder
            {
                fileList.Add(file);
            }
            return fileList;
        }

        public static DnaToolResult DNATool(string filePath, string fileName, string siteAddress) //Scans one PDF and returns found assets
        {
            DnaToolResult result = new DnaToolResult();
            string error = "";
            PdfReader reader = new PdfReader(filePath);
            try
            {

            for (int page = 1; page <= reader.NumberOfPages; page++) //Loops through all the pages
            {
                PdfDictionary pageDict = reader.GetPageN(page); //Honestly not sure how this works...
                PdfArray annotArray = pageDict.GetAsArray(PdfName.ANNOTS);

                if (annotArray == null) //If there are no annotations on that page
                {
                    error += "WARNING: No annotaions found (Page " + page + ")\n";
                    continue;
                }

                foreach (PdfObject annot in annotArray) //Loops through every annotation on that page
                {
                    PdfDictionary annotDict = PdfReader.GetPdfObject(annot) as PdfDictionary;
                    if (annotDict == null)
                        continue;

                    PdfObject subtype = annotDict.Get(PdfName.SUBTYPE);
                    if (!PdfName.LINK.Equals(subtype) && !PdfName.WIDGET.Equals(subtype)) //If the annotation is a hyperlink
                        continue;

                    // Internal page links use /Dest instead of /A - only URI actions matter here.
                    if (annotDict.Get(PdfName.A) == null)
                        continue;
                    PdfDictionary annotAction = PdfReader.GetPdfObject(annotDict.Get(PdfName.A)) as PdfDictionary;
                    if (annotAction == null)
                        continue;

                    if (PdfName.URI.Equals(annotAction.Get(PdfName.S)))
                    {
                        PdfString uriString = annotAction.GetAsString(PdfName.URI);
                        if (uriString == null)
                            continue;
                        string foundURL = uriString.ToString();
                        int index = foundURL.IndexOf("assetnum"); //Searches for blue box links

                        if (index != -1) //If it's a blue box
                        {
                            string assetNum = foundURL.Substring(index + 9, 5); //From counting
                            result.Rows.Add("\"" + assetNum + "\",,\"" + siteAddress + "/" + fileName + "\",,\"" + fileName + "\""); //Adds the asset to the CSV rows
                        }
                    }
                }
            }
            }
            finally
            {
                try { reader.Close(); } catch { }
                try { ((IDisposable)reader).Dispose(); } catch { }
            }
            result.Log = error;
            return result;
        }

        public static string LinkReplacer(string filePath, string fileOutputPath, string find, string replace, int mode) //Replaces links in current PDF and outputs count
        {
            int replaceNum = 0; //Number of replaced instances

            // When output == input, writing directly would truncate the source
            // while PdfReader is still using it. Write to a temp file first.
            bool inPlace = string.Equals(
                Path.GetFullPath(filePath),
                Path.GetFullPath(fileOutputPath),
                StringComparison.OrdinalIgnoreCase);
            string actualOutputPath = fileOutputPath;
            string tempPath = null;
            if (inPlace)
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(fileOutputPath));
                if (string.IsNullOrEmpty(directory))
                {
                    directory = Path.GetTempPath();
                }
                tempPath = Path.Combine(directory,
                    Path.GetFileNameWithoutExtension(filePath) + ".tmp-" + Guid.NewGuid().ToString("N") + Path.GetExtension(filePath));
                actualOutputPath = tempPath;
            }

            PdfReader reader = null;
            FileStream outputStream = null;
            PdfStamper stamper = null;
            try
            {
                reader = new PdfReader(filePath);
                outputStream = new FileStream(actualOutputPath, FileMode.Create);
                stamper = new PdfStamper(reader, outputStream);

            for (int page = 1; page <= reader.NumberOfPages; page++) //Loops through all the pages
            {
                PdfDictionary pageDict = reader.GetPageN(page); //Honestly not sure how this works...
                PdfArray annotArray = pageDict.GetAsArray(PdfName.ANNOTS);

                if (annotArray == null) //If there are no annotations on that page
                {
                    continue;
                }

                foreach (PdfObject annot in annotArray) //Loops through every annotation on that page
                {
                    PdfDictionary annotDict = PdfReader.GetPdfObject(annot) as PdfDictionary;
                    if (annotDict == null)
                        continue;

                    PdfObject subtype = annotDict.Get(PdfName.SUBTYPE);
                    if (!PdfName.LINK.Equals(subtype) && !PdfName.WIDGET.Equals(subtype)) //If the annotation is a hyperlink
                        continue;

                    // Internal page links use /Dest instead of /A - only URI actions matter here.
                    if (annotDict.Get(PdfName.A) == null)
                        continue;
                    PdfDictionary annotAction = PdfReader.GetPdfObject(annotDict.Get(PdfName.A)) as PdfDictionary;
                    if (annotAction == null)
                        continue;

                    if (PdfName.URI.Equals(annotAction.Get(PdfName.S)))
                    {
                        PdfString uriString = annotAction.GetAsString(PdfName.URI);
                        if (uriString == null)
                            continue;
                        string foundURL = uriString.ToString();

                            if (foundURL.IndexOf(find) != -1) //If the string exists
                            {
                                string newURL;
                                switch (mode)
                                {
                                    case 0: //Find-replace all
                                        replaceNum += foundURL.Split(new string[] { find }, System.StringSplitOptions.None).Length - 1; //Counts number of instances
                                        newURL = foundURL.Replace(find, replace);
                                        annotAction.Put(PdfName.URI, new PdfString(newURL)); //Replaces the old URL with the new URL
                                        break;
                                    case 1: //Add to start of strings that contain the find string
                                        replaceNum++;
                                        newURL = replace + foundURL;
                                        annotAction.Put(PdfName.URI, new PdfString(newURL)); //Replaces the old URL with the new URL
                                        break;
                                    case 2: //Add to end of strings that contain the find string
                                        replaceNum++;
                                        newURL = foundURL + replace;
                                        annotAction.Put(PdfName.URI, new PdfString(newURL)); //Replaces the old URL with the new URL
                                        break;
                                }
                            }
                        }
                }
            }
            if (replaceNum > 0)
            {
                stamper.Close();
                stamper = null;
            } else
            {
                try { stamper.Dispose(); } catch { }
                stamper = null;
                try { if (outputStream != null) outputStream.Dispose(); } catch { }
                outputStream = null;
                File.Delete(actualOutputPath);
            }

                try { reader.Close(); } catch { }
                try { if (reader is IDisposable rd) rd.Dispose(); } catch { }
                reader = null;
                try { if (outputStream != null) outputStream.Dispose(); } catch { }
                outputStream = null;

                if (inPlace && replaceNum > 0)
                {
                    // Atomically replace the original only after the temp file is fully written.
                    File.Copy(actualOutputPath, filePath, true);
                    File.Delete(actualOutputPath);
                }

                return " - " + replaceNum + " occurance(s) found";
            }
            catch
            {
                try { if (stamper != null) stamper.Dispose(); } catch { }
                try { if (outputStream != null) outputStream.Dispose(); } catch { }
                try { if (reader != null) reader.Close(); } catch { }
                try { if (reader is IDisposable rd) rd.Dispose(); } catch { }
                // Remove the partial/temp output; never touch the original on failure.
                try { if (actualOutputPath != null && File.Exists(actualOutputPath)) File.Delete(actualOutputPath); } catch { }
                throw;
            }
        }

        public static LinkGrabberResult LinkGrabber(string filePath, string fileName) //Scans one PDF and returns found links
        {
            LinkGrabberResult result = new LinkGrabberResult();
            string error = "\n";
            int foundLinks = 0; //Number of links found
            PdfReader reader = new PdfReader(filePath);
            try
            {

            for (int page = 1; page <= reader.NumberOfPages; page++) //Loops through all the pages
            {
                PdfDictionary pageDict = reader.GetPageN(page); //Honestly not sure how this works...
                PdfArray annotArray = pageDict.GetAsArray(PdfName.ANNOTS);

                if (annotArray == null) //If there are no annotations on that page
                {
                    continue;
                }

                try
                {
                    foreach (PdfObject annot in annotArray) //Loops through every annotation on that page
                    {
                        PdfDictionary annotDict = PdfReader.GetPdfObject(annot) as PdfDictionary;
                        if (annotDict == null)
                            continue;

                        PdfObject subtype = annotDict.Get(PdfName.SUBTYPE);
                        if (!PdfName.LINK.Equals(subtype) && !PdfName.WIDGET.Equals(subtype)) //If the annotation is a hyperlink
                            continue;

                        // Internal page links use /Dest instead of /A - only URI actions matter here.
                        if (annotDict.Get(PdfName.A) == null)
                            continue;
                        PdfDictionary annotAction = PdfReader.GetPdfObject(annotDict.Get(PdfName.A)) as PdfDictionary;
                        if (annotAction == null)
                            continue;

                        if (PdfName.URI.Equals(annotAction.Get(PdfName.S)))
                        {
                            PdfString uriString = annotAction.GetAsString(PdfName.URI);
                            if (uriString == null)
                                continue;
                            string foundURL = uriString.ToString();

                                try
                                {
                                    if (foundURL.Substring(0, 4).Equals("http")) //If it's an actual link
                                    {
                                        result.Rows.Add("\"" + foundURL + "\",\"" + fileName + "\""); //Adds the link to the CSV rows
                                        foundLinks++;
                                    }
                                }
                                catch { } //Intentionally left blank
                            }
                    }
                }
                catch
                {
                    error += "  WARNING: Problem reading page " + page + "\n";
                }
                
            }
            }
            finally
            {
                try { reader.Close(); } catch { }
                try { ((IDisposable)reader).Dispose(); } catch { }
            }
            result.Log = " - " + foundLinks + " link(s) found" + error;
            return result;
        }

        public static FlattenCheckResult FlattenCheck(string filePath, string fileName) //Scans one PDF and reports annotations that still need flattening
        {
            FlattenCheckResult result = null;
            PdfReader reader = new PdfReader(filePath);
            try
            {
                result = CheckFlattening(reader);
            }
            finally
            {
                try { reader.Close(); } catch { }
                try { ((IDisposable)reader).Dispose(); } catch { }
            }

            if (!result.IsFlattened) //Only unflattened files are listed in the CSV
            {
                //Columns: filename, reason breakdown, non-link annotations, total annotations
                result.Rows.Add("\"" + fileName + "\",\"" + result.Reasons + "\",\"" + result.NonLinkAnnotations + "\",\"" + result.TotalAnnotations + "\"");
            }
            return result;
        }

        private static FlattenCheckResult CheckFlattening(PdfReader reader) //Counts the annotations (and form fields) that flattening would have to bake into the pages
        {
            FlattenCheckResult result = new FlattenCheckResult();
            string warnings = "";
            SortedDictionary<string, int> breakdown = new SortedDictionary<string, int>();

            for (int page = 1; page <= reader.NumberOfPages; page++) //Loops through all the pages
            {
                PdfArray annotArray = reader.GetPageN(page).GetAsArray(PdfName.ANNOTS);

                if (annotArray == null) //No annotations on that page
                {
                    continue;
                }

                try
                {
                    foreach (PdfObject annot in annotArray) //Loops through every annotation on that page
                    {
                        PdfDictionary annotDict = PdfReader.GetPdfObject(annot) as PdfDictionary;
                        if (annotDict == null)
                            continue;

                        PdfObject subtype = annotDict.Get(PdfName.SUBTYPE);
                        if (subtype == null)
                            continue;

                        string subtypeName = subtype.ToString().Substring(1); //"/FreeText" -> "FreeText"
                        result.TotalAnnotations++;

                        if (subtypeName.Equals("Link") || subtypeName.Equals("Popup")) //Links must survive flattening; popups are invisible hover windows tied to their parent
                            continue;

                        result.NonLinkAnnotations++;
                        if (breakdown.ContainsKey(subtypeName))
                            breakdown[subtypeName]++;
                        else
                            breakdown[subtypeName] = 1;
                    }
                }
                catch
                {
                    warnings += "  WARNING: Problem reading page " + page + "\n";
                }
            }

            List<string> parts = new List<string>();
            foreach (var entry in breakdown)
                parts.Add(entry.Key + "=" + entry.Value);

            int formFields = reader.AcroFields.Fields.Count; //Interactive form fields (check boxes, text) also mean the PDF is not flattened
            if (formFields > 0)
                parts.Add("form fields=" + formFields);

            result.Reasons = string.Join("; ", parts);

            int totalToFlatten = result.NonLinkAnnotations + formFields;
            if (totalToFlatten > 0)
            {
                result.IsFlattened = false;
                result.Log = " - NOT FLATTENED (" + totalToFlatten + " item(s) to flatten): " + result.Reasons + "\n";
            }
            else
            {
                result.IsFlattened = true;
                result.Log = " - already flat\n";
            }

            if (warnings != "")
                result.Log += warnings;

            return result;
        }

        public static string FlattenPDF(string filePath, string fileOutputPath) //Flattens one PDF: burns markup annotations and form fields into the pages, links stay clickable
        {
            // When output == input, writing directly would truncate the source
            // while PdfReader is still using it. Write to a temp file first.
            bool inPlace = string.Equals(
                Path.GetFullPath(filePath),
                Path.GetFullPath(fileOutputPath),
                StringComparison.OrdinalIgnoreCase);
            string actualOutputPath = fileOutputPath;
            string tempPath = null;
            if (inPlace)
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(fileOutputPath));
                if (string.IsNullOrEmpty(directory))
                {
                    directory = Path.GetTempPath();
                }
                tempPath = Path.Combine(directory,
                    Path.GetFileNameWithoutExtension(filePath) + ".tmp-" + Guid.NewGuid().ToString("N") + Path.GetExtension(filePath));
                actualOutputPath = tempPath;
            }

            PdfReader reader = null;
            FileStream outputStream = null;
            PdfStamper stamper = null;
            try
            {
                reader = new PdfReader(filePath);

                FlattenCheckResult check = CheckFlattening(reader); //Find out if anything needs flattening
                if (check.IsFlattened)
                {
                    try { reader.Close(); } catch { }
                    try { ((IDisposable)reader).Dispose(); } catch { }
                    reader = null;
                    return " - already flat, not written"; //Matches LinkReplacer: files without changes are not written
                }

                outputStream = new FileStream(actualOutputPath, FileMode.Create);
                stamper = new PdfStamper(reader, outputStream);

                stamper.FormFlattening = true; //Bakes interactive form fields into the page content
                stamper.AnnotationFlattening = true; //Burns markup annotations into the page content; link annotations are preserved

                stamper.Close(); //The flattening happens when the stamper closes
                stamper = null;

                try { reader.Close(); } catch { }
                try { ((IDisposable)reader).Dispose(); } catch { }
                reader = null;
                try { if (outputStream != null) outputStream.Dispose(); } catch { }
                outputStream = null;

                if (inPlace)
                {
                    // Atomically replace the original only after the temp file is fully written.
                    File.Copy(actualOutputPath, filePath, true);
                    File.Delete(actualOutputPath);
                }

                return " - flattened (" + check.Reasons + ")";
            }
            catch
            {
                try { if (stamper != null) stamper.Dispose(); } catch { }
                try { if (outputStream != null) outputStream.Dispose(); } catch { }
                try { if (reader != null) reader.Close(); } catch { }
                try { if (reader is IDisposable rd) rd.Dispose(); } catch { }
                // Remove the partial/temp output; never touch the original on failure.
                try { if (actualOutputPath != null && File.Exists(actualOutputPath)) File.Delete(actualOutputPath); } catch { }
                throw;
            }
        }
    }
}
