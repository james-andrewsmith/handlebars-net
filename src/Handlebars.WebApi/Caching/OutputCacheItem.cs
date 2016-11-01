using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handlebars.WebApi
{
    public sealed class OutputCacheItem
    {
        public string Content
        {
            get;
            set;
        }

        public string ContentType
        {
            get;
            set;
        }

        public List<SectionData> Donuts
        {
            get;
            set;
        }

        public int StatusCode
        {
            get;
            set;
        }

        public string Template
        {
            get;
            set;
        }
    }
}
