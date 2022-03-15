namespace Orvina.Engine.Support
{
    internal static class TextBytes
    {
        public static readonly byte asterisk = 0x2a;
        public static readonly byte newLine = 0x0a;
        public static readonly byte questionMark = 0x3f;
        public static readonly byte tilde = 0x7e;

        public struct SearchText
        {
            public readonly byte[] upper;
            public readonly byte[] lower;
            public readonly int maxIdx;
            public readonly int length;

            public SearchText(string text)
            {
                upper = System.Text.Encoding.UTF8.GetBytes(text.ToUpper());
                lower = System.Text.Encoding.UTF8.GetBytes(text.ToLower());
                maxIdx = upper.Length - 1;
                length = upper.Length;
            }
        }

        public static int CountLines(byte[] data, int upToIndex, int startIdx = 0)
        {
            var count = 1;
            for (var i = startIdx; i < data.Length; i++)
            {
                if (i > upToIndex)
                    return count;

                if (data[i] == newLine)
                    count++;
            }

            return count;
        }

        public static int IndexOf(byte[] data, SearchText searchText, int initialIdx = 0)
        {
            var j = 0;
 
            var thoughtIdx = 0;

            for (var i = initialIdx; i < data.Length; i++)
            {
                //is the search string matching up with the text
                var rawMatch = (data[i] == searchText.upper[j] || data[i] == searchText.lower[j])
                            //include questionMark if not preceeded by tilde
                            || (searchText.upper[j] == questionMark && (j - 1 < 0 || searchText.upper[j - 1] != tilde));

                if (j == 0 && rawMatch) //first match
                {
                    //we're on the first character
                    thoughtIdx = i;
                    j++;
                    if (j > searchText.maxIdx)
                        return i;
                }
                else if (j > 0)
                {

                    if (rawMatch) //second+ match
                    {
                        j++;
                        if (j > searchText.maxIdx) //we got to the end of our match string
                            return thoughtIdx;
                    }
                    else //no match
                    {
                        j = 0;
                        i = thoughtIdx;
                    }
                }
            }

            return -1;
        }

        public static int RightNewLineIndex(byte[] data, int initialIdx)
        {
            for (var i = initialIdx; i < data.Length; i++)
            {
                if (data[i] == newLine)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LeftNewLineIndex(byte[] data, int initialIdx)
        {
            for (var i = initialIdx; i >= 0; i--)
            {
                if (data[i] == newLine)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}