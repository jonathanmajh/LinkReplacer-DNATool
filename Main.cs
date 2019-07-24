//*IMPORTANT* Requires iTextSharp v5.5.13 - available in NuGet
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

        public static List<string> ReadSharePoint(string address) //Map chosen SharePoint site to B: drive and gets PDF filepaths
        {
            UnmapSharePoint(); //Unmap B: drive just in case it was already mapped previously
            string map = @"/c net use B: ""\\" + address + "\""; //COMMAND: net use B: "\\address"
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo("cmd.exe");
            cmdStartInfo.Arguments = map;
            cmdStartInfo.CreateNoWindow = true; //Does not show the window
            cmdStartInfo.UseShellExecute = false;
            Process cmd = Process.Start(cmdStartInfo); //Runs the command with given arguments
            cmd.WaitForExit(); //Waits for command to finish

            return ReadFolder(@"B:\"); //Can treat B: as normal folder
        }

        public static List<string> ReadFiles(string[] fileNames) //Individually selected files (no real reason to have a function but is consistent)
        {
            return fileNames.OfType<string>().ToList(); //Converts string array to list of strings
        }

        public static List<string> ReadFolder(string folder) //Gets filepaths of all PDFs in the folder
        {
            List<string> fileList = new List<string>();
            if (Directory.Exists(folder) == false)
            {
                MessageBox.Show("Folder does not exist!", "ERROR");
                return fileList;
            }

            foreach (string file in Directory.GetFiles(folder, "*.pdf")) //All PDFs in the folder
            {
                fileList.Add(file);
            }
            return fileList;
        }

        public static string DNATool(string filePath, string fileName, string siteAddress, StreamWriter writer) //Writes found assets to CSV and outputs any errors
        {
            string error = "";
            PdfReader reader = new PdfReader(filePath);

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
                    PdfDictionary annotDict = (PdfDictionary)PdfReader.GetPdfObject(annot);

                    if (annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.LINK) || annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.WIDGET)) //If the annotation is a hyperlink
                    {
                        PdfDictionary annotAction = (PdfDictionary)PdfReader.GetPdfObject(annotDict.Get(PdfName.A));

                        if (annotAction.Get(PdfName.S).Equals(PdfName.URI))
                        {
                            string foundURL = annotAction.GetAsString(PdfName.URI).ToString();
                            int index = foundURL.IndexOf("assetnum"); //Searches for blue box links

                            if (index != -1) //If it's a blue box
                            {
                                string assetNum = foundURL.Substring(index + 9, 5); //From counting
                                writer.WriteLine("\"" + assetNum + "\",,\"" + siteAddress + "/" + fileName + "\",,\"" + fileName + "\""); //Adds the asset to the CSV file
                            }
                        }
                    }
                }
            }
            return error;
        }

        public static string LinkReplacer(string filePath, string fileOutputPath, string find, string replace, int mode) //Replaces links in current PDF and outputs count
        {
            int replaceNum = 0; //Number of replaced instances
            PdfReader reader = new PdfReader(filePath);
            PdfStamper stamper = new PdfStamper(reader, new FileStream(fileOutputPath, FileMode.Create));

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
                    PdfDictionary annotDict = (PdfDictionary)PdfReader.GetPdfObject(annot);

                    if (annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.LINK) || annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.WIDGET)) //If the annotation is a hyperlink
                    {
                        PdfDictionary annotAction = (PdfDictionary)PdfReader.GetPdfObject(annotDict.Get(PdfName.A));

                        if (annotAction.Get(PdfName.S).Equals(PdfName.URI))
                        {
                            string foundURL = annotAction.GetAsString(PdfName.URI).ToString();

                            if (foundURL.IndexOf(find) != -1) //If the string exists
                            {
                                string newURL;
                                switch (mode)
                                {
                                    case 0: //Find-replace all
                                        replaceNum += foundURL.Split(new string[] { find }, System.StringSplitOptions.RemoveEmptyEntries).Length - 1; //Counts number of instances
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
            }
            stamper.Close();
            reader.Close();

            return " - " + replaceNum + " occurance(s) found";
        }

        public static string LinkGrabber(string filePath, string fileName, StreamWriter writer) //Writes found links to CSV and outputs count
        {
            string error = "\n";
            int foundLinks = 0; //Number of links found
            PdfReader reader = new PdfReader(filePath);

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
                        PdfDictionary annotDict = (PdfDictionary)PdfReader.GetPdfObject(annot);

                        if (annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.LINK) || annotDict.Get(PdfName.SUBTYPE).Equals(PdfName.WIDGET)) //If the annotation is a hyperlink
                        {
                            PdfDictionary annotAction = (PdfDictionary)PdfReader.GetPdfObject(annotDict.Get(PdfName.A));

                            if (annotAction.Get(PdfName.S).Equals(PdfName.URI))
                            {
                                string foundURL = annotAction.GetAsString(PdfName.URI).ToString();

                                try
                                {
                                    if (foundURL.Substring(0, 4).Equals("http")) //If it's an actual link
                                    {
                                        writer.WriteLine("\"" + foundURL + "\",\"" + fileName + "\""); //Adds the link to the CSV file
                                        foundLinks++;
                                    }
                                }
                                catch { } //Intentionally left blank
                            }
                        }
                    }
                }
                catch
                {
                    error += "  WARNING: Problem reading page " + page + "\n";
                }
                
            }
            return " - " + foundLinks + " link(s) found" + error;
        }
    }
}
