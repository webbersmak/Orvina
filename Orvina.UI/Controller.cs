namespace Orvina.UI
{
    internal class Controller
    {
        private readonly Model model;
        private readonly View view;

        public Controller(View view, Model model)
        {
            this.view = view;
            this.model = model;

            this.model.OnTextChanged += Model_OnTextChanged;
            this.model.OnDirectoryChanged += Model_OnDirectoryChanged;
            this.model.OnFilesChanged += Model_OnFilesChanged;
            this.model.OnIsSearchingChanged += Model_OnIsSearchingChanged;
            this.model.OnFoldersOnlyChanged += Model_OnFoldersOnlyChanged;
            this.model.OnHDDModeChanged += Model_OnHDDModeChanged;
            this.model.OnCaseSensitiveChanged += Model_OnCaseSensitiveChanged;

            this.view.OnDirectoryTextChanged += View_OnDirectoryTextChanged;
            this.view.OnSearchTextChanged += View_OnSearchTextChanged;
            this.view.OnFilesTextChanged += View_OnFilesTextChanged;
            this.view.DoSearch += View_DoSearch;
            this.view.OnCancel += View_OnCancel;
            this.view.OnFoldersOnlyChanged += View_OnFoldersOnlyChanged;
            this.view.OnHddModeChanged += View_OnHddModeChanged;
            this.view.OnCaseSensitiveChanged += View_OnCaseSensitiveChanged;

            Model_OnTextChanged();
            Model_OnDirectoryChanged();
            Model_OnFilesChanged();
            Model_OnIsSearchingChanged();
            Model_OnFoldersOnlyChanged();
            Model_OnHDDModeChanged();
            Model_OnCaseSensitiveChanged();
        }

        private void Model_OnCaseSensitiveChanged()
        {
            this.view.CaseSensitive = this.model.CaseSensitive;
        }

        private void Model_OnDirectoryChanged()
        {
            this.view.DirectoryText = model.Directory;
        }

        private void Model_OnFilesChanged()
        {
            this.view.FilesText = model.Files;
        }

        private void Model_OnFoldersOnlyChanged()
        {
            this.view.FoldersOnly = this.model.FoldersOnly;
        }

        private void Model_OnHDDModeChanged()
        {
            this.view.HddMode = this.model.HDDMode;
        }

        private void Model_OnIsSearchingChanged()
        {
            this.view.IsSearching = this.model.IsSearching;
            if (!this.model.IsSearching)//search over, give files
            {
                view.FilesFound = model.FileResults;
            }
        }

        private void Model_OnTextChanged()
        {
            this.view.SearchText = model.SearchText;
        }

        private void View_DoSearch()
        {
            this.model.Search();
        }

        private void View_OnCancel()
        {
            model.CancelSearch();
        }

        private void View_OnCaseSensitiveChanged()
        {
            this.model.CaseSensitive = this.view.CaseSensitive;
        }

        private void View_OnDirectoryTextChanged()
        {
            //new text from box
            this.model.Directory = this.view.DirectoryText;
        }

        private void View_OnFilesTextChanged()
        {
            this.model.Files = this.view.FilesText;
        }

        private void View_OnFoldersOnlyChanged()
        {
            this.model.FoldersOnly = this.view.FoldersOnly;
        }

        private void View_OnHddModeChanged()
        {
            this.model.HDDMode = this.view.HddMode;
        }

        private void View_OnSearchTextChanged()
        {
            this.model.SearchText = this.view.SearchText;
        }
    }
}