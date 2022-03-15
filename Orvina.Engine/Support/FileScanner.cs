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

        public List<LineResult> ScanFile(byte[] data)
        {
            var matchingLines = new List<LineResult>();

            //most cases the file won't contain the searchText at all
            var searchTextIdx = 0;
            var endFileIdx = data.Length - 1;

            //found our word in the text
            while (searchTextIdx < endFileIdx && (searchTextIdx = TextBytes.IndexOf(data, searchText, searchTextIdx)) >= 0 && !stop)
            {
                var searchTextLastIdx = searchTextIdx + searchText.length;
                var lineStartIdx = TextBytes.LeftNewLineIndex(data, searchTextIdx);
                var lineEndIdx = TextBytes.RightNewLineIndex(data, searchTextLastIdx);

                lineStartIdx = lineStartIdx >= 0 ? lineStartIdx + 1 : searchTextIdx;
                lineEndIdx = lineEndIdx >= 0 ? lineEndIdx - 1 : searchTextLastIdx;

                var lr = new LineResult()
                {
                    LineNumber = TextBytes.CountLines(data, lineStartIdx),
                    LineText = System.Text.Encoding.UTF8.GetString(data, lineStartIdx, lineEndIdx - lineStartIdx),
                    MatchStartIdx = searchTextIdx - lineStartIdx
                };
                lr.MatchEndIdx = lr.MatchStartIdx + searchText.length;

                matchingLines.Add(lr);
                searchTextIdx = lineEndIdx;
            }

            return matchingLines;
        }
    }
}
