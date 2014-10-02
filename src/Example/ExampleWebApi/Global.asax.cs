using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

using Ninject;
using Ninject.Modules;

using Handlebars;
using Handlebars.WebApi;

namespace ExampleWebApi
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            
            GlobalConfiguration.Configuration.Formatters.Insert(0, new HandlebarsMediaTypeFormatter(GlobalConfiguration.Configuration.Routes,
                                                                                                    HandlebarsFactory.CreateTemplate(),
                                                                                                    new StandardRequestFormatter()));

            GlobalConfiguration.Configuration
                               .Formatters
                               .JsonFormatter
                               .MediaTypeMappings
                               .Add(new QueryStringMapping("x-format", "json", new MediaTypeHeaderValue("application/json")));

            // var kernel = new StandardKernel();
            // 
            // kernel.Bind<IGridClient>()
            //       .To<GridClusterClient>()
            //       .InSingletonScope();


            // Implement per controller configuration for: 
            // [HandlebarsView]
            // -> otherwise we will see actual API controllers returning HTML (which is so so wrong)
            // http://blogs.msdn.com/b/jmstall/archive/2012/05/11/per-controller-configuration-in-webapi.aspx

        }
    }
}