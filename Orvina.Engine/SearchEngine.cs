namespace Orvina.Engine
{
    public class SearchEngine : IDisposable
    {
        private readonly object diskLock = new();
        private readonly object endLock = new();
        private readonly List<Event> eventsList = new();
        private readonly List<string> files = new();
        private readonly List<Task> tasks = new();

        private List<string> fileExtensions;

        private int finishedTasks;
        private bool listingDirectory;
        private bool searchEnded;
        private bool stop;
        private int taskCount;

        public bool RaiseErrors { set; private get; }

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
                Task.WaitAll(tasks.ToArray());
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
            if (tasks.Any(t => !t.IsCompletedSuccessfully))
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

            eventsList.Clear();
            stop = false;
            searchEnded = false;
            finishedTasks = 0;
            listingDirectory = true;

            this.fileExtensions = fileExtensions.ToList();
            this.taskCount = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 3;

            //directory thread
            tasks.Add(Task.Run(() =>
            {
                ListDirectory(searchPath, includeSubirectories);
                listingDirectory = false;
                TryNotifySearchEnded();

                lock (files)
                {
                    Monitor.PulseAll(files);
                }
            }));

            //notification thread
            tasks.Add(Task.Run(() => DequeueEvents()));

            //search threads
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

            lock (eventsList)
            {
                Monitor.Pulse(eventsList);
            }
        }

        private void DequeueEvents()
        {
            while (!searchEnded)
            {
                lock (eventsList)
                {
                    eventsList.ForEach(e =>
                    {
                        switch (e.EventType)
                        {
                            case Event.EventTypes.OnProgress:
                                this.OnProgress?.Invoke(((OnProgressEvent)e).File);
                                break;

                            case Event.EventTypes.OnFileFound:
                                var fileEvent = (OnFileFoundEvent)e;
                                this.OnFileFound?.Invoke(fileEvent.File, fileEvent.Lines);
                                break;

                            case Event.EventTypes.OnSearchComplete:
                                this.searchEnded = true;
                                this.OnSearchComplete?.Invoke();
                                break;

                            case Event.EventTypes.OnError:
                                this.OnError?.Invoke(((OnErrorEvent)e).Error);
                                break;
                        }
                    });
                    eventsList.Clear();

                    if (!searchEnded)
                    {
                        Monitor.Wait(eventsList);
                    }
                }
            }
        }

        private void ListDirectory(string path, bool includeSubirectories)
        {
            if (stop)
            {
                return;
            }

            QueueEvent(new OnProgressEvent(path));

            try
            {
                var filesToAdd = Directory.GetFiles(path).Where(file => !fileExtensions.Any() || fileExtensions.Any(t => file.EndsWith(t))).ToList();
                if (filesToAdd.Any())
                {
                    lock (files)
                    {
                        files.AddRange(filesToAdd);
                        Monitor.PulseAll(files);
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
                if (RaiseErrors)
                {
                    QueueEvent(new OnErrorEvent(e.ToString()));
                }
            }
        }

        private void QueueEvent(Event eventType)
        {
            lock (eventsList)
            {
                eventsList.Add(eventType);
                Monitor.Pulse(eventsList);
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
                    else if (listingDirectory)
                    {
                        Monitor.Wait(files);
                    }
                }

                if (!string.IsNullOrEmpty(target))
                {
                    try
                    {
                        QueueEvent(new OnProgressEvent(Path.GetFileName(target)));

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
                                    if (stop)
                                    {
                                        break;
                                    }

                                    lineNum++;
                                    if (line.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                                    {
                                        matchingLines.Add($"({lineNum}) " + line);
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
                                    while ((line = reader.ReadLine()) != null && !stop) {
                                        if (line.Contains(searchText, StringComparison.OrdinalIgnoreCase)) {
                                            matchingLines.Add(line);
                                        }
                                    }
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
                        if (RaiseErrors)
                        {
                            QueueEvent(new OnErrorEvent(e.ToString()));
                        }
                    }

                    if (matchingLines.Any())
                    {
                        QueueEvent(new OnFileFoundEvent(target, new List<string>(matchingLines)));
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
                if (finishedTasks == taskCount)
                {
                    QueueEvent(new OnSearchCompleteEvent());
                }
            }
        }
    }
}