using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        private static Orvina.Engine.Support.FileScanner scanner = new();
        private static byte[] data = System.Text.Encoding.UTF8.GetBytes(UnitTests.Properties.Resources.Anthem);












        [TestMethod]
        public void VerifyLines()
        {
            scanner.searchText = new("O say can you see");
            var lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Count == 1);

            scanner.searchText = new("O say");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Count == 2);

            scanner.searchText = new("brave");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Count == 4);
        }

        [TestMethod]
        public void TrimTests()
        {
            Assert.IsTrue("" == Trim("\r").ToString());
            Assert.IsTrue("asdfasf" == Trim("asdfasf").ToString());
            Assert.IsTrue("asdfasf" == Trim("asdfasf\n").ToString());
            Assert.IsTrue("asdfasf" == Trim("\n\n\n\r\rasdfasf\n\n\n\r").ToString());
            Assert.IsTrue("asdfasf" == Trim("\r\n\n\n\r\rasdfasf").ToString());
            Assert.IsTrue("" == Trim("").ToString());
            Assert.IsTrue("  " == Trim("  ").ToString());
            Assert.IsTrue(" " == Trim(" ").ToString());
            Assert.IsTrue("a" == Trim("a\r").ToString());
            Assert.IsTrue("a \r a" == Trim("a \r a").ToString());
            Assert.IsTrue("a \r a" == Trim("a \r a\n").ToString());
            Assert.IsTrue("a \r a" == Trim("\na \r a\n").ToString());
        }

        private static ReadOnlySpan<char> Trim(ReadOnlySpan<char> text)
        {
            if (text.Length == 0)
                return text;

            if (text.Length == 1)
            {
                return (text[0] == '\n' || text[0] == '\r') ? "" : text;
            }

            //all texts 2 or longer
            int i;
            for (i = 0; i < text.Length; i++)
            {
                if (text[i] != '\n' && text[i] != '\r')
                {
                    break;
                }
            }

            int j;
            for (j = text.Length - 1; j >= 0; j--)
            {
                if (text[j] != '\n' && text[j] != '\r')
                {
                    break;
                }
            }

            return text.Slice(i, j - i + 1);
        }

        private static ReadOnlySpan<byte> TrimBytes(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0)
                return data;

            if (data.Length == 1)
            {
                return (data[0] == '\n' || data[0] == '\r') ? data.Slice(0, 0) : data;
            }

            //all texts 2 or longer
            int i;
            for (i = 0; i < data.Length; i++)
            {
                if (data[i] != '\n' && data[i] != '\r')
                {
                    break;
                }
            }

            int j;
            for (j = data.Length - 1; j >= 0; j--)
            {
                if (data[j] != '\n' && data[j] != '\r')
                {
                    break;
                }
            }

            return data.Slice(i, j - i + 1);
        }


        [TestMethod]
        public void VerifyWildcard_QuestionMark()
        {
            var result = "O thus be it ever, when freemen shall stand";

            scanner.searchText = new("f?ee?en");
            var lines = scanner.ScanFile(System.Text.Encoding.UTF8.GetBytes(result));
            Assert.IsTrue(lines[0].LineText == result);
            Assert.IsTrue(lines.Count == 1);

            scanner.searchText = new("free?en");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(Trim(lines[0].LineText.AsSpan()) == result);
            Assert.IsTrue(lines.Count == 1);

            scanner.searchText = new("?ree?en");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines[0].LineText == result);
            Assert.IsTrue(lines.Count == 1);

            scanner.searchText = new("??ee?en");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines[0].LineText == result);
            Assert.IsTrue(lines.Count == 1);



            result = "And this be our motto: 'In God is our trust.'";

            scanner.searchText = new("our motto");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines[0].LineText == result);
            Assert.IsTrue(lines.Count == 1);

            scanner.searchText = new("ou? ????o");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines[0].LineText == result);
            Assert.IsTrue(lines.Count == 1);
        }
    }
}