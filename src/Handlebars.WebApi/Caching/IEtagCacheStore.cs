using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handlebars.WebApi
{
    public interface IStoreEtagCache
    {
        Task<KeyValuePair<string[], string[]>> Get(string key);

        Task Set(string key, KeyValuePair<string[], string[]> set, int duration);

        Task Remove(string key);
    }
}
