using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace HandlebarsViewEngine.Providers
{
    /// <summary>
    /// Can be used to push output caching into an external caching layer.
    /// -> Could be action specific one day
    /// </summary>
    public interface IOutputCacheProvider
    {
        T Get<T>(string key) where T : class;
        void Store(string key, object o, TimeSpan duration);
        void Remove(string key);
    }

    public class AspNetCacheProvider : IOutputCacheProvider
    {
        public AspNetCacheProvider()
        { 
            
        }

        public virtual T Get<T> (string key)
            where T: class
        {
            return HttpContext.Current.Cache.Get(key) as T;
        }

        public virtual void Store(string key, object o, TimeSpan duration)
        {         
            HttpContext.Current.Cache.Insert(key, 
                o, 
                null, 
                DateTime.Now.Add(duration), 
                Cache.NoSlidingExpiration, 
                CacheItemPriority.High,
                new CacheItemRemovedCallback(CacheRemovedCallback));
        }

        public void CacheRemovedCallback(String key, object value, System.Web.Caching.CacheItemRemovedReason removedReason)
        {
            switch (removedReason)
            {
                case CacheItemRemovedReason.Underused:
                    break;
                case CacheItemRemovedReason.DependencyChanged:
                    break;
            }
        }

        public virtual void Remove(string key)
        {
            var ck = new Dictionary<string, string>();
            var en = HttpContext.Current.Cache.GetEnumerator();
            while (en.MoveNext())
            {
                ck.Add(en.Entry.Key.ToString(), en.Entry.Value.ToJsonAsPretty());
            }

            HttpContext.Current.Cache.Remove("dc:" + key);
        }
    }
    
}
