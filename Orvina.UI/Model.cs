using Orvina.Engine;

namespace Orvina.UI
{
    internal class Model
    {
        private bool _searching;
        private string directory;
        private string files;
        private string searchText;

        private bool foldersOnly;
        private bool hddmode;

        public event Action OnDirectoryChanged;

        public event Action OnFilesChanged;

        public event Action OnIsSearchingChanged;

        public event Action OnTextChanged;

        private SearchEngine search;

        public bool FoldersOnly
        {
            get
            {
                return foldersOnly;
            }
            set
            {
                if (!IsSearching)
                {
                    foldersOnly = value;
                }
                OnFoldersOnlyChanged();
            }
        }

        public event Action OnFoldersOnlyChanged;
        public event Action OnHDDModeChanged;
        public event Action OnCaseSensitiveChanged;
        private bool caseSensitive;

        public bool CaseSensitive
        {
            get
            {
                return caseSensitive;
            }
            set
            {
                if (!IsSearching)
                {
                    caseSensitive = value;
                }
                OnCaseSensitiveChanged();
            }
        }

        public bool HDDMode
        {
            get
            {
                return hddmode;
            }
            set
            {
                if (!IsSearching)
                {
                    hddmode = value;
                }
                OnHDDModeChanged();
            }
        }

        public Model()
        {
            var settings = UserSettings.UserSettings.Instance;
            directory = settings.Directories[0];
            searchText = settings.SearchTexts[0]; 
            files = settings.FileTypes[0];
            foldersOnly = settings.FoldersOnly;
            hddmode = settings.HDDMode;
            caseSensitive = settings.CaseSensitive;
        }

        public bool Error { get; private set; }

        public string Directory
        {
            get
            {
                return directory;
            }
            set
            {
                if (!IsSearching)
                {
                    directory = value;
                }

                OnDirectoryChanged();
            }
        }

        public string Files
        {
            get
            {
                return files;
            }
            set
            {
                if (!IsSearching)
                {
                    files = value;
                }
                OnFilesChanged();
            }
        }

        public bool IsSearching
        {
            get
            {
                return _searching;
            }
            private set
            {
                _searching = value;
                if (value)
                {
                    Error = false;
                    fileResults.Clear();
                }
                OnIsSearchingChanged();
            }
        }

        public string SearchText
        {
            get
            {
                return searchText;
            }
            set
            {
                if (!IsSearching)
                {
                    searchText = value;
                }
                OnTextChanged();
            }
        }

        public void CancelSearch()
        {
            this.search.Stop();
        }

        public void Search()
        {
            if (IsSearching)
                return;

            //handle user settings
            var settings = UserSettings.UserSettings.Instance;

            if (directory != settings.Directories[0])
            {
                settings.Directories.Insert(0, directory);
                settings.Save();
            }
            else if (searchText != settings.SearchTexts[0])
            {
                settings.SearchTexts.Insert(0, searchText);
                settings.Save();
            }
            if (files != settings.FileTypes[0])
            {
                settings.FileTypes.Insert(0, files);
                settings.Save();
            }
            else if (foldersOnly != settings.FoldersOnly)
            {
                settings.FoldersOnly = foldersOnly;
                settings.Save();
            }
            else if (hddmode != settings.HDDMode)
            {
                settings.HDDMode = hddmode;
                settings.Save();
            }
            else if (caseSensitive != settings.CaseSensitive)
            {
                settings.CaseSensitive = caseSensitive;
                settings.Save();
            }

            //////////////////

            IsSearching = true;
            search = new SearchEngine();
            search.OnSearchComplete += Search_OnSearchComplete;
            search.OnFileFound += Search_OnFileFound;
            search.OnError += Search_OnError;

            try
            {
                search.Start(Directory,
                    true,
                    SearchText,
                    false,
                    caseSensitive: caseSensitive,
                    slowMode: hddmode,
                    foldersOnly: foldersOnly,
                    Files.Split(','));
            }
            catch (Exception e)
            {
                search.OnSearchComplete -= Search_OnSearchComplete;
                search.OnFileFound -= Search_OnFileFound;
                search.OnError -= Search_OnError;
            }
        }

        private void Search_OnError(string obj)
        {
            search.OnSearchComplete -= Search_OnSearchComplete;
            search.OnFileFound -= Search_OnFileFound;
            search.OnError -= Search_OnError;
            Error = true;
            IsSearching = false;
        }

        private void Search_OnSearchComplete()
        {
            search.OnSearchComplete -= Search_OnSearchComplete;
            search.OnFileFound -= Search_OnFileFound;
            search.OnError -= Search_OnError;
            IsSearching = false;
        }

        private void Search_OnFileFound(string arg1, List<SearchEngine.LineResult> arg2)
        {
            fileResults.Add(arg1, arg2);
        }

        public Dictionary<string, List<SearchEngine.LineResult>> fileResults { get; private set; } = new();
    }
}