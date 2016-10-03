using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Handlebars
{
    /// <summary>
    /// Will always obtain a fresh copy of the template and compile / render
    /// for every use, good for development, bad for any situation where 
    /// performance and resource consumption are an issue.
    /// </summary>
    public sealed class LocalResourceProvider : IHandlebarsResourceProvider
    {
        #region // Constructor //
        public LocalResourceProvider()
        { 
        }
        #endregion

        #region // Dependency Injection //
        #endregion

        public string GetScript(string script)
        {
            var baseUri = HandlebarsConfiguration.Instance.BasePath; // new Uri(HandlebarsConfiguration.Instance.BasePath);
            var scriptUri = HandlebarsConfiguration.Instance.ScriptPath; // new Uri(HandlebarsConfiguration.Instance.ScriptPath);

            string path = Path.Combine(baseUri.ToString(), script.TrimStart('\\'));
            if (File.Exists(path)) return File.ReadAllText(path);

            path = Path.Combine(scriptUri.ToString(), script.TrimStart('\\'));
            if (File.Exists(path)) return File.ReadAllText(path);

            path = Path.Combine(baseUri.ToString(), scriptUri.ToString(), script.TrimStart('\\'));
            if (File.Exists(path)) return File.ReadAllText(path);

            throw new FileNotFoundException(script);
        }

        public string GetTemplate(string template)
        {
            var baseUri = HandlebarsConfiguration.Instance.BasePath; // new Uri(HandlebarsConfiguration.Instance.BasePath);
            var scriptUri = HandlebarsConfiguration.Instance.TemplatePath; // new Uri(HandlebarsConfiguration.Instance.TemplatePath);

            if (!template.EndsWith(".handlebars") &&
                !template.EndsWith(".hbs") && 
                !template.EndsWith(".js"))
                template += ".handlebars";

            string path = Path.Combine(baseUri.ToString(), template.TrimStart('\\'));
            if (File.Exists(path)) return File.ReadAllText(path);

            path = Path.Combine(scriptUri.ToString(), template.TrimStart('\\'));
            if (File.Exists(path)) return File.ReadAllText(path);

            path = Path.Combine(baseUri.ToString(), scriptUri.ToString(), template.TrimStart('\\'));
            if (File.Exists(path)) return File.ReadAllText(path);

            throw new FileNotFoundException(template);
        }
    }
}
