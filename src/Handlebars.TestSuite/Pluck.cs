using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class PluckTestClass
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
        public void PluckFromObjectArray()
        {
            var template = "{{pluck foo 'name'}}";
            var result = _engine.Render("test", template, "{ foo: [{ id: 0, name: 'James' },{ id: 1, name: 'Andrew' },{ id: 2, name: 'Smith' }] }");
            Assert.AreEqual("James,Andrew,Smith", result);            
        }
        
    }
}
