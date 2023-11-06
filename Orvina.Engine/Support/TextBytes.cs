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

            public readonly bool hasStarWildCard = false;

            public readonly int matchCount = 0;

            public SearchText(string text, bool caseSensitive = false)
            {
                if (text[0] == starChar || (text[text.Length - 1] == starChar && (text.Length == 1 || text[text.Length - 2] != tildeChar)))
                    throw new Exception("search cannot start or end with * wildcard");

                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] != tilde || (i + 1 < text.Length && text[i + 1] != questionMark))
                    {
                        matchCount++;
                    }

                    if (!hasStarWildCard && text[i] == starChar && (i == 0 || text[i - 1] != tildeChar))
                    {
                        hasStarWildCard = true;
                    }
                }

                if (caseSensitive)
                {
                    upper = lower = System.Text.Encoding.UTF8.GetBytes(text);
                }
                else
                {
                    upper = System.Text.Encoding.UTF8.GetBytes(text.ToUpper());
                    lower = System.Text.Encoding.UTF8.GetBytes(text.ToLower());
                }
                maxIdx = upper.Length - 1;
            }
        }

        public static int CountLines(ReadOnlySpan<byte> data, int upToIndex, ref int lastIdx, ref int count)
        {
            for (var i = lastIdx; i <= upToIndex && i < data.Length; i++)
            {
                lastIdx = i;
                if (data[i] == newLine)
                    count++;
            }

            return count;
        }

        public static ReadOnlySpan<byte> TrimBytes(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0)
                return data;

            if (data.Length == 1)
            {
                return (data[0] == newLine || data[0] == carriageReturn) ? data.Slice(0, 0) : data;
            }

            //all texts 2 or longer
            int i;
            for (i = 0; i < data.Length; i++)
            {
                if (data[i] != newLine && data[i] != carriageReturn)
                {
                    break;
                }
            }

            int j;
            for (j = data.Length - 1; j >= 0; j--)
            {
                if (data[j] != newLine && data[j] != carriageReturn)
                {
                    break;
                }
            }

            return data.Slice(i, j - i + 1);
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
                if (searchText.upper[j] == tilde && j + 1 <= searchText.maxIdx && searchText.upper[j + 1] == questionMark) //if searching for a ~
                {
                    j++;//move j up
                }

                if ((searchText.upper[j] == questionMark && (j - 1 < 0 || searchText.upper[j - 1] != tilde) && data[i] != newLine && data[i] != carriageReturn) //if searching for a '?'
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

            //return j == searchText.maxIdx ? thoughtIdx : -1;
            return -1;
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