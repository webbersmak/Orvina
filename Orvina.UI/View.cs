using Orvina.Engine;
using System.Diagnostics;
using System.Text;

namespace Orvina.UI
{
    internal class View
    {
        public View(MainForm form)
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

            form.linkLabel.Click += (x, y) =>
            {
                var url = "aHR0cHM6Ly93d3cucGF5cGFsLmNvbS9wYXlwYWxtZS93ZWJiZXJzbWFr";
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(url));
                using (var p = Process.Start(new ProcessStartInfo(decoded) { UseShellExecute = true }))
                {
                };
            };
        }

        public event Action DoSearch;

        public event Action OnCancel;

        public event Action OnCaseSensitiveChanged;

        public event Action OnDirectoryTextChanged;

        public event Action OnFilesTextChanged;

        public event Action OnFoldersOnlyChanged;

        public event Action OnHddModeChanged;

        public event Action OnSearchTextChanged;

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
                    MainForm.searchBox.Items.Clear();
                    MainForm.filesBox.Items.Clear();

                    MainForm.directoryBox.Items.AddRange(UserSettings.UserSettings.Instance.Directories.ToArray());
                    MainForm.searchBox.Items.AddRange(UserSettings.UserSettings.Instance.SearchTexts.ToArray());
                    MainForm.filesBox.Items.AddRange(UserSettings.UserSettings.Instance.FileTypes.ToArray());

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

        public MainForm MainForm { get; private set; }

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

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            MainForm.cancelButton.Enabled = false;
            OnCancel();
        }

        private void CasesensitiveCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            MainForm.casesensitiveCheckBox.ForeColor = MainForm.casesensitiveCheckBox.Checked ?
                SystemColors.ControlText : SystemColors.ButtonShadow;

            OnCaseSensitiveChanged();
        }

        private void DirectoryBox_TextChanged(object? sender, EventArgs e)
        {
            OnDirectoryTextChanged();
        }

        private void FilesBox_TextChanged(object? sender, EventArgs e)
        {
            OnFilesTextChanged();
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
            else
            {
                this.MainForm.richTextBox1.ResetText();
            }
        }

        private void FoldersOnlyCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            MainForm.foldersOnlyCheckBox.ForeColor = MainForm.foldersOnlyCheckBox.Checked ?
                SystemColors.ControlText : SystemColors.ButtonShadow;

            OnFoldersOnlyChanged();
        }

        private void HddmodeCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            MainForm.hddmodeCheckBox.ForeColor = MainForm.hddmodeCheckBox.Checked ?
                SystemColors.ControlText : SystemColors.ButtonShadow;

            OnHddModeChanged();
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            OnSearchTextChanged();
        }

        private void SearchButton_Click(object? sender, EventArgs e)
        {
            DoSearch();
        }

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
    }
}