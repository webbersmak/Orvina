using Orvina.Engine.Support;

namespace Orvina.Engine
{
    public class SearchEngine : IDisposable
    {
        private readonly object endLock = new();
        private readonly List<Task> tasks = new();

        private string[] fileExtensions;

        private int finishedTasks;

        private bool raiseErrors;
        private bool raiseProgress;

        private bool stop;

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
            //saw a bug (windows?) where a completed thread still showed as running
            //this while() will let threads finish up if they needed a few
            //extra microseconds to update their status
            Task.WaitAll(tasks.ToArray());
            tasks.ForEach(t => t.Dispose());
            tasks.Clear();

            options.Reset();
        }

        private string searchText;

        /// <summary>
        /// Call this method to start the search. Or call it to restart the search after the OnSearchComplete event is raised.
        /// </summary>
        /// <param name="searchPath">such as @"C:\my files"</param>
        /// <param name="includeSubirectories">true or false</param>
        /// <param name="searchText">the text the file should contain. Not case sensitive. Not a regular expression (yet)</param>
        /// <param name="fileExtensions">such as ".cs", ".txt"</param>
        public void Start(string searchPath, bool includeSubirectories, string searchText, params string[] fileExtensions)
        {
            //searchPath = @"C:\";
            //searchText = @"downThreads.Any(t => t == true);";

            if (tasks.Any(t => !t.IsCompletedSuccessfully))
            {
                //looks like the search is already running...
                return;
            }
            else
            {
                //clean up from previous run
                Dispose();
            }

            this.searchText = searchText;
            raiseErrors = OnError != null;
            raiseProgress = OnProgress != null;

            options.Enqueue(searchPath);
            stop = false;

            finishedTasks = 0;

            this.fileExtensions = fileExtensions;

            //search threads
            var factory = new TaskFactory();
            var threadTotal = Environment.ProcessorCount;
            downThreads = new bool[threadTotal];
            for (var i = 0; i < threadTotal; i++)
            {
                tasks.Add(factory.StartNew((param) => MultiSearch((int)param), i));
            }
        }

        /// <summary>
        /// Request the search to stop. There may be a delay between calling this method
        /// and seeing the OnSearchComplete event
        /// </summary>
        public void Stop()
        {
            stop = true; //let threads run to completion
            HandleEvent(new OnSearchCompleteEvent());
        }

        private readonly SimpleQueue<string> options = new(32);
        private bool[] downThreads;

        private readonly SimpleQueue<string> filesQ = new(32);

        private void MultiSearch(int runnerId)
        {
            string path;
            var processing = true;
            while (processing && !stop)
            {
                path = null;
                lock (options)
                {
                    if (options.TryDequeue(out string next))
                    {
                        path = next;
                    }
                }

                if (!string.IsNullOrEmpty(path))
                {
                    downThreads[runnerId] = true;
                    MultiSearchInner(path);
                    downThreads[runnerId] = false;
                }
                else
                {
                    processing = downThreads.Any(t => t == true);
                }
            }

            processing = true;
            string nextFile;
            while (processing && !stop)
            {
                nextFile = null;
                lock (filesQ)
                {
                    if (filesQ.TryDequeue(out string file))
                    {
                        nextFile = file;
                    }
                    else
                    {
                        processing = false;
                    }
                }

                if (!string.IsNullOrEmpty(nextFile))
                {
                    ScanFile(nextFile);
                }
            }

            void TryNotifySearchEnded()
            {
                lock (endLock)
                {
                    if (++finishedTasks == tasks.Count)
                    {
                        HandleEvent(new OnSearchCompleteEvent());
                    }
                }
            }

            TryNotifySearchEnded();
        }

        private void MultiSearchInner(string path)
        {
            if (raiseProgress)
            {
                HandleEvent(new OnProgressEvent(path, false));
            }

            var queueId = QFactory<string>.GetQ();

            try
            {
                foreach (var entry in Directory.EnumerateDirectories(path))
                {
                    QFactory<string>.Enqueue(queueId, entry);
                }

                if (QFactory<string>.Any(queueId))
                {
                    MultiSearchInner(QFactory<string>.Dequeue(queueId));

                    if (QFactory<string>.Any(queueId))
                    {
                        lock (options)
                        {
                            while (QFactory<string>.TryDequeue(queueId, out string r))
                            {
                                options.Enqueue(r);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (raiseErrors)
                {
                    HandleEvent(new OnErrorEvent(ex.ToString()));
                }

                QFactory<string>.ReturnQ(queueId);
                return;
            }

            try
            {
                foreach (var fileType in fileExtensions)
                {
                    foreach (var fen in Directory.EnumerateFiles(path, $"*{fileType}"))
                    {
                        QFactory<string>.Enqueue(queueId, fen);
                    }
                }

                if (QFactory<string>.Any(queueId))
                {
                    lock (filesQ)
                    {
                        while (QFactory<string>.TryDequeue(queueId, out string file))
                        {
                            filesQ.Enqueue(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (raiseErrors)
                {
                    HandleEvent(new OnErrorEvent(ex.ToString()));
                }
            }
            QFactory<string>.ReturnQ(queueId);
        }

        private void HandleEvent(Event e)
        {
            lock (this)
            {
                switch (e.EventType)
                {
                    case Event.EventTypes.OnProgress:
                        var pe = (OnProgressEvent)e;
                        OnProgress?.Invoke(pe.File, pe.IsFile);
                        break;

                    case Event.EventTypes.OnFileFound:
                        var fileEvent = (OnFileFoundEvent)e;
                        OnFileFound?.Invoke(fileEvent.File, fileEvent.Lines);
                        break;

                    case Event.EventTypes.OnSearchComplete:
                        OnSearchComplete?.Invoke();
                        break;

                    case Event.EventTypes.OnError:
                        OnError?.Invoke(((OnErrorEvent)e).Error);
                        break;
                }
            }
        }

        private void ScanFile(string nextFile)
        {
            if (raiseProgress)
            {
                HandleEvent(new OnProgressEvent(Path.GetFileName(nextFile), true));
            }

            var matchingLines = QFactory<string>.GetQ();

            try
            {
                try //bulk read
                {
                    string all;
                    using (var fs = File.OpenRead(nextFile))
                    {
                        using (var bs = new BufferedStream(fs, 1024))
                        {
                            using (var b = new BinaryReader(bs))
                            {
                                var data = b.ReadBytes((int)fs.Length);
                                all = System.Text.Encoding.UTF8.GetString(data);
                            }
                        }
                    }

                    //var all = File.ReadAllText(nextFile);

                    //most cases the file won't contain the searchText at all
                    var idx = 0;
                    while (idx < (all.Length - 1) && (idx = all.IndexOf(searchText, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        var lineStartIdx = all.LastIndexOf("\n", idx);
                        var lineEndIdx = all.IndexOf("\n", idx + searchText.Length);

                        lineStartIdx = lineStartIdx >= 0 ? lineStartIdx + 1 : idx;
                        lineEndIdx = lineEndIdx >= 0 ? lineEndIdx - 1 : idx + searchText.Length;

                        int newLineIdx = -1;
                        int startIdx = 0;
                        var lineNum = 1;
                        while (startIdx < all.Length - 1 && (newLineIdx = all.IndexOf("\n", startIdx, StringComparison.OrdinalIgnoreCase)) >= 0 && newLineIdx < idx)
                        {
                            startIdx = newLineIdx + 1;
                            lineNum++;
                        }

                        QFactory<string>.Enqueue(matchingLines, $"({lineNum}) {all.Substring(lineStartIdx, lineEndIdx - startIdx)}");
                        idx++;
                    }
                }
                catch (OutOfMemoryException)
                {
                    //line by line
                    using (var reader = (TextReader)(new StreamReader(nextFile)))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null && !stop)
                        {
                            if (line.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                            {
                                QFactory<string>.Enqueue(matchingLines, line);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (raiseErrors)
                {
                    HandleEvent(new OnErrorEvent(ex.ToString()));
                }
            }

            if (QFactory<string>.Any(matchingLines))
            {
                var lines = new string[QFactory<string>.Count(matchingLines)];
                var i = 0;
                while (QFactory<string>.TryDequeue(matchingLines, out string value))
                {
                    lines[i++] = value;
                }

                HandleEvent(new OnFileFoundEvent(nextFile, lines));
            }

            QFactory<string>.ReturnQ(matchingLines);
        }
    }
}