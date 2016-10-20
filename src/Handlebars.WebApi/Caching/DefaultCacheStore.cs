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
        private static readonly Dictionary<string, OutputCacheItem> _output = new Dictionary<string, OutputCacheItem>(StringComparer.OrdinalIgnoreCase);        
        
        public Task<OutputCacheItem> Get(string key)
        {
            if (!_output.ContainsKey(key)) return Task.FromResult<OutputCacheItem>(null);
            return Task.FromResult(_output[key]);
        }

        public Task Set(string key, string[] dependencies, int duration, OutputCacheItem item)
        {
            _output[key] = item;
            return Task.CompletedTask;
        } 
    }

    public class DefaultEtagCacheStore : IStoreEtagCache
    {
        private static readonly Dictionary<string, KeyValuePair<string[], string[]>> _etag = new Dictionary<string, KeyValuePair<string[], string[]>>(StringComparer.OrdinalIgnoreCase);

        public Task<KeyValuePair<string[], string[]>> Get(string key)
        {
            if (!_etag.ContainsKey(key)) return Task.FromResult(new KeyValuePair<string[], string[]>(null, null));
            return Task.FromResult(_etag[key]);
        }

        public Task Remove(string key)
        {
            _etag.Remove(key);
            return Task.CompletedTask;
        }

        public Task Set(string key, KeyValuePair<string[], string[]> set, int duration)
        {
            _etag[key] = set;
            return Task.CompletedTask;
        }

    }
}
