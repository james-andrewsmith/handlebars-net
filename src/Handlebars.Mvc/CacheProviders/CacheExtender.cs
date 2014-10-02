using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandlebarsViewEngine.Providers
{
    internal static class CacheExtender
    {
        public static T GetFromCache<T>(this IOutputCacheProvider provider, string key, Func<T> fetchAction, TimeSpan duration)
            where T: class
        {
            var obj = provider.Get<T>(key);

            // gotta change this, we'll have to add some <nil> value or something, as this is going to break one day
            if(obj == null)
            {
                obj = fetchAction();
                provider.Store(key, obj, duration);
            }

            return obj;
        }
    }
}
