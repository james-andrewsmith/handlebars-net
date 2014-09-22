using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;


namespace Handlebars
{

    public class ScriptIncludeCollection : ConfigurationElementCollection
    {
        public ScriptIncludeCollection()
        {
        }

        public ScriptIncludeElement this[int index]
        {
            get { return (ScriptIncludeElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(ScriptIncludeElement serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ScriptIncludeElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ScriptIncludeElement)element).Source;
        }

        public void Remove(ScriptIncludeElement serviceConfig)
        {
            BaseRemove(serviceConfig.Source);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }
}
