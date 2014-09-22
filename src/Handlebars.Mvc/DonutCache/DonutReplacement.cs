using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace HandlebarsViewEngine
{
    class FakeView : IView
    {
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    internal class DonutCacheProvider
    {
        private static readonly Regex PartialAction = new Regex(@"(####donut:(.+)####)", RegexOptions.IgnoreCase);

        public string Replace(HttpContextBase httpContext, ControllerContext controllerContext, string input)
        {
            Stopwatch sw = Stopwatch.StartNew();

            //Use HtmlHelper to render partial view to fake context  
            var html = new HtmlHelper(new ViewContext(controllerContext,
                                                      new FakeView(),
                                                      new ViewDataDictionary(),
                                                      new TempDataDictionary(),
                                                      new StringWriter()),
                                                      new ViewPage());

            // todo:
            // potentially make this a parallel operation
            foreach (Match m in PartialAction.Matches(input))
            {
                var split = m.Value.Split(':');

                string action = split[2].TrimEnd('#');
                string controller = controller = split[1];
                object routeValues = new { area = "" };
                if (m.Groups["area"].Success)
                {
                    string area = m.Groups["area"].Value;
                    if (!string.IsNullOrEmpty(area))
                    {
                        routeValues = new { area };
                    }
                }

                using (var ms = new MemoryStream())
                using (var tw = new StreamWriter(ms))
                {
                    html.ViewContext.Writer = tw;

                    Stopwatch actionSw = Stopwatch.StartNew();

                    // use the extension method to get this happening
                    html.RenderAction(action, controller, routeValues);

                    actionSw.Stop();
                    // Trace.Write(string.Format("Donut hole: {0} took {1:F2} ms.", action, actionSw.Elapsed.TotalMilliseconds));

                    tw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);

                    using (var sr = new StreamReader(ms))
                    {
                        input = input.Replace(m.Value, sr.ReadToEnd());
                    }
                }
            }

            sw.Stop();

            httpContext.Items["trace"] = ((httpContext.Items["trace"] == null) ? "" : httpContext.Items["trace"].ToString()) +
                                          string.Format("Filled Donut Hole in {0:F2}ms\n", sw.Elapsed.TotalMilliseconds);

            return input;
        }

    }
}
