namespace Orvina.UI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            SearchButton = new Button();
            FilesListBox = new ListBox();
            SearchingLabel = new Label();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            richTextBox1 = new RichTextBox();
            splitContainer1 = new SplitContainer();
            directoryBox = new ComboBox();
            searchBox = new ComboBox();
            filesBox = new ComboBox();
            cancelButton = new Button();
            foldersOnlyCheckBox = new CheckBox();
            hddmodeCheckBox = new CheckBox();
            toolTip1 = new ToolTip(components);
            casesensitiveCheckBox = new CheckBox();
            label4 = new Label();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // SearchButton
            // 
            SearchButton.Font = new Font("Verdana", 10F, FontStyle.Bold, GraphicsUnit.Point);
            SearchButton.Location = new Point(12, 56);
            SearchButton.Name = "SearchButton";
            SearchButton.Size = new Size(91, 26);
            SearchButton.TabIndex = 3;
            SearchButton.Text = "Search";
            SearchButton.UseVisualStyleBackColor = true;
            // 
            // FilesListBox
            // 
            FilesListBox.BackColor = Color.FromArgb(79, 80, 81);
            FilesListBox.Dock = DockStyle.Fill;
            FilesListBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            FilesListBox.ForeColor = SystemColors.Window;
            FilesListBox.FormattingEnabled = true;
            FilesListBox.HorizontalScrollbar = true;
            FilesListBox.ItemHeight = 17;
            FilesListBox.Location = new Point(0, 0);
            FilesListBox.Name = "FilesListBox";
            FilesListBox.Size = new Size(776, 161);
            FilesListBox.TabIndex = 4;
            // 
            // SearchingLabel
            // 
            SearchingLabel.AutoSize = true;
            SearchingLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            SearchingLabel.ForeColor = Color.Green;
            SearchingLabel.Location = new Point(109, 62);
            SearchingLabel.Name = "SearchingLabel";
            SearchingLabel.Size = new Size(71, 15);
            SearchingLabel.TabIndex = 5;
            SearchingLabel.Text = "Searching...";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(58, 15);
            label1.TabIndex = 6;
            label1.Text = "Directory:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(227, 9);
            label2.Name = "label2";
            label2.Size = new Size(69, 15);
            label2.TabIndex = 7;
            label2.Text = "Search Text:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(443, 9);
            label3.Name = "label3";
            label3.Size = new Size(60, 15);
            label3.TabIndex = 8;
            label3.Text = "File Types:";
            label3.TextAlign = ContentAlignment.TopCenter;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = Color.FromArgb(1, 36, 86);
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.DetectUrls = false;
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            richTextBox1.ForeColor = SystemColors.Window;
            richTextBox1.Location = new Point(0, 0);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(776, 185);
            richTextBox1.TabIndex = 9;
            richTextBox1.Text = "";
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(12, 88);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(FilesListBox);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(richTextBox1);
            splitContainer1.Size = new Size(776, 350);
            splitContainer1.SplitterDistance = 161;
            splitContainer1.TabIndex = 10;
            // 
            // directoryBox
            // 
            directoryBox.AutoCompleteMode = AutoCompleteMode.Suggest;
            directoryBox.AutoCompleteSource = AutoCompleteSource.FileSystem;
            directoryBox.FormattingEnabled = true;
            directoryBox.Location = new Point(12, 27);
            directoryBox.Name = "directoryBox";
            directoryBox.Size = new Size(209, 23);
            directoryBox.TabIndex = 11;
            // 
            // searchBox
            // 
            searchBox.FormattingEnabled = true;
            searchBox.Location = new Point(227, 27);
            searchBox.Name = "searchBox";
            searchBox.Size = new Size(210, 23);
            searchBox.TabIndex = 12;
            // 
            // filesBox
            // 
            filesBox.FormattingEnabled = true;
            filesBox.Location = new Point(443, 27);
            filesBox.Name = "filesBox";
            filesBox.Size = new Size(156, 23);
            filesBox.TabIndex = 13;
            // 
            // cancelButton
            // 
            cancelButton.Font = new Font("Verdana", 10F, FontStyle.Bold, GraphicsUnit.Point);
            cancelButton.Location = new Point(186, 56);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(91, 26);
            cancelButton.TabIndex = 14;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // foldersOnlyCheckBox
            // 
            foldersOnlyCheckBox.AutoSize = true;
            foldersOnlyCheckBox.ForeColor = SystemColors.ButtonShadow;
            foldersOnlyCheckBox.Location = new Point(605, 12);
            foldersOnlyCheckBox.Name = "foldersOnlyCheckBox";
            foldersOnlyCheckBox.Size = new Size(92, 19);
            foldersOnlyCheckBox.TabIndex = 15;
            foldersOnlyCheckBox.Text = "Folders Only";
            toolTip1.SetToolTip(foldersOnlyCheckBox, "search for folders instead of files");
            foldersOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // hddmodeCheckBox
            // 
            hddmodeCheckBox.AutoSize = true;
            hddmodeCheckBox.ForeColor = SystemColors.ButtonShadow;
            hddmodeCheckBox.Location = new Point(605, 37);
            hddmodeCheckBox.Name = "hddmodeCheckBox";
            hddmodeCheckBox.Size = new Size(85, 19);
            hddmodeCheckBox.TabIndex = 16;
            hddmodeCheckBox.Text = "HDD Mode";
            toolTip1.SetToolTip(hddmodeCheckBox, "use on older, mechanical drives");
            hddmodeCheckBox.UseVisualStyleBackColor = true;
            // 
            // casesensitiveCheckBox
            // 
            casesensitiveCheckBox.AutoSize = true;
            casesensitiveCheckBox.ForeColor = SystemColors.ButtonShadow;
            casesensitiveCheckBox.Location = new Point(337, 8);
            casesensitiveCheckBox.Name = "casesensitiveCheckBox";
            casesensitiveCheckBox.Size = new Size(100, 19);
            casesensitiveCheckBox.TabIndex = 18;
            casesensitiveCheckBox.Text = "Case Sensitive";
            toolTip1.SetToolTip(casesensitiveCheckBox, "use on older, mechanical drives");
            casesensitiveCheckBox.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.ForeColor = SystemColors.ButtonShadow;
            label4.Location = new Point(663, 70);
            label4.Name = "label4";
            label4.Size = new Size(125, 15);
            label4.TabIndex = 17;
            label4.Text = "(double-click to open)";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(casesensitiveCheckBox);
            Controls.Add(label4);
            Controls.Add(hddmodeCheckBox);
            Controls.Add(foldersOnlyCheckBox);
            Controls.Add(cancelButton);
            Controls.Add(filesBox);
            Controls.Add(searchBox);
            Controls.Add(directoryBox);
            Controls.Add(splitContainer1);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(SearchingLabel);
            Controls.Add(SearchButton);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Orvina High Performance Text Search";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        public ComboBox directoryBox;
        public Button SearchButton;
        public ListBox FilesListBox;
        public Label SearchingLabel;
        private Label label1;
        private Label label2;
        private Label label3;
        public RichTextBox richTextBox1;
        private SplitContainer splitContainer1;
        public ComboBox searchBox;
        public ComboBox filesBox;
        public Button cancelButton;
        private ToolTip toolTip1;
        private Label label4;
        public CheckBox foldersOnlyCheckBox;
        public CheckBox hddmodeCheckBox;
        public CheckBox casesensitiveCheckBox;
    }
}