using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                                        IControllerArgumentBinder controllerArgumentBinder)
        {
            // Basic services
            this._actionSelector = actionSelector;
            this._attributeRouteHandler = attributeRouteHandler;
            this._actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            this._controllerActionInvokerCache = controllerActionInvokerCache;
            this._controllerFactory = controllerFactory;
            this._controllerArgumentBinder = controllerArgumentBinder;
            
            // get the tree from the attribute routes 
            this._router = router.Get();
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
        private readonly IRouter _router;
        private readonly IControllerArgumentBinder _controllerArgumentBinder;
        #endregion
         
        public async Task<IActionResult> ExecuteAsync(HttpContext context, string url)
        {
            try
            {              
                var rc = new RouteContext(context);            
                rc.RouteData.Routers.Add(_router);

                await _router.RouteAsync(rc);

                var values = rc.RouteData.Values;
                if (values.ContainsKey("controller"))
                    context.Items.Add("controller", values["controller"]);
                if (values.ContainsKey("action"))
                    context.Items.Add("action", values["action"]);

                var candidates = _actionSelectionDecisionTree.Select(rc.RouteData.Values);                
                var actionDescriptor = _actionSelector.SelectBestCandidate(rc, candidates) as ControllerActionDescriptor;
                var actionMethodInfo = actionDescriptor.MethodInfo;
                var controllerTypeInfo = actionDescriptor.ControllerTypeInfo;

                // todo:
                // consider implementing some of the filter logic here, eg:
                // output caching, as this will be needed for better donut performance

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
