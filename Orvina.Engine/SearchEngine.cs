using Orvina.Engine.Support;

namespace Orvina.Engine
{
    public class SearchEngine : IDisposable
    {
        private readonly SimpleQueue<string> directories = new();
        private readonly object endLock = new();
        private readonly SimpleQueue<Event> eventsList = new();
        private readonly SimpleQueue<string> files = new();
        private readonly List<Task> tasks = new();

        private string[] fileExtensions;

        private int finishedTasks;
        private bool listingDirectory;
        private bool listingFiles;
        private bool raiseErrors;
        private bool raiseProgress;
        private bool searchEnded;
        private bool stop;
        private int taskCount;

        /// <summary>
        /// returns error message
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// returns the file and the lines in the file that match the search
        /// </summary>
        public event Action<string, string[]> OnFileFound;

        /// <summary>
        /// returns the currently scanned file
        /// </summary>
        public event Action<string, bool> OnProgress;

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
                tasks.ForEach(t => t.Dispose());
                tasks.Clear();
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
            }

            raiseErrors = OnError != null;
            raiseProgress = OnProgress != null;

            eventsList.Reset();
            stop = false;
            searchEnded = false;
            finishedTasks = 0;
            listingDirectory = true;
            listingFiles = true;

            this.fileExtensions = fileExtensions;

            var baseCount = 4;
            this.taskCount = baseCount + (Environment.ProcessorCount > baseCount ? Environment.ProcessorCount - baseCount : 0);

            //directory thread
            tasks.Add(Task.Run(() =>
            {
                ListDirectory(searchPath, includeSubirectories);
                listingDirectory = false;
            }));

            //list thread
            tasks.Add(Task.Run(() =>
            {
                ListFiles();
                listingFiles = false;
                TryNotifySearchEnded();
            }));

            //notification thread
            tasks.Add(Task.Run(DequeueEvents));

            //search threads
            for (var i = 0; i <= taskCount - baseCount; i++)
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
            QueueEvent(new OnSearchCompleteEvent());
        }

        private void DequeueEvents()
        {
            var queue = new SimpleQueue<Event>();
            while (!searchEnded)
            {
                lock (eventsList)
                {
                    while (eventsList.TryDequeue(out Event e))
                    {
                        queue.Enqueue(e);
                    }
                }

                while (queue.TryDequeue(out Event e))
                {
                    if (stop)
                    {
                        this.searchEnded = true;
                        this.OnSearchComplete?.Invoke();
                        break;
                    }

                    switch (e.EventType)
                    {
                        case Event.EventTypes.OnProgress:
                            var pe = (OnProgressEvent)e;
                            this.OnProgress?.Invoke(pe.File, pe.IsFile);
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
                }
            }
        }

        private void ListDirectory(string path, bool includeSubirectories)
        {
            if (stop)
            {
                return;
            }

            if (raiseProgress)
            {
                QueueEvent(new OnProgressEvent(path, false));
            }

            try
            {
                if (includeSubirectories)
                {
                    var dirs = Directory.GetDirectories(path);

                    if (dirs.Any())
                    {
                        lock (directories)
                        {
                            foreach (var dir in dirs)
                            {
                                directories.Enqueue(dir);
                            }
                        }
                    }

                    foreach (var dir in dirs)
                    {
                        ListDirectory(dir, includeSubirectories);
                    }
                }
            }
            catch (Exception e)
            {
                if (raiseErrors)
                {
                    QueueEvent(new OnErrorEvent(e.ToString()));
                }
            }
        }

        private void ListFiles()
        {
            string path;
            do
            {
                path = "";
                lock (directories)
                {
                    if (directories.TryDequeue(out string result))
                    {
                        path = result;
                    }
                }

                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var filesToAdd = Directory.GetFiles(path).Where(file => fileExtensions.Any(t => file.EndsWith(t) || !fileExtensions.Any()));
                        if (filesToAdd.Any())
                        {
                            lock (files)
                            {
                                foreach (var entry in filesToAdd)
                                {
                                    files.Enqueue(entry);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (raiseErrors)
                        {
                            QueueEvent(new OnErrorEvent(e.ToString()));
                        }
                    }
                }
            } while (!stop && (listingDirectory || !string.IsNullOrEmpty(path)));
        }

        private void QueueEvent(Event eventType)
        {
            lock (eventsList)
            {
                eventsList.Enqueue(eventType);
            }
        }

        private void SearchFile(string searchText)
        {
            string target;
            var matchingLines = new SimpleQueue<string>();

            var sw = new SpinWait();

            do
            {
                target = "";

                lock (files)
                {
                    if (files.TryDequeue(out string result))
                    {
                        target = result;
                    }
                }

                if (!string.IsNullOrEmpty(target))
                {
                    try
                    {
                        if (raiseProgress)
                        {
                            QueueEvent(new OnProgressEvent(Path.GetFileName(target), true));
                        }

                        try //bulk read
                        {
                            var all = File.ReadAllText(target);

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
                                        matchingLines.Enqueue($"({lineNum}) " + line);
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
                                    while ((line = reader.ReadLine()) != null && !stop)
                                    {
                                        if (line.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                                        {
                                            matchingLines.Enqueue(line);
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
                        if (raiseErrors)
                        {
                            QueueEvent(new OnErrorEvent(e.ToString()));
                        }
                    }

                    if (matchingLines.Any)
                    {
                        QueueEvent(new OnFileFoundEvent(target, matchingLines.ToArray));
                        matchingLines.Reset();
                    }
                }
                else
                {
                    sw.SpinOnce();
                }
            } while (!stop && (!string.IsNullOrEmpty(target) || listingFiles));
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