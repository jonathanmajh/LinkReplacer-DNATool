namespace LinkReplacer
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.comboSite = new System.Windows.Forms.ComboBox();
            this.radioSharePoint = new System.Windows.Forms.RadioButton();
            this.panelSharepoint = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.radioFolder = new System.Windows.Forms.RadioButton();
            this.radioFiles = new System.Windows.Forms.RadioButton();
            this.panelFiles = new System.Windows.Forms.Panel();
            this.buttonFiles = new System.Windows.Forms.Button();
            this.labelNumOfFilesLocal = new System.Windows.Forms.Label();
            this.panelFolder = new System.Windows.Forms.Panel();
            this.buttonFolder = new System.Windows.Forms.Button();
            this.textFolder = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabLinkReplacer = new System.Windows.Forms.TabPage();
            this.buttonLinkReplacer = new System.Windows.Forms.Button();
            this.textReplaceString = new System.Windows.Forms.TextBox();
            this.textFindString = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tabDNATool = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.comboDNATool = new System.Windows.Forms.ComboBox();
            this.buttonDNATool = new System.Windows.Forms.Button();
            this.buttonOutputFolder = new System.Windows.Forms.Button();
            this.textOutputFolder = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textLog = new System.Windows.Forms.RichTextBox();
            this.panelSharepoint.SuspendLayout();
            this.panelFiles.SuspendLayout();
            this.panelFolder.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabLinkReplacer.SuspendLayout();
            this.tabDNATool.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboSite
            // 
            this.comboSite.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboSite.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboSite.FormattingEnabled = true;
            this.comboSite.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.comboSite.Location = new System.Drawing.Point(277, 6);
            this.comboSite.Name = "comboSite";
            this.comboSite.Size = new System.Drawing.Size(223, 24);
            this.comboSite.TabIndex = 1;
            this.comboSite.SelectedIndexChanged += new System.EventHandler(this.ComboSite_SelectedIndexChanged);
            // 
            // radioSharePoint
            // 
            this.radioSharePoint.AutoSize = true;
            this.radioSharePoint.Location = new System.Drawing.Point(12, 12);
            this.radioSharePoint.Name = "radioSharePoint";
            this.radioSharePoint.Size = new System.Drawing.Size(465, 21);
            this.radioSharePoint.TabIndex = 2;
            this.radioSharePoint.Text = "Get PDFs from SharePoint (MUST BE LOGGED INTO SHAREPOINT)";
            this.radioSharePoint.UseVisualStyleBackColor = true;
            this.radioSharePoint.CheckedChanged += new System.EventHandler(this.RadioSharePoint_CheckedChanged);
            // 
            // panelSharepoint
            // 
            this.panelSharepoint.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelSharepoint.Controls.Add(this.label1);
            this.panelSharepoint.Controls.Add(this.comboSite);
            this.panelSharepoint.Location = new System.Drawing.Point(12, 39);
            this.panelSharepoint.Name = "panelSharepoint";
            this.panelSharepoint.Size = new System.Drawing.Size(776, 39);
            this.panelSharepoint.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(235, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Site:";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "PDF Files (*.pdf)|*.pdf";
            this.openFileDialog1.Multiselect = true;
            // 
            // radioFolder
            // 
            this.radioFolder.AutoSize = true;
            this.radioFolder.Location = new System.Drawing.Point(12, 154);
            this.radioFolder.Name = "radioFolder";
            this.radioFolder.Size = new System.Drawing.Size(150, 21);
            this.radioFolder.TabIndex = 4;
            this.radioFolder.Text = "Choose local folder";
            this.radioFolder.UseVisualStyleBackColor = true;
            this.radioFolder.CheckedChanged += new System.EventHandler(this.RadioFolder_CheckedChanged);
            // 
            // radioFiles
            // 
            this.radioFiles.AutoSize = true;
            this.radioFiles.Location = new System.Drawing.Point(12, 84);
            this.radioFiles.Name = "radioFiles";
            this.radioFiles.Size = new System.Drawing.Size(139, 21);
            this.radioFiles.TabIndex = 2;
            this.radioFiles.Text = "Choose local files";
            this.radioFiles.UseVisualStyleBackColor = true;
            this.radioFiles.CheckedChanged += new System.EventHandler(this.RadioFiles_CheckedChanged);
            // 
            // panelFiles
            // 
            this.panelFiles.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelFiles.Controls.Add(this.buttonFiles);
            this.panelFiles.Location = new System.Drawing.Point(12, 111);
            this.panelFiles.Name = "panelFiles";
            this.panelFiles.Size = new System.Drawing.Size(778, 37);
            this.panelFiles.TabIndex = 3;
            // 
            // buttonFiles
            // 
            this.buttonFiles.Location = new System.Drawing.Point(327, 3);
            this.buttonFiles.Name = "buttonFiles";
            this.buttonFiles.Size = new System.Drawing.Size(120, 29);
            this.buttonFiles.TabIndex = 0;
            this.buttonFiles.Text = "Choose Files";
            this.buttonFiles.UseVisualStyleBackColor = true;
            this.buttonFiles.Click += new System.EventHandler(this.ButtonFile_Click);
            // 
            // labelNumOfFilesLocal
            // 
            this.labelNumOfFilesLocal.AutoSize = true;
            this.labelNumOfFilesLocal.Location = new System.Drawing.Point(16, 226);
            this.labelNumOfFilesLocal.Name = "labelNumOfFilesLocal";
            this.labelNumOfFilesLocal.Size = new System.Drawing.Size(107, 17);
            this.labelNumOfFilesLocal.TabIndex = 1;
            this.labelNumOfFilesLocal.Text = "Number of files:";
            // 
            // panelFolder
            // 
            this.panelFolder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelFolder.Controls.Add(this.buttonFolder);
            this.panelFolder.Controls.Add(this.textFolder);
            this.panelFolder.Controls.Add(this.label3);
            this.panelFolder.Location = new System.Drawing.Point(12, 181);
            this.panelFolder.Name = "panelFolder";
            this.panelFolder.Size = new System.Drawing.Size(778, 38);
            this.panelFolder.TabIndex = 5;
            // 
            // buttonFolder
            // 
            this.buttonFolder.Location = new System.Drawing.Point(740, 4);
            this.buttonFolder.Name = "buttonFolder";
            this.buttonFolder.Size = new System.Drawing.Size(33, 29);
            this.buttonFolder.TabIndex = 1;
            this.buttonFolder.Text = "...";
            this.buttonFolder.UseVisualStyleBackColor = true;
            this.buttonFolder.Click += new System.EventHandler(this.ButtonFolder_Click);
            // 
            // textFolder
            // 
            this.textFolder.Location = new System.Drawing.Point(56, 6);
            this.textFolder.Name = "textFolder";
            this.textFolder.Size = new System.Drawing.Size(678, 22);
            this.textFolder.TabIndex = 3;
            this.textFolder.LostFocus += new System.EventHandler(this.TextFolder_LostFocus);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "Folder:";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(16, 418);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(770, 25);
            this.progressBar1.Step = 1;
            this.progressBar1.TabIndex = 6;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabLinkReplacer);
            this.tabControl1.Controls.Add(this.tabDNATool);
            this.tabControl1.Location = new System.Drawing.Point(12, 282);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(778, 128);
            this.tabControl1.TabIndex = 7;
            // 
            // tabLinkReplacer
            // 
            this.tabLinkReplacer.BackColor = System.Drawing.SystemColors.Control;
            this.tabLinkReplacer.Controls.Add(this.buttonLinkReplacer);
            this.tabLinkReplacer.Controls.Add(this.textReplaceString);
            this.tabLinkReplacer.Controls.Add(this.textFindString);
            this.tabLinkReplacer.Controls.Add(this.label5);
            this.tabLinkReplacer.Controls.Add(this.label4);
            this.tabLinkReplacer.Location = new System.Drawing.Point(4, 25);
            this.tabLinkReplacer.Name = "tabLinkReplacer";
            this.tabLinkReplacer.Padding = new System.Windows.Forms.Padding(3);
            this.tabLinkReplacer.Size = new System.Drawing.Size(770, 99);
            this.tabLinkReplacer.TabIndex = 0;
            this.tabLinkReplacer.Text = "Link Replacer";
            // 
            // buttonLinkReplacer
            // 
            this.buttonLinkReplacer.Location = new System.Drawing.Point(324, 64);
            this.buttonLinkReplacer.Name = "buttonLinkReplacer";
            this.buttonLinkReplacer.Size = new System.Drawing.Size(119, 29);
            this.buttonLinkReplacer.TabIndex = 4;
            this.buttonLinkReplacer.Text = "Replace";
            this.buttonLinkReplacer.UseVisualStyleBackColor = true;
            this.buttonLinkReplacer.Click += new System.EventHandler(this.ButtonLinkReplacer_Click);
            // 
            // textReplaceString
            // 
            this.textReplaceString.Location = new System.Drawing.Point(150, 35);
            this.textReplaceString.Name = "textReplaceString";
            this.textReplaceString.Size = new System.Drawing.Size(614, 22);
            this.textReplaceString.TabIndex = 3;
            // 
            // textFindString
            // 
            this.textFindString.Location = new System.Drawing.Point(150, 5);
            this.textFindString.Name = "textFindString";
            this.textFindString.Size = new System.Drawing.Size(614, 22);
            this.textFindString.TabIndex = 2;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 38);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(144, 17);
            this.label5.TabIndex = 1;
            this.label5.Text = "String to replace with:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(57, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 17);
            this.label4.TabIndex = 0;
            this.label4.Text = "String to find:";
            // 
            // tabDNATool
            // 
            this.tabDNATool.BackColor = System.Drawing.SystemColors.Control;
            this.tabDNATool.Controls.Add(this.label6);
            this.tabDNATool.Controls.Add(this.comboDNATool);
            this.tabDNATool.Controls.Add(this.buttonDNATool);
            this.tabDNATool.Location = new System.Drawing.Point(4, 25);
            this.tabDNATool.Name = "tabDNATool";
            this.tabDNATool.Padding = new System.Windows.Forms.Padding(3);
            this.tabDNATool.Size = new System.Drawing.Size(770, 99);
            this.tabDNATool.TabIndex = 1;
            this.tabDNATool.Text = "DNA Tool";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(174, 43);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(36, 17);
            this.label6.TabIndex = 2;
            this.label6.Text = "Site:";
            // 
            // comboDNATool
            // 
            this.comboDNATool.FormattingEnabled = true;
            this.comboDNATool.Location = new System.Drawing.Point(217, 40);
            this.comboDNATool.Name = "comboDNATool";
            this.comboDNATool.Size = new System.Drawing.Size(223, 24);
            this.comboDNATool.TabIndex = 1;
            // 
            // buttonDNATool
            // 
            this.buttonDNATool.Location = new System.Drawing.Point(446, 38);
            this.buttonDNATool.Name = "buttonDNATool";
            this.buttonDNATool.Size = new System.Drawing.Size(130, 29);
            this.buttonDNATool.TabIndex = 0;
            this.buttonDNATool.Text = "Generate CSV";
            this.buttonDNATool.UseVisualStyleBackColor = true;
            this.buttonDNATool.Click += new System.EventHandler(this.ButtonDNATool_Click);
            // 
            // buttonOutputFolder
            // 
            this.buttonOutputFolder.Location = new System.Drawing.Point(753, 247);
            this.buttonOutputFolder.Name = "buttonOutputFolder";
            this.buttonOutputFolder.Size = new System.Drawing.Size(33, 29);
            this.buttonOutputFolder.TabIndex = 8;
            this.buttonOutputFolder.Text = "...";
            this.buttonOutputFolder.UseVisualStyleBackColor = true;
            this.buttonOutputFolder.Click += new System.EventHandler(this.ButtonOutputFolder_Click);
            // 
            // textOutputFolder
            // 
            this.textOutputFolder.Location = new System.Drawing.Point(69, 251);
            this.textOutputFolder.Name = "textOutputFolder";
            this.textOutputFolder.Size = new System.Drawing.Size(678, 22);
            this.textOutputFolder.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 254);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 17);
            this.label2.TabIndex = 9;
            this.label2.Text = "Output:";
            // 
            // textLog
            // 
            this.textLog.Location = new System.Drawing.Point(16, 454);
            this.textLog.Name = "textLog";
            this.textLog.ReadOnly = true;
            this.textLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.textLog.Size = new System.Drawing.Size(770, 135);
            this.textLog.TabIndex = 11;
            this.textLog.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 601);
            this.Controls.Add(this.textLog);
            this.Controls.Add(this.buttonOutputFolder);
            this.Controls.Add(this.textOutputFolder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.labelNumOfFilesLocal);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.panelFolder);
            this.Controls.Add(this.radioSharePoint);
            this.Controls.Add(this.radioFolder);
            this.Controls.Add(this.panelFiles);
            this.Controls.Add(this.radioFiles);
            this.Controls.Add(this.panelSharepoint);
            this.Name = "Form1";
            this.Text = "Link Replacer/DNA Tool";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panelSharepoint.ResumeLayout(false);
            this.panelSharepoint.PerformLayout();
            this.panelFiles.ResumeLayout(false);
            this.panelFolder.ResumeLayout(false);
            this.panelFolder.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabLinkReplacer.ResumeLayout(false);
            this.tabLinkReplacer.PerformLayout();
            this.tabDNATool.ResumeLayout(false);
            this.tabDNATool.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ComboBox comboSite;
        private System.Windows.Forms.RadioButton radioSharePoint;
        private System.Windows.Forms.Panel panelSharepoint;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.RadioButton radioFolder;
        private System.Windows.Forms.RadioButton radioFiles;
        private System.Windows.Forms.Panel panelFiles;
        private System.Windows.Forms.Button buttonFiles;
        private System.Windows.Forms.Panel panelFolder;
        private System.Windows.Forms.Button buttonFolder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelNumOfFilesLocal;
        private System.Windows.Forms.TextBox textFolder;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabLinkReplacer;
        private System.Windows.Forms.TabPage tabDNATool;
        private System.Windows.Forms.Button buttonOutputFolder;
        private System.Windows.Forms.TextBox textOutputFolder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textReplaceString;
        private System.Windows.Forms.TextBox textFindString;
        private System.Windows.Forms.Button buttonLinkReplacer;
        private System.Windows.Forms.Button buttonDNATool;
        private System.Windows.Forms.RichTextBox textLog;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboDNATool;
    }
}

