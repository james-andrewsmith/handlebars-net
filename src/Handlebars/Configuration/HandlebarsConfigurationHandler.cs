using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Handlebars
{
    internal class HandlebarsConfigurationHandler : ConfigurationSection
    {
        public static HandlebarsConfigurationHandler Get()
        {
            return (HandlebarsConfigurationHandler)ConfigurationManager.GetSection("handlebars/settings");
        }

        [ConfigurationProperty("engine", IsRequired = true)]
        public string Engine
        {
            get
            {
                return (string)this["engine"];
            }
            set
            {
                this["engine"] = value;
            }
        }

        [ConfigurationProperty("basePath", IsRequired = false)]
        public string BasePath
        {
            get
            {
                return (string)this["basePath"];
            }
            set
            {
                this["basePath"] = value;
            }
        }

        [ConfigurationProperty("scriptPath", IsRequired = false)]
        public string ScriptPath
        {
            get
            {
                return (string)this["scriptPath"];
            }
            set
            {
                this["scriptPath"] = value;
            }
        }

        [ConfigurationProperty("templatePath", IsRequired = false)]
        public string TemplatePath
        {
            get
            {
                return (string)this["templatePath"];
            }
            set
            {
                this["templatePath"] = value;
            }
        }

        [ConfigurationProperty("include", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ScriptIncludeCollection),
                                 AddItemName = "add")]
        public ScriptIncludeCollection Include
        {
            get
            {
                return (ScriptIncludeCollection)base["include"];
            }
        }

    }
}
