using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;

using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace handlebars.cs
{    

    public sealed class HandleBarsClearScript
    {        
        public HandleBarsClearScript()
        {
            _context = new V8ScriptEngine(V8ScriptEngineFlags.DisableGlobalMembers);
            var _assembly = Assembly.GetExecutingAssembly();
            using (var _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("handlebars.cs.handlebars.js.handlebars-1.0.0.js")))
                _context.Execute(_textStreamReader.ReadToEnd());                        
        }

        private V8ScriptEngine _context; 
         
        /// <summary>
        /// Used to add application specific registered helpers for the templates, for example
        /// some templates might use a custom 'product_list' helper.
        /// </summary>
        /// <param name="js"></param>
        public void Run(string js)
        {
            _context.Execute(js);
        }
          
        public string Run(string name, dynamic context)
        {
            return Run(name, JsonConvert.SerializeObject(context));
        }

        public string Run(string name, string context)
        {
            return (string)_context.Evaluate("Handlebars.templates['" + name + "'](" + context + ");");    
        }

        public string Run(string name, string template, dynamic context)
        {
            return Run(name, template, JsonConvert.SerializeObject(context));
        }

        public string Evaluate(string code)
        {
            return (string)_context.Evaluate(code);
        }

        public string Run(string name, string template, string context)
        {           
            return (string)_context.Evaluate("Handlebars.templates['" + name + "'](" + context + ");");            
        }          

    }
}
