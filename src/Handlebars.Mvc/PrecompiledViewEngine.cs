using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Reflection;

using System.IO;
using handlebars.cs;

namespace HandlebarsViewEngine
{

    /// <summary>
    /// </summary>
    public sealed class PrecompiledViewEngine : IViewEngine
    {

        public PrecompiledViewEngine(IViewContextSerializer viewContextSerializer, 
                                     string javascript)
            : base()
        {

            _viewContextSerializer = viewContextSerializer;
            // _templateProvider = new FakeTemplateProvider(); 

            var handlebars = new HandleBarsClearScript();
            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly.GetManifestResourceNames()
                                .Where(_ => _.EndsWith(".js"))
                                .ToList();

            foreach (var resource in names)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resource))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    handlebars.Run(content);
                }
            }
            
            // var precog = System.IO.File.ReadAllText(@"C:\Home\Development\archfashion\platform-hybrid\com.archfashion.platform.cli\bin-12\all-template.js");
            handlebars.Run(javascript);

            _handlebars = handlebars;
        }

        private IViewContextSerializer _viewContextSerializer;
        private HandleBarsClearScript _handlebars;     

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            var path = controllerContext.Controller.GetType().Name.Replace("Controller", "") +
                       "/" +
                       partialViewName;

            path = path.ToLower();

            var view = new HandlebarsViewLite(_handlebars,
                                              _viewContextSerializer,
                                              path);

            var ver = new ViewEngineResult(view, this);
            return ver;
        }

        public bool IncludeAreaInPrefix = true;
        public bool IncludeControllerInPrefix = true;
        public bool Cache
        {
            get { return false; }
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            
            var path = (IncludeAreaInPrefix && controllerContext.RouteData.DataTokens.ContainsKey("area") ? controllerContext.RouteData.DataTokens["area"] + "/" : string.Empty) +
                       (IncludeControllerInPrefix ? controllerContext.RouteData.Values["controller"] + "/" : string.Empty) +
                        viewName;

            if (viewName.StartsWith("../"))
                path = viewName.Substring(3);

            path = path.ToLower();
                        
            var view = new HandlebarsViewLite(_handlebars,
                                              _viewContextSerializer,
                                              path);                       

            return new ViewEngineResult(view, this); 
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {

        }
    }
}
