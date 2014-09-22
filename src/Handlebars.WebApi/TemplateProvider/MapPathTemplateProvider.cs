using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Handlebars.WebApi
{
    /// <summary>
    /// Will always obtain a fresh copy of the template and compile / render
    /// for every use, good for development, bad for any situation where 
    /// performance and resource consumption are an issue.
    /// </summary>
    public sealed class MapPathTemplateProvider : IHandlebarsResourceProvider
    {
        #region // Constructor //
        public MapPathTemplateProvider(string path)
        {
            this._path = path;
        }
        #endregion

        #region // Dependency Injection //
        private readonly string _path;
        #endregion

        public string GetTemplate(string view)
        {
            return File.ReadAllText(MapPath.Map("~/bin/_template/" + view + ".handlebars"));            
        }

        public string GetScript(string script)
        {
            throw new NotImplementedException();
        }

    }
}
