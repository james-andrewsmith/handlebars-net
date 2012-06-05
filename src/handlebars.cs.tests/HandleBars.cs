using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using handlebars.cs;

namespace handlebars.cs.tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class HandleBarsTestClass
    {

        private const string template = "<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>";

        [TestMethod]
        public void CompileTemplate()
        {

            HandleBars.Compile("test", template);
            //
            // TODO: Add test logic here
            //
            
        }

        [TestMethod]
        public void RemoveTemplate()
        {

            //
            // TODO: Add test logic here
            //

            HandleBars.Compile("test", template);
            HandleBars.Delete("test");

        }

        [TestMethod]
        public void GetPreCompileJS()
        {
            HandleBars.Compile("test", template);
            var test = HandleBars.GetPreCompileJS();

            // note: yui compress can gain 50% before gzip, 70% after.
        }


        [TestMethod]
        public void ExecuteCompiledTemplate()
        {
            //
            // TODO: Add test logic here
            //

            HandleBars.Compile("test", template);
            HandleBars.Run("test", new { title = "My New Post", body = "This is my first post!" });            

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
