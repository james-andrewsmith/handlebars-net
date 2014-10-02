using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class PluralizeTestClass
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
        public void Pluralize_WithCount()
        {
            var template = "{{pluralize 'record' 1}}";
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("record", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Pluralize_WithCount_ExpectPlural()
        {
            var template = "{{pluralize 'record' 2}}";
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("records", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Pluralize_Baby()
        {
            var template = "{{pluralize 'baby'}}";
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("babies", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Pluralize_Alumnus()
        {
            var template = "{{pluralize 'Alumnus'}}";
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("Alumni", result);
            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Pluralize_IBM()
        {
            var template = "{{pluralize 'IBM'}}";
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("IBMs", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Pluralize_Octopus()
        {
            var template = "{{pluralize 'octopus'}}";
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("octopuses", result);            
        }
    }
}
