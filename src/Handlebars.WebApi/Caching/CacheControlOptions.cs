
namespace Handlebars.WebApi
{
    public class CacheControlOptions
    { 

        /// <summary>
        /// The name of the default settings to use, which will pre
        /// </summary>        
        public string Profile
        {
            get;
            set;
        }

        /// <summary>
        /// How long should items remain in the etag or output cache
        /// </summary>
        public int Duration
        {
            get;
            set;
        }

        /// <summary>
        /// Set the output duration for how long an item will remain in the cache
        /// </summary>
        public int OutputDuration
        {
            get;
            set;
        }

        public int EtagDuration
        {
            get;
            set;
        }

        /// <summary>
        /// The output cache will store redirection results
        /// </summary>
        public bool CacheRedirects
        {
            get;
            set;
        }

        /// <summary>
        /// Process etags in the request header
        /// </summary>
        public bool CacheEtag
        {
            get;
            set;
        }

        /// <summary>
        /// Keep the output of the response in a store
        /// </summary>
        public bool CacheOutput
        {
            get;
            set;
        }

        /// <summary>
        /// Vary the cache key in the store by the following Request.QueryString items
        /// </summary>
        public string[] VaryByQuery
        {
            get;
            set;
        }

        /// <summary>
        /// Vary the cache key in the store by the following RouteData items
        /// </summary>
        public string[] VaryByRoute
        {
            get;
            set;
        }

        /// <summary>
        /// Vary the cache key in the store by the following HttpContext.Items
        /// </summary>
        public string[] VaryByItem
        {
            get;
            set;
        }

        /// <summary>
        /// Vary the cache key in the store by the Request.HttpContext.User 
        /// (anonymous is stored as <null>)
        /// </summary>
        public bool VaryByUser
        {
            get;
            set;
        }

        /// <summary>
        /// Uses identity is in role to detect build a key
        /// </summary>
        public string[] VaryByRole
        {
            get;
            set;
        }


        /// <summary>
        /// The hash is used both as an etag and as a method to determine if the 
        /// output cache is still fresh or has gone stale, the integers are 
        /// passed to an application specific service which uses them to build
        /// a collection of strings represent the state of objects to be hashed.
        /// </summary>
        public int[] BuildHashWith
        {
            get;
            set;
        }
    }
}
