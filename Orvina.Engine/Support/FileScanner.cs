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

                var lineResult = new LineResult(TextBytes.CountLines(data, lineStartIdx));

                var matchStartIdx = searchTextIdx - lineStartIdx;
                var matchEndIdx = matchStartIdx + searchText.length;
                if (matchStartIdx == 0)
                {
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(lineStartIdx, matchEndIdx)), true));
                }
                else
                {
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(lineStartIdx, matchStartIdx)), false));
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(lineStartIdx + (matchStartIdx), searchText.length)), true));
                }


                //secondary instances in the line
                var prevIdx = (lineStartIdx + (matchStartIdx)) + (matchEndIdx - (matchStartIdx));
                while ((searchTextIdx = TextBytes.IndexOf(data, searchText, (searchTextIdx + searchText.length), lineEndIdx)) >= 0 && !stop)
                {
                    if (prevIdx < searchTextIdx)
                    {
                        //house
                        lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(prevIdx, searchTextIdx - prevIdx)), false));
                    }

                    //house
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(searchTextIdx, searchText.length)), true));
                    prevIdx = searchTextIdx + searchText.length;
                }

                if (prevIdx < lineEndIdx)
                {
                    lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(data.Slice(prevIdx, lineEndIdx - prevIdx+1)), false));
                }

                matchingLines.Add(lineResult);

                searchTextIdx = lineEndIdx + 1;
            }

            return matchingLines;
        }

        private List<LineResult> ScanFileStar(ReadOnlySpan<byte> data)
        {
            var matchingLines = new List<LineResult>();
            var fsm = new StarStateMachine();

            var lastNewline = -1;
            var lastIdx = data.Length - 1;
            var lineCount = 0;
            for (var i = 0; i < data.Length && !stop; i++)
            {
                if (data[i] == TextBytes.newLine || i == lastIdx)
                {
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
                                lineResult.LineParts.Add(new LinePart(Encoding.UTF8.GetString(lineData.Slice(prevIdx, lineData.Length - prevIdx)), false));
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
