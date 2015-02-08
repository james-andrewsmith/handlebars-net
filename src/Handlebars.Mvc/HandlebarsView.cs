using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

using handlebars.cs;

namespace HandlebarsViewEngine
{

    public class HandlebarsView : IView
    {
        private IViewContextSerializer _viewContextSerializer;
        private ITemplateProvider _templateProvider;
        private HandleBars _handlebars;
        private string _version;
        private string _path;
        private string _json;
        private bool _debug;

        public HandlebarsView(HandleBars handlebars,
                              ITemplateProvider templateProvider,
                              string path,
                              bool debug)
            : this(handlebars,
                   templateProvider,
                   new DefaultViewSerializer(),
                   path,
                   "",
                   debug)
        {
        }

        public HandlebarsView(HandleBars handlebars,
                              ITemplateProvider templateProvider, 
                              string path,
                              string version,
                              bool debug) : this(handlebars, 
                                                 templateProvider, 
                                                 new DefaultViewSerializer(),
                                                 path, 
                                                 version,
                                                 debug)
        {
        }

        public HandlebarsView(HandleBars handlebars,
                              ITemplateProvider templateProvider, 
                              IViewContextSerializer viewContextSerializer,                  
                              string path,
                              string version,
                              bool debug)
        {
            _handlebars = handlebars;
            _viewContextSerializer = viewContextSerializer;
            _templateProvider = templateProvider;
            _debug = true;
            _path = path;
            _version = version;
        }

        #region IView Members

        public void Preload(string git)
        {
            _path = _path.Replace(git + "/template/", "");
            _json = _viewContextSerializer.Serialize(null);
            string html = string.Empty,
                   template = string.Empty;

            try
            {
                if (_debug || !_handlebars.Exists(_path))
                {
                    template = _templateProvider.Get(_version, _path);
                    if (string.IsNullOrEmpty(template))
                        return;

                    EnsurePartialAvailable(template);
                    html = _debug ? _handlebars.SingleRun(template, _json) :
                                    _handlebars.Run(_path, template, _json);
                }
                else
                {
                    html = _handlebars.Run(_path, _json);
                }
            }
            catch (Exception exp)
            {
                throw new Exception("Error in template: " + _path + "\n" + template, exp);
            }
             
            html = FillSectionData(html, null);            
        }

        public void Render(ViewContext viewContext, System.IO.TextWriter writer)
        {            
            _json = _viewContextSerializer.Serialize(viewContext);

            string html = string.Empty,
                   template = string.Empty;
            
            try
            {
                if (_debug || !_handlebars.Exists(_path))
                {
                    template = _templateProvider.Get(_version, _path);
                    if (string.IsNullOrEmpty(template))
                        return;

                    EnsurePartialAvailable(template);
                    html = _debug ? _handlebars.SingleRun(template, _json) :
                                    _handlebars.Run(_path, template, _json);
                }
                else 
                {
                    html = _handlebars.Run(_path, _json); 
                }                 
            }
            catch (Exception exp)
            {                
                throw new Exception("Error in template: " + _path + "\n" + template, exp);                
            }
            
            // so developer proxy can render the page
            // todo:
            // make this only use the top level template, if we have a child donut
            // template, that is driven by the top template and irrelevent
            if (!viewContext.HttpContext.Response.IsRequestBeingRedirected)
                viewContext.HttpContext.Response.AddHeader("x-template", _path.Replace("~/views", "").Replace(".hbs", "").Replace(".handlebars", "").ToLower());

            html = FillSectionData(html, viewContext.ViewData);
#if DEBUG
            html = FillDonutHoles(viewContext.HttpContext, viewContext.Controller.ControllerContext, html);
#endif

            //             
            writer.Write(html);
        }

        private void EnsurePartialAvailable(string template)
        {

            List<string> partialTemplateCheck = new List<string>();
            partialTemplateCheck.Add(template);
            for (int i = 0; i < partialTemplateCheck.Count; i++)
            {
                var partials = PartialRegex.Matches(partialTemplateCheck[i]);
                foreach (Match partial in partials)
                {
                    var value = partial.Value;

                    var name = value.Substring(value.IndexOf("{{> ") + 4, value.IndexOf("}}") - value.IndexOf("{{> ") - 4);
                    if (name.IndexOf(' ') > -1)
                    {
                        name = name.Substring(0, name.IndexOf(' '));
                    }
                     
                    if (!_handlebars.PartialExists(name))
                        {
                            var partialTemplate = _templateProvider.Get(_version, name + ".handlebars");
                            if (string.IsNullOrEmpty(partialTemplate))
                                throw new Exception("Partial " + name + " is empty, found in: \n" + template);

                            partialTemplateCheck.Add(partialTemplate);
                            _handlebars.Partial(name, partialTemplate);                            
                        }
                    
                }
            }
        }

        #endregion

        #region // Donuts //
        private static readonly Regex DonutAction = new Regex(@"(####donut:(.+)####)", RegexOptions.IgnoreCase);
        private static readonly Regex PartialRegex = new Regex(@"({{> (.+)}})", RegexOptions.IgnoreCase);

        public string FillDonutHoles(HttpContextBase httpContext, ControllerContext controllerContext, string input)
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
            foreach (Match m in DonutAction.Matches(input))
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
                    Trace.Write(string.Format("Donut hole: {0} took {1:F2} ms.", action, actionSw.Elapsed.TotalMilliseconds));

                    tw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);

                    using (var sr = new StreamReader(ms))
                    {
                        input = input.Replace(m.Value, sr.ReadToEnd());
                    }
                }
            }

            sw.Stop();

            Trace.Write(string.Format("Fill Donut Hole took {0:F2} ms.", sw.Elapsed.TotalMilliseconds));

            return input;
        }
        #endregion

        private static readonly Regex MasterRegex = new Regex(@"(####master:(.+)####)", RegexOptions.IgnoreCase);
        private static readonly Regex SectionRegex = new Regex(@"(<!--####section:(.+)####-->)", RegexOptions.IgnoreCase);

        private class SectionData
        {
            public int NameLength;
            public int Start;
            public int Stop;
            public string Contents;
        }

        public string FillSectionData(string contents, ViewDataDictionary viewdata)
        {
            // extract other actions from the views.
            // -> header
            // -> footer
            // -> menu(s), admin or top
            // -> cart            
            // return Regex.Replace(contents, "\\{(.+)\\}", m => GetMatch(m, viewdata));

           
            // instead of just returning the template the template provider can 
            // return the sections, master and their positions?

            // ideally this "find what sections exist" only happens once per template
            // then we cache the "replace" tags and find the lastindex and replace 
            // working backwards so we don't need to rerun the regex


            
            var sectionMatches = SectionRegex.Matches(contents);
            var sections = new Dictionary<string, SectionData>();

            // int start = -1, stop = -1;

            foreach (Match match in sectionMatches)
            {
                var sectionData = match.Value.Split(':');
                var operation = sectionData[1];
                var name = sectionData[2].TrimEnd('#', '-', '>');

                // extract the matched sections into variables
                // match.Value.Split(':')[1]
                // match.Value.Split(':')[2]

                if (!sections.ContainsKey(name))
                    sections.Add(name, new SectionData() { NameLength = name.Length });

                switch (operation)
                {
                    case "start":
                        sections[name].Start = match.Index + match.Length;
                        break;
                    case "stop":
                        sections[name].Stop = match.Index;
                        sections[name].Contents = contents.Substring(sections[name].Start, sections[name].Stop - sections[name].Start).Trim(' ', '\n', '\t', '\r');
                        break;
                }
            }
            
            // find the master for this template
            // ###master            
            // todo:
            // return an HTML error describing the missing
            var masterMatch = MasterRegex.Match(contents, 0);
            if (!masterMatch.Success)
                return contents;

            var removal = sections.Values.OrderByDescending(_ => _.Stop);
            foreach (SectionData sd in removal)
            {
                // <!--####section:start:####-->
                // <!--####section:stop:####-->
                int start = sd.Start - sd.NameLength - 29;
                int stop = sd.Stop + sd.NameLength + 28;
                contents = contents.Remove(start, stop - start);
            }


            // remove the master tag from the render pipeline
            contents = contents.Remove(masterMatch.Index, masterMatch.Length);

            // this logic is only needed if there is a master template with sections
            // any content not in a section will be automatically assumed as the 
            // "content" section and appended to it (if it was already created)
            if (!sections.ContainsKey("contents"))
            {
                sections.Add("contents", new SectionData { });
                sections["contents"].Contents = contents.Trim(' ', '\n', '\t', '\r');
            }

            var masterPath = masterMatch.Value.Split(':')[1].TrimEnd('#');
            masterPath = masterPath + ".handlebars";

            string master = string.Empty;

            try
            {
                if (_debug || !_handlebars.Exists(masterPath))
                {
                    master = _templateProvider.Get(_version, masterPath);

                    try
                    {
                        master = _debug ? _handlebars.SingleRun(master, _json) :
                                          _handlebars.Run(masterPath, master, _json);
                    }
                    catch(Exception e)
                    {
                        if (!_debug)
                            master = _handlebars.SingleRun(master, _json);
                        else
                            throw e;
                    }
                }
                else
                {
                    master = _handlebars.Run(masterPath, _json);                    
                }
            }
            catch (Exception exp)
            {
                throw new Exception("Error in template: " + _path + "\n" + master, exp);
            }

            // recycle variable for efficiency
            sectionMatches = SectionRegex.Matches(master);

            // foreach section in the master, 
            // replace the section with the contents from the template
            // if the sections don't exist then leave them because there
            // might be default content

            var masterSections = new Dictionary<string, SectionData>();
            foreach (Match match in sectionMatches)
            {
                var sectionData = match.Value.Split(':');
                var operation = sectionData[1];
                var name = sectionData[2].TrimEnd('#', '-', '>');

                // extract the matched sections into variables
                // match.Value.Split(':')[1]
                // match.Value.Split(':')[2]

                if (!masterSections.ContainsKey(name))
                    masterSections.Add(name, new SectionData() { NameLength = name.Length });

                switch (operation)
                {
                    case "start":
                        masterSections[name].Start = match.Index + match.Length;
                        break;
                    case "stop":
                        masterSections[name].Stop = match.Index;                            
                        break;
                }
            }

            var replacement = masterSections.OrderByDescending(_ => _.Value.Stop);
            foreach (KeyValuePair<string, SectionData> kvp in replacement)
            {
                if (sections.ContainsKey(kvp.Key))
                {
                    master = master.Remove(masterSections[kvp.Key].Start, masterSections[kvp.Key].Stop - masterSections[kvp.Key].Start);
                    master = master.Insert(masterSections[kvp.Key].Start, "\n" + sections[kvp.Key].Contents + "\n");
                }
            }
                     
            return master;
        }
         
    }
}
