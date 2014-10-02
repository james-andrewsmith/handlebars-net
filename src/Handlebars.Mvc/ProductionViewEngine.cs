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


    public sealed class ProductionViewEngine : IViewEngine
    {

        #region // Constructors //
        public ProductionViewEngine(ITemplateProvider templateProvider)
            : this(templateProvider, new DefaultViewSerializer())
        {

        }

        public ProductionViewEngine(ITemplateProvider templateProvider,
                                    IViewContextSerializer viewContextSerializer)
            : base()
        {
            _handlebars = new Dictionary<string, HandleBars>();
            _viewContextSerializer = viewContextSerializer;
            _templateProvider = templateProvider;
        }
        #endregion 

        #region // Properties //

        /// <summary>
        /// Should handlebars cache the template or reload it each time.
        /// </summary>
        public bool Cache
        {
            get;
            private set;
        }

        public bool IncludeAreaInPrefix = true;
        public bool IncludeControllerInPrefix = true;

        public ITemplateProvider _templateProvider;
        private Dictionary<string, HandleBars> _handlebars;

        #endregion

        private object _sync = new object();

        private HandleBars GetHandlebars(string git)
        {
            // could be causing corrupt templates
            // if (_handlebars.ContainsKey(git))
            //     return _handlebars[git];

            lock(_sync)
            {
                if (!_handlebars.ContainsKey(git))
                {
                    _handlebars.Add(git, new HandleBars());


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
                            _handlebars[git].AddHelpers(content);
                        }
                    }
                }

                return _handlebars[git];
            }
            
        }

        public List<string> ListWithPrefix(string prefix)
        {
            throw new NotImplementedException();
            // return _handlebars.Templates()
            //                   .Where(_ => _.StartsWith(prefix))
            //                   .ToList();
        }

        private IViewContextSerializer _viewContextSerializer;

        /// <summary>
        /// Remove all prefixes with this prefix (ie: a git version)
        /// </summary>
        /// <param name="prefix"></param>
        public void RemoveWithPrefix(string prefix)
        {
            throw new NotImplementedException();
            // var templates = _handlebars.Templates()
            //                            .Where(_ => _.StartsWith(prefix))
            //                            .ToList();
            // 
            // foreach (string template in templates)
            //     _handlebars.Delete(template);            
        }

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            // find the view 
            var git = controllerContext.HttpContext.Application["x-git"] as string;

            var view = new HandlebarsView(GetHandlebars(git),
                                          _templateProvider, 
                                          _viewContextSerializer, 
                                          controllerContext.Controller.GetType().Name.Replace("Controller", "") + "\\" + partialViewName + ".handlebars",
                                          git,
                                          true);

            var ver = new ViewEngineResult(view, this);
            return ver;
        }

        public AggregateException Preload(string git)
        {
            var exceptions = new List<Exception>();

            var handlebars = GetHandlebars(git);
            var list = _templateProvider.List(git);
            foreach (var item in list)
            {
                try
                {
                    var view = new HandlebarsView(GetHandlebars(git),
                                                  _templateProvider,
                                                  _viewContextSerializer,
                                                  item,
                                                  git,
                                                  true);

                    view.Preload(git);
                }
                catch(Exception exp)
                {
                    exceptions.Add(exp);
                }
            }

            return new AggregateException(exceptions);
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
             
            var path = (IncludeAreaInPrefix && controllerContext.RouteData.DataTokens.ContainsKey("area") ? controllerContext.RouteData.DataTokens["area"] + "\\" : string.Empty) +
                        (IncludeControllerInPrefix ? controllerContext.RouteData.Values["controller"] + "\\" : string.Empty) +
                        viewName +
                        ".handlebars";

            var git = controllerContext.HttpContext.Application["x-git"] as string;

            lock (_sync)
            {
                var view = new HandlebarsView(GetHandlebars(git),
                                                _templateProvider,
                                                _viewContextSerializer,
                                                path.ToLower(),
                                                git,
                                                true);

                return new ViewEngineResult(view, this);
            }
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {            
            // throw new NotImplementedException();
        }

    }
}
