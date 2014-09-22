using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Handlebars
{
    public sealed class HandlebarsConfiguration
    {
        
        private static HandlebarsConfiguration instance;
        public static HandlebarsConfiguration Instance
        {
            get { return instance; }
        }

        static HandlebarsConfiguration()
        {
            instance = new HandlebarsConfiguration();
            instance.GetFromFile();
        }

        private void GetFromFile()
        {

            var oconfig = HandlebarsConfigurationHandler.Get();
            if (oconfig == null)
                throw new Exception("Handlebars section was not found in app/web.config");

            instance.Engine = oconfig.Engine;
            instance.BasePath = oconfig.BasePath;
            instance.ScriptPath = oconfig.ScriptPath;
            instance.TemplatePath = oconfig.TemplatePath;

            instance.Include = new List<ScriptIncludeElement>();
            foreach (ScriptIncludeElement inc in oconfig.Include)
            {
                instance.Include.Add(inc);
            }
        }

        #region // Constructor //
        public HandlebarsConfiguration()
        {
        }
        #endregion

        #region // Properties //
        public string Engine
        {
            get;
            set;
        }
        
        /// <summary>
        /// If all resources are available from the same path
        /// </summary>
        public string BasePath
        {
            get;
            set;
        }

        /// <summary>
        /// If scripts are not prefixed, or stored in an completely 
        /// different path.
        /// </summary>
        public string ScriptPath
        {
            get;
            set;
        }

        /// <summary>
        /// If templates are not prefixed and are stored in a completely
        /// different path
        /// </summary>
        public string TemplatePath
        {
            get;
            set;
        }

        public List<ScriptIncludeElement> Include
        {
            get;
            set;
        }
        #endregion
    }

    
}
