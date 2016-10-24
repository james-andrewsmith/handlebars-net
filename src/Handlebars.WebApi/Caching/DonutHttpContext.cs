using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Handlebars.WebApi
{
    public class DonutHttpContext : DefaultHttpContext
    {
        public DonutHttpContext(IFeatureCollection features, IHeaderDictionary headers) : base(features)
        {
            ((DonutHttpRequest)Request).SetHeaders(headers);
        }

        protected override HttpRequest InitializeHttpRequest() => new DonutHttpRequest(this);
    }
}
