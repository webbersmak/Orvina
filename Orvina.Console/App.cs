using Orvina.Engine;
using System.Diagnostics;

namespace Orvina.Console
{
    internal class App
    {
        private readonly List<FileResult> searchResults = new();
        private readonly AppState state;
        private readonly Stopwatch stopwatch = new();
        private bool attemptQuit;
        private CommandArgs cmdArgs = new();
        private int searchCount = 0;
        private bool searchEnded;

        public App(string[] args)
        {
            state = ProcessArgs(args);
        }

        private enum AppState
        {
            ShowHelp,
            Run
        }

        public void Run()
        {
            switch (state)
            {
                case AppState.ShowHelp:
                    WriteLine("Quickly find files containing the desired text.\n");
                    WriteLine("Usage:\n");
                    WriteLine("orvina <search path> <search text> <file extensions> [-nosub]\n");
                    WriteLine("     <search path>   Specifies the directory to search");
                    WriteLine("     <search text>   Declares the text to search for in the files.");
                    WriteLine("                     Wildcards * and ? are supported. Use ~? or ~* to search for those characters.");
                    WriteLine("     <file extensions>   Comma separated list of file extensions. Restricts searching to specific file types.");
                    WriteLine("     -cases              Search text is case sensitive.");
                    WriteLine("     -progress           Show the current path or file being scanned.");
                    WriteLine("     -nosub              Do not search subdirectories in the search path.");
                    WriteLine("     -debug              Show error messages.");
                    WriteLine("     -hidden             Search hidden directories.");
                    WriteLine("     -slow               Single thread mode. Can be useful for older, mechanical hdds.");
                    WriteLine("");
                    WriteLine("Example:\n");
                    WriteLine("orvina.exe \"C:\\my files\" \"return 1\"  \".cs,.js\"\n");
                    break;

                case AppState.Run:
                    DoSearch();
                    break;
            }
        }

        private static ReadOnlySpan<char> ConsoleTruncate(ReadOnlySpan<char> text)
        {
            var width = System.Console.WindowWidth - 1;
            return text.Length > width ? text.Slice(0, width) : text;
        }

        private static void PrintWipe(ReadOnlySpan<char> text)
        {
            System.Console.Write($"\r{ConsoleTruncate(text)}".PadRight(System.Console.BufferWidth));
        }

        private static void SetColor(ConsoleColor color)
        {
            System.Console.ForegroundColor = color;
        }

        private static void WriteLine(string msg)
        {
            System.Console.WriteLine(msg);
        }

        private void DoSearch()
        {
            using (var search = new SearchEngine())
            {
                if (cmdArgs.showErrors)
                {
                    search.OnError += Search_OnError;
                }
                search.OnFileFound += Search_OnFileFound;
                search.OnSearchComplete += Search_OnSearchComplete;
                if (cmdArgs.showProgress)
                {
                    search.OnProgress += Search_OnProgress;
                }
                WriteLine("searching...('q' to quit)\n");
                stopwatch.Start();

                try
                {
                    search.Start(cmdArgs.searchPath,
                        cmdArgs.includeSubdirectories,
                        cmdArgs.searchText,
                        cmdArgs.showHidden,
                        cmdArgs.caseSensitive,
                        cmdArgs.slowmode,
                        cmdArgs.fileExtensions);
                }
                catch (Exception e)
                {
                    WriteLine(e.Message);
                    Environment.Exit(0);
                }

                while (!searchEnded)
                {
                    if (!attemptQuit && System.Console.KeyAvailable
                        && System.Console.ReadKey(true).Key == ConsoleKey.Q)
                    {
                        search.Stop();
                        attemptQuit = true;
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }

                var sawAFile = searchResults.Count > 0;
                while (!attemptQuit && sawAFile)
                {
                    WriteLine("Open File? Enter Id or 'q' to quit: ");

                    var key = System.Console.ReadKey();
                    attemptQuit = key.Key == ConsoleKey.Q;

                    if (!attemptQuit && char.IsNumber(key.KeyChar))
                    {
                        var fileId = key.KeyChar + System.Console.ReadLine();
                        var fileOpened = false;
                        if (int.TryParse(fileId, out int result))
                        {
                            if (result - 1 < searchResults.Count)
                            {
                                var file = searchResults[result - 1].file;

                                WriteLine($"Opening {file}...\n");
                                try
                                {
                                    using (var p = Process.Start(new ProcessStartInfo(file) { UseShellExecute = true }))
                                    {
                                        fileOpened = true;
                                    };
                                }
                                catch (Exception e)
                                {
                                    if (cmdArgs.showErrors)
                                    {
                                        WriteLine(e.ToString());
                                    }
                                }
                            }
                        }

                        if (!fileOpened)
                        {
                            WriteLine("That didn't work!");
                        }
                    }
                    else if (!attemptQuit)
                    {
                        WriteLine("\nThat didn't work!");
                    }
                }

                //being a gentleman
                search.OnError -= Search_OnError;
                search.OnFileFound -= Search_OnFileFound;
                search.OnSearchComplete -= Search_OnSearchComplete;
                search.OnProgress -= Search_OnProgress;
            }
        }

        private void OutputResults()
        {
            for (var i = 0; i < searchResults.Count; i++)
            {
                PrintFileFound(searchResults[i].file, i + 1);

                //Print Line Data
                foreach (var lineResult in searchResults[i].lineResults)
                {
                    if (attemptQuit)
                        return;

                    SetColor(ConsoleColor.Yellow);
                    System.Console.Write($"({lineResult.LineNumber}) ");

                    foreach (var linePart in lineResult.LineParts)
                    {
                        SetColor(linePart.IsMatch ? ConsoleColor.DarkCyan : ConsoleColor.Yellow);
                        System.Console.Write(linePart.Text);
                    }

                    System.Console.WriteLine();
                }
            }
        }

        private void PrintFileFound(string file, int fileId)
        {
            if (cmdArgs.showProgress)
                PrintWipe("");

            var fileSpan = file.AsSpan();
            var lastSlashIdx = fileSpan.LastIndexOf('\\') + 1;
            var prefix = fileSpan.Slice(0, lastSlashIdx);
            var fileName = fileSpan.Slice(lastSlashIdx, fileSpan.Length - lastSlashIdx).ToString();

            SetColor(ConsoleColor.Green);
            System.Console.Write($"\nFound: {prefix}");
            SetColor(ConsoleColor.Red);
            System.Console.Write(fileName);
            SetColor(ConsoleColor.DarkGray);
            WriteLine($"({fileId})");
        }

        private AppState ProcessArgs(string[] args)
        {
            try
            {
                if (args.Length == 1) //probably -help flag
                {
                    switch (args[0])
                    {
                        case "-h":
                        case "-help":
                        case "/H":
                        case "/h":
                            return AppState.ShowHelp;
                    }
                }
                else
                {
                    cmdArgs.searchPath = args[0];
                    cmdArgs.searchText = args[1];
                    cmdArgs.fileExtensions = args[2].Split(',');
                    cmdArgs.includeSubdirectories = !args.Any(a => a == "-nosub" || a == "/nosub");
                    cmdArgs.showErrors = args.Any(a => a == "-debug" || a == "/debug");
                    cmdArgs.showProgress = args.Any(a => a == "-progress" || a == "/progress");
                    cmdArgs.showHidden = args.Any(a => a == "-hidden" || a == "/hidden");
                    cmdArgs.caseSensitive = args.Any(a => a == "-cases" || a == "/cases");
                    cmdArgs.slowmode = args.Any(a => a == "-slow" || a == "/slow");
                    return AppState.Run;
                }
            }
            catch
            {
            }

            return AppState.ShowHelp;
        }

        private void Search_OnError(string error)
        {
            if (cmdArgs.showErrors)
            {
                if (cmdArgs.showProgress)
                    PrintWipe("");

                SetColor(ConsoleColor.Red);
                WriteLine($"\r{error}");
            }
        }

        private void Search_OnFileFound(string file, List<SearchEngine.LineResult> matchingLines)
        {
            searchResults.Add(new FileResult { file = file, lineResults = matchingLines });
        }

        private void Search_OnProgress(string filePath, bool isFile)
        {
            if (isFile)
            {
                searchCount++;
            }

            SetColor(ConsoleColor.Green);
            PrintWipe(filePath);
        }

        private void Search_OnSearchComplete()
        {
            stopwatch.Stop();
            OutputResults();
            searchEnded = true;

            if (cmdArgs.showProgress)
                PrintWipe("");

            SetColor(ConsoleColor.Gray);
            WriteLine($"\nSearch Complete in {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)}s!\n");

            if (cmdArgs.showProgress)
            {
                WriteLine($"\nSearched {searchCount} files\n");
            }
        }

        private struct CommandArgs
        {
            public bool caseSensitive;
            public string[] fileExtensions;
            public bool includeSubdirectories;
            public string searchPath;
            public string searchText;
            public bool showErrors;
            public bool showHidden;
            public bool showProgress;
            public bool slowmode;
        }

        private struct FileResult
        {
            public string file;
            public List<SearchEngine.LineResult> lineResults;
        }
    }
}