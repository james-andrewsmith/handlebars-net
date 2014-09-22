using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class RenderTestClass
    {
        #region // Engine Management //       
        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            _engine = HandlebarsFactory.CreateEngine();
        }

        [AssemblyCleanup()]
        public static void Teardown()
        {
            _engine.Dispose();
        }

        private static IHandlebarsEngine _engine;
        #endregion

        [TestMethod]
        [TestCategory("Render")]
        public void RenderSingle()
        {
            var result = _engine.Render("test", "<body>{{body}}</body>", "{\"body\":\"This is a test\"}");
            Assert.AreEqual(result, "<body>This is a test</body>");
        }

        [TestMethod]
        [TestCategory("Render")]
        public void CompileThenRender()
        {
            _engine.Compile("test-then-render", "<body>{{body}}</body>");
            var result = _engine.Render("test-then-render", "{\"body\":\"This is a test\"}");
            Assert.AreEqual(result, "<body>This is a test</body>");
        }

        [TestMethod]
        [TestCategory("Render")]
        public void CompileThenExists()
        {
            _engine.Compile("test-then-exists", "<body>{{body}}</body>");
            var result = _engine.Exists("test-then-exists");
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory("Render")]
        public void CompileThenExportPrecompile()
        {
            _engine.Compile("test-then-export-precompile", "<body>{{body}}</body>");
            var result = _engine.ExportPrecompile();
            Assert.IsNotNull(result);
        }
    }
}
