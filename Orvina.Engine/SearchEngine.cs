using Orvina.Engine.Support;

namespace Orvina.Engine
{
    public class SearchEngine : IDisposable
    {
        private readonly object endLock = new();
        private readonly FileTractor fileTractor = new();
        private readonly SimpleQueue<string> options = new(32);
        private readonly List<Task> tasks = new();

        private bool[] downThreads;
        private string[] fileExtensions;

        private int filesEnroute;
        private int finishedTasks;

        private bool raiseErrors;
        private bool raiseProgress;

        private string searchText;
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
            filesEnroute = 0;
            finishedTasks = 0;

            this.fileExtensions = fileExtensions;

            //search threads
            var factory = new TaskFactory();
            var threadTotal = Environment.ProcessorCount;
            downThreads = new bool[threadTotal];
            for (var runnerId = 0; runnerId < threadTotal; runnerId++)
            {
                tasks.Add(factory.StartNew((param) => MultiSearch((int)param), runnerId));
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

        private SpinLock optionsLock = new();
        private SpinLock tractorLock = new();

        private void MultiSearch(int runnerId)
        {
            string path;
            var processing = true;
            while (processing && !stop)
            {
                path = null;

                LockHelper.RunLock(ref optionsLock, (out string path) => {
                    return options.TryDequeue(out path);
                }, out path);

                //lock (options)
                //{
                //    if (options.TryDequeue(out string next))
                //    {
                //        path = next;
                //    }
                //}

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

            bool waitingForFiles;
            do
            {
                waitingForFiles = LockHelper.RunLock(ref tractorLock, () => {
                    return --filesEnroute >= 0;
                });


                //lock (fileTractor)
                //{
                //    waitingForFiles = --filesEnroute >= 0;
                //}

                if (waitingForFiles && !stop)
                {
                    if (fileTractor.TryGetFile(out FileTractor.CompleteFile nextFile))
                    {
                        ScanFile(nextFile);
                    }
                }
            } while (waitingForFiles && !stop);

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

        private readonly EnumerationOptions DirOptions = new()
        {
            BufferSize = 4096 * 2 
        };

        private void MultiSearchInner(string path)
        {
            if (raiseProgress)
            {
                HandleEvent(new OnProgressEvent(path, false));
            }

            var queueId = QFactory<string>.GetQ();

            try
            {
                foreach (var entry in Directory.EnumerateDirectories(path, "*", DirOptions))
                {
                    QFactory<string>.Enqueue(queueId, entry);
                }

                if (QFactory<string>.Any(queueId))
                {
                    MultiSearchInner(QFactory<string>.Dequeue(queueId));

                    if (QFactory<string>.Any(queueId))
                    {

                        LockHelper.RunLock(ref optionsLock, () => {
                            while (QFactory<string>.TryDequeue(queueId, out string r))
                            {
                                options.Enqueue(r);
                            }
                        });


                        //lock (options)
                        //{
                        //    while (QFactory<string>.TryDequeue(queueId, out string r))
                        //    {
                        //        options.Enqueue(r);
                        //    }
                        //}
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
                    foreach (var fen in Directory.EnumerateFiles(path, $"*{fileType}", DirOptions))
                    {
                        QFactory<string>.Enqueue(queueId, fen);
                    }
                }

                while (QFactory<string>.TryDequeue(queueId, out string file))
                {
                    if (fileTractor.TryEnqueue(file))
                    {
                        LockHelper.RunLock(ref tractorLock, () => {
                            filesEnroute++;
                        });
                        //lock (fileTractor)
                        //{
                        //    filesEnroute++;
                        //}
                    }

                    if (raiseProgress)
                    {
                        HandleEvent(new OnProgressEvent(Path.GetFileName(file), true));
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

        private void ScanFile(FileTractor.CompleteFile file)
        {
            var matchingLines = QFactory<string>.GetQ();

            try
            {
                var all = System.Text.Encoding.UTF8.GetString(file.data);

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

                HandleEvent(new OnFileFoundEvent(file.fileName, lines));
            }

            QFactory<string>.ReturnQ(matchingLines);
        }
    }
}