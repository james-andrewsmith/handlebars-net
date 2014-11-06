using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
            var js = new WebClient().DownloadString(uri);
            _engine.ImportPrecompile(js);
        }
        #endregion

        #region // Dependency Injection //
        private readonly IHandlebarsEngine _engine;
        private readonly IHandlebarsResourceProvider _provider;
        #endregion

        #region // IHandlebarsTemplate //

        public string Render(string name, string json)
        {
            try
            {
                return _engine.Render(name.ToLower(), json);
            }
            catch (Exception exp)
            {
                return @"<html>
                         <head></head>
                         <body>
                         <h4>" + exp.Message + @"</h4>
                         <pre>" + exp.StackTrace + @"</pre>
                         </body>
                         </html>";                        
            }
        }

        #endregion

    }
}
