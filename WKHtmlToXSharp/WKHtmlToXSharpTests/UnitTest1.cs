using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace WKHtmlToXSharpTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {

            var b = await NeuroSpeech.WKHtmlToXSharp.WKHtmlToX.HtmlToPdfAsync("<html><body><div>t</div></body></html>", new NeuroSpeech.WKHtmlToXSharp.ConversionTask());
            Assert.IsTrue(b.Length > 0);
        }
    }
}
