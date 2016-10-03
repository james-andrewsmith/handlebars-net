using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
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
                                        IControllerFactory controllerFactory)
        {
            // Basic services
            this._actionSelector = actionSelector;
            this._attributeRouteHandler = attributeRouteHandler;
            this._actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            this._controllerActionInvokerCache = controllerActionInvokerCache;
            this._controllerFactory = controllerFactory;
            
            // get the tree from the attribute routes 
            this._actionSelectionDecisionTree = new HandlebarsActionSelectionDecisionTree(_actionDescriptorCollectionProvider.ActionDescriptors);
        }
        #endregion

        #region // Dependency Injection //
        private readonly IActionSelector _actionSelector;
        private readonly MvcAttributeRouteHandler _attributeRouteHandler;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly IActionSelectionDecisionTree _actionSelectionDecisionTree;
        private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
        private readonly IControllerFactory _controllerFactory;
        #endregion

        public async Task<IActionResult> ExecuteAsync(HttpContext context)
        {
            var http = new DefaultHttpContext(context.Features);
            var rc = new RouteContext(http);
            // var rc = new RouteContext(new DefaultHttpContext());
            rc.RouteData.Values.Add("action", "Get");
            rc.RouteData.Values.Add("controller", "Home");
            rc.RouteData.Values.Add("id", 2);

            IList<IRouter> routers = (IList<IRouter>)context.Items["routers"];

            // Maybe this isn't needed, will need to see once Landing Page Engine is implemented
            foreach (var router in routers)
                rc.RouteData.Routers.Add(router);
                     
            try
            {                
                var t1 = _actionSelectionDecisionTree.Select(new Dictionary<string, object>
                {
                    { "controller" , "Home" },
                    { "action" , "Get" },
                    { "id" , 2 },
                });
                
                // var filter = tree.DecisionTree.Select(rc.RouteData.Values);
                var actionDescriptor = _actionSelector.SelectBestCandidate(rc, t1) as ControllerActionDescriptor;

                // Perhaps this isn't needed, do more testing to find out
                // foreach (var kvp in actionDescriptor.RouteValues)
                // {
                //     if (!string.IsNullOrEmpty(kvp.Value))
                //     {
                //         rc.RouteData.Values[kvp.Key] = kvp.Value;
                //     }
                // }


                var actionMethodInfo = actionDescriptor.MethodInfo;
                var controllerTypeInfo = actionDescriptor.ControllerTypeInfo;

                // todo:
                // consider implementing some of the filter logic here, eg:
                // output caching, as this will be needed for better donut performance

                // try using a fresh "defaulthttpcontext" here instead so the headers don't overlap etc
                var ac = new ActionContext(http, rc.RouteData, actionDescriptor);
                // var ac = new ActionContext(context, rc.RouteData, actionDescriptor);
                var controllerContext = new ControllerContext(ac);
                
                var cacheEntry = _controllerActionInvokerCache.GetState(controllerContext);
                var executor = cacheEntry.ActionMethodExecutor;
                
                var controller = _controllerFactory.CreateController(controllerContext);

                var arguments = ControllerActionExecutor.PrepareArguments(
                    rc.RouteData.Values,
                    executor);

                if (executor.IsTypeAssignableFromIActionResult)
                {
                    if (executor.IsMethodAsync)
                    {
                        return  (IActionResult)await executor.ExecuteAsync(controller, arguments);
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
