using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Handlebars;

namespace Handlebars.Proxy
{
    
    public sealed class ProxyStartup
    {

        #region // Constructor //
        public ProxyStartup(IHandlebarsEngine engine, IHandlebarsTemplate template)
        {
            _handlebars = engine;
            _template = template;

            HandlebarsConfiguration.Instance.TemplatePath = HandlebarsProxyConfiguration.Instance.Directory + "\\template";
        }
        #endregion

        #region // Dependency Injection //
        private readonly IHandlebarsEngine _handlebars;
        private readonly IHandlebarsTemplate _template;
        #endregion

        #region // Owin Entry Point //
        public void Configuration(IAppBuilder app)
        {
            app.Run(Invoke);
        }
        #endregion 

        // Invoked once per request.
        public Task Invoke(IOwinContext context)
        {
            using (var trace = new Trace(context.Request.Uri.PathAndQuery.ToString()))
            {
                string json, templateName, templateData;

                if (context.Request.Uri.PathAndQuery == "/favicon.ico")
                {
                    context.Response.StatusCode = 404;
                    return context.Response.WriteAsync(new byte[] { });
                }


                using (var client = new WebClient())
                {
                    try
                    {
                        var proxyUri = GetProxyUri(context.Request.Uri);
                        client.Headers["User-Agent"] = context.Request.Headers["User-Agent"];
                        var data = client.DownloadData(proxyUri);

                        if (context.Request.Uri.PathAndQuery.StartsWith("/api") ||
                            !client.ResponseHeaders["Content-Type"].Contains("application/json"))
                        {
                            context.Response.StatusCode = 200;
                            context.Response.ContentType = client.ResponseHeaders["Content-Type"];
                            return context.Response.WriteAsync(data);
                        }
                         
                        json = Encoding.UTF8.GetString(data);
                        var o = JObject.Parse(json);

                        o["debug"] = true;

                        // here we need to ensure that we are replacing the hostname with the configured local version
                        // this means any logic around hostnames uses this

                        // o["_config"]["cdn"] = "//" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.CdnPort;
                        o["_config"]["cdn"] = "//cdn.archfashion.dev";
                        o["_request"]["protocol"] = "http";
                        o["_request"]["gzip"] = false;
                        o["_request"]["fqdn"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                        o["_request"]["hostname"] = HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                        
                        json = o.ToString(Formatting.None);

                        templateName = client.ResponseHeaders["x-template"];
                        if (string.IsNullOrEmpty(templateName))
                        {
                            Console.Error.WriteLineAsync("No x-template header for URL: " + proxyUri.ToString());
                            return context.Response.WriteAsync("No x-template header for URL: " + proxyUri.ToString());
                        }

                        templateData = File.ReadAllText(Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                                     "\\template",
                                                                     templateName.Replace("/", "\\") + ".handlebars"));

                        // if this is file not found send a friendly message
                        using (var render = new Trace("render"))
                        {
                            // EnsurePartialAvailable(templateData);

                            var r = _template.Render(templateName, json);

                            var html = FillSectionData(r, json);

                            var donuts = new List<string>();

                            // detect the donuts
                            int index = html.IndexOf("####donut:", 0, false);
                            int length = 4;
                            while (index != -1)
                            {
                                length = html.IndexOf("####", index + 10, false) - index - 10;
                                donuts.Add(html.ToString(index + 10, length));
                                if (index + length > html.Length) break;
                                index = html.IndexOf("####donut:", index + length, false);
                            }

                            // execute any donuts
                            var sync = new object();

                            var tasks = new List<Task<KeyValuePair<string, string>>>(donuts.Count);
                            if (donuts.Count > 0)
                            {

                                // using (HttpMessageInvoker client = new HttpMessageInvoker(server))
                                // {
                                
                                HttpClient Client = new HttpClient();
                                foreach (var donut in donuts)
                                {                                    
                                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, GetProxyUri(new Uri("http://" + HandlebarsProxyConfiguration.Instance.Domain + "/" + donut))))
                                    {
                                        using (HttpResponseMessage response = Client.SendAsync(request, CancellationToken.None).Result)
                                        {
                                            if (response.IsSuccessStatusCode)
                                                lock (sync)
                                                    tasks.Add(response.Content
                                                                      .ReadAsStringAsync()
                                                                      .ContinueWith((_) =>
                                                                      {
                                                                          var doo = JObject.Parse(_.Result);


                                                                          doo["debug"] = true;

                                                                          // here we need to ensure that we are replacing the hostname with the configured local version
                                                                          // this means any logic around hostnames uses this

                                                                          // o["_config"]["cdn"] = "//" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.CdnPort;
                                                                          doo["_config"]["cdn"] = "//cdn.archfashion.dev";
                                                                          doo["_request"]["protocol"] = "http";
                                                                          doo["_request"]["gzip"] = false;
                                                                          doo["_request"]["fqdn"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                                                                          doo["_request"]["hostname"] = HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;

                                                                          return new KeyValuePair<string, string>(donut, _template.Render(donut, doo.ToString(Formatting.None)));
                                                                      }));
                                        }
                                    }
                                }
                                // }

                                Task.WaitAll(tasks.ToArray());
                            }

                            foreach(var task in tasks)
                            {
                                html.Replace("####donut:" + task.Result.Key + "####", 
                                             task.Result.Value);
                            }
                            
                            context.Response.Headers["cookie"] = client.ResponseHeaders["cookie"];
                            context.Response.ContentType = "text/html";

                            // make sure the temp partials are cleared
                            _handlebars.Clear();

                            return context.Response.WriteAsync(html.ToString());
                        }
                         



                    }
                    catch (Exception exp)
                    {
                        return context.Response.WriteAsync(context.Request.Uri.ToString() +
                                                     "\n" +
                                                     exp.Message +
                                                     "\n" +
                                                     exp.StackTrace);
                        Console.Error.WriteLineAsync(context.Request.Uri.ToString() +
                                                     "\n" +
                                                     exp.Message +
                                                     "\n" +
                                                     exp.StackTrace);
                    }

                    

                }
            }
            
        }

        

        internal static string ProxyExternalSite(string html)
        {
            int index = html.IndexOf("http://");

            List<string> replacement = new List<string>();

            while(index > 0)
            {
                var end = html.IndexOfAny(new char[] { ' ', '"', '\'', '\n', '\r', ')', ';' }, index);
                if (end > 0)
                {
                    replacement.Add(html.Substring(index, end - index));
                    index = end;
                }
                index = html.IndexOf("http://", index); 
            }

            // Hopefully not needed when combined with browser-sync.io
            /*
            replacement = replacement.Where(_ => _.EndsWith(".jpg") ||
                                                 _.EndsWith(".jpeg") ||
                                                 _.EndsWith(".js") ||
                                                 _.EndsWith(".gif") ||
                                                 _.EndsWith(".css") ||
                                                 _.EndsWith(".png"))
                                     .Where(_ => !_.Contains(HandlebarsProxyConfiguration.Instance.Hostname + ":"))
                                     .Where(_ => { Uri o; return Uri.TryCreate(_, UriKind.Absolute, out o); })
                                     .Select(_ => _)
                                     .Distinct()
                                     .ToList();

            foreach (var rpl in replacement)
            {
                var uri = new Uri(rpl);
                var prx = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.ExternalPort;
                prx += "?protocol=" + uri.Scheme + "&url=" + uri.DnsSafeHost + uri.PathAndQuery;
                html = html.Replace(rpl, prx);
            }*/
            
            // find every instance of "http://"

            // change links to use the proxy

            // http:// + Hostname + "?protocol=http&url=" + domain + path

            // 
            return html;
        }

        private string GetDevice(string ua)
        {
            if (string.IsNullOrEmpty(ua))
                ua = "";

            ua = ua.ToLower();
            string type = "desktop";

            if (ua.Contains("blackberry"))
                type = "mobile";

            if (ua.Contains("iphone"))
                type = "mobile";

            if (ua.Contains("ipad"))
                type = "tablet";

            if (ua.Contains("android"))
            {
                if (ua.Contains("mobile"))
                    type = "mobile";
                else
                    type = "tablet";
            }

            return type;
        }

        private Uri GetProxyUri(Uri uri)
        {
            var builder = new UriBuilder(uri);
            builder.Host = HandlebarsProxyConfiguration.Instance.Domain;
            builder.Port = 80;
            if (string.IsNullOrEmpty(builder.Query))
                builder.Query = "x-format=json";
            else
                builder.Query = builder.Query.TrimStart('?') + "&x-format=json";
            
            return builder.Uri;
        }

        private static readonly Regex PartialRegex = new Regex(@"({{> (.+)}})", RegexOptions.Compiled);
        private static readonly Regex DonutAction = new Regex(@"(####donut:(.+)####)", RegexOptions.Compiled);
        private static readonly Regex MasterRegex = new Regex(@"(####master:(.+)####)", RegexOptions.Compiled);
        private static readonly Regex SectionRegex = new Regex(@"(<!--####section:(.+)####-->)", RegexOptions.Compiled);

        public string FillDonutHoles(string input)
        { 

            // todo:
            // potentially make this a parallel operation
            
            foreach (Match m in DonutAction.Matches(input))
            {
                var split = m.Value.Split(':');

                string action = split[1].TrimEnd('#');
                string controller = controller = split[1];
                string area = "";

                if (m.Groups["area"].Success)
                {
                    area = m.Groups["area"].Value;                    
                }

                // Console.WriteLine("TODO: Fill Donut for: " + (string.IsNullOrEmpty(area) ? "" :  area + "\\") + controller + "\\" + action);

                using (var client = new WebClient())
                {
                    var data = client.DownloadData(GetProxyUri(new Uri( "http://" +
                                                                          HandlebarsProxyConfiguration.Instance.Domain + 
                                                                          "/" +
                                                                          (string.IsNullOrEmpty(area) ? "" : area + "/") + 
                                                                          (string.IsNullOrEmpty(controller) ? "" : controller + "/") + 
                                                                          (string.IsNullOrEmpty(action) ? "" : action)
                                                                          )));
                    var json = Encoding.UTF8.GetString(data);
                    var o = JObject.Parse(json);

                    o["debug"] = true;
                    // o["_config"]["cdn"] = "//" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.CdnPort;
                    o["_request"]["fqdn"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                    o["_request"]["hostname"] = HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                    json = o.ToString(Formatting.None);

                    var templateName = client.ResponseHeaders["x-template"];
                    var templateData = File.ReadAllText(Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                                     "\\template",
                                                                     templateName.Replace("/", "\\") + ".handlebars"));

                    var html = _handlebars.Render(templateData, json);
                    input = input.Replace(m.Value, html);
                }

            }
            
            return input;
        }


        private class SectionData
        {
            public int NameLength;
            public int Start;
            public int Stop;
            public string Contents;
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
                        var partialTemplate =  File.ReadAllText(Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                                             "\\template",
                                                                             name.Replace("/", "\\") + ".handlebars"));

                        if (string.IsNullOrEmpty(partialTemplate))
                            throw new Exception("Partial " + name + " is empty, found in: \n" + template);

                        partialTemplateCheck.Add(partialTemplate);
                        _handlebars.PartialCompile(name, partialTemplate);
                    }

                }
            }
        }
    }     

    

}
