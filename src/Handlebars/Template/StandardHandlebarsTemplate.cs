using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Handlebars
{

    /// <summary>
    /// The general purpose handlebars template which combines
    /// </summary>
    public sealed class StandardHandlebarsTemplate : IHandlebarsTemplate
    {
        #region // Constructor //
        public StandardHandlebarsTemplate(IHandlebarsEngine engine,
                                          IHandlebarsResourceProvider provider)
        {
            this._engine = engine;
            this._provider = provider;
        }
        #endregion

        #region // Dependencies //
        private readonly IHandlebarsEngine _engine;
        private readonly IHandlebarsResourceProvider _provider;
        #endregion

        #region // IHandlebarsTemplate //
        public string Render(string name, string json)
        {
            if (!_engine.Exists(name))
            {
                var template = _provider.GetTemplate(name);
                if (string.IsNullOrEmpty(template))
                    return template;

                EnsurePartialTemplate(template);
                _engine.Compile(name, template);
            }
            
            return _engine.Render(name, json);            
            // File.ReadAllText(MapPath.Map("~/bin/_template/" + masterPath + ".handlebars"))
        }

        private void EnsurePartialTemplate(string template)
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

                    if (!_engine.PartialExists(name))
                    {
                        var partialTemplate = _provider.GetTemplate(name);

                        if (string.IsNullOrEmpty(partialTemplate))
                            throw new Exception("Partial " + name + " is empty, found in: \n" + template);

                        partialTemplateCheck.Add(partialTemplate);
                        _engine.PartialCompile(name, partialTemplate);
                    }

                }
            }
        }

        private readonly Regex PartialRegex = new Regex(@"({{> (.+)}})", RegexOptions.IgnoreCase);
        #endregion
    }
}
