namespace Orvina.Engine.Support
{
    internal static class TextBytes
    {
        public const byte asterisk = 0x2a;
        public const byte newLine = 0x0a;
        public const byte questionMark = 0x3f;
        public const byte tilde = 0x7e;
        public const byte carriageReturn = 0x0d;

        public const char starChar = '*';
        public const char tildeChar = '~';


        public struct SearchText
        {
            public readonly byte[] upper;
            public readonly byte[] lower;
            public readonly int maxIdx;
            public readonly int length;

            public readonly bool hasStarWildCard = false;

            public SearchText(string text)
            {
                if (text[0] == starChar || (text[text.Length - 1] == starChar && (text.Length == 1 || text[text.Length - 2] != tildeChar)))
                    throw new Exception("search cannot start or end with * wildcard");

                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] == starChar && (i == 0 || text[i - 1] != tildeChar))
                    {
                        hasStarWildCard = true;
                        break;
                    }
                }

                upper = System.Text.Encoding.UTF8.GetBytes(text.ToUpper());
                lower = System.Text.Encoding.UTF8.GetBytes(text.ToLower());
                maxIdx = upper.Length - 1;
                length = upper.Length;
            }
        }

        public static int CountLines(ReadOnlySpan<byte> data, int upToIndex)
        {
            var count = 1;
            for (var i = 0; i <= upToIndex && i < data.Length; i++)
            {
                if (data[i] == newLine)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// searchText can have uppercase, lowercase, ? wildcard only
        /// </summary>
        /// <param name="data"></param>
        /// <param name="searchText"></param>
        /// <param name="initialIdx"></param>
        /// <returns></returns>
        public static int IndexOf(ReadOnlySpan<byte> data, SearchText searchText, int initialIdx = 0, int maxIndex = 0)
        {
            var j = 0;
            var thoughtIdx = -1;

            for (var i = initialIdx; i < data.Length && (maxIndex == 0 || i <= maxIndex); i++)
            {
                if (searchText.upper[j] == tilde && j + 1 < searchText.length && searchText.upper[j + 1] == questionMark) //if searching for a ~
                {
                    j++;//move j up
                }

                if ((searchText.upper[j] == questionMark && (j - 1 < 0 || searchText.upper[j - 1] != tilde)) //if searching for a '?'
                    || (data[i] == searchText.upper[j] || data[i] == searchText.lower[j])) //if searching for exact match
                {
                    if (thoughtIdx == -1)
                        thoughtIdx = i;

                    j++;
                    if (j > searchText.maxIdx)
                        return thoughtIdx;
                }
                else
                {
                    j = 0;
                    if (thoughtIdx >= 0)
                        i = thoughtIdx;
                    thoughtIdx = -1;
                }
            }

            return j == searchText.maxIdx ? thoughtIdx : -1;
        }

        public static int RightNewLineIndex(ReadOnlySpan<byte> data, int initialIdx)
        {
            for (var i = initialIdx; i < data.Length; i++)
            {
                if (data[i] == newLine || data[i] == carriageReturn)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LeftNewLineIndex(ReadOnlySpan<byte> data, int initialIdx)
        {
            for (var i = initialIdx; i >= 0; i--)
            {
                if (data[i] == newLine || data[i] == carriageReturn)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}