using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Handlebars.WebApi
{
    public static class RequestExtension
    {
        public static HttpResponseMessage CreateRedirectResponse(this HttpRequestMessage request, Uri uri, bool permanent = false)
        {
            var response = request.CreateResponse(permanent ? HttpStatusCode.MovedPermanently : HttpStatusCode.Redirect, "<html></html>");
            response.Headers.Location = uri;

            var sb = new StringBuilder();
            sb.Append("<html><head><title>Moved</title></head><body><h1>Moved</h1><p>This page has moved to ");
            sb.Append("<a href=\"");
            sb.Append(uri);
            sb.Append("\">");
            sb.Append(uri);
            sb.Append("</a>.</p></body></html>");

            response.Content = new StringContent(sb.ToString(), Encoding.UTF8, "text/html");


            return response;
        }

        public static HttpResponseMessage CreateRedirectResponse(this HttpRequestMessage request, string uri, bool permanent = false)
        {
            var builder = new UriBuilder(request.RequestUri);
            // ensure we don't pass any variables along that aren't wanted...
            builder.Query = string.Empty;                        
            builder.Path = uri;
            
            var response = request.CreateResponse(permanent ? HttpStatusCode.MovedPermanently : HttpStatusCode.Redirect, "<html></html>");
            response.Headers.Location = builder.Uri;

            var sb = new StringBuilder();
            sb.Append("<html><head><title>Moved</title></head><body><h1>Moved</h1><p>This page has moved to ");
            sb.Append("<a href=\"");
            sb.Append(builder.Uri);
            sb.Append("\">");
            sb.Append(builder.Uri);
            sb.Append("</a>.</p></body></html>");

            response.Content = new StringContent(sb.ToString(), Encoding.UTF8, "text/html");

            return response;
        }
    }
}
