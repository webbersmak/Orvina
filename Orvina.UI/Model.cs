using Orvina.Engine;

namespace Orvina.UI
{
    internal class Model
    {
        private bool _searching;
        private bool caseSensitive;
        private string directory;
        private string files;
        private bool foldersOnly;
        private bool hddmode;
        private SearchEngine search;
        private string searchText;
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

        public event Action OnCaseSensitiveChanged;

        public event Action OnDirectoryChanged;

        public event Action OnFilesChanged;

        public event Action OnFoldersOnlyChanged;

        public event Action OnHDDModeChanged;

        public event Action OnIsSearchingChanged;

        public event Action OnTextChanged;
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

        public Dictionary<string, List<SearchEngine.LineResult>> FileResults { get; private set; } = new();

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
        public bool IsSearching
        {
            get
            {
                return _searching;
            }
            private set
            {
                _searching = value;
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

            FileResults.Clear();

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
            search?.Dispose();
            search = new SearchEngine();

            search.OnSearchComplete += Search_OnSearchComplete;
            search.OnFileFound += Search_OnFileFound;

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
                IsSearching = false;
            }

        }

        private void Search_OnFileFound(string arg1, List<SearchEngine.LineResult> arg2)
        {
            FileResults.Add(arg1, arg2);
        }

        private void Search_OnSearchComplete()
        {
            search.OnSearchComplete -= Search_OnSearchComplete;
            search.OnFileFound -= Search_OnFileFound;
            IsSearching = false;
        }
    }
}