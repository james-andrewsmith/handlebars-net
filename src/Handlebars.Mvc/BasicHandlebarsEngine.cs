using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using handlebars.cs;

namespace HandlebarsViewEngine
{
    public class UnitTestHandlebarsEngine : IDisposable
    {
        public UnitTestHandlebarsEngine()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly.GetManifestResourceNames()
                                .Where(_ => _.EndsWith(".js"))
                                .Reverse()
                                .ToList();

            foreach (var resource in names)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resource))
                using (StreamReader reader = new StreamReader(stream))
                {
                    System.Diagnostics.Debug.WriteLine("Resource: " + resource);
                    string content = reader.ReadToEnd();
                    HandleBars.Instance.AddHelpers(content);
                }
            }

        }

        public string Run(string template, string context = "{}")
        {
            // trim each line, this leads to faster template performance 
            // as handlebars compiles the spaces into the HTML otherwise
            template = string.Join("\n", template.Split('\n').Select(s => s.Trim()));
            return HandleBars.Instance.SingleRun(template, context);
        }
        
        public void Dispose()
        {
            HandleBars.Instance.Clear();
        }
    }
}
