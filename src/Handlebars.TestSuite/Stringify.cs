using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class StringifyTestClass
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
        public void Stringify_Context()
        {
            var template = "{{stringify this}}";
            var result = _engine.Render("test", template, "{ \"jas\": 123, \"foo\": \"bar\" }");
            Assert.AreEqual("{\"jas\":123,\"foo\":\"bar\"}", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Stringify_Context_SubObject()
        {
            var template = "{{stringify jas}}";
            var result = _engine.Render("test", template, "{ jas: { db: 'iphone' }, foo: 'bar' }");
            Assert.AreEqual("{\"db\":\"iphone\"}", result);            
        } 
    }
}
