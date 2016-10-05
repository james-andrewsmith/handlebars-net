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
        Task<string> GetKey(HttpContext context, int[] buildKeyWith);

        Task<string[]> GetKeyValue(HttpContext context, int[] hashOf);
    }
}
