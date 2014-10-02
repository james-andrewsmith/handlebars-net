using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Handlebars
{
    /// <summary>
    /// Used in production deployments where the templates have already been compiled
    /// into a single file and will be loaded with the engine.
    /// </summary>
    public sealed class PrecompiledHandlebarsTemplate : IHandlebarsTemplate
    {
        #region // Constructor //
        public PrecompiledHandlebarsTemplate(IHandlebarsEngine engine,
                                             IHandlebarsResourceProvider provider, 
                                             Uri uri)
        {
            this._engine = engine;
            this._provider = provider;

            // download the string
            var js = "";
            _engine.ImportPrecompile(js);
        }
        #endregion

        #region // Dependency Injection //
        private readonly Uri _uri;
        private readonly IHandlebarsEngine _engine;
        private readonly IHandlebarsResourceProvider _provider;
        #endregion

        #region // IHandlebarsTemplate //

        public string Render(string name, string json)
        {
            return _engine.Render(name, json);
        }

        #endregion

    }
}
