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

        public static HandlebarsJsonFormatter JsonFormatter
        {
            set { _json = value; }
            get { return _json; }
        }

        private static HandlebarsMediaTypeFormatter _formatter;
        private static HandlebarsJsonFormatter _json; 

        public string Area
        {
            get;
            set;
        }

        public void Initialize(HttpControllerSettings controllerSettings,
                               HttpControllerDescriptor controllerDescriptor)
        {
            controllerSettings.Formatters.Remove(controllerSettings.Formatters.JsonFormatter);
            controllerSettings.Formatters.Insert(0, _formatter);
            controllerSettings.Formatters.Insert(1, _json);

            if (!string.IsNullOrEmpty(Area))
                controllerDescriptor.Properties["hb-prefix"] = Area; 

            // ensure we can control the media type via a querystring (for handlebars proxy)
            // controllerSettings.Formatters
            //                   .JsonFormatter
            //                   .MediaTypeMappings
            //                   .Add(new QueryStringMapping("x-format", "json", new MediaTypeHeaderValue("application/json")));

        }
    }
}
