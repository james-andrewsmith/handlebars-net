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
    public class HandlebarsFormatterAttribute : Attribute, IAsyncResourceFilter
    {
         
        public string Area
        {
            get;
            set;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            // Ensure the right formatter runs for the cache with any additional options needed 
            if (context.ActionDescriptor is ControllerActionDescriptor)
            {
                var actionDescriptor = ((ControllerActionDescriptor)context.ActionDescriptor);
                if (actionDescriptor.MethodInfo.ReturnType != typeof(Task))
                {                    
                    context.HttpContext.Items["formatter"] = context.HttpContext.Request.Query.ContainsKey("x-format") ? context.HttpContext.Request.Query["x-format"].ToString()
                                                                                                                        : "html";
                    // if there is an area add that too
                    if (!string.IsNullOrEmpty(Area))
                        context.HttpContext.Items["hb-area"] = Area;                    
                }
            }

            await next();

        }
    }
}
