using Orvina.Engine;
using System.Diagnostics;
using System.Text;

namespace Orvina.UI
{
    internal class View
    {
        private class ListBoxItem
        {
            public readonly string display;

            public readonly List<SearchEngine.LineResult> lineResults;

            public ListBoxItem(string display, List<SearchEngine.LineResult> lineResults)
            {
                this.display = display;
                this.lineResults = lineResults;
            }

            public override string ToString()
            {
                return display;
            }
        }

        public Form1 MainForm { get; private set; }

        public event Action OnDirectoryTextChanged;

        public event Action OnSearchTextChanged;

        public event Action OnFilesTextChanged;

        public event Action DoSearch;

        public event Action OnCancel;

        public event Action OnFoldersOnlyChanged;
        public event Action OnHddModeChanged;
        public event Action OnCaseSensitiveChanged;

        public View(Form1 form)
        {
            this.MainForm = form;

            form.FilesListBox.SelectedIndexChanged += (x, y) => { FilesListBox_SelectedIndexChanged(); };

            form.directoryBox.TextChanged += DirectoryBox_TextChanged;
            form.searchBox.TextChanged += SearchBox_TextChanged;
            form.filesBox.TextChanged += FilesBox_TextChanged;
            form.FilesListBox.MouseDoubleClick += FilesListBox_MouseDoubleClick;

            form.SearchButton.Click += SearchButton_Click;
            form.cancelButton.Click += CancelButton_Click;
            form.AcceptButton = MainForm.SearchButton;

            form.foldersOnlyCheckBox.CheckedChanged += FoldersOnlyCheckBox_CheckedChanged;
            form.hddmodeCheckBox.CheckedChanged += HddmodeCheckBox_CheckedChanged;
            form.casesensitiveCheckBox.CheckedChanged += CasesensitiveCheckBox_CheckedChanged;
        }

        private void CasesensitiveCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            MainForm.casesensitiveCheckBox.ForeColor = MainForm.casesensitiveCheckBox.Checked ?
                SystemColors.ControlText : SystemColors.ButtonShadow;

            OnCaseSensitiveChanged();
        }

        private void HddmodeCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            MainForm.hddmodeCheckBox.ForeColor = MainForm.hddmodeCheckBox.Checked ?
                SystemColors.ControlText : SystemColors.ButtonShadow;

            OnHddModeChanged();
        }

        private void FoldersOnlyCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            MainForm.foldersOnlyCheckBox.ForeColor = MainForm.foldersOnlyCheckBox.Checked ?
                SystemColors.ControlText : SystemColors.ButtonShadow;

            OnFoldersOnlyChanged();
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            MainForm.cancelButton.Enabled = false;
            OnCancel();
        }

        private void FilesListBox_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (this.MainForm.FilesListBox.SelectedItem != null)
            {
                var item = (ListBoxItem)this.MainForm.FilesListBox.SelectedItem;
                using (var p = Process.Start(new ProcessStartInfo(item.display) { UseShellExecute = true }))
                {
                };
            }
        }

        private void FilesListBox_SelectedIndexChanged()
        {
            if (MainForm.FilesListBox.SelectedItem != null)
            {
                var results = ((ListBoxItem)MainForm.FilesListBox.SelectedItem).lineResults;
                var builder = new StringBuilder();

                var greenTextMap = new Dictionary<int, int>();//idx, length

                var idx = 0;
                foreach (var lineResult in results)
                {
                    var next = $"({lineResult.LineNumber}) ";
                    builder.Append(next);
                    idx += next.Length;

                    foreach (var part in lineResult.LineParts)
                    {
                        builder.Append(part.Text);

                        if (part.IsMatch)
                        {
                            greenTextMap.Add(idx, part.Text.Length);
                        }

                        idx += part.Text.Length;
                    }

                    builder.Append('\n');
                    idx++;
                }

                MainForm.richTextBox1.Text = builder.ToString();

                foreach (var entry in greenTextMap)
                {
                    MainForm.richTextBox1.Select(entry.Key, entry.Value);
                    MainForm.richTextBox1.SelectionColor = Color.ForestGreen;
                }

                MainForm.richTextBox1.SelectionLength = 0;
                MainForm.FilesListBox.Focus();
            }
            else {
                this.MainForm.richTextBox1.ResetText();
            }
        }

        private void SearchButton_Click(object? sender, EventArgs e)
        {
            DoSearch();
        }

        private void FilesBox_TextChanged(object? sender, EventArgs e)
        {
            OnFilesTextChanged();
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            OnSearchTextChanged();
        }

        private void DirectoryBox_TextChanged(object? sender, EventArgs e)
        {
            OnDirectoryTextChanged();
        }

        public string DirectoryText
        {
            set
            {
                if (value != MainForm.directoryBox.Text)
                {
                    MainForm.directoryBox.Text = value;
                }
            }
            get
            {
                return MainForm.directoryBox.Text;
            }
        }

        public string FilesText
        {
            set
            {
                if (value != MainForm.filesBox.Text)
                {
                    MainForm.filesBox.Text = value;
                }
            }
            get
            {
                return MainForm.filesBox.Text;
            }
        }

        public string SearchText
        {
            set
            {
                if (value != MainForm.searchBox.Text)
                {
                    MainForm.searchBox.Text = value;
                }
            }
            get
            {
                return MainForm.searchBox.Text;
            }
        }

        public bool FoldersOnly
        {
            set
            {
                if (value != MainForm.foldersOnlyCheckBox.Checked)
                {
                    MainForm.foldersOnlyCheckBox.Checked = value;
                }
            }
            get
            {
                return MainForm.foldersOnlyCheckBox.Checked;
            }
        }

        public bool HddMode
        {
            set
            {
                if (value != MainForm.hddmodeCheckBox.Checked)
                {
                    MainForm.hddmodeCheckBox.Checked = value;
                }
            }
            get
            {
                return MainForm.hddmodeCheckBox.Checked;
            }
        }

        public bool CaseSensitive
        {
            set
            {
                if (value != MainForm.casesensitiveCheckBox.Checked)
                {
                    MainForm.casesensitiveCheckBox.Checked = value;
                }
            }
            get
            {
                return MainForm.casesensitiveCheckBox.Checked;
            }
        }

        public Dictionary<string, List<SearchEngine.LineResult>> FilesFound
        {
            set
            {
                if (MainForm.InvokeRequired)
                {
                    MainForm.Invoke(() =>
                    {
                        FilesFound = value;
                    });
                }
                else
                {
                    MainForm.FilesListBox.Items.Clear();
                    foreach (var item in value)
                    {
                        MainForm.FilesListBox.Items.Add(new ListBoxItem(item.Key, item.Value));
                    }

                    MainForm.FilesListBox.SelectedIndex = MainForm.FilesListBox.Items.Count > 0 ? 0 : -1;
                }
            }
        }

        public bool IsSearching
        {
            set
            {
                if (this.MainForm.InvokeRequired)
                {
                    this.MainForm.Invoke(() =>
                    {
                        IsSearching = value;
                    });
                }
                else
                {
                    MainForm.directoryBox.Items.Clear();
                    foreach (var x in UserSettings.UserSettings.Instance.Directories)
                    {
                        MainForm.directoryBox.Items.Add(x);
                    }

                    MainForm.searchBox.Items.Clear();
                    foreach (var x in UserSettings.UserSettings.Instance.SearchTexts)
                    {
                        MainForm.searchBox.Items.Add(x);
                    }

                    MainForm.filesBox.Items.Clear();
                    foreach (var x in UserSettings.UserSettings.Instance.FileTypes)
                    {
                        MainForm.filesBox.Items.Add(x);
                    }

                    MainForm.cancelButton.Enabled = true;

                    MainForm.directoryBox.Enabled = !value;
                    MainForm.searchBox.Enabled = !value;
                    MainForm.filesBox.Enabled = !value;
                    MainForm.SearchButton.Enabled = !value;
                    MainForm.FilesListBox.Enabled = !value;

                    MainForm.cancelButton.Visible = value;
                    MainForm.SearchingLabel.Visible = value;

                    if (value)
                    {
                        MainForm.FilesListBox.Items.Clear();
                        this.MainForm.richTextBox1.ResetText();
                    }

                    MainForm.UseWaitCursor = value;
                    this.MainForm.richTextBox1.Focus();
                }
            }
        }
    }
}