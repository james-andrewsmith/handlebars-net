using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace Handlebars.WebApi
{
    public sealed class StandardRequestFormatter : IRequestFormatter
    {
        public string GetContext(HttpRequestMessage request, object context)
        {
            var o = new
            {
#if DEBUG
                debug = true,
#else
                _debug = false,
#endif
                _device = new
                {
                    type = "crawler",
                }, 
                _request = new 
                {
                    host = request.RequestUri.Host,
                    scheme = request.RequestUri.Scheme,
                    path = request.RequestUri.AbsolutePath,
                    query = request.RequestUri.Query,
                    fqdn = request.RequestUri.Scheme + "://" + request.RequestUri.Host
                },
                _config = new 
                {
                    engine = HandlebarsConfiguration.Instance.Engine,
                    donut = request.Properties.ContainsKey("donut")
                },
                data = context
            };

            return JsonConvert.SerializeObject(o);
        }
    }
}
