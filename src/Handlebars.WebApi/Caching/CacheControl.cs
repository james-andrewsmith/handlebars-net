using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Handlebars.WebApi
{

    public class CacheControl : Attribute, IFilterFactory  
    {

        #region // IFilterFactory //
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            GetOptions();

            // Build the specific filter object which will be used for 
            // requests to this action
            return new OutputCacheFilter(
                serviceProvider.GetService<IHandlebarsTemplate>(),
                serviceProvider.GetService<IRequestFormatter>(),
                serviceProvider.GetService<HandlebarsActionExecutor>(),
                serviceProvider.GetService<ICacheKeyProvider>(),
                serviceProvider.GetService<IStoreEtagCache>(),
                serviceProvider.GetService<IStoreOutputCache>(),
                new Lazy<ILogger<OutputCacheFilter>>(() => serviceProvider.GetService<ILogger<OutputCacheFilter>>()),
                _options
            );
        }

        private void GetOptions()
        {
            // Ensure the more specific attributes are used if provided
            var etagDuration = this.EtagDuration == 0 ? this.Duration : this.EtagDuration;
            var outputDuration = this.OutputDuration == 0 ? this.Duration : this.OutputDuration;

            // To improve readability we use an class to hold this information in an object,
            // given how much this is passed to interfaces this makes updating the config
            // a lot less impactful on other classes
            _options = new CacheControlOptions
            {
                CacheEtag = this.CacheEtag,
                CacheOutput = this.CacheOutput,
                CacheRedirects = this.CacheRedirects,
                EtagDuration = etagDuration,
                OutputDuration = outputDuration,
                VaryByRoute = this.VaryByRoute,
                VaryByQuery = this.VaryByQuery,
                VaryByItem = this.VaryByItem,
                VaryByUser = this.VaryByUser,
                VaryByRole = this.VaryByRole,
                BuildHashWith = this.BuildHashWith
            };
        }

        private CacheControlOptions _options;
        internal CacheControlOptions Options
        {
            get
            {
                if (_options == null) GetOptions();
                return _options;
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }

        #endregion

        #region // Attribute Properties //

        /// <summary>
        /// The name of the default settings to use, which will pre
        /// </summary>        
        public string Profile
        {
            get { return _profile; }
            set
            {
                _profile = value;
                DefaultProfileChanged();
            }
        }
        private string _profile;

        /// <summary>
        /// Todo: Power these defaults from the new aspnetcore configuration
        /// and options system, so we don't have these set in code
        /// </summary>
        private void DefaultProfileChanged()
        {
            switch (_profile)
            {
                case CacheControlProfile.Handlebars:
                    CacheEtag = true;
                    CacheOutput = true;
                    CacheRedirects = true;
                    VaryByQuery = new[] { "x-format" };
                    VaryByUser = false;
                    VaryByItem = new[] { "x-device" };
                    break;
                case CacheControlProfile.ApiEndpoint:
                    CacheEtag = true;
                    CacheOutput = true;
                    VaryByRoute = new[] { "id", "domain_id", "website_id" };
                    VaryByUser = false;
                    VaryByRole = new[] { "admin" };
                    break;
            }
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

        #endregion 

        private class OutputCacheFilter : IAsyncResourceFilter
        {

            #region // Constructor //
            public OutputCacheFilter(IHandlebarsTemplate template,
                                     IRequestFormatter formatter,
                                     HandlebarsActionExecutor executor,
                                     ICacheKeyProvider keyProvider,
                                     IStoreEtagCache storeEtag,
                                     IStoreOutputCache storeOutput,
                                     Lazy<ILogger<OutputCacheFilter>> logger,
                                     CacheControlOptions options) : base()
            {
                // Service injection
                _template = template;
                _formatter = formatter;
                _executor = executor;
                _keyProvider = keyProvider;
                _storeEtag = storeEtag;
                _storeOutput = storeOutput;                
                 
                // Setup filter configuration 
                _options = options;
                _logger = logger;                          
            }
            #endregion 

            #region // Services //             
            private readonly CacheControlOptions _options;
            private readonly HandlebarsActionExecutor _executor;
            private readonly IHandlebarsTemplate _template;
            private readonly IRequestFormatter _formatter;
            private readonly IStoreOutputCache _storeOutput;
            private readonly IStoreEtagCache _storeEtag;
            private readonly ICacheKeyProvider _keyProvider;
            private readonly Lazy<ILogger<OutputCacheFilter>> _logger;
            #endregion
             
            public async Task OnResourceExecutionAsync(ResourceExecutingContext context,
                                                       ResourceExecutionDelegate next)
            {
                KeyValuePair<string[], string[]> set;
                Stopwatch sw = Stopwatch.StartNew();

                // Has the client sent an etag to the server, if so, we will calculate
                // if there has been any change to it here 
                string hash;
                if (context.HttpContext.Request.Headers.ContainsKey("If-None-Match"))
                {
                    // use the etag to check the store
                    var etagHash = Convert.ToString(context.HttpContext.Request.Headers["If-None-Match"]);
                    var etagSetUsed = true;

                    // convert the weak reference into just the hash 
                    _logger.Value.LogInformation("Get existing etag {etagHash} set in {ElapsedMilliseconds}", etagHash, sw.ElapsedMilliseconds);
                    set = await _storeEtag.Get(await _keyProvider.GetKey(context.HttpContext, _options, etagHash.Substring(3, etagHash.Length - 4)));

                    // if the cache has been cleared call back to recreating the set 
                    // from the configuration and getting the keys fresh
                    if (set.Value.Length != _options.BuildHashWith.Length)
                    {
                        set = await _keyProvider.GetKeyValue(context.HttpContext, _options);
                        etagSetUsed = false;
                    }

                    // calculate the actual hash from the values 
                    _logger.Value.LogInformation("Get hash of set for etag {etagHash} in {ElapsedMilliseconds}", etagHash, sw.ElapsedMilliseconds);
                    hash = await _keyProvider.GetHashOfValue(context.HttpContext, _options, set.Value);
                    if (_options.CacheEtag && $"W/\"{hash}\"" == etagHash)
                    {
                        
                        // sometimes the local or distributed cache might expire, but the has is still legimate
                        // in those cases we want to ensure the hash gets saved again (but only if it works) 
                        if (!etagSetUsed)
                        {
                            _logger.Value.LogInformation("Saving set to redis in {ElapsedMilliseconds}", sw.ElapsedMilliseconds);
                            await _storeEtag.Set(
                                await _keyProvider.GetKey(context.HttpContext, _options, hash), 
                                set, 
                                _options.EtagDuration > _options.OutputDuration ? _options.EtagDuration : _options.OutputDuration
                            );
                        }

                        // if the ETag matches don't do any work, just send the not modified                           
                        _logger.Value.LogInformation("Returning 304 Response in {ElapsedMilliseconds}", sw.ElapsedMilliseconds);
                        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotModified;
                        context.HttpContext.Response.ContentType = "text/html";
                        context.HttpContext.Response.Headers["ETag"] = etagHash;
                        context.HttpContext.Response.Headers["Cache-Control"] = "public, max-age=" + _options.EtagDuration;
                        return;
                    }
                }
                else
                {
                    // if no etag was provided then calculate it for this request, as at some point
                    // in the request chain this will be needed for naming the output cache item
                    // and controlling it's dependency chain via cancel tokens
                    _logger.Value.LogInformation("Calculating Set");
                    set = await _keyProvider.GetKeyValue(context.HttpContext, _options);
                    _logger.Value.LogInformation("Calculating Hash");
                    hash = await _keyProvider.GetHashOfValue(context.HttpContext, _options, set.Value);
                }

                // if this filter is running we can assume either CacheEtag or CacheOutput 
                // is true, so no need for an additional check
                var cacheKey = await _keyProvider.GetKey(context.HttpContext, _options, hash);

                // We want future requests from this endpoint to use donut caching
                if (_options.CacheEtag && !context.HttpContext.Items.ContainsKey("donut"))
                {
                    // this will save time on future runs
                    _logger.Value.LogInformation("Saving set to redis for {cacheKey} in {ElapsedMilliseconds}", cacheKey, sw.ElapsedMilliseconds);
                    await _storeEtag.Set(cacheKey, set, _options.EtagDuration > _options.OutputDuration ? _options.EtagDuration : _options.OutputDuration);

                    // put this in the response early 
                    var response = context.HttpContext.Response; 
                    response.Headers["ETag"] = $"W/\"{hash}\"";
                    response.Headers["Cache-Control"] = "public, max-age=" + _options.EtagDuration;                    
                }

                if (_options.CacheOutput)
                {
                    OutputCacheItem item = await _storeOutput.Get(cacheKey);
                    if (item != null)
                    {
                        // Deserialize the output cache item 
                        _logger.Value.LogInformation("Get OutputCache Item from store for {cacheKey} in {ElapsedMilliseconds}", cacheKey, sw.ElapsedMilliseconds);

                        // Needed for every repsonse
                        context.HttpContext.Response.StatusCode = item.StatusCode;
                        context.HttpContext.Response.ContentType = item.ContentType;

                        // Check if this is redirect response 
                        if (item.StatusCode == (int)HttpStatusCode.Redirect ||
                            item.StatusCode == (int)HttpStatusCode.MovedPermanently)
                        {
                            // Add the redirection header
                            context.HttpContext.Response.Headers.Add("location", item.Content);

                            // Prepare HTML for browsers which ignore headers
                            var html = string.Concat(
                                "<html><head><title>Moved</title></head><body><h1>Moved</h1><p>This page has moved to <a href=\"",
                                item.Content,
                                "\">",
                                item.Content,
                                "</a>.</p></body></html>"
                            );

                            // Send to client then exit
                            await context.HttpContext.Response.WriteAsync(html);
                            return;
                        }

                        // Check if we need to fill any donut holes
                        var donuts = item.Donuts;
                        if (donuts == null || donuts.Count == 0)
                        {
                            // Write out the string directly (no cost from a stringbuilder)
                            await context.HttpContext.Response.WriteAsync(item.Content);
                            return;
                        }

                        // Get a stringbuilder for handling the donut insertion 
                        var response = new StringBuilder(item.Content);
                        var original = context.HttpContext.Request;

                        // To avoid enumator collision issues with the mutliple donuts
                        // cache the keys in a new object up here and use index access
                        var keys = original.Headers.Keys.ToArray();
                        var tasks = new Task<IActionResult>[donuts.Count];
                        var contexts = new DefaultHttpContext[donuts.Count];

                        // move backwards through the donuts 
                        _logger.Value.LogInformation("Starting Donuts for {cacheKey} in {ElapsedMilliseconds}", cacheKey, sw.ElapsedMilliseconds);
                        for (int i = 0; i < donuts.Count; i++)
                        {
                            // Perf: avoid allocations 
                            var kvp = donuts[i];
                            string url = kvp.Key;

                            // copy revevant details to new context
                            var features = new FeatureCollection(context.HttpContext.Features);
                            features.Set<IItemsFeature>(new ItemsFeature());
                            var http = new DefaultHttpContext(features);

                            for (var k = 0; k < keys.Length; k++)
                                http.Request.Headers[keys[k]] = original.Headers[keys[k]];

                            http.Items["donut"] = true;

                            // Ensure A/B testing stays consistent between calls
                            if (context.HttpContext.Items.ContainsKey("experiment"))
                                http.Items.Add("experiment", context.HttpContext.Items["experiment"]);
                            if (original.HttpContext.Items.ContainsKey("x-account"))
                                http.Items.Add("x-account", original.HttpContext.Items["x-account"]);
                            
                            http.Request.Path = url;
                            http.User = context.HttpContext.User;

                            contexts[i] = http;
                            tasks[i] = _executor.ExecuteAsync(http, url);
                        }

                        // Wait for all the donuts to complete
                        _logger.Value.LogInformation("Executing Donuts for {cacheKey} in {ElapsedMilliseconds}", cacheKey, sw.ElapsedMilliseconds);
                        await Task.WhenAll(tasks);

                        // Actually fill the donut hole 
                        bool donutCacheKeyChanged = false;
                        for (int i = (donuts.Count - 1); i >= 0; i--)
                        {
                            var kvp = donuts[i];
                            string url = kvp.Key;

                            string value = $"<!-- {kvp.Key} -->";
                            string contentType = "text/html";
                            int statusCode = 200;

                            var task = tasks[i];
                            var http = contexts[i];
                            var result = task.Result;

                            // Check for a response which is from the cache
                            if (result != null)
                            {
                                if (result is ContentResult)
                                {
                                    var cr = ((ContentResult)result);
                                    value = cr.Content;
                                    statusCode = cr.StatusCode == null ? 200 : (int)cr.StatusCode;
                                    contentType = cr.ContentType;
                                }
                                else
                                {
                                    var view = HandlebarsMediaTypeFormatter.GetView(http);
                                    if (result is OkObjectResult)
                                    {
                                        var ok = ((OkObjectResult)result);
                                        var j = _formatter.GetContext(http.Request, ok.Value);
                                        value = _template.Render(view, j);
                                        statusCode = ok.StatusCode == null ? 200 : (int)ok.StatusCode;
                                        contentType = "text/html";
                                    }
                                }

                                // We know this wasn't from a cache hit, so add this to the cache
                                if (!http.Items.ContainsKey("cache-hit") &&
                                    http.Items.ContainsKey("cache-key"))
                                {                                   
                                    await _storeOutput.Set(
                                        Convert.ToString(http.Items["cache-key"]),
                                        http.Items["cache-set"] as string[],
                                        Convert.ToInt32(http.Items["cache-duration"]), 
                                        new OutputCacheItem
                                        {
                                            ContentType = contentType,
                                            StatusCode = statusCode,
                                            Content = value
                                        }
                                    );                                       
                                } 
                            }

                            // Fil the donut hole using the string builder 
                            response.Remove(kvp.Start, kvp.Stop - kvp.Start);
                            response.Insert(kvp.Start, value);
                        }

                        _logger.Value.LogInformation("Finished Donuts for {cacheKey} in {ElapsedMilliseconds}", cacheKey, sw.ElapsedMilliseconds);

                        // Stream the contents of the stringbuilder to client
                        await context.HttpContext.Response.WriteAsync(response.ToString());
                        return;
                    }

                    // allows the media type formatter to work
                    context.HttpContext.Items.Add("cache", "");
                }

                await next();

                 
                if (_options.CacheOutput && 
                    !string.IsNullOrEmpty(cacheKey))
                {
                    var response = context.HttpContext.Response;
                    if (response.StatusCode == (int)HttpStatusCode.OK || 
                        response.StatusCode == (int)HttpStatusCode.Created)
                    {
                        // check if there are any handlebars items here 
                        var body = context.HttpContext.Items["cache"] as string;
                        if (!string.IsNullOrEmpty(body))
                        {
                            var item = new OutputCacheItem();
                            item.StatusCode = response.StatusCode;
                            item.ContentType = response.ContentType;
                            item.Content = body;

                            if (context.HttpContext.Items.ContainsKey("cache-donut"))
                                item.Donuts = context.HttpContext.Items["cache-donut"] as List<SectionData>;

                            await _storeOutput.Set(
                                cacheKey, 
                                set.Key,
                                _options.OutputDuration,
                                item
                            );

                            _logger.Value.LogInformation("Saved Output of {cacheKey} in {ElapsedMilliseconds}", cacheKey, sw.ElapsedMilliseconds);
                        }
                    }
                    else if (_options.CacheRedirects && 
                             (response.StatusCode == (int)HttpStatusCode.Redirect || 
                              response.StatusCode == (int)HttpStatusCode.MovedPermanently))
                    {
                        // cache the redirection header etc
                        var item = new OutputCacheItem();
                        item.StatusCode = response.StatusCode;
                        item.ContentType = response.ContentType;
                        item.Content = response.Headers["location"];

                        await _storeOutput.Set(
                            cacheKey, 
                            set.Key,
                            _options.OutputDuration,
                            item
                        ); 

                        // Prepare HTML for browsers which ignore headers
                        var html = string.Concat(
                            "<html><head><title>Moved</title></head><body><h1>Moved</h1><p>This page has moved to <a href=\"",
                            item.Content,
                            "\">",
                            item.Content,
                            "</a>.</p></body></html>"
                        );

                        // Send to client then exit
                        await context.HttpContext.Response.WriteAsync(html);
                    }
                     
                }
            }
        }
         
  }
}