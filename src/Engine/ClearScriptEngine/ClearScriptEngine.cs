using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Handlebars
{

    public sealed class ClearScriptBridgeFunctions
    {
        private readonly ScriptEngine _engine;
        private readonly IHandlebarsResourceProvider _resourceProvider;

        public ClearScriptBridgeFunctions(ScriptEngine engine,
                                          IHandlebarsResourceProvider resourceProvider)
        {
            _engine = engine;
            _resourceProvider = resourceProvider;            
        }

        public void require(string url)
        {
            var resource = _resourceProvider.GetScript(url);            
            _engine.Execute(resource);                         
        }
    }

    public sealed class ClearScriptEngine : IHandlebarsEngine
    {
        #region // Constructor //
        public ClearScriptEngine(IHandlebarsResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;

            _context = new V8ScriptEngine();
            _context.AddHostObject("clearScriptBridge", 
                                   HostItemFlags.GlobalMembers,
                                   new ClearScriptBridgeFunctions(_context, _resourceProvider));

            // _context.Script.moduleLoader = new ModuleLoader(_context);
            // _context.Execute(File.ReadAllText("require.js"));
            // _context.Execute(@"require.load = function(context, name, url) { moduleLoader.LoadModuleAsync(context, name, url); };");
                
            _context.Execute("var raw = [];");
            _context.Execute("require('./Script/handlebars-1.0.0.js');");
            foreach (var script in HandlebarsConfiguration.Instance.Include)
            {
                _context.Execute("require('./" + script.Source + "');"); 
            }

        }
        #endregion

        #region // Properties //
        private readonly IHandlebarsResourceProvider _resourceProvider;
        private readonly V8ScriptEngine _context;
        #endregion
    
        public void Clear()
        {
            _context.Execute("Handlebars.templates = []; " + 
                                    "Handlebars.partials = []; ");
            
            // a good opportunity to return ram to it's rightful owner
            _context.CollectGarbage(true);
        }

        public void Compile(string name, string template)
        {
            template = HandlebarsUtilities.ToJavaScriptString(template);
            _context.Execute("raw['" + name + "'] = '" + template + "';\n" + 
                             "Handlebars.templates['" + name + "'] = Handlebars.compile('" + template + "');");
        }

        public bool Exists(string name)
        {
            return (bool)_context.Evaluate("typeof Handlebars.templates['" + name + "'] == 'function';");
        }

        public string ExportPrecompile()
        {
            var exportJS = @"(function() {
                                 var response = '';
                                 var keys = Object.keys(Handlebars.templates);
                                 for (var i = 0; i < keys.length; i++) {
                                     var pre = Handlebars.precompile(raw[keys[i]]);
                                     response += 'Handlebars.templates[\'' + keys[i] + '\'] = Handlebars.template(' + pre.toString() + ');';
                                 }
                                 return response;
                             })()";

            return (string)_context.Evaluate(exportJS);
        }

        public void ImportPrecompile(string js)
        {
            _context.Execute(js);
        }

        public void PartialCompile(string name, string template)
        {
            template = HandlebarsUtilities.ToJavaScriptString(template);
            _context.Execute("raw['" + name + "'] = '" + template + "';\n" +
                             "Handlebars.registerPartial('" + name + "','" + template + "');");                        
        }

        public bool PartialExists(string name)
        {
            return (bool)_context.Evaluate("typeof Handlebars.partials['" + name + "'] == 'function';");
        }

        public string Render(string name, object context)
        {
            var json = context is string ? (string)context : HandlebarsUtilities.ToJson(context);
            return (string)_context.Evaluate("Handlebars.templates['" + name + "'](" + json + ");");
        }

        public string Render(string name, string template, string json)
        {
            if (string.IsNullOrEmpty(json)) json = "{}";            
            return (string)_context.Evaluate("Handlebars.compile('" +  HandlebarsUtilities.ToJavaScriptString(template) + "')(" + json + ");");             
        }

        public string Render(string name, string template, object context)
        {
            string json = context is string ? (string)context : HandlebarsUtilities.ToJson(context);
            return Render(name, template, json);
        }

        public void Remove(string name)
        {
            _context.Execute("delete Handlebars.templates['" + name + "'];");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
