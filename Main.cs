//*IMPORTANT* Requires iTextSharp v5.5.13 - available in NuGet
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace LinkReplacer
{
    public class Main
    {
        Form1 form = new Form1();

        public static void UnmapSharePoint()
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo("cmd.exe");
            string unmap = "/c net use B: /delete /y";
            cmdStartInfo.Arguments = unmap; //Unmaps B: drive and waits for command to finish
            cmdStartInfo.CreateNoWindow = true;
            cmdStartInfo.UseShellExecute = false;
            Process cmd = Process.Start(cmdStartInfo);
            cmd.WaitForExit();
        }

        public static List<string> ReadSharePoint(string address)
        {
            UnmapSharePoint(); //Unmap B: drive just in case
            string map = @"/c net use B: ""\\" + address + "\""; //COMMAND: net use B: "\\address"
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo("cmd.exe");
            cmdStartInfo.Arguments = map; //Maps from SharePoint onto B: drive and waits for command to finish
            cmdStartInfo.CreateNoWindow = true; //Does not show the window
            cmdStartInfo.UseShellExecute = false;
            Process cmd = Process.Start(cmdStartInfo);
            cmd.WaitForExit();

            return ReadFolder(@"B:\");
        }

        public static List<string> ReadFiles(string[] fileNames)
        {
            List<string> fileList = new List<string>();
            foreach (string file in fileNames)
            {
                fileList.Add(file);
            }
            return fileList;
        }

        public static List<string> ReadFolder(string folder)
        {
            List<string> fileList = new List<string>();
            if (Directory.Exists(folder) == false)
            {
                MessageBox.Show("Folder does not exist!", "ERROR");
                return fileList;
            }
            foreach (string file in Directory.GetFiles(folder, "*.pdf"))
            {
                fileList.Add(file);
            }
            return fileList;
        }

        public static string DNATool(string filePath, string fileName, string siteAddress, StreamWriter writer)
        {
            string error = "";
            PdfReader reader = new PdfReader(filePath);

            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                PdfDictionary pageDict = reader.GetPageN(page);
                PdfArray annotArray = pageDict.GetAsArray(PdfName.ANNOTS);

                if (annotArray == null)
                {
                    error += "ERROR: No annotaions found (Page " + page + ")\n";
                    continue;
                }

                foreach (PdfObject annot in annotArray)
                {
                    PdfDictionary annotDict = (PdfDictionary)PdfReader.GetPdfObject(annot);

                    if (annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.LINK) || annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.WIDGET))
                    {
                        PdfDictionary annotAction = (PdfDictionary)PdfReader.GetPdfObject(annotDict.Get(PdfName.A));

                        if (annotAction.Get(PdfName.S).Equals(PdfName.URI))
                        {
                            string foundURL = annotAction.GetAsString(PdfName.URI).ToString();
                            int index = foundURL.IndexOf("assetnum");

                            if (index != -1)
                            {
                                string assetNum = foundURL.Substring(index + 9, 5);
                                writer.WriteLine("\"" + assetNum + "\",,\"" + siteAddress + "/" + fileName + "\",,\"" + fileName + "\"");
                            }
                        }
                    }
                }
            }
            return error;
        }

        public static string LinkReplacer(string filePath, string fileOutputPath, string find, string replace)
        {
            int replaceNum = 0;
            PdfReader reader = new PdfReader(filePath);
            PdfStamper stamper = new PdfStamper(reader, new FileStream(fileOutputPath, FileMode.Create));

            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                PdfDictionary pageDict = reader.GetPageN(page);
                PdfArray annotArray = pageDict.GetAsArray(PdfName.ANNOTS);

                if (annotArray == null)
                {
                    continue;
                }

                foreach (PdfObject annot in annotArray)
                {
                    PdfDictionary annotDict = (PdfDictionary)PdfReader.GetPdfObject(annot);

                    if (annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.LINK) || annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.WIDGET))
                    {
                        PdfDictionary annotAction = (PdfDictionary)PdfReader.GetPdfObject(annotDict.Get(PdfName.A));

                        if (annotAction.Get(PdfName.S).Equals(PdfName.URI))
                        {
                            string foundURL = annotAction.GetAsString(PdfName.URI).ToString();

                            if (foundURL.IndexOf(find) != -1)
                            {
                                replaceNum += foundURL.Split(new string[] { find }, System.StringSplitOptions.RemoveEmptyEntries).Length - 1;
                                string newURL = foundURL.Replace(find, replace);
                                annotAction.Put(PdfName.URI, new PdfString(newURL));
                            }
                        }
                    }
                }
            }
            stamper.Close();
            reader.Close();

            return replaceNum + " occurance(s) found";
        }
    }
}
