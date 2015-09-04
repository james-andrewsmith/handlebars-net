using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Handlebars;

namespace Handlebars.TestSuite
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class HandleBarsTestClass
    {
        #region // Engine Management //
        [ClassInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            _engine = HandlebarsFactory.CreateEngine();
        }

        private static IHandlebarsEngine _engine;
        #endregion

        private const string template = "<div class=\"entry\"><h1>{{this.title}}</h1><div class=\"body\">{{{body}}}</div></div>";        

        [TestMethod]
        public void CompileTemplate()
        {

            _engine.Compile("test", template);
            //
            // TODO: Add test logic here
            //
            
        }

        [TestMethod]
        public void RemoveTemplate()
        {
            _engine.Compile("test-template-to-remove", template);
            _engine.Remove("test-template-to-remove");
            Assert.IsFalse(_engine.Exists("test-template-to-remove"));
        }

        [TestMethod]
        public void GetPreCompileJS()
        {
            _engine.Compile("test-precompile", template);
            var test = _engine.ExportPrecompile();

            // note: yui compress can gain 50% before gzip, 70% after.
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void ImportPreCompileJS()
        {
            _engine.Compile("test-precompile", template);
            var js = _engine.ExportPrecompile();
            _engine.ImportPrecompile(js);

            var output = _engine.Render("test-precompile", new { title = "My New Post", body = "This is my first post!" });

            // note: yui compress can gain 50% before gzip, 70% after.
            Assert.AreEqual("<div class=\"entry\"><h1>My New Post</h1><div class=\"body\">This is my first post!</div></div>",
                            output);
        }


        [TestMethod]
        public void ExecuteCompiledTemplate()
        {            
            _engine.Compile("test-compiled-template", template);
            var output = _engine.Render("test-compiled-template", new { title = "My New Post", body = "This is my first post!" });
            Assert.AreEqual("<div class=\"entry\"><h1>My New Post</h1><div class=\"body\">This is my first post!</div></div>",
                            output);
        }

        /// <summary>
        /// Parallel execution of 100,000 requests to a template engine.
        /// </summary>
        [TestMethod]
        public void ExecuteCompiledTemplate_100000()
        {
            //
            // TODO: Add test logic here
            //

            int count = 1000;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            //Parallel.For(0, count, (i) =>
            //{
            //    context.Run("console.Print(template({title: \"My New Post\", body: \"" + i + "\"}));");
            //});
            sw.Stop();
            Console.WriteLine("{0} tps", count / sw.Elapsed.TotalSeconds);
        }



    }
}
