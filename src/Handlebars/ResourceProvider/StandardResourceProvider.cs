using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Handlebars
{
    /// <summary>
    /// A general purpose template provider which combines the standard providers
    /// and uses them as approciate based on the app/web.config and/or the URI
    /// provided, and/or the suffix (eg: .js vs .handlebars)
    /// </summary>
    public sealed class StandardResourceProvider : IHandlebarsResourceProvider
    {
        #region // Constructor //
        public StandardResourceProvider()
        {
            _local = new LocalResourceProvider();
            _web = new WebResourceProvider();
        }
        #endregion

        #region // Dependency Injection //
        private readonly LocalResourceProvider _local;
        private readonly WebResourceProvider _web;
        #endregion

        #region // IHandlebarsTemplateProvider //
        public string GetScript(string name)
        {
            // if this an absolute URI?


            if (name.StartsWith("http"))
                return _web.GetScript(name);

            return _local.GetScript(name);
        }

        public string GetTemplate(string name)
        {
            if (name.StartsWith("http"))
                return _web.GetTemplate(name);

            return _local.GetTemplate(name);
        }
        #endregion

    }
}
