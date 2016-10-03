using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Handlebars.WebApi
{
    public static class GetRoutesMiddlewareExtensions
    {
        public static IApplicationBuilder UseHandlebarsRouteCache(this IApplicationBuilder app, 
                                          Action<IRouteBuilder> configureRoutes,
                                          HandlebarsRouteCache routeCache)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var routes = new RouteBuilder(app)
            {
                DefaultHandler = (MvcRouteHandler)app.ApplicationServices.GetService(typeof(MvcRouteHandler))
            };

            configureRoutes(routes);
            routes.Routes.Insert(0, AttributeRouting.CreateAttributeMegaRoute(app.ApplicationServices));
            var router = routes.Build();

            routeCache.Set(router);

            return app;
        }
    }

    public class HandlebarsRouteCache
    {
        public HandlebarsRouteCache()
        {
            // this._router;
        }

        private IRouter _router;

        public IRouter Get()
        {
            return _router;
        }

        public void Set(IRouter router)
        {
            _router = router;
        }
    }

    public class GetRoutesMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IRouter _router;

        public GetRoutesMiddleware(RequestDelegate next, IRouter router)
        {
            this.next = next;
            _router = router;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var context = new RouteContext(httpContext);
            context.RouteData.Routers.Add(_router);

            await _router.RouteAsync(context);

            if (context.Handler != null)
            {
                httpContext.Features[typeof(IRoutingFeature)] = new RoutingFeature()
                {
                    RouteData = context.RouteData
                };
            }

            // proceed to next...
            await next(httpContext);
        }
    }

    public class RoutingFeature : IRoutingFeature
    {
        public RouteData RouteData { get; set; }
    }
}
