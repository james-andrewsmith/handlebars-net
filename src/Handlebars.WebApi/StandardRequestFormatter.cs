    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Handlebars.WebApi
{
    public sealed class StandardRequestFormatter : IRequestFormatter
    {
        public string GetContext(HttpRequest request, object context)
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
                    host = request.Host.Host,
                    scheme = request.Scheme,
                    path = request.Path.Value,
                    query = request.QueryString.ToUriComponent(),
                    fqdn = request.Scheme + "://" + request.Host.Host
                },
                _config = new 
                {
                    engine = HandlebarsConfiguration.Instance.Engine,
                    donut = request.HttpContext.Items.ContainsKey("donut")
                },
                data = context
            };

            return JsonConvert.SerializeObject(o);
        }
    }
}
