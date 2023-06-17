using System.Runtime.CompilerServices;
using System.Text;
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

            var lineCount = 1;
            var notUsed = 0;

            //found our word in the text
            while ((searchTextIdx = TextBytes.IndexOf(data, searchText, ref lineCount, searchTextIdx)) >= 0 && !stop)
            {
                //newLines chars on left and right
                var lineStartIdx = TextBytes.LeftNewLineIndex(data, searchTextIdx);
                var lineEndIdx = TextBytes.RightNewLineIndex(data, (searchTextIdx + searchText.matchCount));
                lineStartIdx = lineStartIdx >= 0 ? lineStartIdx + 1 : 0;
                lineEndIdx = lineEndIdx >= 0 ? lineEndIdx - 1 : endFileIdx;

                //suppose searchText = 50, and lineStartIdx = 40, then 10 is the index of searchText in this line

                //var o = TextBytes.CountLines(data, lineStartIdx);
                var lineResult = new LineResult(lineCount);

                var matchStartIdx = searchTextIdx - lineStartIdx;
                var matchEndIdx = matchStartIdx + searchText.matchCount;
                if (matchStartIdx == 0)
                {
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(lineStartIdx, matchEndIdx)), true));
                }
                else
                {
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(lineStartIdx, matchStartIdx)), false));
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(lineStartIdx + (matchStartIdx), searchText.matchCount)), true));
                }

                //secondary instances in the line
                var prevIdx = (lineStartIdx + (matchStartIdx)) + (matchEndIdx - (matchStartIdx));

                while ((searchTextIdx = TextBytes.IndexOf(data, searchText, ref notUsed, (searchTextIdx + searchText.matchCount), lineEndIdx)) >= 0 && !stop)
                {
                    if (prevIdx < searchTextIdx)
                    {
                        //house
                        lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(prevIdx, searchTextIdx - prevIdx)), false));
                    }

                    //house
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(searchTextIdx, searchText.matchCount)), true));
                    prevIdx = searchTextIdx + searchText.matchCount;
                }

                if (prevIdx <= lineEndIdx)
                {
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(prevIdx, lineEndIdx - prevIdx + 1)), false));
                }

                matchingLines.Add(lineResult);

                searchTextIdx = lineEndIdx + 1;
            }

            return matchingLines;
        }

        private List<LineResult> ScanFileStar(ReadOnlySpan<byte> data)
        {
            var matchingLines = new List<LineResult>();
            var fsm = new StarStateMachine(searchText);

            var lastNewline = -1;
            var lastIdx = data.Length - 1;
            var lineCount = 0;
            for (var i = 0; i < data.Length && !stop; i++)
            {
                if ((data[i] == TextBytes.newLine || data[i] == TextBytes.carriageReturn) || i == lastIdx)
                {
                    if (i != lastIdx)
                        i++;

                    lineCount++;
                    if (i > lastNewline)
                    {
                        lastNewline = lastNewline < 0 ? 0 : lastNewline;
                        var lineData = data.Slice(lastNewline, i - lastNewline + 1);

                        int idx;
                        if ((idx = fsm.IndexOf(lineData, searchText, out int endIdx)) >= 0)
                        {
                            var lineResult = new LineResult(lineCount);

                            if (idx == 0)
                            {
                                lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(lineData.Slice(idx, endIdx - idx)), true));
                            }
                            else
                            {
                                //test in the large dry
                                lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(lineData.Slice(0, idx)), false));
                                //house
                                lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(lineData.Slice(idx, endIdx - idx)), true));
                            }

                            //secondary instances in the line
                            var prevIdx = idx + (endIdx - idx);
                            while ((idx = fsm.IndexOf(lineData, searchText, out endIdx, endIdx)) >= 0)
                            {
                                if (prevIdx < idx)
                                {
                                    //house
                                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(lineData.Slice(prevIdx, idx - prevIdx)), false));
                                }

                                //house
                                lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(lineData.Slice(idx, endIdx - idx + 1)), true));
                                prevIdx = idx + (endIdx - idx + 1);
                            }

                            if (prevIdx < lineData.Length - 1)
                            {
                                lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(TextBytes.TrimBytes(lineData.Slice(prevIdx, lineData.Length - prevIdx))), false));
                            }

                            matchingLines.Add(lineResult);
                        }

                        lastNewline = i + 1;
                    }
                }
            }

            return matchingLines;
        }
    }
}