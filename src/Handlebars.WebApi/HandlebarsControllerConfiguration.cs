using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;


namespace Handlebars.WebApi
{
    
    public class HandlebarsFormatterAttribute : Attribute, IControllerConfiguration
    {

        public static HandlebarsMediaTypeFormatter Formatter
        {
            set { _formatter = value; }
            get { return _formatter; }
        }

        private static HandlebarsMediaTypeFormatter _formatter;

        public string Area
        {
            get;
            set;
        }

        public void Initialize(HttpControllerSettings controllerSettings,
                               HttpControllerDescriptor controllerDescriptor)
        {
            controllerSettings.Formatters.Insert(0, _formatter);

            if (!string.IsNullOrEmpty(Area))
                controllerDescriptor.Properties["hb-prefix"] = Area; 

            // ensure we can control the media type via a querystring (for handlebars proxy)
            controllerSettings.Formatters
                              .JsonFormatter
                              .MediaTypeMappings
                              .Add(new QueryStringMapping("x-format", "json", new MediaTypeHeaderValue("application/json")));

        }
    }
}
