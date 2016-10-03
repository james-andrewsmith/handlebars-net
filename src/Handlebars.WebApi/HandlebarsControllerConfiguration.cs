using System;
using System.Collections.Generic;
using System.Linq; 
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks; 

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;


namespace Handlebars.WebApi
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HandlebarsFormatterAttribute : Attribute, IResourceFilter
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

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            
            /*
            controllerSettings.Formatters.Remove(controllerSettings.Formatters.JsonFormatter);
            controllerSettings.Formatters.Insert(0, _formatter);
            controllerSettings.Formatters.Insert(1, _json);

            if (!string.IsNullOrEmpty(Area))
                controllerDescriptor.Properties["hb-prefix"] = Area;
            */
        }

    }
}
