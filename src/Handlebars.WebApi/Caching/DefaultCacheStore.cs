using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Handlebars.WebApi
{
    public class DefaultOutputCacheStore : IStoreOutputCache
    {
        private static readonly Dictionary<string, byte[]> _output = new Dictionary<string, byte[]>();        
        
        public Task<byte[]> Get(string key)
        {
            if (!_output.ContainsKey(key)) return Task.FromResult<byte[]>(null);
            return Task.FromResult(_output[key]);
        }

        public Task Set(string key, byte[] item)
        {
            _output[key] = item;
            return Task.CompletedTask;
        } 
    }

    public class DefaultEtagCacheStore : IStoreEtagCache
    {
        private static readonly Dictionary<string, string> _etag = new Dictionary<string, string>();

        public Task<string> Get(string key)
        {
            if (!_etag.ContainsKey(key)) return Task.FromResult<string>(null);
            return Task.FromResult(_etag[key]);
        }
         
        public Task Set(string key, string etag)
        {
            _etag[key] = etag;
            return Task.CompletedTask;
        }

    }
}
