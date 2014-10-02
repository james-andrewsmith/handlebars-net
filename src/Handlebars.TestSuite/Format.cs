using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class FormatTestClass
    {
        #region // Engine Management //
        [ClassInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            _engine = HandlebarsFactory.CreateEngine();
        }

        private static IHandlebarsEngine _engine;
        #endregion

        [TestMethod]
        [TestCategory("Handlebars")]
        public void FormatNumber()
        {
            var template = "{{format foo}}";

            var result = _engine.Render("test", template, "{ \"foo\": 1 }");
            Assert.AreEqual("1", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void FormatNumber_Large()
        {
            var template = "{{format foo}}";

            var result = _engine.Render("test", template, "{ \"foo\": 100000 }");
            Assert.AreEqual("100,000", result);            
        } 
        
    }
}
