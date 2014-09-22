using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Handlebars
{
    public interface IHandlebarsResourceProvider
    { 
        /// <summary>
        /// Get an individual template
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string GetTemplate(string name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string GetScript(string name);
    }
}
