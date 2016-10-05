using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Http;

namespace Handlebars.WebApi
{
    public class DefaultCacheKeyProvider : ICacheKeyProvider
    {
        #region // Constructor //
        public DefaultCacheKeyProvider()
        {
            _md5 = MD5.Create();
        }
        #endregion

        #region // Dependency Injection //
        private readonly MD5 _md5;
        #endregion 

        public async Task<string> GetKey(HttpContext context, int[] builtWithKey)
        {
            return context.Request.Path.ToString();
        }

        public async Task<string[]> GetKeyValue(HttpContext context, int[] hashOf)
        {
            return new[] { "" };
        }
    }
}
