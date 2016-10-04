using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Handlebars.WebApi
{
    public class DefaultCacheKeyProvider : ICacheKeyProvider
    {
        public async Task<string> GetKey(HttpContext context, int[] builtWithKey)
        {
            return context.Request.Path.ToString();
        }
    }
}
