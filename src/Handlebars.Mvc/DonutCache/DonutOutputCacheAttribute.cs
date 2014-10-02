using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;

using System.Reflection;
using System.Web;
using System.Web.UI;
using System.Web.Mvc;
using System.Web.Routing;
using HandlebarsViewEngine.Providers;

namespace HandlebarsViewEngine
{

    internal class OutputCacheItem
    {
        public string Content { get; set; }
        public string ContentType { get; set; }
        public string XTemplate { get; set; }
    }

    /// <summary>
    /// Attaches to the page-rendering process, and allows for better control after the rendering of a page
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class DonutOutputCache : ActionFilterAttribute, IExceptionFilter
    {
        // default cache provider is asp.net met 10 minuten
        internal static IOutputCacheProvider CacheProvider { get; private set; }
        internal static DonutCacheProvider DonutProvider { get; private set; }

        static DonutOutputCache()
        {
            // default cacheprovider uses aspnet cache
            CacheProvider = new AspNetCacheProvider();
            DonutProvider = new DonutCacheProvider();
        }

        /// <summary>
        /// Initialize the MothAction (do this in global.asax) with a custom Provider
        /// </summary>
        /// <param name="provider"></param>
        public static void Initialize(IOutputCacheProvider cacheProvider)
        {
            CacheProvider = cacheProvider;
        }
                 
        #region // Attribute Properties //

        private bool? _noStore;
        private CacheSettings _cacheSettings;

        public int Duration { get; set; }
        public string VaryByParam { get; set; }
        public string VaryByCustom { get; set; }
        public string VaryByHeader { get; set; }
        public string VaryByContentEncoding { get; set; }
        public string CacheProfile { get; set; }
        public OutputCacheLocation Location { get; set; }

        public bool NoStore
        {
            get { return _noStore ?? false; }
            set { _noStore = value; }
        }

        

        #endregion 

        public void OnException(ExceptionContext filterContext)
        {
            if (_cacheSettings != null)
            {
                ExecuteCallback(filterContext, true);
            }
        }

        /// <summary>
        /// Executes the callback.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="hasErrors">if set to <c>true</c> [has errors].</param>
        private void ExecuteCallback(ControllerContext context, bool hasErrors)
        {
            var cacheKey = ComputeCacheKey(context, _cacheSettings);
            var callback = context.HttpContext.Items[cacheKey] as Action<bool>;

            if (callback != null)
            {
                callback.Invoke(hasErrors);
            }
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            // Get configuration of the cache
            _cacheSettings = BuildCacheSettings();
            var cacheKey = ComputeCacheKey(filterContext, _cacheSettings);

            if (!_cacheSettings.IsServerCachingEnabled ||
                filterContext.HttpContext.Response.IsRequestBeingRedirected || 
                filterContext.HttpContext.Request.HttpMethod == "POST" || 
                filterContext.HttpContext.Request.HttpMethod == "PUT" || 
                filterContext.HttpContext.Request.HttpMethod == "DELETE")
            {
                // sometimes things have changed.
                CacheProvider.Remove(cacheKey);
                return;
            }

            // do we have anything in the cache, and is that valid?
            var cacheResult = CacheProvider.Get<OutputCacheItem>(cacheKey);
            if (cacheResult != null && !string.IsNullOrEmpty(cacheResult.Content))
            {
                // swap in the donut actions
                var content = DonutProvider.Replace(filterContext.HttpContext, 
                                                    filterContext.Controller.ControllerContext,
                                                    cacheResult.Content);
                    
                // We inject the previous result into the MVC pipeline
                // The MVC action won't execute as we injected the previous cached result.                   
                if (!string.IsNullOrEmpty(cacheResult.XTemplate))
                    filterContext.HttpContext.Response.Headers["x-template"] = cacheResult.XTemplate;

                filterContext.Result = new ContentResult() 
                {
                    Content = content, 
                    ContentType = cacheResult.ContentType 
                };
                return;
            } 

            // We are hooking into the pipeline to replace the response Output writer
            // by something we own and later eventually gonna cache
            var cachingWriter = new StringWriter(CultureInfo.InvariantCulture);
            var originalWriter = filterContext.HttpContext.Response.Output;
            filterContext.HttpContext.Response.Output = cachingWriter;

            // Will be called back by OnResultExecuted -> ExecuteCallback
            filterContext.HttpContext.Items[cacheKey] = new Action<bool>(hasErrors =>
            {
                // Removing this executing action from the context
                filterContext.HttpContext.Items.Remove(cacheKey);

                // We restore the original writer for response
                filterContext.HttpContext.Response.Output = originalWriter;

                // Now we use owned caching writer to actually store data
                var cacheItem = new OutputCacheItem
                {
                    Content = cachingWriter.ToString(),
                    ContentType = filterContext.HttpContext.Response.ContentType,
                    XTemplate = filterContext.HttpContext.Response.Headers["x-template"]
                };

                // now actually write the response
                filterContext.HttpContext.Response.Write(
                    DonutProvider.Replace(filterContext.HttpContext,
                                          filterContext.Controller.ControllerContext,
                                          cacheItem.Content)
                );

                // Something went wrong, we are not going to cache something bad
                if (hasErrors)
                {
                    return; 
                }

                // save the output
                if (filterContext.HttpContext.Response.StatusCode == 200 &&
                    filterContext.HttpContext.Response.IsClientConnected &&
                   !filterContext.HttpContext.Response.IsRequestBeingRedirected &&
                    _cacheSettings.Duration > 0)
                    CacheProvider.Store(cacheKey, cacheItem, new TimeSpan(0, 0, _cacheSettings.Duration));
                
            });

        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (_cacheSettings == null)
            {
                return;
            }

#if DEBUG
            // Sometimes useful for debuging caching issues.
            filterContext.RequestContext.HttpContext.Response.Headers.Add("x-cache-key", ComputeCacheKey(filterContext, _cacheSettings));
#endif

            // See OnActionExecuting
            ExecuteCallback(filterContext, filterContext.Exception != null);


            // If we are in the context of a child action, the main action is responsible for setting
            // the right HTTP Cache headers for the final response.
            // if (!filterContext.IsChildAction)
            // {
            //     CacheHeadersHelper.SetCacheHeaders(filterContext.HttpContext.Response, CacheSettings);
            // }
        }
         

        private static string ComputeCacheKey(ControllerContext context, CacheSettings cacheSettings)
        {
            var actionName = context.RouteData.Values["action"].ToString();
            var controllerName = (context.RouteData.DataTokens.ContainsKey("area") ? context.RouteData.DataTokens["area"].ToString() + "."  : "") +
                                  context.RouteData.Values["controller"].ToString();

            // remove controller, action and DictionaryValueProvider which is added by the framework for child actions
            var filteredRouteData = context.RouteData.Values.Where(
                x => x.Key.ToLowerInvariant() != "controller" &&
                     x.Key.ToLowerInvariant() != "action" &&
                     !(x.Value is DictionaryValueProvider<object>)
            );
             
            var routeValues = new RouteValueDictionary(filteredRouteData.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value));

            if (!context.IsChildAction)
            {
                // note that route values take priority over form values and form values take priority over query string values
                
                foreach (var formKey in context.HttpContext.Request.Form.AllKeys)
                {
                    if (routeValues.ContainsKey(formKey.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var item = context.HttpContext.Request.Form[formKey];
                    routeValues.Add(
                        formKey.ToLowerInvariant(),
                        item != null
                            ? item.ToLowerInvariant()
                            : string.Empty
                    );
                }
                             
                foreach (var queryStringKey in context.HttpContext.Request.QueryString.AllKeys)
                {
                    // queryStringKey is null if url has qs name without value. e.g. test.com?q
                    if (queryStringKey == null || routeValues.ContainsKey(queryStringKey.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var item = context.HttpContext.Request.QueryString[queryStringKey];
                    routeValues.Add(
                        queryStringKey.ToLowerInvariant(),
                        item != null
                            ? item.ToLowerInvariant()
                            : string.Empty
                    );
                } 
            }

            if (!string.IsNullOrEmpty(cacheSettings.VaryByParam))
            {
                if (cacheSettings.VaryByParam.ToLowerInvariant() == "none")
                {
                    routeValues.Clear();
                }
                else if (cacheSettings.VaryByParam != "*")
                {
                    var parameters = cacheSettings.VaryByParam.ToLowerInvariant().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    routeValues = new RouteValueDictionary(routeValues.Where(x => parameters.Contains(x.Key))
                                                                      .ToDictionary(x => x.Key, x => x.Value));
                }
            }

            if (!string.IsNullOrEmpty(cacheSettings.VaryByHeader))
            {
                var parameters = cacheSettings.VaryByHeader
                                              .ToLowerInvariant()
                                              .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach(var header in parameters)
                {
                    if (!string.IsNullOrEmpty(context.RequestContext.HttpContext.Request.Headers[header]))
                        routeValues.Add(header, context.RequestContext.HttpContext.Request.Headers[header]);
                }
                    
            }

            // custom cache key  
            if (!string.IsNullOrEmpty(cacheSettings.VaryByCustom))
            {
                routeValues.Add(
                    cacheSettings.VaryByCustom.ToLowerInvariant(),
                    context.HttpContext.ApplicationInstance.GetVaryByCustomString(HttpContext.Current, cacheSettings.VaryByCustom)
                );
            }

            return BuildKey(controllerName, actionName, routeValues);
        }

        public static string BuildKey(string controllerName, string actionName, RouteValueDictionary routeValues)
        {
            var builder = new StringBuilder();

            if (controllerName != null)
            {
                builder.AppendFormat("{0}.", controllerName.ToLowerInvariant());
            }

            if (actionName != null)
            {
                builder.AppendFormat("{0}#", actionName.ToLowerInvariant());
            }

            if (routeValues != null)
            {
                foreach (var routeValue in routeValues)
                {
                    builder.Append(BuildKeyFragment(routeValue));
                    // builder.Append(routeValue.Value == null ? "<null>" : routeValue.Value.ToString().ToLowerInvariant() + ";");
                }
            }

            return builder.ToString();
        }

        public static string BuildKeyFragment(KeyValuePair<string, object> routeValue)
        {
            var value = routeValue.Value == null ? "<null>" : routeValue.Value.ToString().ToLowerInvariant();

            return string.Format("{0}={1}#", routeValue.Key.ToLowerInvariant(), value);
        }

        private CacheSettings BuildCacheSettings()
        {
            CacheSettings cacheSettings;

            if (string.IsNullOrEmpty(CacheProfile))
            {
                cacheSettings = new CacheSettings
                {
                    IsCachingEnabled = true,
                    Duration = Duration,
                    VaryByParam = VaryByParam,
                    VaryByCustom = VaryByCustom,
                    VaryByHeader = VaryByHeader,
                    Location = (int)Location == -1 ? OutputCacheLocation.Server : Location,
                    NoStore = NoStore
                };
            }
            else
            {
                cacheSettings = new CacheSettings
                {
                    // IsCachingEnabled = cacheProfile.Enabled,
                    // Duration = Duration == -1 ? cacheProfile.Duration : Duration,
                    // VaryByCustom = VaryByCustom ?? cacheProfile.VaryByCustom,
                    // VaryByParam = VaryByParam ?? cacheProfile.VaryByParam,
                    // VaryByHeader = VaryByHeader ?? cacheProfile.VaryByHeader,
                    Location = (int)Location == -1 ? OutputCacheLocation.Server : Location,
                    NoStore = _noStore.HasValue ? _noStore.Value : false
                };
            }

            if (cacheSettings.Duration == -1)
            {
                throw new HttpException("The directive or the configuration settings profile must specify the 'duration' attribute.");
            }

            if (cacheSettings.Duration < 0)
            {
                throw new HttpException("The 'duration' attribute must have a value that is greater than or equal to zero.");
            }

            return cacheSettings;
        }
    }

    public class CacheSettings
    {
        public bool IsCachingEnabled { get; set; }
        public int Duration { get; set; }
        public string VaryByParam { get; set; }
        public string VaryByCustom { get; set; }
        public string VaryByHeader { get; set; }
        public OutputCacheLocation Location { get; set; }
        public bool NoStore { get; set; }

        public bool IsServerCachingEnabled
        {
            get
            {
                return IsCachingEnabled && Duration > 0 && (Location == OutputCacheLocation.Any ||
                                                            Location == OutputCacheLocation.Server ||
                                                            Location == OutputCacheLocation.ServerAndClient);
            }
        }
    }

}
