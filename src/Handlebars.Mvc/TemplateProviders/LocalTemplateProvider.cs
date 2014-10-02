using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;


namespace HandlebarsViewEngine.TemplateProviders
{
    public class LocalTemplateProvider : ITemplateProvider
    {
        public LocalTemplateProvider(string path)
        {
            _path = path;
        }

        private string _path;

        /// <summary>
        /// Should handlebars cache the template
        /// </summary>
        public bool Cache
        {
            get
            {
                return false;
            }
        }

        public void RemoveWithPrefix(string prefix)
        {
            throw new NotImplementedException();
        }

        public string GetTemplate(string view)
        {
            var path = HttpContext.Current.Server.MapPath(view);
            return System.IO.File.ReadAllText(path);
        }

        public List<string> List()
        {
            throw new NotImplementedException();
        }

        public List<string> List(string prefix)
        {
            throw new NotImplementedException();
        }

        public string Get(string view)
        {            
            var path = System.IO.Path.Combine(_path, view.TrimStart('\\'));
            return System.IO.File.ReadAllText(path);
        }

        public string Get(string version, string view)
        {
            return Get(view);
        }
    }
}
