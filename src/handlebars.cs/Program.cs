using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Noesis.Javascript;

namespace handlebars.cs
{
    // accounting.js
    // money.js
    // moment.js


    class Program
    {
        public class SystemConsole
        {
            public SystemConsole() { }

            public void Print(string iString)
            {
                Console.WriteLine(iString);
            }
        }

        static void Main(string[] args)
        { /*
            context.SetParameter("console", new SystemConsole());
            context.Run(System.IO.File.ReadAllText("handlebars.js\\handlebars-1.0.0.beta.6.js"));
            context.Run("var source = '<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>';");
            context.Run("var template = Handlebars.compile(source);");

            int count = 1000;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Parallel.For(0, count, (i) =>
            {
                context.Run("console.Print(template({title: \"My New Post\", body: \"" + i + "\"}));");
            });
            sw.Stop();
            Console.WriteLine("{0} tps", count / sw.Elapsed.TotalSeconds);

           
            */
            // Initialize the context
            using (JavascriptContext context = new JavascriptContext())
            {

                // Setting the externals parameters of the context
                context.SetParameter("console", new SystemConsole());
                context.SetParameter("message", "Hello World !");
                context.SetParameter("number", 1);

                var a = context.Run("'abc';");

                context.SetParameter("output", "''");
                context.Run("output = 'test';");
                var o = context.GetParameter("output");

                // Running the script
                var _assembly = Assembly.GetExecutingAssembly();
                using (var _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("handlebars.cs.handlebars.js.handlebars-1.0.0.beta.6.js")))
                {
                    context.Run(_textStreamReader.ReadToEnd());
                }
                context.Run("var i; for (i = 0; i < 5; i++) console.Print(message + ' (' + i + ')'); number += i;");

                context.Run("var source = '<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>';");
                context.Run("var template = Handlebars.compile(source);");
                context.Run("var context = {title: \"My New Post\", body: \"This is my first post!\"};");
                var test = context.Run("console.Print(template(context));");

                

                // Getting a parameter
                Console.WriteLine("number: " + context.GetParameter("number"));
            }
        }

        private static JavascriptContext context = new JavascriptContext();


    }
}
