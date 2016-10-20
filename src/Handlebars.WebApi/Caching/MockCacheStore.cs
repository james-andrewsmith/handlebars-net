using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Handlebars.WebApi
{
    public class MockOutputCacheStore : IStoreOutputCache
    {
        public Task<OutputCacheItem> Get(string key)
        {
            return Task.FromResult<OutputCacheItem>(null);            
        }

        public Task Set(string key, string[] dependencies, int duration, OutputCacheItem item)
        {
            return Task.CompletedTask;
        } 
    }

    public class MockEtagCacheStore : IStoreEtagCache
    {
        public Task<KeyValuePair<string[], string[]>> Get(string key)
        {
            return Task.FromResult(new KeyValuePair<string[], string[]>(null, null));
        }

        public Task Remove(string key)
        {
            return Task.CompletedTask;
        }

        public Task Set(string key, KeyValuePair<string[], string[]> set, int duration)
        {
            return Task.CompletedTask;
        }

    }
}
