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
            set { instance = value; }
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
