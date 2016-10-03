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
         

        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        { 
            return true;
        }
         
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

        public StringBuilder FillSectionData(StringBuilder html, string json)
        {
            // detect the master section tag 
            int masterIndex = html.IndexOf("####master:", 0, false);

            // when it's not present this means there will be no 
            // section replacement, so just return the html
            if (masterIndex == -1)
                return html;

            // Examples:
            // ####master:default.handlebars####
            // ####master:default-oembed.handlebars####
            int masterIndexFinish = html.IndexOf("####", masterIndex + 11, false);
            string masterPath = html.ToString(masterIndex + 11, masterIndexFinish - masterIndex - 11);
            string masterHtml = _template.Render(masterPath, json);

            // Remove the master tag from view
            html.Remove(masterIndex, masterIndexFinish + 4);

            var master = new StringBuilder(masterHtml);
            var sections = new List<SectionData>();

            // Find each section the master template
            // Example: 
            // <!--####section:start:head####-->
            // <!--####section:stop:head####-->

            SectionData section = new SectionData();
            int index = master.IndexOf("<!--####section:start:", 0, false);
            int length = 4;
            while (index != -1)
            {
                length = master.IndexOf("####-->", index + 22, false) - index - 22;
                var key = master.ToString(index + 22, length);

                section.Key = key;
                section.Start = index;
                section.NameLength = length;
                section.Stop = index + length + 29;
                
                // Find the section start / finish in view template                
                var contentStart = html.IndexOf($"<!--####section:start:{section.Key}####-->", 0, false);
                if (contentStart > -1)
                {
                    var contentStop = html.IndexOf($"<!--####section:stop:{section.Key}####-->", contentStart, false);
                    section.Contents = html.ToString(contentStart, contentStop - contentStart + length + 28) + "\n";

                    // remove the section from the html, this allows the content section 
                    // to not declare itself (or be lazy), lower we assume any remaining 
                    // content to be part of the content section 
                    html.Remove(contentStart, contentStop - contentStart + length + 28);
                }

                sections.Add(section);

                if (index + length > master.Length) break;
                section = new SectionData();
                index = master.IndexOf("<!--####section:start:", index + length, false);
            }

            // Go backwards through the sections 
            for (int i = sections.Count; i > 0; i--)
            {
                var kvp = sections[i - 1];
                
                // Ensure the contents section can be found (even when no tags are used)
                if (string.Equals(kvp.Key, "contents", StringComparison.OrdinalIgnoreCase) && 
                    string.IsNullOrEmpty(kvp.Contents))
                {
                    // because we have been calling "remove" on the html string builder 
                    // we can assume whatever is left to be the "contents" tag
                    kvp.Contents = html.ToString();
                }                

                // Insert the sections from the view template into the master
                master.Remove(kvp.Start, kvp.Stop - kvp.Start + kvp.NameLength + 29);
                master.Insert(kvp.Start, kvp.Contents);
            }

            return master;
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

            string render = _template.Render(view, json);            
            StringBuilder html = new StringBuilder(render);

            // prevent nested donuts
            if (!context.HttpContext.Items.ContainsKey("donut"))
            {
                html = FillSectionData(html, json);
                
                // todo:
                // hooks for adding to output cache
                   
                // While playing with sections comment this out
                // html = await FillDonutData(html, context.HttpContext.Request);
            }
          

            string output, contentType;

            // Allow output to render as javascript inside a document.write
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
