using Orvina.Engine;
using System.Diagnostics;

namespace Orvina.Console
{
    internal class App
    {
        private readonly Dictionary<int, string> fileMap = new();
        private readonly AppState state;

        private readonly Stopwatch stopwatch = new();

        private bool attemptQuit;

        private string[] fileExtensions;

        private int fileId;
        private bool includeSubdirectories;

        private int searchCount = 0;
        private bool searchEnded;

        private string searchPath;

        private string searchText;

        private bool showErrors;
        private bool showHidden;
        private bool showProgress;

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
                    WriteLine("     <search text>   Declares the text to search for in the files");
                    WriteLine("     <file extensions>   Comma separated list of file extensions. Restricts searching to specific file types.");
                    WriteLine("     -progress           If given, show the current path or file being scanned.");
                    WriteLine("     -nosub              If given, do not search subdirectories in the search path.");
                    WriteLine("     -debug              If given, show error messages.");
                    WriteLine("     -hidden             If given, search hidden directories.");
                    WriteLine("");
                    WriteLine("Example:\n");
                    WriteLine("orvina.exe \"C:\\my files\" \"return 1\"  \".cs,.js\"\n");
                    break;

                case AppState.Run:
                    DoSearch();
                    break;
            }
        }

        private static string ConsoleTruncate(string text)
        {
            var width = System.Console.WindowWidth - 1;
            return text.Length > width ? text.Substring(0, width) : text;
        }

        private static void PrintWipe(string text)
        {
            text = ConsoleTruncate(text);
            SetColor(ConsoleColor.Green);

            System.Console.Write($"\r{text}".PadRight(System.Console.BufferWidth));

            //System.Console.Write($"\r{consoleClear}");
            //System.Console.Write($"\r{text}");
        }

        private static void SetColor(ConsoleColor color)
        {
            System.Console.ForegroundColor = color;
        }

        private static List<string> SplitAndKeepDelimiter(string input, string delimiter)
        {
            var parts = new List<string>();

            int startIdx;
            while ((startIdx = input.IndexOf(delimiter, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                if (startIdx > 0)
                {
                    parts.Add(input.Substring(0, startIdx));
                    input = input.Remove(0, startIdx);
                }
                else //if (startIdx == 0)
                {
                    parts.Add(input.Substring(0, delimiter.Length));
                    input = input.Remove(0, delimiter.Length);
                }
            }

            if (input.Length > 0) //any leftover
            {
                parts.Add(input);
            }

            return parts;
        }

        private static void WriteLine(string msg)
        {
            System.Console.WriteLine(msg);
        }

        private void DoSearch()
        {
            using (var search = new SearchEngine())
            {
                if (showErrors)
                {
                    search.OnError += Search_OnError;
                }
                search.OnFileFound += Search_OnFileFound;
                search.OnSearchComplete += Search_OnSearchComplete;
                if (showProgress)
                {
                    search.OnProgress += Search_OnProgress;
                }
                WriteLine("searching...('q' to quit)\n");
                stopwatch.Start();
                search.Start(searchPath, includeSubdirectories, searchText, showHidden, fileExtensions);

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

                var sawAFile = fileId > 0;
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
                            if (fileMap.ContainsKey(result))
                            {
                                var file = fileMap[result];

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
                                    if (showErrors)
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
                    var nosubFlag = args.Any(a => a == "-nosub" || a == "/nosub");
                    var debugFlag = args.Any(a => a == "-debug" || a == "/debug");
                    var progressFlag = args.Any(a => a == "-progress" || a == "/progress");
                    var hiddenFlag = args.Any(a => a == "-hidden" || a == "/hidden");

                    searchPath = args[0];
                    searchText = args[1];
                    fileExtensions = args[2].Split(',');
                    includeSubdirectories = !nosubFlag;
                    showErrors = debugFlag;
                    showProgress = progressFlag;
                    showHidden = hiddenFlag;
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
            if (showErrors)
            {
                SetColor(ConsoleColor.Red);
                WriteLine($"\r{error}");
            }
        }

        private void Search_OnFileFound(string file, string[] matchingLines)
        {
            fileId++;

            PrintWipe("");

            var fileParts = SplitAndKeepDelimiter(file, @"\");

            SetColor(ConsoleColor.Green);
            System.Console.Write($"\nFound: " + string.Join("", fileParts.Select(p => p == fileParts.Last() ? "" : p)));
            SetColor(ConsoleColor.Red);
            System.Console.Write(fileParts.Last());

            SetColor(ConsoleColor.DarkGray);
            WriteLine($"({fileId})");

            fileMap.Add(fileId, file);

            SetColor(ConsoleColor.Green);
            foreach (string line in matchingLines.Select(line => ConsoleTruncate(line)))
            {
                var parts = SplitAndKeepDelimiter(line, searchText);
                foreach (var part in parts)
                {
                    SetColor(part.Equals(searchText, StringComparison.OrdinalIgnoreCase)
                        ? ConsoleColor.DarkCyan
                        : ConsoleColor.Yellow);
                    System.Console.Write(ConsoleTruncate(part));
                }

                WriteLine("");

                if (attemptQuit)
                {
                    break;
                }
            }
        }

        private void Search_OnProgress(string filePath, bool isFile)
        {
            if (isFile)
            {
                searchCount++;
            }
            PrintWipe(filePath);
        }
        private void Search_OnSearchComplete()
        {
            PrintWipe("");
            stopwatch.Stop();
            SetColor(ConsoleColor.Gray);
            WriteLine($"\nSearch Complete in {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)}s!\n");
            searchEnded = true;

            if (showProgress)
            {
                WriteLine($"\nSearched {searchCount} files\n");
            }
        }
    }
}