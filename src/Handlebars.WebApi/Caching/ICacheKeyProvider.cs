using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Handlebars.WebApi
{
    public interface ICacheKeyProvider
    {
        Task<string> GetKey(HttpContext context, CacheControlOptions options);

        Task<string[]> GetKeyValue(HttpContext context, CacheControlOptions options);

        Task<string> GetHashOfValue(HttpContext context, CacheControlOptions options, string[] set);
    }
}
