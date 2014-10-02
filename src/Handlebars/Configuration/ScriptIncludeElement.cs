using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Handlebars
{
    public class ScriptIncludeElement : ConfigurationElement
    {
        public ScriptIncludeElement() { }

        public ScriptIncludeElement(string name, string source)
        {
            Name = name;
            Source = source;
        }

        [ConfigurationProperty("src", IsRequired = true, IsKey = true)]
        public string Source
        {
            get { return (string)this["src"]; }
            set { this["src"] = value; }
        }

        [ConfigurationProperty("name", IsRequired = false, IsKey = false)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }
    }

}
