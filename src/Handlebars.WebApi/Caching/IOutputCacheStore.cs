using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handlebars.WebApi
{
    public interface IStoreOutputCache
    {
        Task<byte[]> Get(string key);

        Task Set(string key, byte[] item);
    }
}
