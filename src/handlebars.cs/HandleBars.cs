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

    public sealed class HandleBars
    {
        private static readonly HandleBars instance = new HandleBars();

        public static HandleBars Instance
        {
            get 
            {
                return instance; 
            }
        } 

        public HandleBars()
        {

            _context = new HandleBarsClearScript();            
            var _assembly = Assembly.GetExecutingAssembly();
            using (var _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("handlebars.cs.handlebars.js.handlebars-1.0.0.js")))
                _context.Run(_textStreamReader.ReadToEnd());
            
            // ensure there is a 'templates' property
            _context.Run("Handlebars.templates = Handlebars.templates || {};");
            _context.Run("Handlebars.partials = Handlebars.partials || {};");
            
            // setup an object which contains the compiled templates as properties.
            // _context.Run("var templates = {};");
            _templates = new Dictionary<string, string>();
            _partials = new Dictionary<string, string>();
        }

        private HandleBarsClearScript _context;
        private Dictionary<string, string> _templates;
        private Dictionary<string, string> _partials;
        private readonly object _contextLock = new object();

        public List<string> Templates()
        {
            return _templates.Keys.ToList();
        }

        /// <summary>
        /// Used to add application specific registered helpers for the templates, for example
        /// some templates might use a custom 'product_list' helper.
        /// </summary>
        /// <param name="js"></param>
        public void AddHelpers(string js)
        {
            _context.Run(js);
        }

        /// <summary>
        /// Templates to pre-compile and attach to a variable in the JavaScript 
        /// engine.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="template"></param>
        public bool Compile(string name, string template)
        {
            lock (_contextLock)
            {
                if (_templates.ContainsKey(name))
                    return false;
                    // throw new Exception("There is already a template with that name which has been compiled.");

                // note: this is not doing anything to check if the template is properly escaped.            
                _context.Run(string.Concat("Handlebars.templates['", 
                                           name,
                                           "'] = Handlebars.compile('",
                                           FormatTemplate(template),
                                           "');"));

                _templates.Add(name, FormatTemplate(template));
            }
            return true;
        }

        private string FormatTemplate(string template)
        {
            return HttpUtility.JavaScriptStringEncode(template);

            //return template.Replace("\r\n", " ")
            //               .Replace("\r", " ")
            //               .Replace("\t", " ")
            //               .Replace("  ", " ")
            //               .Replace("\n", " ");
        }

        public void Delete(string name)
        {
            _context.Run("delete Handlebars.templates['" + name + "'];");
            _templates.Remove(name);
        }

        /// <summary>
        /// Return all the precompiled templates as a single JavaScript object
        /// with each template as a property, this means we don't need the 
        /// front end to download all the templates (which are larger) and the 
        /// code to compile the templates (again faster). 
        /// </summary>
        /// <returns></returns>
        public string GetPreCompileJS()
        {
            var sb = new StringBuilder();
            sb.Append("var Handlebars = Handlebars || {};\n");
            sb.Append("Handlebars.templates = Handlebars.templates || {};\n");
            sb.Append("Handlebars.partials = Handlebars.partials || {};\n");

            foreach(var kvp in _partials)
                sb.AppendLine("Handlebars.partials['" + kvp.Key + "'] = Handlebars.template(" + (string)_context.Evaluate("Handlebars.precompile('" + kvp.Value + "');") + ");");

            foreach(var kvp in _templates)
                sb.AppendLine("Handlebars.templates['" + kvp.Key + "'] = Handlebars.template(" + (string)_context.Evaluate("Handlebars.precompile('" + kvp.Value + "');") + ");");
            
            return sb.ToString();
        }

        public void Clear()
        {
            foreach (var kvp in _templates)
                _context.Run("delete Handlebars.templates['" + kvp.Key + "'];");

            foreach (var kvp in _partials)
                _context.Run("delete Handlebars.partials['" + kvp.Key + "'];");

            _partials.Clear();
            _templates.Clear();
        }

        public string SingleRun(string template, dynamic context)
        {
            return SingleRun(template, JsonConvert.SerializeObject(context));        
        }
        /// <summary>
        /// Here we presume the template has already been compiled and doesn't need to be passed again.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string SingleRun(string template, string context)
        { 
            return (string)_context.Evaluate(string.Concat("Handlebars.compile('", FormatTemplate(template), "')(", context, ");"));             
        }
         
        // Engine.Run("product-form", System.IO.ReadToEnd("_template/.handlebars"), JsonHelper.ToJson(new { title = "My New Post", body = "This is my first post!" }));

        public bool Exists(string name)
        {           
            return _templates.ContainsKey(name);
        }

        public bool PartialExists(string name)
        {
            return _partials.ContainsKey(name);
        }

        public string Run(string name, dynamic context)
        {
            return Run(name, JsonConvert.SerializeObject(context));
        }

        public string Run(string name, string context)
        {
            if (!_templates.ContainsKey(name))
                throw new Exception("Could not find template \"" + name + "\"");

            return (string)_context.Evaluate("Handlebars.templates['" + name + "'](" + context + ");");    
        }

        public string Run(string name, string template, dynamic context)
        {
            return Run(name, template, JsonConvert.SerializeObject(context));
        }

        public string Run(string name, string template, string context)
        {
            if (!_templates.ContainsKey(name))
                Compile(name, template);

            return (string)_context.Evaluate("Handlebars.templates['" + name + "'](" + context + ");");            
        }
         
        public void Partial(string name, string template)
        {
            lock (_contextLock)
            {
                if (_partials.ContainsKey(name))
                    return;

                _context.Run("Handlebars.registerPartial('" + name + "', '" + FormatTemplate(template) + "');");
                _partials.Add(name, FormatTemplate(template));
            }
        }

    }
}
