using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class MoneyTestClass
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
        public void FormatMoney()
        {
            var template = "{{money 10000}}";                
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("$100.00", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void FormatMoney_CommaCheck()
        {
            var template = "{{money 1000000}}";
            var result = _engine.Render("test", template, null); 
            Assert.AreEqual("$10,000.00", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void FormatMoney_NoSymbol()
        {
            var template = "{{money 10000 ''}}";
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("100.00", result);            
        }
        
    }
}
