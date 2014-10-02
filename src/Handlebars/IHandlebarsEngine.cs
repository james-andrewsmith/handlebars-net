using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Handlebars
{
    public interface IHandlebarsEngine : IDisposable
    {
        void Clear();        
        void Compile(string name, string template);
        bool Exists(string name);
        string ExportPrecompile();
        void ImportPrecompile(string javascript);
        void PartialCompile(string name, string template);
        bool PartialExists(string name);
        string Render(string name, object context);
        string Render(string name, string template, string json);
        string Render(string name, string template, object context);
        void Remove(string name);        
    }
}
