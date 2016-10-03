using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;


using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;


namespace Handlebars.WebApi
{
    public class SampleAsyncActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {

            var values = context.RouteData.Values;

            context.HttpContext.Items["routers"] = context.RouteData.Routers;

            if (values.ContainsKey("controller"))
                context.HttpContext.Items.Add("controller", values["controller"]);
            if (values.ContainsKey("action"))
                context.HttpContext.Items.Add("action", values["action"]);


            await System.Console.Out.WriteLineAsync("AsyncActionFilter: " + context.HttpContext.Request.Path.Value);

            // do something before the action executes
            await next();
            // do something after the action executes
            

        }
    }
}