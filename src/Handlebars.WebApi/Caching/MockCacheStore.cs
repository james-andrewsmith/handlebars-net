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
        public Task<byte[]> Get(string key)
        {
            return Task.FromResult<byte[]>(null);            
        }

        public Task Set(string key, byte[] item)
        {
            return Task.CompletedTask;
        } 
    }

    public class MockEtagCacheStore : IStoreEtagCache
    {        
        public Task<string> Get(string key)
        {
            return Task.FromResult<string>(null);            
        }

        public Task Remove(string key)
        {
            return Task.CompletedTask;
        }

        public Task Set(string key, string etag)
        {
            return Task.CompletedTask;
        }

    }
}
