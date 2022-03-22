using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Orvina.Engine.SearchEngine;

[assembly: InternalsVisibleTo("UnitTests")]
namespace Orvina.Engine.Support
{
    internal class FileScanner
    {
        public bool stop;
        public TextBytes.SearchText searchText;

        public List<LineResult> ScanFile(ReadOnlySpan<byte> data)
        {
            if (data.Length >= 3 && data[0] == 239 && data[1] == 187 && data[2] == 191)
            {
                //239 187 191 BOM, rip it off
                data = data.Slice(3, data.Length - 3);
            }

            if (searchText.hasStarWildCard)
                return ScanFileStar(data);

            var matchingLines = new List<LineResult>();

            //most cases the file won't contain the searchText at all
            var searchTextIdx = 0;
            var endFileIdx = data.Length - 1;

            //found our word in the text
            while ((searchTextIdx = TextBytes.IndexOf(data, searchText, searchTextIdx)) >= 0 && !stop)
            {
                //newLines chars on left and right
                var lineStartIdx = TextBytes.LeftNewLineIndex(data, searchTextIdx);
                var lineEndIdx = TextBytes.RightNewLineIndex(data, (searchTextIdx + searchText.length) + 1);
                lineStartIdx = lineStartIdx >= 0 ? lineStartIdx + 1 : 0;
                lineEndIdx = lineEndIdx >= 0 ? lineEndIdx - 1 : endFileIdx;

                //suppose searchText = 50, and lineStartIdx = 40, then 10 is the index of searchText in this line
                var matchStartIdx = searchTextIdx - lineStartIdx;

                matchingLines.Add(new LineResult()
                {
                    LineNumber = TextBytes.CountLines(data, lineStartIdx),
                    LineText = Encoding.UTF8.GetString(data.Slice(lineStartIdx, lineEndIdx - lineStartIdx + 1)),
                    LineMatches = new()
                    {
                        new LineMatch
                        {
                            MatchStartIdx = matchStartIdx,
                            MatchEndIdx = matchStartIdx + searchText.length
                        }
                    }
                });

                while ((searchTextIdx = TextBytes.IndexOf(data, searchText, (searchTextIdx + searchText.length), lineEndIdx)) >= 0 && !stop)
                {
                    matchStartIdx = searchTextIdx - lineStartIdx;
                    matchingLines[matchingLines.Count - 1].LineMatches.Add(new LineMatch
                    {
                        MatchStartIdx = matchStartIdx,
                        MatchEndIdx = matchStartIdx + searchText.length
                    });
                }

                searchTextIdx = lineEndIdx + 1;
            }
            //[0][1][2]
            return matchingLines;
        }

        private List<LineResult> ScanFileStar(ReadOnlySpan<byte> data)
        {
            var matchingLines = new List<LineResult>();
            var fsm = new StarStateMachine();

            var lastNewline = -1;
            var lastIdx = data.Length - 1;
            var lineCount = 1;
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] == TextBytes.newLine || i == lastIdx)
                {
                    lineCount++;
                    if (i > lastNewline)
                    {
                        lastNewline = lastNewline < 0 ? 0 : lastNewline;
                        var lineData = data.Slice(lastNewline, i - lastNewline - 1);

                        int idx;
                        if ((idx = fsm.IndexOf(lineData, searchText, out int endIdx)) >= 0)
                        {


                            matchingLines.Add(new LineResult()
                            {
                                LineNumber = lineCount,
                                LineText = Encoding.UTF8.GetString(lineData),
                                LineMatches = new()
                                {
                                    new LineMatch
                                    {
                                        MatchStartIdx = idx,
                                        MatchEndIdx = endIdx
                                    }
                                }
                            });
                        }

                        lastNewline = i + 1;
                    }
                }
            }

            return matchingLines;
        }
    }
}
