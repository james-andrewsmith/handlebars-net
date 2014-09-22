using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class HelperCompareTestClass
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
        public void StringConcat()
        {
            var template = "{{concat 'foo' 'bar'}}";
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("foobar", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void MathAddition()
        {            
            var template = "{{math 2 1}}";            
            var result = _engine.Render("test", template, null);
            Assert.AreEqual("3", result);
        }


        [TestMethod]
        [TestCategory("Handlebars")]
        public void Assign_BasicString()
        {
            var template = "{{assign \"test\" \"abc\"}}" + 
                           "{{test}}";

            var result = _engine.Render("test", template, null);
            Assert.AreEqual("abc", result);
            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Assign_Number()
        {
            var template = "{{assign \"test\" 123}}" +
                           "{{test}}";

            var result = _engine.Render("test", template, null);
            Assert.AreEqual("123", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Assign_FromContext()
        {
            var template = "{{assign \"test\" data.foo}}" +
                           "{{test}}";

            
            var result = _engine.Render("test", template, "{ data: { foo: 'bar' } }");
            Assert.AreEqual("bar", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Assign_FromNestedMoneyFormat()
        {
            var template = "{{assign \"test\" money 1000}}" +
                           "{{test}}";

            var result = _engine.Render("test", template, null);
            Assert.AreEqual("$10.00", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Assign_ChainedNestedCommands()
        {
            var template = "{{assign \"test\" money 1000}}" +
                           "{{assign \"test\" concat 'AUD' test}}" + 
                           "{{test}}";

            var result = _engine.Render("test", template, null);
            Assert.AreEqual("AUD$10.00", result);            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Nesting_Example()
        {
            var template = "{{numFormat sum 1 2}} === {{numFormat 3}} === {{sum 1 2}}";

            var result = _engine.Render("test", template, null);
            Assert.AreEqual("3 === 3 === 3", result);            
        }

        
    }
}
