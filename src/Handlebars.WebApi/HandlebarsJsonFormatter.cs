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
    public sealed class HandlebarsJsonFormatter : MediaTypeFormatter
    {
        public HandlebarsJsonFormatter(IRequestFormatter formatter) : base()
        {
            this._formatter = formatter;
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
            this.MediaTypeMappings.Add(new QueryStringMapping("x-format", "json", new MediaTypeHeaderValue("application/json")));
        }

        public HandlebarsJsonFormatter(IRequestFormatter formatter, string view, HttpRequestMessage request) : this(formatter)
        {
            this._view = view;
            this._request = request;
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

            return new HandlebarsJsonFormatter(_formatter, view, request);
        }

        #region // Dependency Injection // 
        private readonly IRequestFormatter _formatter;
        private readonly string _view;
        private readonly HttpRequestMessage _request;
        #endregion

        public override async Task WriteToStreamAsync(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken)
        {            
            var json = _formatter.GetContext(_request, value);
            var writer = new StreamWriter(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Headers.TryAddWithoutValidation("x-template", _view);
            await writer.WriteAsync(json);
            await writer.FlushAsync();
            return;
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
