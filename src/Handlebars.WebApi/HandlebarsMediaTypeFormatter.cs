using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers; 

using Newtonsoft.Json;

namespace Handlebars.WebApi
{
    public sealed class HandlebarsMediaTypeFormatter : MediaTypeFormatter
    {
        #region // Constructor //
        public HandlebarsMediaTypeFormatter(HttpRouteCollection routes,
                                            IHandlebarsTemplate template,
                                            IRequestFormatter formatter) : this(new HttpConfiguration(routes), template, formatter)
        {
        }

        public HandlebarsMediaTypeFormatter(HttpConfiguration config,
                                            IHandlebarsTemplate template,
                                            IRequestFormatter formatter)
            : base()
        {            
            this._template = template;
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            this._formatter = formatter;
                        
            config.Formatters.Insert(0, this);
            _config = config;
            _server = new HttpServer(config);
            _client = new HttpMessageInvoker(_server);
        }
        
        public HandlebarsMediaTypeFormatter(HttpConfiguration config,
                                            IHandlebarsTemplate template,
                                            IRequestFormatter formatter,
                                            HttpServer server, 
                                            HttpMessageInvoker client, 
                                            string view,
                                            HttpRequestMessage request)
        {

            this._formatter = formatter;
            this._template = template;
            this._request = request;
            this._server = server;
            this._config = config;
            this._client = client;
            this._view = view;

            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));            
        }
        #endregion

        #region // Dependency Injection //
        private readonly HttpConfiguration _config;
        private readonly IHandlebarsTemplate _template;
        private readonly HttpServer _server;
        private readonly HttpMessageInvoker _client;
        private readonly IRequestFormatter _formatter;
        private readonly HttpRequestMessage _request;
        private readonly string _view;
        #endregion

        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {
            if (typeof(HttpError) == type) 
                return false;

            return true;
        }

        private static readonly Regex MasterRegex = new Regex(@"(####master:(.+)####)", RegexOptions.IgnoreCase);
        private static readonly Regex SectionRegex = new Regex(@"(<!--####section:(.+)####-->)", RegexOptions.IgnoreCase);

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

        public override async Task WriteToStreamAsync(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken)
        {
            // await Console.Out.WriteLineAsync("HB Started: " + Request.RequestUri.PathAndQuery);
            var json = _formatter.GetContext(_request, value);
            string r = _template.Render(_view, json);
            
            StringBuilder html;

            var donuts = new List<string>();

            if (_request.Properties.ContainsKey("donut") &&
                (bool)_request.Properties["donut"] == true)
            {
                // detect any master or sections and fill them
                html = new StringBuilder(r);
            }
            else
            {
                html = FillSectionData(r, json);

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
            }             
                
            // execute any donuts
            var sync = new object();

            var donutContent = new Dictionary<string, string>();
            if (donuts.Count > 0)
            {

                foreach (var donut in donuts)
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://" + _request.RequestUri.DnsSafeHost + (_request.RequestUri.IsDefaultPort ? "" : ":" + _request.RequestUri.Port) + "/" + donut))
                    {
                        request.Properties.Add("donut", true);

                        // ensure any AB testing in donut actions uses the same grouping
                        if (_request.Properties.ContainsKey("experiment"))
                            request.Properties.Add("experiment", _request.Properties["experiment"]);

                        // so we can use the same identify and context information in higher up 
                        // donut functions.
                        if (_request.Properties.ContainsKey("MS_OwinContext"))
                            request.Properties.Add("MS_OwinContext", _request.Properties["MS_OwinContext"]);

                        // temp: try catch to debug "bad headers"
                        foreach (var header in _request.Headers)
                        {
                            try
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }
                            catch(Exception e)
                            {
                                Console.Out.WriteLineAsync("Handlebars - Add Header: " + e.Message);
                            }
                        }

                        // this was previously causing a deadlock, never use .Result!!!! it is the 
                        // root of all things evil.
                        
                        using (HttpResponseMessage response = await _client.SendAsync(request, CancellationToken.None))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                try
                                {
                                    var donutHtml = await response.Content.ReadAsStringAsync();
                                    lock (sync)
                                        donutContent.Add(donut, donutHtml);
                                }
                                catch (Exception exp)
                                {
                                    lock (sync)
                                        donutContent.Add(donut, exp.Message);
                                }
                            }
                        }
                    }
                }

                // wait for the donut requests
                // Console.Out.WriteLineAsync("HandlebarsMediaTypeFormatter: Donuts() " + sw.ElapsedMilliseconds + "ms");            
            }

            // StreamWriter writer = new StreamWriter(writeStream);
            
            
                
            foreach(var donut in donutContent)
            {
                html.Replace("####donut:" + donut.Key + "####", donut.Value);
            }
            // Console.Out.WriteLineAsync("HandlebarsMediaTypeFormatter: Replace() " + sw.ElapsedMilliseconds + "ms");
            
                
            // await writer.WriteAsync(html.ToString());
            var writer = new StreamWriter(stream);

            string output, contentType;
            if (_request.Properties.ContainsKey("hb-as-javascript"))
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

            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            await writer.WriteAsync(output);
            await writer.FlushAsync();
            return;            
        }

        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            HttpActionDescriptor actionDescriptor = request.GetActionDescriptor();
            if (actionDescriptor == null)
                return base.GetPerRequestFormatterInstance(type, request, mediaType);

            var view = actionDescriptor.ControllerDescriptor.ControllerName + "/" +
                       actionDescriptor.ActionName;

            if (actionDescriptor.ControllerDescriptor.Properties.ContainsKey("hb-prefix"))
                view = (actionDescriptor.ControllerDescriptor.Properties["hb-prefix"] as string) + "/" + view;

            if (request.Properties.ContainsKey("hb-view"))
                view = request.Properties["hb-view"] as string;


            return new HandlebarsMediaTypeFormatter(_config, _template, _formatter, _server, _client, view, request);

        }
        
    }
}
