using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandlebarsViewEngine
{
    /// <summary>
    /// Responsible for getting templates from external data sources
    /// like the local drive, blob storage, git, whatever. Does not 
    /// manage the template once it has been retrieved, that is the 
    /// role of the ViewManager.
    /// </summary>
    public interface ITemplateProvider
    {
        /// <summary>
        /// List all the templates available
        /// </summary>
        List<string> List();

        /// <summary>
        /// List all the templates with this prefix
        /// </summary>
        List<string> List(string prefix);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        string Get(string view);


        string Get(string version, string view);        

    }
}
