using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orvina.Engine.Support;

namespace TestProject1
{
    [TestClass]
    public class Test_TextBytes
    {
        private static byte[] data = System.Text.Encoding.UTF8.GetBytes(UnitTests.Properties.Resources.Anthem);

        [TestMethod]
        public void Simple_QuestionMark_Test()
        {
            var search = new TextBytes.SearchText("O say?");//
            var idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("??say?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("O say???");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("O???y");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);
        }
    }
}