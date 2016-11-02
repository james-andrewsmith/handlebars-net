using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
 
namespace Handlebars.WebApi
{
    public class HandlebarsActionExecutor
    {

        #region // Constructor //
        public HandlebarsActionExecutor(IActionSelector actionSelector,
                                        MvcAttributeRouteHandler attributeRouteHandler,
                                        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
                                        IActionSelectorDecisionTreeProvider actionSelectorDecisionTreeProvider,
                                        ControllerActionInvokerCache controllerActionInvokerCache,
                                        IControllerFactory controllerFactory,
                                        HandlebarsRouteCache router,
                                        IControllerArgumentBinder controllerArgumentBinder,
                                        ICacheKeyProvider keyProvider,
                                        IStoreOutputCache storeOutput)
        {
            // Basic services
            _actionSelector = actionSelector;
            _attributeRouteHandler = attributeRouteHandler;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _controllerActionInvokerCache = controllerActionInvokerCache;
            _controllerFactory = controllerFactory;
            _controllerArgumentBinder = controllerArgumentBinder;
            _keyProvider = keyProvider;
            _storeOutput = storeOutput;

            // get the tree from the attribute routes 
            _router = router.Get();
            _actionSelectionDecisionTree = new HandlebarsActionSelectionDecisionTree(_actionDescriptorCollectionProvider.ActionDescriptors);             
        }
        #endregion

        #region // Dependency Injection //
        private readonly IActionSelector _actionSelector;
        private readonly MvcAttributeRouteHandler _attributeRouteHandler;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly IActionSelectionDecisionTree _actionSelectionDecisionTree;
        private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
        private readonly IControllerFactory _controllerFactory;
        private readonly IRouter _router;
        private readonly IControllerArgumentBinder _controllerArgumentBinder;
        private readonly ICacheKeyProvider _keyProvider;
        private readonly IStoreOutputCache _storeOutput;
        #endregion        

        public async Task<IActionResult> ExecuteAsync(HttpContext context)
        {

#if TIMING
            var url = context.Request.Path.Value;
            var sw = Stopwatch.StartNew();
            Console.WriteLine("{1} | Starting ExecuteAsync in {0}", sw.ElapsedMilliseconds, url);
#endif

            try
            {              
                var rc = new RouteContext(context);            
                rc.RouteData.Routers.Add(_router);

                await _router.RouteAsync(rc);
#if TIMING 
                Console.WriteLine("{1} | _router.RouteAsync in {0}", sw.ElapsedMilliseconds, url);
#endif

                context.Features[typeof(IRoutingFeature)] = new RoutingFeature()
                {
                    RouteData = rc.RouteData
                };

                var candidates = _actionSelectionDecisionTree.Select(rc.RouteData.Values);
#if TIMING
                Console.WriteLine("{1} | _actionSelectionDecisionTree.Select in {0}", sw.ElapsedMilliseconds, url);
#endif

                var actionDescriptor = _actionSelector.SelectBestCandidate(rc, candidates) as ControllerActionDescriptor;
#if TIMING
                Console.WriteLine("{1} | _actionSelector.SelectBestCandidate in {0}", sw.ElapsedMilliseconds, url);
#endif

                if (actionDescriptor == null)
                    return null;

                // Find the output cache filter
                var caching = actionDescriptor.FilterDescriptors
                                              .Where(_ => _.Filter is CacheControl)
                                              .FirstOrDefault();
                
                if (caching != null)
                {
                    var filter = caching.Filter as CacheControl;
                    
                    // Get the key                    
                    var set = await _keyProvider.GetKeyValue(context, filter.Options);
#if TIMING
                    Console.WriteLine("{1} | _keyProvider.GetKeyValue in {0}", sw.ElapsedMilliseconds, url);
#endif

                    var hash = await _keyProvider.GetHashOfValue(context, filter.Options, set.Value);
#if TIMING
                    Console.WriteLine("{1} | _keyProvider.GetHashOfValue in {0}", sw.ElapsedMilliseconds, url);
#endif
                    var key = await _keyProvider.GetKey(context, filter.Options, hash);
#if TIMING
                    Console.WriteLine("{1} | _keyProvider.GetKey in {0}", sw.ElapsedMilliseconds, url);
#endif

                    OutputCacheItem item = await _storeOutput.Get(key);
#if TIMING
                    Console.WriteLine("{1} | _storeOutput.Get in {0}", sw.ElapsedMilliseconds, url);
#endif

                    if (item != null)
                    {
                        // Ensure that other parts of the application avoid recaching
                        // this donut result as it was a hit
                        context.Items["cache-hit"] = true;
#if TIMING
                        Console.WriteLine("{1} | ExecuteAsync Finished in {0}", sw.ElapsedMilliseconds, url);
#endif
                        return new ContentResult
                        {
                            Content = item.Content,
                            ContentType = item.ContentType,
                            StatusCode = item.StatusCode
                        };
                    }                                        
                     
                    // If we didn't find the key we will want to cache it once the response 
                    // is finished so add this to the items so the donut processor can do that
                    context.Items["cache-key"] = key;
                    context.Items["cache-set"] = set;
                    context.Items["cache-hash"] = hash;
                    context.Items["cache-duration"] = filter.Options.OutputDuration;
                }
                else
                {
                    context.Items.Remove("cache-key");
                }

                // Proceed with executing the action and then working with the result
                var actionMethodInfo = actionDescriptor.MethodInfo;
                var controllerTypeInfo = actionDescriptor.ControllerTypeInfo;
                var actionContext = new ActionContext(context, rc.RouteData, actionDescriptor);
                var controllerContext = new ControllerContext(actionContext);
                
                var cacheEntry = _controllerActionInvokerCache.GetState(controllerContext);
                var executor = cacheEntry.ActionMethodExecutor;
                
                var controller = _controllerFactory.CreateController(controllerContext);

                var _arguments = new Dictionary<string, object>(executor.ActionParameters.Length, StringComparer.OrdinalIgnoreCase); 
                foreach(var param in executor.ActionParameters)
                {
                    if (rc.RouteData.Values.ContainsKey(param.Name))
                    {
                        if (param.ParameterType == typeof(int))
                            _arguments.Add(param.Name, Convert.ToInt32(rc.RouteData.Values[param.Name]));
                        else if (param.ParameterType == typeof(long))
                            _arguments.Add(param.Name, Convert.ToInt64(rc.RouteData.Values[param.Name]));
                        else if (param.ParameterType == typeof(short))
                            _arguments.Add(param.Name, Convert.ToInt16(rc.RouteData.Values[param.Name]));
                        else 
                            _arguments.Add(param.Name, rc.RouteData.Values[param.Name]);
                    }
                }

                // Might need to copy and change this class locally, will see
                // await _controllerArgumentBinder.BindArgumentsAsync(controllerContext, controller, _arguments);
               
                var arguments = ControllerActionExecutor.PrepareArguments(
                    _arguments,
                    executor
                );

#if TIMING
                Console.WriteLine("{1} | ControllerActionExecutor.PrepareArguments in {0}", sw.ElapsedMilliseconds, url);
#endif

                // adding back to the cache is done once the template is rendered
                if (executor.IsTypeAssignableFromIActionResult)
                {
                    if (executor.IsMethodAsync)
                    {
                        return (IActionResult)await executor.ExecuteAsync(controller, arguments);
                    }
                    else
                    {                        
                        return (IActionResult)executor.Execute(controller, arguments);
                    }
                } 

                throw new NotImplementedException();

            }
            catch (Exception exp)
            {
                throw exp;
            }
        } 

    }
}
