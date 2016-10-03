using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;


using Newtonsoft.Json;

namespace Handlebars.WebApi
{
    public sealed class HandlebarsMediaTypeFormatter : IOutputFormatter
    {
        #region // Constructor // 

        public HandlebarsMediaTypeFormatter(IHandlebarsTemplate template,
                                            IRequestFormatter formatter,
                                            Lazy<HandlebarsActionExecutor> executor)
            : base()
        {            
            this._template = template;
            this._formatter = formatter;
            this._executor = executor;  
        }

        #endregion

        #region // Dependency Injection //
        private readonly Lazy<HandlebarsActionExecutor> _executor; 
        private readonly IHandlebarsTemplate _template;  
        private readonly IRequestFormatter _formatter; 
        #endregion


        public void Test(HttpContext httpContext, IRouter router, ActionSelector actionSelector)
        {
            /*
            httpContext.Features.Select<RoutingFeature>();

            RouteContext context = new RouteContext(httpContext);
            context.RouteData = new RouteData();

            // context.RouteData.Values.Add("area", moduleContext.ModuleInfo.Name);
            // context.RouteData.Values.Add("pageModuleId", moduleContext.PageModuleId);
            context.RouteData.Values.Add("controller", "home");
            context.RouteData.Values.Add("action", "get");
            context.RouteData.PushState(router, null, null);

            var actionDescriptor = actionSelector.SelectBestCandidate(conext, )
            if (actionDescriptor == null)
                throw new NullReferenceException("Action cannot be located, please check whether module has been installed properly");

            var moduleActionContext = new ActionContext(actionContext.HttpContext, context.RouteData, actionDescriptor);

            var invoker = moduleInvokerProvider.CreateInvoker(moduleActionContext, actionDescriptor as ControllerActionDescriptor);
            var result = await invoker.InvokeAction() as ViewResult; null)
            */
        }


        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        { 
            return true;
        }

        private static readonly Regex MasterRegex = new Regex(@"(####master:(.+)####)", RegexOptions.IgnoreCase);
        private static readonly Regex SectionRegex = new Regex(@"(<!--####section:(.+)####-->)", RegexOptions.IgnoreCase);

        private class SectionData
        {
            public string Key
            {
                get;
                set;
            }

            public int NameLength
            {
                get;
                set;
            }

            public int Start
            {
                get;
                set;
            }

            public int Stop
            {
                get;
                set;
            }

            public string Contents
            {
                get;
                set;
            }
        }

        public StringBuilder FillSectionData(string contents, string json)
        {
            var sectionMatches = SectionRegex.Matches(contents);
            var sections = new Dictionary<string, SectionData>();
             
            foreach (Match match in sectionMatches)
            {
                // extract the matched sections into variables 
                var sectionData = match.Value.Split(':');
                var operation = sectionData[1];
                var name = sectionData[2].TrimEnd('#', '-', '>');

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
                return new StringBuilder(contents, contents.Length * 2);

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
            string master = _template.Render(masterPath, json);

            // recycle variable for efficiency
            sectionMatches = SectionRegex.Matches(master);

            // foreach section in the master, 
            // replace the section with the contents from the template
            // if the sections don't exist then leave them because there
            // might be default content

            var masterSections = new Dictionary<string, SectionData>();
            foreach (Match match in sectionMatches)
            {
                // extract the matched sections into variables
                var sectionData = match.Value.Split(':');
                var operation = sectionData[1];
                var name = sectionData[2].TrimEnd('#', '-', '>'); 

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

            // use a pesamistic estimate for the length of the string builder (considering we might get donuts later
            var sb = new StringBuilder(master, (master.Length + sections.Sum(_ => _.Value.Contents.Length)) * 2);

            var replacement = masterSections.OrderByDescending(_ => _.Value.Stop);
            foreach (KeyValuePair<string, SectionData> kvp in replacement)
            {
                if (sections.ContainsKey(kvp.Key))
                {                    
                    sb.Remove(masterSections[kvp.Key].Start, masterSections[kvp.Key].Stop - masterSections[kvp.Key].Start);
                    sb.Insert(masterSections[kvp.Key].Start, "\n" + sections[kvp.Key].Contents + "\n");
                }
            }

            return sb;
        }

        private string GetView(HttpContext context)
        {
            // todo: optimise
            var view = "";

            if (context.Items.ContainsKey("hb-view"))
                return context.Items["hb-view"] as string;

            view += context.Items["controller"];
            view += "/";
            view += context.Items["action"];

            // if (actionDescriptor.ControllerDescriptor.Properties.ContainsKey("hb-prefix"))
            //     view = (actionDescriptor.ControllerDescriptor.Properties["hb-prefix"] as string) + "/" + view;

            // context.ObjectType.

            return view;
        }

        public async Task WriteAsync(OutputFormatterWriteContext context)
        {
            // 1. Get controller from route (with DI done)

            // 2. Invoke action from controller 

            // - Action can be raw object, does not need to apply type format,
            //   as we can do that here

            var view = GetView(context.HttpContext);             
            var json = _formatter.GetContext(context.HttpContext.Request, context.Object);
            string r = _template.Render(view, json);
            
            StringBuilder html;

            if (context.HttpContext.Items.ContainsKey("donut"))
            {
                // detect any master or sections and fill them
                html = new StringBuilder(r);
            }
            else
            {
                html = FillSectionData(r, json);
                
                // todo:
                // hooks for adding to output cache
                                
                html = await FillDonutData(html, context.HttpContext.Request);
            }
          


            string output, contentType;
            if (context.HttpContext.Items.ContainsKey("hb-as-javascript"))
            {
                var lines = html.ToString()
                                .Split(new[] { "\n\r", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

                var sb = new StringBuilder();
                foreach (var line in lines)
                {
                    sb.Append("document.write('" +
                              HandlebarsUtilities.ToJavaScriptString(line)
                              + "');");
                }
                output = sb.ToString();
                contentType = "application/javascript";
            }
            else
            {
                contentType = "text/html";
                output = html.ToString();
            }

            context.HttpContext.Response.ContentType = contentType;

            using (var writer = context.WriterFactory(context.HttpContext.Response.Body, Encoding.UTF8))
            {
                await writer.WriteAsync(output);
                await writer.FlushAsync();
            }
        }

        public async Task<StringBuilder> FillDonutData(StringBuilder html, HttpRequest original)
        {
            var donuts = new List<SectionData>();
            
            // detect the donuts
            SectionData section = new SectionData();
            int index = html.IndexOf("####donut:", 0, false);
            int length = 4;
            while (index != -1)
            {
                length = html.IndexOf("####", index + 10, false) - index - 10;
                var key = html.ToString(index + 10, length);

                section.Key = key;
                section.Start = index;
                section.NameLength = length;
                section.Stop = index + length + 14;
                donuts.Add(section);

                if (index + length > html.Length) break;
                section = new SectionData();
                index = html.IndexOf("####donut:", index + length, false);
            }
            
            // execute any donuts
            if (donuts.Count > 0)
            {
                // move backwards through the donuts 
                
                for(int i = donuts.Count; i > 0; i--)
                {
                    var kvp = donuts[i - 1];

                    string url = kvp.Key;

                    // copy revevant details to new context
                    var features = new FeatureCollection(original.HttpContext.Features); 
                    features.Set<IItemsFeature>(new ItemsFeature()); 
                    var http = new DefaultHttpContext(features);
                    
                    foreach (var header in original.Headers)
                        http.Request.Headers[header.Key] = header.Value;

                    http.Items["donut"] = true;
                    if (original.HttpContext.Items.ContainsKey("experiment"))
                        http.Items.Add("experiment", original.HttpContext.Items["experiment"]);

                    http.Request.Path = url;
                    http.User = original.HttpContext.User;

                    IActionResult result = await _executor.Value.ExecuteAsync(http, url);
                    string value = $"<!-- {kvp.Key} -->";

                    var view = GetView(http);

                    if (result is OkObjectResult)
                    {
                        var ok = ((OkObjectResult)result);                        
                        var j = _formatter.GetContext(http.Request, ok.Value);
                        value = _template.Render(view, j);                        
                    }

                    html.Remove(kvp.Start, kvp.Stop - kvp.Start);
                    html.Insert(kvp.Start, value);
                }                 
            }            
            
            return html;
        } 
          
        
    }
}
