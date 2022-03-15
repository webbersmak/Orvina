using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        public void VerifyWildcard_QuestionMark()
        {
            var result = "O thus be it ever, when freemen shall stand";

            scanner.searchText = new("free?en");
            var lines = scanner.ScanFile(data);
            Assert.IsTrue(lines[0].LineText == result);
            Assert.IsTrue(lines.Count == 1);

            scanner.searchText = new("?ree?en");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines[0].LineText == result);
            Assert.IsTrue(lines.Count == 1);

            scanner.searchText = new("??ee?en");
            lines = scanner.ScanFile(data);
            Assert.IsTrue(lines[0].LineText == result);
            Assert.IsTrue(lines.Count == 1);

            scanner.searchText = new("f?ee?en");
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