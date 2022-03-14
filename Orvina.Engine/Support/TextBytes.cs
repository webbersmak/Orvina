using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orvina.Engine.Support
{
    internal static class TextBytes
    {
        public static readonly byte newLine = 0x0A; //10

        public static int IndexOf(byte[] data, byte[] searchUpper, byte[] searchLower, int initialIdx = 0)
        {
            var j = 0;
            var jLimit = searchUpper.Length - 1;
            var thoughtIdx = 0;

            for (var i = initialIdx; i < data.Length; i++)
            {
                if (j==0 && (data[i] == searchLower[j] || data[i] == searchUpper[j]))
                {
                    //we're on the first character
                    thoughtIdx = i;
                    j++;
                    if (j > jLimit)
                        return i;
                }
                else //if (j > 0)
                {
                    if (data[i] == searchLower[j] || data[i] == searchUpper[j])
                    {
                        j++;
                        if (j > jLimit) //we got to the end of our match string
                            return thoughtIdx;
                    }
                    else //out string didn't match
                    {
                        j = 0;
                        i = thoughtIdx;
                    }
                }
            }

            return -1;
        }

        public static int MarkerIndexOf(byte[] data, byte marker, int initialIdx = 0)
        {
            for (var i = initialIdx; i < data.Length; i++)
            {
                if (data[i] == marker)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int MarkerLastIndexOf(byte[] data, byte marker, int initialIdx = 0)
        {
            for (var i = initialIdx; i >= 0; i--)
            {
                if (data[i] == marker)
                {
                    return i;
                }
            }

            return -1;
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
    }
}
