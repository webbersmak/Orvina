using Orvina.Engine.Support;
using System.IO.Enumeration;

namespace Orvina.Engine
{
    public class SearchEngine : IDisposable
    {
        private readonly Dictionary<int, SimpleQueue<string>> runnerList = new();
        private readonly List<Task> tasks = new();
        private string[] fileExtensions;
        private int filesEnroute;
        private FileTractor fileTractor;
        private int finishedTasks;

        private bool raiseErrors;
        private bool raiseProgress;

        private SpinLock runnerListLock = new();
        private int runnerQueueId;
        private SpinLock runnerQueueIdLock = new();
        private string searchText;
        private bool stop;

        private int totalQueueCount;
        private SpinLock tractorLock = new();

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
            runnerList.Clear();

            fileTractor.Dispose();
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
            if (tasks.Any() && tasks.Any(t => !t.IsCompletedSuccessfully))
            {
                //looks like the search is already running...
                return;
            }
            else if (tasks.Any())
            {
                //clean up from previous run
                Dispose();
            }

            fileTractor = new();

            this.searchText = searchText;
            raiseErrors = OnError != null;
            raiseProgress = OnProgress != null;

            runnerQueueId = 0;
            stop = false;
            filesEnroute = 0;
            finishedTasks = 0;

            this.fileExtensions = fileExtensions;

            //search threads
            var factory = new TaskFactory();
            var threadTotal = Environment.ProcessorCount;
            for (var i = 0; i < threadTotal; i++)
            {
                runnerList.Add(i, new SimpleQueue<string>());
            }
            runnerList[0].Enqueue(searchPath);
            totalQueueCount = 1;

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
            //HandleEvent(new OnSearchCompleteEvent());
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

        private void MultiSearch(int runnerId)
        {
            var processing = true;
            string path;
            bool gotItem;
            while (processing && !stop)
            {
                var target = runnerList[runnerId];
                lock (target) //lock my list
                {
                    gotItem = target.TryDequeue(out path);
                }

                if (gotItem)
                {
                    MultiSearchInner(path);
                    LockHelper.RunLock(ref runnerListLock, () =>
                    {
                        totalQueueCount--;
                    });
                }
                else
                {
                    processing = LockHelper.RunLock(ref runnerListLock, () =>
                    {
                        return totalQueueCount > 0;
                    });
                }
            }

            bool waitingForFiles;
            do
            {
                waitingForFiles = LockHelper.RunLock(ref tractorLock, () =>
                {
                    return --filesEnroute >= 0;
                });

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
                lock (tasks)
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

            int i;
            var pathEntries = new FileSystemEnumerable<FileEntry>(path, (ref FileSystemEntry entry) => new FileEntry { IsDirectory = entry.IsDirectory, Path = entry.ToFullPath() }).ToArray();

            try
            {
                for (i = 0; i < pathEntries.Length; i++)
                {
                    var entry = pathEntries[i];

                    if (entry.IsDirectory)
                    {
                        LockHelper.RunLock(ref runnerListLock, () =>
                        {
                            totalQueueCount++;
                        });

                        int targetQueueId = LockHelper.RunLock(ref runnerQueueIdLock, () =>
                        {
                            runnerQueueId += runnerQueueId + 1 == runnerList.Count ? -runnerQueueId : 1;
                            return runnerQueueId;
                        });

                        var target = runnerList[targetQueueId];
                        lock (target) //lock my list
                        {
                            target.Enqueue(entry.Path);
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

                return;
            }

            try
            {
                for (var k = 0; k < fileExtensions.Length; k++)
                {
                    for (i = 0; i < pathEntries.Length; i++)
                    {
                        var entry = pathEntries[i];

                        if (!entry.IsDirectory && entry.Path.EndsWith(fileExtensions[k], StringComparison.OrdinalIgnoreCase))
                        {
                            if (fileTractor.TryEnqueue(entry.Path))
                            {
                                LockHelper.RunLock(ref tractorLock, () =>
                                {
                                    filesEnroute++;
                                });
                            }

                            if (raiseProgress)
                            {
                                HandleEvent(new OnProgressEvent(Path.GetFileName(entry.Path), true));
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
        }

        private void ScanFile(FileTractor.CompleteFile file)
        {
            var matchingLines = QFactory<string>.GetQ();

            try
            {
                var all = System.Text.Encoding.UTF8.GetString(file.data);

                //most cases the file won't contain the searchText at all
                var searchTextIdx = 0;
                var endFileIdx = all.Length - 1;
                while (searchTextIdx < endFileIdx && (searchTextIdx = all.IndexOf(searchText, searchTextIdx, StringComparison.OrdinalIgnoreCase)) >= 0 && !stop)
                {
                    var lineStartIdx = all.LastIndexOf("\n", searchTextIdx);
                    var lineEndIdx = all.IndexOf("\n", searchTextIdx + searchText.Length);

                    lineStartIdx = lineStartIdx >= 0 ? lineStartIdx + 1 : searchTextIdx;
                    lineEndIdx = lineEndIdx >= 0 ? lineEndIdx : searchTextIdx + searchText.Length;

                    var extractedLine = all.Substring(lineStartIdx, lineEndIdx - lineStartIdx);

                    int newLineIdx;//idx of \n character
                    int startIdx = 0;
                    var lineNum = 1;
                    while (startIdx < endFileIdx && (newLineIdx = all.IndexOf("\n", startIdx, StringComparison.OrdinalIgnoreCase)) >= 0 && newLineIdx < lineStartIdx && !stop)
                    {
                        startIdx = newLineIdx + 1;
                        lineNum++;
                    }

                    QFactory<string>.Enqueue(matchingLines, lineNum > 1 ? $"({lineNum}) {extractedLine}" : $"(??) {extractedLine}");

                    searchTextIdx = lineEndIdx;
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

                    if (stop)
                        break;
                }

                HandleEvent(new OnFileFoundEvent(file.fileName, lines));
            }

            QFactory<string>.ReturnQ(matchingLines);
        }

        private struct FileEntry
        {
            public bool IsDirectory;
            public string Path;
        }
    }
}