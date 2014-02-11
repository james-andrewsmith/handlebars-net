using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
 
using Noesis.Javascript;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace handlebars.cs
{    

    public sealed class HandleBarsLite
    {        
        public HandleBarsLite()
        {
            lock(_sync)
                _context = new JavascriptContext();            
            var _assembly = Assembly.GetExecutingAssembly();
            using (var _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("handlebars.cs.handlebars.js.handlebars-1.0.0.js")))
                lock(_sync)
                    _context.Run(_textStreamReader.ReadToEnd());                        
        }

        private JavascriptContext _context; 
        private readonly object _sync = new object();
         
        /// <summary>
        /// Used to add application specific registered helpers for the templates, for example
        /// some templates might use a custom 'product_list' helper.
        /// </summary>
        /// <param name="js"></param>
        public void Run(string js)
        {
            lock(_sync)
                _context.Run(js);
        }
          
        public string Run(string name, dynamic context)
        {
            return Run(name, JsonConvert.SerializeObject(context));
        }

        public string Run(string name, string context)
        {          
            lock(_sync)
                return (string)_context.Run("Handlebars.templates['" + name + "'](" + context + ");");    
        }

        public string Run(string name, string template, dynamic context)
        {
            return Run(name, template, JsonConvert.SerializeObject(context));
        }

        public string Run(string name, string template, string context)
        {           
            lock(_sync)
                return (string)_context.Run("Handlebars.templates['" + name + "'](" + context + ");");            
        }          

    }
}
