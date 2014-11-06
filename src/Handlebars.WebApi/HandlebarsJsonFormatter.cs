using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Handlebars.WebApi
{
    public sealed class HandlebarsJsonFormatter : BufferedMediaTypeFormatter
    {
        public HandlebarsJsonFormatter(IRequestFormatter formatter) : base()
        {
            this._formatter = formatter;
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
            this.MediaTypeMappings.Add(new QueryStringMapping("x-format", "json", new MediaTypeHeaderValue("application/json")));
        }

        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            HttpActionDescriptor actionDescriptor = request.GetActionDescriptor();
            if (actionDescriptor == null)
                return base.GetPerRequestFormatterInstance(type, request, mediaType);

            var view = actionDescriptor.ControllerDescriptor.ControllerName + "/" +
                       actionDescriptor.ActionName;

            if (actionDescriptor.ControllerDescriptor.Properties.ContainsKey("hb-prefix"))
                view = (actionDescriptor.ControllerDescriptor.Properties["hb-prefix"] as string) + "/" + view;

            if (request.Properties.ContainsKey("hb-view"))
                view = request.Properties["hb-view"] as string;

            HandlebarsJsonFormatter formatter = (HandlebarsJsonFormatter)base.GetPerRequestFormatterInstance(type, request, mediaType);
            formatter.View = view; 
            formatter.Request = request;
            return formatter;
        }

        #region // Dependency Injection // 
        private readonly IRequestFormatter _formatter;

        public string View;
        public HttpRequestMessage Request;
        #endregion

        public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
        {
            var json = _formatter.GetContext(Request, value);

            using (StreamWriter writer = new StreamWriter(writeStream))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                writer.WriteAsync(json);
            }
        }

        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {            
            return true;
        }

    }
}
