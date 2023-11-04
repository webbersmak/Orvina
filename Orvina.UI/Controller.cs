namespace Orvina.UI
{
    internal class Controller
    {
        private readonly View view;
        private readonly Model model;

        public Controller(View view, Model model)
        {
            this.view = view;
            this.model = model;

            this.model.OnTextChanged += Model_OnTextChanged;
            this.model.OnDirectoryChanged += Model_OnDirectoryChanged;
            this.model.OnFilesChanged += Model_OnFilesChanged;
            this.model.OnIsSearchingChanged += Model_OnIsSearchingChanged;

            this.view.OnDirectoryTextChanged += View_OnDirectoryTextChanged;
            this.view.OnSearchTextChanged += View_OnSearchTextChanged;
            this.view.OnFilesTextChanged += View_OnFilesTextChanged;
            this.view.DoSearch += View_DoSearch;
            this.view.OnCancel += View_OnCancel;

            Model_OnTextChanged();
            Model_OnDirectoryChanged();
            Model_OnFilesChanged();
            Model_OnIsSearchingChanged();
        }

        private void View_OnCancel()
        {
            model.CancelSearch();
        }

        private void View_DoSearch()
        {
            this.model.Search();
        }

        private void View_OnFilesTextChanged()
        {
            this.model.Files = this.view.FilesText;
        }

        private void View_OnSearchTextChanged()
        {
            this.model.SearchText = this.view.SearchText;
        }

        private void View_OnDirectoryTextChanged()
        {
            //new text from box
            this.model.Directory = this.view.DirectoryText;
        }

        private void Model_OnIsSearchingChanged()
        {
            this.view.IsSearching = this.model.IsSearching;
            if (!this.model.IsSearching)//search over, give files
            {
                view.FilesFound = model.fileResults;
            }
        }

        private void Model_OnFilesChanged()
        {
            this.view.FilesText = model.Files;
        }

        private void Model_OnDirectoryChanged()
        {
            this.view.DirectoryText = model.Directory;
        }

        private void Model_OnTextChanged()
        {
            this.view.SearchText = model.SearchText;
        }
    }
}