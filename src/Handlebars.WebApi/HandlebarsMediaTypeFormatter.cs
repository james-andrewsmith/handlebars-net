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
    public sealed class HandlebarsMediaTypeFormatter : BufferedMediaTypeFormatter
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
            this.Formatter = formatter;
                        
            config.Formatters.Insert(0, this);
            Server = new HttpServer(config);
            Client = new HttpMessageInvoker(Server);
        }


        #endregion

        #region // Dependency Injection //
        private readonly IHandlebarsTemplate _template;
        private readonly HttpServer Server;
        private readonly HttpMessageInvoker Client;
        private readonly IRequestFormatter Formatter;
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

        public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
        {

            var json = Formatter.GetContext(Request, value);            
            var r = _template.Render(View, json);

            // detect any master or sections and fill them
            var html = FillSectionData(r, json); 
            // 1. Get the whole template
            // 2. Find any "donut" tags
            // 3. Dynamically execute request
            //    -> Ensure user is maintained
            //    -> Make sure no vunrabilities through this 
            //    -> Keep any A/B testing decisions
            //    -> Keep cookies etc
            //    -> Allow cookies to be set?
            

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

                foreach (var donut in donuts)
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://" + Request.RequestUri.DnsSafeHost + (Request.RequestUri.IsDefaultPort ? "" : ":" + Request.RequestUri.Port) + "/" + donut))
                    {
                        request.Properties.Add("donut", true);

                        // so we can use the same identify and context information in higher up 
                        // donut functions.
                        if (Request.Properties.ContainsKey("MS_OwinContext"))
                            request.Properties.Add("MS_OwinContext", Request.Properties["MS_OwinContext"]);

                        // 
                        foreach (var header in Request.Headers)
                            request.Headers.Add(header.Key, header.Value);

                        using (HttpResponseMessage response = Client.SendAsync(request, CancellationToken.None).Result)
                        {
                            if (response.IsSuccessStatusCode)
                                lock (sync)
                                    tasks.Add(response.Content
                                                      .ReadAsStringAsync()
                                                      .ContinueWith((_) =>
                                                      {
                                                          return new KeyValuePair<string, string>(donut, _.Result);
                                                      }));
                        }
                    }
                }

                Task.WaitAll(tasks.ToArray());            
            }

            using (StreamWriter writer = new StreamWriter(writeStream))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

                // if we are going to cache the output for "donut" caching
                // here is where we need to do it...

                // -> Attribute
                // -> OutputCacheStore (eg: MemoryCache or a file cache)
                // -> MetaCacheStore (eg: Redis)
                // -> Perhaps move the whole donut logic into a class
                //    which is then reused for the donut substituion against
                //    a cached result?

                foreach(var task in tasks)
                {
                    html.Replace("####donut:" + task.Result.Key + "####", 
                                 task.Result.Value);
                }

                writer.WriteAsync(html.ToString());          
            }
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

            HandlebarsMediaTypeFormatter formatter = (HandlebarsMediaTypeFormatter)base.GetPerRequestFormatterInstance(type, request, mediaType);
            formatter.View = view;
            formatter.Request = request;
            return formatter;
        }
        
        public HttpRequestMessage Request;
        public string View;
    }
}
