using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net; 
using System.Text;
using System.Threading;
using System.Threading.Tasks; 

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Handlebars.WebApi
{
    public sealed class HandlebarsJsonFormatter : IOutputFormatter
    {
        public HandlebarsJsonFormatter(IRequestFormatter formatter) : base()
        {
            this._formatter = formatter;  
        }
         
        #region // Dependency Injection // 
        private readonly IRequestFormatter _formatter;
        #endregion

        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return true;            
        }
         
        public async Task WriteAsync(OutputFormatterWriteContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));


            // todo: optimise
            var view = "";
            view += context.HttpContext.Items["controller"];
            view += "/";
            view += context.HttpContext.Items["action"];
            

            // if (actionDescriptor.ControllerDescriptor.Properties.ContainsKey("hb-prefix"))
            //     view = (actionDescriptor.ControllerDescriptor.Properties["hb-prefix"] as string) + "/" + view;

            // context.ObjectType.
            if (context.HttpContext.Items.ContainsKey("hb-view"))
                view = context.HttpContext.Items["hb-view"] as string;
                
            var response = context.HttpContext.Response;
            response.ContentType = "application/json";
            response.Headers["x-template"] = "TODO";

            var json = _formatter.GetContext(context.HttpContext.Request, context.Object);

            using (var writer = context.WriterFactory(response.Body, Encoding.UTF8))
            {
                await writer.WriteAsync(json);
                await writer.FlushAsync();
            }
        }
    }

} 