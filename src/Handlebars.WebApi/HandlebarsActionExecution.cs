using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;

using Wire;

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
            this._actionSelector = actionSelector;
            this._attributeRouteHandler = attributeRouteHandler;
            this._actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            this._controllerActionInvokerCache = controllerActionInvokerCache;
            this._controllerFactory = controllerFactory;
            this._controllerArgumentBinder = controllerArgumentBinder;
            this._keyProvider = keyProvider;
            this._storeOutput = storeOutput;

            // get the tree from the attribute routes 
            this._router = router.Get();
            this._actionSelectionDecisionTree = new HandlebarsActionSelectionDecisionTree(_actionDescriptorCollectionProvider.ActionDescriptors);

            // Setup Wire for fastest performance
            var types = new[] {
                    typeof(OutputCacheItem),
                    typeof(SectionData)
                };

            this._serializer = new Serializer(new SerializerOptions(knownTypes: types));
            this._ss = _serializer.GetSerializerSession();
            this._ds = _serializer.GetDeserializerSession();
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
        private readonly Serializer _serializer;
        private readonly SerializerSession _ss;
        private readonly DeserializerSession _ds;


        public async Task<IActionResult> ExecuteAsync(HttpContext context, string url)
        {
            try
            {              
                var rc = new RouteContext(context);            
                rc.RouteData.Routers.Add(_router);

                await _router.RouteAsync(rc);
                
                context.Features[typeof(IRoutingFeature)] = new RoutingFeature()
                {
                    RouteData = rc.RouteData
                };

                var candidates = _actionSelectionDecisionTree.Select(rc.RouteData.Values);                
                var actionDescriptor = _actionSelector.SelectBestCandidate(rc, candidates) as ControllerActionDescriptor;

                if (actionDescriptor == null)
                    return null;

                // Find the output cache filter
                var caching = actionDescriptor.FilterDescriptors
                                              .Where(_ => _.Filter is CacheControl)
                                              .FirstOrDefault();
                string key;
                if (caching != null)
                {
                    var filter = caching.Filter as CacheControl;

                    // Get the key
                    key = await _keyProvider.GetKey(context, filter.BuildHashWith);                    

                    // Return the cached response if it exists
                    var cachedValue = await _storeOutput.Get(key);
                    if (cachedValue != null)
                    {
                        OutputCacheItem item;
                        using (var ms = new MemoryStream(cachedValue))
                            item = _serializer.Deserialize<OutputCacheItem>(ms, _ds);

                        // Ensure that other parts of the application avoid recaching
                        // this donut result as it was a hit
                        context.Items["cache-hit"] = true;

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
