using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handlebars
{
    public static class ConfigurationHelper
    {
        public static void GetFromFile()
        {
            var oconfig = HandlebarsConfigurationHandler.Get();
            if (oconfig == null)
                throw new Exception("Handlebars section was not found in app/web.config");

            HandlebarsConfiguration.Instance = new HandlebarsConfiguration();
            HandlebarsConfiguration.Instance.Engine = oconfig.Engine;
            HandlebarsConfiguration.Instance.BasePath = oconfig.BasePath;
            HandlebarsConfiguration.Instance.ScriptPath = oconfig.ScriptPath;
            HandlebarsConfiguration.Instance.TemplatePath = oconfig.TemplatePath;

            HandlebarsConfiguration.Instance.Include = new List<ScriptIncludeElement>();
            foreach (ScriptIncludeElement inc in oconfig.Include)
            {
                HandlebarsConfiguration.Instance.Include.Add(inc);
            }

        }
 
    }
}
