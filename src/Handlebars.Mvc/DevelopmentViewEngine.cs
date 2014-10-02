using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using handlebars.cs;

using System.IO;
using System.Web.Mvc;

namespace HandlebarsViewEngine
{


    public sealed class DevelopmentViewEngine : IViewEngine
    {

        #region // Constructors //
        public DevelopmentViewEngine(ITemplateProvider templateProvider)
            : this(templateProvider, new DefaultViewSerializer())
        {

        }

        public DevelopmentViewEngine(ITemplateProvider templateProvider,
                                     IViewContextSerializer viewContextSerializer)
            : base()
        {
            _viewContextSerializer = viewContextSerializer;
            _templateProvider = templateProvider;
            _handlebars = new HandleBars();
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
                    _handlebars.AddHelpers(content);
                }
            }
        }
        #endregion 

        #region // Properties //

        /// <summary>
        /// Should handlebars cache the template or reload it each time.
        /// </summary>
        public bool Cache
        {
            get { return false; }
        }

        private HandleBars _handlebars;
        public bool IncludeAreaInPrefix = true;
        public bool IncludeControllerInPrefix = true;

        private ITemplateProvider _templateProvider;         

        #endregion


        public List<string> ListWithPrefix(string prefix)
        {
            return _handlebars.Templates()
                             .Where(_ => _.StartsWith(prefix))
                             .ToList();
        }

        private IViewContextSerializer _viewContextSerializer;

        /// <summary>
        /// Remove all prefixes with this prefix (ie: a git version)
        /// </summary>
        /// <param name="prefix"></param>
        public void RemoveWithPrefix(string prefix)
        {
            var templates = _handlebars.Templates()
                                      .Where(_ => _.StartsWith(prefix))
                                      .ToList();

            foreach (string template in templates)
                _handlebars.Delete(template);            
        }

         

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            // find the view 

            var view = new HandlebarsView(_handlebars, 
                                          _templateProvider, 
                                          _viewContextSerializer, 
                                          controllerContext.Controller.GetType().Name.Replace("Controller", "") + "\\" + partialViewName + ".handlebars", 
                                          "",
                                          true);

            
            var ver = new ViewEngineResult(view, this);
            return ver;
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {            
            var path = (IncludeAreaInPrefix && controllerContext.RouteData.DataTokens.ContainsKey("area") ? controllerContext.RouteData.DataTokens["area"] + "\\" : string.Empty) +
                       (IncludeControllerInPrefix ? controllerContext.RouteData.Values["controller"] + "\\" : string.Empty) + 
                       viewName + ".handlebars";

            var view = new HandlebarsView(_handlebars, 
                                          _templateProvider, 
                                          _viewContextSerializer,                                           
                                          path, 
                                          "",
                                          true);
            
            var ver = new ViewEngineResult(view, this);            
            return ver;
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        { 
            // throw new NotImplementedException();
        }

    }
}
