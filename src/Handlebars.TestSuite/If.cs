using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class IfTestClass
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
        public void BasicPropertyCheck()
        {
            var template = "{{#if foo}}" + 
                           "bar" + 
                           "{{else}}" +
                           "notbar" + 
                           "{{/if}}";

            var result = _engine.Render("test", template, "{ \"foo\": true }");
            Assert.AreEqual("bar", result);
            result = _engine.Render("test", template, "{ \"foo\": false }");
            Assert.AreEqual("notbar", result);
            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Conditionals_Equals ()
        {
            var template = "{{#if foo '===' 'bar'}}" +
                           "bar" +
                           "{{else}}" +
                           "notbar" +
                           "{{/if}}";

            var result = _engine.Render("test", template, "{ foo: 'bar' }");
            Assert.AreEqual("bar", result);

            result = _engine.Render("test", template, "{ foo: 'foo' }");
            Assert.AreEqual("notbar", result);
            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Conditionals_ContextProperty_InListNumber()
        {
            var template = "{{#if foo \"in\" [1,2,5] }}" +
                           "bar" +
                           "{{else}}" +
                           "notbar" +
                           "{{/if}}";

            var result = _engine.Render("test", template, "{ foo: 1 }");
            Assert.AreEqual("bar", result);

            result = _engine.Render("test", template, "{ foo: 2 }");
            Assert.AreEqual("bar", result);

            result = _engine.Render("test", template, "{ foo: 3 }");
            Assert.AreEqual("notbar", result);
            
        }

        [TestMethod]
        [TestCategory("Handlebars")]
        public void Conditionals_ContextProperty_InListString()
        {
            var template = "{{#if foo 'in' ['bar','notbar','reallynotbar']}}" +
                           "bar" +
                           "{{else}}" +
                           "notbar" +
                           "{{/if}}";

            var result = _engine.Render("test", template, "{ foo: 'bar' }");
            Assert.AreEqual("bar", result);

            result = _engine.Render("test", template, "{ foo: 'notbar' }");
            Assert.AreEqual("bar", result);

            result = _engine.Render("test", template, "{ foo: 'foobar' }");
            Assert.AreEqual("notbar", result);           
        }

        

    }
}
