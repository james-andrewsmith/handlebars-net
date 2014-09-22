using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Handlebars
{
    /// <summary>
    /// Will attempt to download the template from a remote URL, used for
    /// .js or .handlebars files and will provide the raw item back.
    /// </summary>
    public sealed class WebResourceProvider : IHandlebarsResourceProvider
    {
        public string GetScript(string name)
        {
            throw new NotImplementedException();            
        }

        public string GetTemplate(string name)
        {
            throw new NotImplementedException();
        }
    }
}
