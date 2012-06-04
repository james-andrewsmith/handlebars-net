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
        [TestMethod]
        public void CompileTemplate()
        {

            HandleBars.Compile("test", "<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>");
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

            HandleBars.Compile("test", "<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>");
            HandleBars.Delete("test");

        }

        [TestMethod]
        public void GetPreCompileJS()
        {
            HandleBars.Compile("test", "<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>");
            var test = HandleBars.GetPreCompileJS();

            // note: yui compress can gain 50% before gzip, 70% after.
        }


        [TestMethod]
        public void ExecuteCompiledTemplate()
        {
            //
            // TODO: Add test logic here
            //

            HandleBars.Compile("test", "<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>");
            // HandleBars.Run("test", new { title = "My New Post", body = "This is my first post!" });            

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

        }



    }
}
