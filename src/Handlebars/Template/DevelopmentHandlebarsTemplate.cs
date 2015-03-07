using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Handlebars
{
    public class DevelopmentHandlebarsTemplate : IHandlebarsTemplate
    {
        #region // Constructor //
        public DevelopmentHandlebarsTemplate(IHandlebarsEngine engine,
                                             IHandlebarsResourceProvider provider)
        {
            this._engine = engine;
            this._provider = provider;
        }
        #endregion

        #region // Dependencies //
        private readonly static object _sync = new object();
        private readonly IHandlebarsEngine _engine;
        private readonly IHandlebarsResourceProvider _provider;
        #endregion

        #region // IHandlebarsTemplate //
        public string Render(string name, string json)
        {
            try
            {
                lock (_sync)
                {
                    var template = _provider.GetTemplate(name);
                    EnsurePartialTemplate(template);
                    var html = _engine.Render(name, template, json);
                    _engine.Clear();
                    return html;
                }
            }
            catch (Exception exp)
            {
                return @"<h4>" + exp.Message + @"</h4>
                         <pre>" + exp.StackTrace + @"</pre>";                        
            }
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
