using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;

using System.IO;

namespace Handlebars 
{
    public sealed class Precompiler
    {
        private IHandlebarsEngine _handlebars;
        private List<string> _templates;
        private readonly Regex PartialRegex = new Regex(@"({{> (.+)}})", RegexOptions.Compiled);
        private string _root;

        public Precompiler(string root)
        {
            _root = root;
            _handlebars = HandlebarsFactory.CreateEngine();
            _templates = System.IO.Directory.EnumerateFiles(_root,
                                                            "*.handlebars",
                                                            SearchOption.AllDirectories).ToList();            

            // var assembly = Assembly.GetExecutingAssembly();
            // var names = assembly.GetManifestResourceNames()
            //                     .Where(_ => _.EndsWith(".js"))
            //                     .ToList();
            // 
            // foreach (var resource in names)
            // {
            //     using (Stream stream = assembly.GetManifestResourceStream(resource))
            //     using (StreamReader reader = new StreamReader(stream))
            //     {
            //         string content = reader.ReadToEnd();
            //         _handlebars.AddHelpers(content);
            //     }
            // }
        }

        public void Run(string export = "all.js", bool debug = false)
        {
            foreach (var path in _templates)
            {
                var name = path.Replace(_root, "")
                               .Replace("_template\\", "")
                               .Replace("template\\", "")
                               .Replace(".handlebars", "")
                               .Replace("\\", "/")
                               .Trim('/')
                               .ToLower();
                
                Console.WriteLine("Compiling template \"" + name + "\"");

                var template = CompactTemplate(File.ReadAllText(path, System.Text.Encoding.UTF8));

                EnsurePartial(template);

                _handlebars.Compile(name, template);                      
                if (debug) _handlebars.ExportPrecompile();
            }
            
            File.WriteAllText(export,
                              _handlebars.ExportPrecompile(),
                              System.Text.Encoding.UTF8);

            _handlebars.Clear();     
        }

        private string CompactTemplate(string data)
        {
            var lines = data.Split('\n');
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var nl = line.Trim(' ', '\n', '\r', '\t')
                            .Replace("\t", " ")
                            .Replace(GetSpace(20), " ")
                            .Replace(GetSpace(19), " ")
                            .Replace(GetSpace(18), " ")
                            .Replace(GetSpace(17), " ")
                            .Replace(GetSpace(16), " ")
                            .Replace(GetSpace(15), " ")
                            .Replace(GetSpace(14), " ")
                            .Replace(GetSpace(13), " ")
                            .Replace(GetSpace(12), " ")
                            .Replace(GetSpace(11), " ")
                            .Replace(GetSpace(10), " ")
                            .Replace(GetSpace(9), " ")
                            .Replace(GetSpace(8), " ")
                            .Replace(GetSpace(7), " ")
                            .Replace(GetSpace(6), " ")
                            .Replace(GetSpace(5), " ")
                            .Replace(GetSpace(4), " ")
                            .Replace(GetSpace(3), " ")
                            .Replace(GetSpace(2), " ");

                if (!string.IsNullOrEmpty(nl))
                    sb.Append(nl + "\n");
            }
            return sb.ToString()
                     .Replace("}}\n", "}}")
                     .Replace("{{>", "\n{{>")
                     .Replace("{{/section}}{{#section", 
                              "{{/section}}\n{{#section");
        }

        private string GetSpace(int length = 1)
        {
            var eb = "";
            for (var i = 0; i < length; i++)
                eb += " ";
            return eb;
        }


        private void EnsurePartial(string template)
        {
            List<string> partialTemplateCheck = new List<string>();
            partialTemplateCheck.Add(template);
            for (int i = 0; i < partialTemplateCheck.Count; i++)
            {
                var partials = PartialRegex.Matches(partialTemplateCheck[i]);
                foreach (Match partial in partials)
                {
                    var value = partial.Value;

                    var name = value.Substring(value.IndexOf("{{> ") + 4, value.IndexOf("}}") - value.IndexOf("{{> ") - 4);
                    if (name.IndexOf(' ') > -1)
                    {
                        name = name.Substring(0, name.IndexOf(' '));
                    }
 
                    if (!_handlebars.PartialExists(name))
                    {
                        Console.WriteLine("Compiling parital \"" + name + "\""); 
                        var path = System.IO.Path.Combine(_root, name + ".handlebars");
                        var partial_template = CompactTemplate(File.ReadAllText(path, System.Text.Encoding.UTF8));
                        _handlebars.PartialCompile(name, partial_template);
                    }
                }
            }
        }


    }
}
