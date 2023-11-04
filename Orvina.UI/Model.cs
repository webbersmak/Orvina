using Orvina.Engine;

namespace Orvina.UI
{
    internal class Model
    {
        private bool _searching;
        private string directory;
        private string files;
        private string searchText;

        public event Action OnDirectoryChanged;

        public event Action OnFilesChanged;

        public event Action OnIsSearchingChanged;

        public event Action OnTextChanged;

        private SearchEngine search;

        public Model()
        {
            var settings = UserSettings.UserSettings.Instance;

            if (settings.Directories.Any())
            {
                directory = settings.Directories[0];
            }
            if (settings.SearchTexts.Any())
            {
                searchText = settings.SearchTexts[0];
            }
            if (settings.FileTypes.Any())
            {
                files = settings.FileTypes[0];
            }
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
            var saveNeeded = false;
            var settings = UserSettings.UserSettings.Instance;
            if (!settings.Directories.Any())
            {
                saveNeeded = true;
            }
            else if (directory != settings.Directories[0])
            {
                settings.Directories.Insert(0, directory);
                saveNeeded = true;
            }

            if (!settings.SearchTexts.Any())
            {
                saveNeeded = true;
            }
            else if (searchText != settings.SearchTexts[0])
            {
                settings.SearchTexts.Insert(0, searchText);
                saveNeeded = true;
            }

            if (!settings.FileTypes.Any())
            {
                saveNeeded = true;
            }
            else if (files != settings.FileTypes[0])
            {
                settings.FileTypes.Insert(0, files);
                saveNeeded = true;
            }

            if (saveNeeded)
            {
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
                    false,
                    false,
                    foldersOnly: false,
                    Files.Split(','));
            }
            catch (Exception e)
            {
                //WriteLine(e.Message);
                //Environment.Exit(0);
            }
        }

        private void Search_OnError(string obj)
        {
            search.OnSearchComplete -= Search_OnSearchComplete;
            search.OnFileFound -= Search_OnFileFound;
            Error = true;
            IsSearching = false;
        }

        private void Search_OnSearchComplete()
        {
            search.OnSearchComplete -= Search_OnSearchComplete;
            search.OnFileFound -= Search_OnFileFound;
            IsSearching = false;
        }

        private void Search_OnFileFound(string arg1, List<SearchEngine.LineResult> arg2)
        {
            fileResults.Add(arg1, arg2);
        }

        public Dictionary<string, List<SearchEngine.LineResult>> fileResults { get; private set; } = new();
    }
}