using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace LinkReplacer {
    public partial class Form1 : Form
    {
        List<string> idList = new List<string>();
        List<string> addressList = new List<string>();
        List<string> fileList = new List<string>();

        private void ProgressBarDrawString(string value) //Draws the percentage on the progressbar
        {
            progressBar1.Refresh();
            progressBar1.CreateGraphics().DrawString(value, new Font(FontFamily.GenericSansSerif, (float)7.8, FontStyle.Regular), Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 7));
        }

        private void ProgressBarNoAnimation(int value) //Prevent the progressbar from lagging behind, disables the animation by going backwards
        {
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

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (var reader = new StreamReader("Sites.csv")) 
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
            fileList = Main.ReadSharePoint(addressList[comboSite.SelectedIndex]);
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
            fileList = Main.ReadFolder(textFolder.Text);
            labelNumOfFilesLocal.Text = "Number of files: " + fileList.Count; //Updates number of files
        }

        private void ButtonOutputFolder_Click(object sender, EventArgs e) //Displays the Folder Browser Dialog
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textOutputFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void ButtonLinkReplacer_Click(object sender, EventArgs e) //Finds and replaces matching strings in PDF hyperlinks
        {
            if (fileList.Count == 0)
            {
                MessageBox.Show("No files found!", "ERROR");
                return;
            } else if (textFindString.Text.Equals(""))
            {
                MessageBox.Show("Please input a string to find!", "ERROR");
                return;
            }

            string find = textFindString.Text;
            string replace = textReplaceString.Text;
            Directory.CreateDirectory(textOutputFolder.Text); //Output folder for the new PDFs

            //Initializes progress bar and log
            progressBar1.Value = 0;
            progressBar1.Maximum = fileList.Count;
            ProgressBarDrawString("0%");
            textLog.Clear();

            for (int i = 0; i < fileList.Count; i++)
            {
                string filePath = fileList[i];
                string fileName = filePath.Substring(filePath.LastIndexOf(@"\") + 1);
                string fileOutputPath = textOutputFolder.Text + @"\" + fileName;

                textLog.AppendText(fileName + " - "); //Separate refresh in case it gets stuck reading the PDF
                textLog.Refresh();
                textLog.AppendText(Main.LinkReplacer(filePath, fileOutputPath, find, replace) + "\n"); //Replaces links in current PDF and outputs count
                textLog.Refresh();
                textLog.ScrollToCaret(); //Scroll to bottom

                //Progresses the progress bar
                ProgressBarNoAnimation(progressBar1.Value + progressBar1.Step);
                ProgressBarDrawString(i * 100 / fileList.Count + "%");
            }
            textLog.AppendText("Done! :)");
            textLog.Refresh();
            ProgressBarDrawString("100% Done! :)");

            Main.UnmapSharePoint(); //Unmaps B: drive in case files were grabbed from SharePoint
        }

        private void ButtonDNATool_Click(object sender, EventArgs e) //Generates a CSV file of all the assets in the PDFs
        {
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


            Directory.CreateDirectory(textOutputFolder.Text); //Output folder for CSV
            StreamWriter writer = new StreamWriter(textOutputFolder.Text + @"\" + idList[index] + ".csv");

            //Initializes progress bar and log
            progressBar1.Value = 0;
            progressBar1.Maximum = fileList.Count;
            ProgressBarDrawString("0%");
            textLog.Clear();

            for (int i = 0; i < fileList.Count; i++)
            {
                string filePath = fileList[i];
                string fileName = filePath.Substring(filePath.LastIndexOf(@"\") + 1);

                textLog.AppendText(fileName + "\n"); //Separate refresh in case it gets stuck reading the PDF
                textLog.Refresh();
                textLog.AppendText(Main.DNATool(filePath, fileName, addressList[index], writer)); //Writes found assets to CSV and outputs any errors
                textLog.Refresh();
                textLog.ScrollToCaret(); //Scroll to bottom

                //Progresses the progress bar
                ProgressBarNoAnimation(progressBar1.Value + progressBar1.Step);
                ProgressBarDrawString(i * 100 / fileList.Count + "%");
            }
            textLog.AppendText("Done! :)");
            textLog.Refresh();
            ProgressBarDrawString("100% Done! :)");

            writer.Close();
            Main.UnmapSharePoint(); // Unmaps B: drive in case files were grabbed from SharePoint
        }
    }
}
