using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Handlebars.Net.Http
{
    public static class RequestExtension
    {
        public static HttpResponseMessage CreateRedirectResponse(this HttpRequestMessage request, Uri uri)
        {
            var response = request.CreateResponse(HttpStatusCode.Redirect);
            response.Headers.Location = uri;
            return response;
        }

        public static HttpResponseMessage CreateRedirectResponse(this HttpRequestMessage request, string uri)
        {
            var builder = new UriBuilder(request.RequestUri);
            builder.Path = uri;

            var response = request.CreateResponse(HttpStatusCode.Redirect, "<html></html>");
            response.Headers.Location = builder.Uri;
            return response;
        }
    }
}
