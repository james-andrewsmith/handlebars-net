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
        static HandleBars()
        {
            _context = new JavascriptContext();            
            var _assembly = Assembly.GetExecutingAssembly();
            using (var _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("handlebars.cs.handlebars.js.handlebars-1.0.0.beta.6.js")))
                _context.Run(_textStreamReader.ReadToEnd());
            
            // ensure there is a 'templates' property
            _context.Run("Handlebars.templates = Handlebars.templates || {};");
            
            // setup an object which contains the compiled templates as properties.
            // _context.Run("var templates = {};");
            _templates = new Dictionary<string, string>();
            _partials = new Dictionary<string, string>();
        }

        private static JavascriptContext _context;
        private static Dictionary<string, string> _templates;
        private static Dictionary<string, string> _partials;        

        /// <summary>
        /// Used to add application specific registered helpers for the templates, for example
        /// some templates might use a custom 'product_list' helper.
        /// </summary>
        /// <param name="js"></param>
        public static void AddHelpers(string js)
        {
            _context.Run(js);
        }

        /// <summary>
        /// Templates to pre-compile and attach to a variable in the JavaScript 
        /// engine.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="template"></param>
        public static void Compile(string name, string template)
        {
            if (_templates.ContainsKey(name))
                throw new Exception("There is already a template with that name which has been compiled.");

            // note: this is not doing anything to check if the template is properly escaped.            
            _context.Run("Handlebars.templates['" + name + "'] = Handlebars.compile('" + FormatTemplate(template) + "');");
            _templates.Add(name, FormatTemplate(template));
        }

        private static string FormatTemplate(string template)
        {
            return HttpUtility.JavaScriptStringEncode(template);

            //return template.Replace("\r\n", " ")
            //               .Replace("\r", " ")
            //               .Replace("\t", " ")
            //               .Replace("  ", " ")
            //               .Replace("\n", " ");
        }

        public static void Delete(string name)
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
        public static string GetPreCompileJS()
        {
            var sb = new StringBuilder();
            sb.Append("var Handlebars = Handlebars || {};\n");
            sb.Append("Handlebars.template = Handlebars.template || {};\n");
            foreach(var kvp in _templates)
                sb.AppendLine("Handlebars.template['" + kvp.Key + "'] = Handlebars.template(" + (string)_context.Run("Handlebars.precompile('" + kvp.Value + "');") + ");");
            
            return sb.ToString();
        }

        public static void Clear()
        {
            foreach (var kvp in _templates)
                _context.Run("delete Handlebars.template['" + kvp.Key + "'];");

            _templates.Clear();
        }

        public static string SingleRun(string template, dynamic context)
        {
            return SingleRun(template, JsonConvert.SerializeObject(context));        
        }
        /// <summary>
        /// Here we presume the template has already been compiled and doesn't need to be passed again.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string SingleRun(string template, string context)
        { 
            return (string)_context.Run("Handlebars.compile('" + FormatTemplate(template) + "')(" + context + ");");             
        }
         
        // Engine.Run("product-form", System.IO.ReadToEnd("_template/.handlebars"), JsonHelper.ToJson(new { title = "My New Post", body = "This is my first post!" }));

        public static bool Exists(string name)
        {           
            return _templates.ContainsKey(name);
        }

        public static bool PartialExists(string name)
        {
            return _partials.ContainsKey(name);
        }

        public static string Run(string name, dynamic context)
        {
            return Run(name, JsonConvert.SerializeObject(context));
        }

        public static string Run(string name, string context)
        {
            if (!_templates.ContainsKey(name))
                throw new Exception("Could not find template \"" + name + "\"");

            return (string)_context.Run("Handlebars.templates['" + name + "'](" + context + ");");    
        }

        public static string Run(string name, string template, dynamic context)
        {
            return Run(name, template, JsonConvert.SerializeObject(context));
        }

        public static string Run(string name, string template, string context)
        {
            if (!_templates.ContainsKey(name))
                Compile(name, template);

            return (string)_context.Run("Handlebars.templates['" + name + "'](" + context + ");");            
        }
         
        public static void Partial(string name, string template)
        {
            if (_partials.ContainsKey(name))
                return;

            _context.Run("Handlebars.registerPartial('" + name + "', '" + FormatTemplate(template) + "');");
        }

    }
}
