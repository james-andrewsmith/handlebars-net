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
using Microsoft.AspNetCore.Routing;

using Microsoft.Extensions.Logging;

using Wire;

namespace Handlebars.WebApi
{
    public sealed class HandlebarsMediaTypeFormatter : IOutputFormatter
    {
        #region // Constructor // 

        public HandlebarsMediaTypeFormatter(IHandlebarsTemplate template,
                                            IRequestFormatter formatter,
                                            Lazy<HandlebarsActionExecutor> executor,
                                            IStoreOutputCache storeOutput,
                                            Lazy<ILogger<HandlebarsMediaTypeFormatter>> logger)
            : base()
        {            
            this._template = template;
            this._formatter = formatter;
            this._executor = executor;
            this._storeOutput = storeOutput;
            this._logger = logger;


            // Setup Wire for fastest performance
            var types = new[] {
                    typeof(OutputCacheItem),
                    typeof(SectionData)
                };

            this._serializer = new Serializer(new SerializerOptions(knownTypes: types));
            this._ss = _serializer.GetSerializerSession();
            this._ds = _serializer.GetDeserializerSession();
        }

        #endregion

        #region // Dependency Injection //
        private readonly Lazy<ILogger<HandlebarsMediaTypeFormatter>> _logger;
        private readonly Lazy<HandlebarsActionExecutor> _executor; 
        private readonly IHandlebarsTemplate _template;  
        private readonly IRequestFormatter _formatter;
        private readonly IStoreOutputCache _storeOutput;

        private readonly Serializer _serializer;
        private readonly SerializerSession _ss;
        private readonly DeserializerSession _ds;
        #endregion


        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {                        
            if (context is OutputFormatterWriteContext)
            {
                var writeContext = context as OutputFormatterWriteContext;
                return writeContext.HttpContext.Items.ContainsKey("formatter");
            }
            return false;
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
         
        internal static string GetView(HttpContext context)
        {
            // todo: 
            // Check if any controllers need/use the hb-prefix (if not remove this)
            // if (actionDescriptor.ControllerDescriptor.Properties.ContainsKey("hb-prefix"))
            //     view = (actionDescriptor.ControllerDescriptor.Properties["hb-prefix"] as string) + "/" + view;

            if (context.Items.ContainsKey("hb-view"))
            {
                return context.Items["hb-view"] as string;
            }
            else
            {
                var prefix = context.Items.ContainsKey("hb-area") ? $"{context.Items["hb-area"]}/" : string.Empty;                                                                
                var routing = context.Features.Get<IRoutingFeature>();
                var routeData = routing.RouteData.Values;

                if (routeData.ContainsKey("controller") &&
                    routeData.ContainsKey("action"))
                    return $"{prefix}{routeData["controller"]}/{routeData["action"]}";
                else if (routeData.ContainsKey("controller"))
                    return $"{prefix}{routeData["controller"]}";
                else if (routeData.ContainsKey("action"))
                    return $"{prefix}{routeData["action"]}";
                else
                    return $"{prefix}{context.Request.Path.Value.ToLower()}";
            }                 
        }

        public async Task WriteAsync(OutputFormatterWriteContext context)
        {
            // Get the view template 
            var view = GetView(context.HttpContext);             
            var json = _formatter.GetContext(context.HttpContext.Request, context.Object);

            // Return the JSON 
            if ((string)context.HttpContext.Items["formatter"] == "json")
            {
                if (context.HttpContext.Items.ContainsKey("cache"))
                    context.HttpContext.Items["cache"] = json;

                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.Headers["x-template"] = view;

                // Buffer to the client and dispose all streams etc
                using (var writer = context.WriterFactory(context.HttpContext.Response.Body, Encoding.UTF8))
                {
                    await writer.WriteAsync(json);
                    await writer.FlushAsync();
                }

                // Do not proceed any further
                return;
            }

            // Return the HTML
            string render = _template.Render(view, json);            
            StringBuilder html = new StringBuilder(render);

            // prevent nested donuts
            if (!context.HttpContext.Items.ContainsKey("donut"))
            {
                html = FillSectionData(html, json);

                // todo:
                // hooks for adding to output cache
                if (context.HttpContext.Items.ContainsKey("cache"))
                    context.HttpContext.Items["cache"] = html.ToString();
                   
                // While playing with sections comment this out
                html = await FillDonutData(html, context.HttpContext);
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

            // Buffer to the client and dispose all streams etc
            using (var writer = context.WriterFactory(context.HttpContext.Response.Body, Encoding.UTF8))
            {
                await writer.WriteAsync(output);
                await writer.FlushAsync();
            }
        }

        public async Task<StringBuilder> FillDonutData(StringBuilder html, HttpContext context)
        {
            HttpRequest original = context.Request;
            var donuts = new List<SectionData>();
            
            // detect the donuts
            SectionData section = new SectionData();
            int index = html.IndexOf("####donut:", 0, false);
            int length = 4;
            while (index != -1)
            {
                length = html.IndexOf("####", index + 10, false) - index - 10;
                var key = html.ToString(index + 10, length);

                section.Key = key.StartsWith("/", StringComparison.Ordinal) ? key : $"/{key}";
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
                // Cache the donut meta data with the template, 
                // this allows the output cache to execute donuts
                // directly and not perform any text scans
                if (context.Items.ContainsKey("cache"))
                    context.Items["cache-donut"] = donuts;

                // To avoid enumator collision issues with the mutliple donuts
                // cache the keys in a new object up here and use index access
                var keys = original.Headers.Keys.ToArray();
                var tasks = new Task<IActionResult>[donuts.Count];
                var contexts = new DefaultHttpContext[donuts.Count];                    

                // move backwards through the donuts 
                for (int i = 0; i < donuts.Count; i++)
                {
                    var kvp = donuts[i];
                    string url = kvp.Key; 

                    _logger.Value.LogInformation("Starting donut {url}", url);

                    // copy revevant details to new context
                    var features = new FeatureCollection(original.HttpContext.Features);
                    features.Set<IItemsFeature>(new ItemsFeature());
                    var http = new DefaultHttpContext(features);

                    for(var k = 0; k < keys.Length; k++)
                        http.Request.Headers[keys[k]] = original.Headers[keys[k]];                        
                        
                    http.Items["donut"] = true;
                    if (original.HttpContext.Items.ContainsKey("experiment"))
                        http.Items.Add("experiment", original.HttpContext.Items["experiment"]);
                    if (original.HttpContext.Items.ContainsKey("x-account"))
                        http.Items.Add("x-account", original.HttpContext.Items["x-account"]);

                    http.Request.Path = url;
                    http.User = original.HttpContext.User;

                    _logger.Value.LogInformation("Executing donut {url}", url);
                    contexts[i] = http;
                    tasks[i] = _executor.Value.ExecuteAsync(http, url);
                }

                // Wait for all the donuts to complete
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp.Message);
                    Console.WriteLine(exp.StackTrace);
                }

                // Actually fill the donut hole 
                for (int i = (donuts.Count - 1); i >= 0; i--)
                {
                    var kvp = donuts[i];
                    string url = kvp.Key;

                    string value = $"<!-- {kvp.Key} -->";
                    string contentType = "text/html";
                    int statusCode = 200;

                    var task = tasks[i];
                    var http = contexts[i];
                    var result = task.Result;

                    // Check for a response which is from the cache
                    if (result != null)
                    {
                        if (result is ContentResult)
                        {
                            var cr = ((ContentResult)result);
                            value = cr.Content;
                            statusCode = cr.StatusCode == null ? 200 : (int)cr.StatusCode;
                            contentType = cr.ContentType;
                        }
                        else
                        {
                            var view = GetView(http);
                            if (result is OkObjectResult)
                            {
                                var ok = ((OkObjectResult)result);
                                var j = _formatter.GetContext(http.Request, ok.Value);
                                value = _template.Render(view, j);
                                statusCode = ok.StatusCode == null ? 200 : (int)ok.StatusCode;
                                contentType = "text/html";
                            }
                        }

                        // We know this wasn't from a cache hit, so add this to the cache
                        if (!http.Items.ContainsKey("cache-hit") &&
                            http.Items.ContainsKey("cache-key"))
                        {
                            using (var ms = new MemoryStream())
                            {
                                _serializer.Serialize(new OutputCacheItem
                                {
                                    ContentType = contentType,
                                    StatusCode = statusCode,
                                    Content = value
                                }, ms, _ss);

                                await _storeOutput.Set(Convert.ToString(http.Items["cache-key"]), ms.ToArray());
                            }
                        }
                    }

                    // Fill the donut hole using stringbuilder
                    html.Remove(kvp.Start, kvp.Stop - kvp.Start);
                    html.Insert(kvp.Start, value);
                    _logger.Value.LogInformation("Finished donut {url}", url);
                }
                                
            }            
            
            return html;
        } 
          
        
    }
}
