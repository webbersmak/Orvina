using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace TestProject1
{
    [TestClass]
    public class Test_FileScanner
    {
        private static readonly Orvina.Engine.Support.FileScanner scanner = new();
        private static readonly byte[] data = System.Text.Encoding.UTF8.GetBytes(UnitTests.Properties.Resources.Anthem);

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


        [TestMethod]
        public void VerifyWildcard_QuestionMark()
        {
            scanner.searchText = new("f?ee?en");
            var lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Any(l => l.LineParts.Any(l => l.Text == "freemen" && l.IsMatch)));

            scanner.searchText = new("free?en");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Any(l => l.LineParts.Any(l => l.Text == "freemen" && l.IsMatch)));

            scanner.searchText = new("?ree?en");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Any(l => l.LineParts.Any(l => l.Text == "freemen" && l.IsMatch)));

            scanner.searchText = new("??ee?en");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Any(l => l.LineParts.Any(l => l.Text == "freemen" && l.IsMatch)));

            scanner.searchText = new("our motto");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Any(l => l.LineParts.Any(l => l.Text == "our motto" && l.IsMatch)));

            scanner.searchText = new("ou? ????o");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines.Any(l => l.LineParts.Any(l => l.Text == "our motto" && l.IsMatch)));
        }
    }
}