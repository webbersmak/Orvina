using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orvina.Engine.Support;

namespace TestProject1
{
    [TestClass]
    public class Test_TextBytes
    {
        private static byte[] data = System.Text.Encoding.UTF8.GetBytes(UnitTests.Properties.Resources.Anthem);

        [TestMethod]
        public void Simple_StringLiterals_Test()
        {
            var data = System.Text.Encoding.UTF8.GetBytes("happy.doogoo.");

            var search = new TextBytes.SearchText("doogoo");//
            var idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 6);

            search = new TextBytes.SearchText("ha");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("happy");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("h");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("happy.doogoo.");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("hh");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == -1);

            search = new TextBytes.SearchText("happy.doogoo. ");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == -1);

            search = new TextBytes.SearchText(" happy.doogoo. ");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == -1);


        }

        [TestMethod]
        public void Simple_QuestionMarkWildcard_Test()
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

            search = new TextBytes.SearchText("free???");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx >= 0);

            search = new TextBytes.SearchText("?reem??");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx >= 0);
        }

        [TestMethod]
        public void Simple_QuestionMark_Test()
        {
            var data = System.Text.Encoding.UTF8.GetBytes("?welc?ome");

            var search = new TextBytes.SearchText("~?");//
            var idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("~?w");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("~?x");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == -1);

            search = new TextBytes.SearchText("c~?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 4);

            search = new TextBytes.SearchText("c~?o");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 4);

            search = new TextBytes.SearchText("c~?ome");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 4);

            search = new TextBytes.SearchText("c~?x");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == -1);

            search = new TextBytes.SearchText("welcome~?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == -1);

            search = new TextBytes.SearchText("welcome~?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == -1);

            //combos ~? and ? wildcards
            data = System.Text.Encoding.UTF8.GetBytes("???");

            search = new TextBytes.SearchText("~?~?~?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("???");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("~?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);


            data = System.Text.Encoding.UTF8.GetBytes("?x?");
            search = new TextBytes.SearchText("~?x");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("?x~?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);

            search = new TextBytes.SearchText("?x?");//
            idx = TextBytes.IndexOf(data, search);
            Assert.IsTrue(idx == 0);
        }
    }
}