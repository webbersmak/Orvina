namespace Orvina.Engine
{
    public class SearchEngine : IDisposable
    {
        private readonly object diskLock = new();
        private readonly object endLock = new();
        private readonly List<string> files = new();
        private readonly object notificationLock = new();
        private readonly List<Task> tasks = new();

        private List<string> fileExtensions;

        private int taskCount;
        private bool searchEnded;

        private volatile int finishedTasks;
        private volatile bool listingDirectory;
        private volatile bool stop;

        /// <summary>
        /// returns error message
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// returns the file and the lines in the file that match the search
        /// </summary>
        public event Action<string, List<string>> OnFileFound;

        /// <summary>
        /// returns the currently scanned file
        /// </summary>
        public event Action<string> OnProgress;

        /// <summary>
        /// this will be raised when the search is complete. Expect this event even if Stop() is called
        /// </summary>
        public event Action OnSearchComplete;

        /// <summary>
        /// Safe to call after the OnSearchComplete event is raised.
        /// </summary>
        public void Dispose()
        {
            if (searchEnded)
            {
                //saw a bug (windows?) where a completed thread still showed as running
                //this while() will let threads finish up if they needed a few
                //extra microseconds to update their status
                while (tasks.Any(t => t.Status == TaskStatus.Running)) { }
                tasks.ForEach(t => { t.Dispose(); });
            }
        }

        /// <summary>
        /// Call this method to start the search. Or call it to restart the search after the OnSearchComplete event is raised.
        /// </summary>
        /// <param name="searchPath">such as @"C:\my files"</param>
        /// <param name="includeSubirectories">true or false</param>
        /// <param name="searchText">the text the file should contain. Not case sensitive. Not a regular expression (yet)</param>
        /// <param name="fileExtensions">such as ".cs", ".txt"</param>
        public void Start(string searchPath, bool includeSubirectories, string searchText, params string[] fileExtensions)
        {
            if (tasks.Any(t => t.Status != TaskStatus.RanToCompletion))
            {
                //looks like the search is already running...
                return;
            }
            else
            {
                //clean up tasks from previous run
                Dispose();
                tasks.Clear();
            }

            stop = false;
            searchEnded = false;
            finishedTasks = 0;
            listingDirectory = true;

            this.fileExtensions = fileExtensions.ToList();
            this.taskCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount : 2;

            tasks.Add(Task.Run(() =>
            {
                ListDirectory(searchPath, includeSubirectories);
                listingDirectory = false;
                TryNotifySearchEnded();
            }));

            for (var i = 1; i < taskCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    SearchFile(searchText);
                    TryNotifySearchEnded();
                }));
            }
        }

        /// <summary>
        /// Request the search to stop. There may be a delay between calling this method
        /// and seeing the OnSearchComplete event
        /// </summary>
        public void Stop()
        {
            stop = true; //let threads run to completion
        }

        private void ListDirectory(string path, bool includeSubirectories)
        {
            if (stop)
            {
                return;
            }

            lock (notificationLock)
                OnProgress?.Invoke(path);

            try
            {
                var filesToAdd = Directory.GetFiles(path).Where(file => !fileExtensions.Any() || fileExtensions.Any(t => file.EndsWith(t))).ToList();
                if (filesToAdd.Any())
                {
                    lock (files)
                    {
                        files.AddRange(filesToAdd);
                    }
                }

                if (includeSubirectories)
                {
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        ListDirectory(dir, includeSubirectories);
                    }
                }
            }
            catch (Exception e)
            {
                lock (notificationLock)
                    OnError?.Invoke(e.ToString());
            }
        }

        private void SearchFile(string searchText)
        {
            int fileCount;
            string target;

            var matchingLines = new List<string>();

            do
            {
                target = "";

                lock (files)
                {
                    fileCount = files.Count;
                    if (fileCount > 0)
                    {
                        target = files[0];
                        files.RemoveAt(0);
                        fileCount--;
                    }
                }

                if (!string.IsNullOrEmpty(target))
                {
                    try
                    {
                        lock (notificationLock)
                            OnProgress?.Invoke(Path.GetFileName(target));

                        try //bulk read
                        {
                            string all;
                            lock (diskLock) //get on/off disk ASAP
                            {
                                all = File.ReadAllText(target);
                            }

                            //most cases the file won't contain the searchText at all
                            if (all.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                            {
                                var lineNum = 0;
                                foreach (var line in all.Split("\n"))
                                {
                                    lineNum++;
                                    if (line.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                                    {
                                        matchingLines.Add($"({lineNum}) " + line);
                                    }

                                    if (stop)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        catch (OutOfMemoryException)
                        {
                            try //line by line
                            {
                                using (TextReader reader = new StreamReader(target))
                                {
                                    string line;
                                    do
                                    {
                                        line = reader.ReadLine();
                                        if (line != null && line.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                                        {
                                            matchingLines.Add(line);
                                        }

                                        if (stop)
                                        {
                                            break;
                                        }
                                    } while (line != null);
                                }
                            }
                            catch
                            {
                                throw;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        lock (notificationLock)
                            OnError?.Invoke(e.ToString());
                    }

                    if (matchingLines.Any())
                    {
                        lock (notificationLock)
                            OnFileFound?.Invoke(target, matchingLines);

                        matchingLines.Clear();
                    }
                }
            } while (!stop && (fileCount > 0 || listingDirectory));
        }

        private void TryNotifySearchEnded()
        {
            lock (endLock)
            {
                finishedTasks++;
                if (!searchEnded && finishedTasks == taskCount)
                {
                    searchEnded = true;
                    lock (notificationLock)
                        OnSearchComplete?.Invoke();
                }
            }
        }
    }
}