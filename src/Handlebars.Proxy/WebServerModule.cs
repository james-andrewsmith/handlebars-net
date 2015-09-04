using System;
using System.Collections;
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
using System.Reflection;

using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Handlebars;
using RestSharp;
using System.Runtime.Caching;


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
            WebRequestHandler webRequestHandler = new WebRequestHandler();

            // we don't do this, as we miss the authentication headers
            // we just pass redirects onto the browser and handle it like that
            webRequestHandler.AllowAutoRedirect = false;
            webRequestHandler.UseCookies = false;

            _store = MemoryCache.Default;
            _client = new HttpClient(webRequestHandler, false)
            {
                BaseAddress = new Uri(HandlebarsProxyConfiguration.Instance.Scheme +
                                      "://" +
                                      HandlebarsProxyConfiguration.Instance.Domain +
                                      ":" +
                                      HandlebarsProxyConfiguration.Instance.DomainPort),                                      

            };

            

            // 
            if (!string.IsNullOrEmpty(HandlebarsProxyConfiguration.Instance.Username) &&
                !string.IsNullOrEmpty(HandlebarsProxyConfiguration.Instance.Password))
            {
                _authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                                     Encoding.ASCII.GetBytes(string.Format("{0}:{1}", 
                                                                           HandlebarsProxyConfiguration.Instance.Username,
                                                                           HandlebarsProxyConfiguration.Instance.Password)))
                );

                _client.DefaultRequestHeaders.Authorization = _authHeader;                
            }
            
        }
        #endregion

        #region // Dependency Injection //
        private readonly AuthenticationHeaderValue _authHeader;
        private readonly ObjectCache _store;
        private readonly IHandlebarsEngine _handlebars;
        private readonly IHandlebarsTemplate _template;
        private readonly HttpClient _client;
        #endregion

        #region // Owin Entry Point //
        public void Configuration(IAppBuilder app)
        {
            app.Run(Invoke);
        }
        #endregion 

        // Invoked once per request.
        public async Task Invoke(IOwinContext context)
        {
            using (var trace = new Trace(context.Request.Uri.PathAndQuery.ToString()))
            {
                string json, templateName, templateData;

                if (context.Request.Uri.PathAndQuery == "/favicon.ico")
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(new byte[] { });
                    return;
                }

                try
                {
                    // pass the same method through for all requests
                    HttpMethod method = new HttpMethod(context.Request.Method);
                    var uri = (HandlebarsProxyConfiguration.Instance.Scheme + "://" +
                               HandlebarsProxyConfiguration.Instance.Domain + ":" +
                               HandlebarsProxyConfiguration.Instance.DomainPort +
                               GetProxyUri(context.Request.Uri)).ToLower();

                    HttpRequestMessage request = new HttpRequestMessage(method, new Uri(uri));
                    
                    // keep the same user agent
                    request.Headers.TryAddWithoutValidation("User-Agent", context.Request.Headers["User-Agent"]);
                    
                    // hack the cookies we do and don't want
                    if (context.Request.Headers["Cookie"] != null &&
                        !string.IsNullOrEmpty(context.Request.Headers["Cookie"]))
                    {
                        request.Headers.TryAddWithoutValidation("Cookie", context.Request.Headers["Cookie"]);
                    }

                    // check if there is a response for this URI
                    HttpResponseMessage cachedResponse = _store[uri] as HttpResponseMessage;
                    if (cachedResponse != null && context.Request.Method == "GET")
                    {
                        // add an "IfNoneMatch" to the new request to validate this one.
                        if (cachedResponse.Headers.ETag != null)
                            request.Headers.TryAddWithoutValidation("If-None-Match", cachedResponse.Headers.ETag.ToString());                        
                    }

                    
                    byte[] data;
                    HttpResponseMessage response;
                    switch (context.Request.Method)
                    {
                        case "PUT":
                        case "POST":
                            using (var ms = new MemoryStream())
                            {
                                context.Request.Body.CopyTo(ms);

                                var originalFormContent = Encoding.UTF8.GetString(ms.ToArray());

                                var newFormContent = 
                                new FormUrlEncodedContent(originalFormContent.Split('&')
                                                                             .Select(_ =>
                                                                             {
                                                                                 var parts = _.Split('=');
                                                                                 var kvp = new KeyValuePair<string, string>(Uri.UnescapeDataString(parts[0]), 
                                                                                                                            Uri.UnescapeDataString(parts[1]));
                                                                                 return kvp;
                                                                             }));

                                request.Content = newFormContent;
                                // request.Content.Headers.ContentType = new MediaTypeHeaderValue(context.Request.Headers["Content-Type"]);

                                
                                response = await _client.SendAsync(request);
                                data = await response.Content.ReadAsByteArrayAsync();

                                if (response.Headers.Contains("Set-Cookie"))
                                {
                                    var setCookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
                                    if (!string.IsNullOrEmpty(setCookie))
                                    {
                                        var cookie = setCookie.ToString();
                                        cookie = cookie.Replace("." + HandlebarsProxyConfiguration.Instance.Domain.Replace("www", "").Trim('.'),
                                                                HandlebarsProxyConfiguration.Instance.Hostname);

                                        Console.WriteLine("Set Cookie: " + cookie);
                                        context.Response.Headers["Set-Cookie"] = cookie;
                                    }
                                }
                            }
                            break;

                        default:
                            response = await _client.SendAsync(request);
                            data = await response.Content.ReadAsByteArrayAsync();
                            break;
                    }

                    // use the cached version
                    if (cachedResponse != null && 
                        response.StatusCode == HttpStatusCode.NotModified)
                    {   
                        response = cachedResponse;
                        data = await cachedResponse.Content.ReadAsByteArrayAsync();
                        await Console.Out.WriteLineAsync("Using Cached Response");
                    }                      
                    // save the response in the cache
                    else if (response.Headers.ETag != null && cachedResponse == null)
                    {
                        CacheItemPolicy policy = new CacheItemPolicy();
                        policy.AbsoluteExpiration = DateTimeOffset.Now.AddDays(1);
                        _store.Set(request.RequestUri.ToString(), response, policy);
                    }

                    // set headers we need for compatibility in weird situations                       
                    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                    context.Response.Headers["Access-Control-Allow-Methods"] = "GET,PUT,POST,DELETE";
                    context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";

                    // if this is a redirect, pass it through
                    var location = response.Headers.Location;
                    if (location != null)
                    {
                        var locationUri = new Uri(location.ToString());
                        // locationUri.Host = HandlebarsProxyConfiguration.Instance.Hostname;
                        // locationUri.Port = HandlebarsProxyConfiguration.Instance.Port;
                        context.Response.Headers["Location"] = locationUri.PathAndQuery;
                    }

                    // if this is an API request, then pass it staight to the client, no template logic
                    if (context.Request.Uri.PathAndQuery.StartsWith("/api") ||
                        !response.Content.Headers.ContentType.MediaType.Contains("application/json") ||
                        response.StatusCode == HttpStatusCode.Found ||
                        response.StatusCode == HttpStatusCode.Redirect ||
                        response.StatusCode == HttpStatusCode.TemporaryRedirect)
                    {
                        context.Response.StatusCode = (int)response.StatusCode;
                        context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                        await context.Response.WriteAsync(data);
                        return;
                    }

                    // ok, now we apply some smarts, first things first, get the data as a JSON object
                    json = Encoding.UTF8.GetString(data);
                    var o = JObject.Parse(json);

                    o["debug"] = true;

                    // here we need to ensure that we are replacing the hostname with the configured local version
                    // this means any logic around hostnames uses this
                    o["_config"]["api"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                    o["_config"]["cdn"] = HandlebarsProxyConfiguration.Instance.ContentDeliveryNetwork; // "//cdn.archfashion.dev";
                    o["_request"]["protocol"] = "http";
                    o["_request"]["gzip"] = false;
                    o["_request"]["fqdn"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                    o["_request"]["hostname"] = HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;

                    // Add an experiment with the variation as instructed by the request
                    var qs = new UrlEncodingParser(context.Request.QueryString.ToUriComponent());
                    if (!string.IsNullOrEmpty(qs["experiment"]) &&
                        !string.IsNullOrEmpty(qs["alias"]))
                    {
                        JObject experiment = o["_experiment"] as JObject;
                        if (experiment == null)
                        {
                            experiment = new JObject();
                            o["_experiment"] = experiment;
                        }
                        experiment.RemoveAll();
                        experiment.Add(qs["experiment"].Urlify(), JObject.Parse(@"{""id"":""rAnDoMlEtTeRs"",""variation"":0,""variation_alias"":""" + qs["alias"].Urlify() + @"""}"));
                    }

                    json = o.ToString(Formatting.None);

                    // send the modified response back to the client (obviously the developer is 
                    // trying to work out what is being combined with the template)
                    if (qs["x-format"] == "json")
                    {
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                        await context.Response.WriteAsync(json);
                        return;
                    }

                    // get the header which has the handlebars template in it
                    var xTemplate = response.Headers.GetValues("x-template").FirstOrDefault();
                    if (string.IsNullOrEmpty(xTemplate))
                    {
                        await Console.Error.WriteLineAsync("No x-template header for URL: " + request.RequestUri.ToString());
                        await context.Response.WriteAsync("No x-template header for URL: " + request.RequestUri.ToString());
                        return;
                    }

                    // get the local template
                    templateName = xTemplate.ToString();
                    var templatePath = Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                    "\\template",
                                                    templateName.Replace("/", "\\") + ".handlebars");

                    // try different file extension
                    if (!File.Exists(templatePath))
                        templatePath = Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                    "\\template",
                                                    templateName.Replace("/", "\\") + ".hbs");

                    // get the data of the template
                    templateData = File.ReadAllText(templatePath);

                    // if this is file not found send a friendly message
                    using (var render = new Trace("render"))
                    {

                        var r = _template.Render(templateName, json);
                        var html = FillSectionData(r, json);

                        var donuts = new List<string>();
                        var templates = new Dictionary<string, string>();

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
                                using (var handler = new WebRequestHandler()
                                {
                                    UseCookies = false,
                                    AllowAutoRedirect = false                                    
                                })
                                using (HttpClient client = new HttpClient(handler, false)
                                {
                                    BaseAddress = new Uri(HandlebarsProxyConfiguration.Instance.Scheme + "://" +
                                                          HandlebarsProxyConfiguration.Instance.Domain + ":" +
                                                          HandlebarsProxyConfiguration.Instance.DomainPort)

                                })
                                {

                                    foreach (var donut in donuts)
                                    {
                                        var duri = new Uri((HandlebarsProxyConfiguration.Instance.Scheme + "://" +
                                                            HandlebarsProxyConfiguration.Instance.Domain + ":" +
                                                            HandlebarsProxyConfiguration.Instance.DomainPort +
                                                            GetProxyUri(donut)).ToLower());

                                        await Console.Out.WriteLineAsync("Getting Donut: " + duri.ToString());
                                        HttpRequestMessage drequest = new HttpRequestMessage(HttpMethod.Get, duri);

                                        drequest.Headers.Authorization = _authHeader;
                                        drequest.Headers.TryAddWithoutValidation("User-Agent", context.Request.Headers["User-Agent"]);
                                        drequest.Headers.TryAddWithoutValidation("Cookie", context.Request.Headers["Cookie"]);

                                        // the donut response's don't have etags so instead compare the cookies
                                        // as changes to the cookie state will be what impacts the cache, eg: login and out
                                        // or experiment changes
                                        HttpResponseMessage dcachedResponse = _store[duri.ToString()] as HttpResponseMessage;
                                        if (dcachedResponse != null &&
                                            (dcachedResponse.Headers.Contains("Cookie") && 
                                            string.Join(",", dcachedResponse.RequestMessage.Headers.GetValues("Cookie").ToArray()) ==
                                            string.Join(",", drequest.Headers.GetValues("Cookie").ToArray())) &&
                                            (dcachedResponse.Headers.Contains("User-Agent") && 
                                            string.Join(",", dcachedResponse.RequestMessage.Headers.GetValues("User-Agent").ToArray()) ==
                                            string.Join(",", drequest.Headers.GetValues("User-Agent").ToArray())) 
                                            )
                                        {
                                            // OK, from here we can do the continue task without actually "dong anything"
                                            await Console.Out.WriteLineAsync("Cached Response: " + duri.ToString());
                                        
                                            var dDataTask = dcachedResponse.Content.ReadAsByteArrayAsync();
                                            Task.WaitAll(dDataTask);

                                            var doo = JObject.Parse(Encoding.UTF8.GetString(dDataTask.Result));

                                            // within the handlebars environment replace the 
                                            doo["_config"]["api"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                                            doo["_config"]["cdn"] = HandlebarsProxyConfiguration.Instance.ContentDeliveryNetwork;
                                            doo["_request"]["protocol"] = "http";
                                            doo["_request"]["gzip"] = false;
                                            doo["_request"]["fqdn"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                                            doo["_request"]["hostname"] = HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;

                                            try
                                            {
                                                doo.Remove("_experiment");
                                                doo.Add("_experiment", o["_experiment"].ToString(Formatting.Indented));
                                            }
                                            catch (Exception exp)
                                            {
                                                Console.Out.WriteLineAsync("Cached Donut Error: " + duri.ToString() + " : " + exp.Message);                                                                                        
                                            }

                                            var dxTemplate = donut;
                                            if (dcachedResponse.Headers.Contains("x-template"))
                                                dxTemplate = dcachedResponse.Headers.GetValues("x-template").FirstOrDefault();                                                                  
                        
                                            var t = Task.FromResult(new KeyValuePair<string, string>(donut, _template.Render(dxTemplate, doo.ToString(Formatting.None))));
                                            lock (sync)
                                            {
                                                tasks.Add(t);
                                            }
                                        }
                                        else
                                        {
                                            var t = client.SendAsync(drequest, CancellationToken.None)
                                                          .ContinueWith(_ =>
                                                          {
                                                              
                                                              if (_.IsFaulted ||
                                                                  !_.Result.IsSuccessStatusCode)
                                                              {
                                                                  Console.Out.WriteLine("Live Donut Error: " + duri.ToString());                                        
                                                              }
                                                              else
                                                              {
                                                                  CacheItemPolicy policy = new CacheItemPolicy();
                                                                  policy.AbsoluteExpiration = DateTimeOffset.Now.AddHours(1);
                                                                  _store.Set(_.Result.RequestMessage.RequestUri.ToString(), _.Result, policy);
                                                              }

                                                              var tcontent = _.Result.Content.ReadAsStringAsync();
                                                              Task.WaitAll(tcontent);

                                                              KeyValuePair<string, string> kvp;

                                                              var dxTemplate = donut;
                                                              if (_.Result.Headers.Contains("x-template"))
                                                                  dxTemplate = _.Result.Headers.GetValues("x-template").FirstOrDefault();

                                                              try
                                                              {                                                                  
                                                                  var doo = JObject.Parse(tcontent.Result);

                                                                  // within the handlebars environment replace the 
                                                                  doo["_config"]["api"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                                                                  doo["_config"]["cdn"] = HandlebarsProxyConfiguration.Instance.ContentDeliveryNetwork;
                                                                  doo["_request"]["protocol"] = "http";
                                                                  doo["_request"]["gzip"] = false;
                                                                  doo["_request"]["fqdn"] = "http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;
                                                                  doo["_request"]["hostname"] = HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port;

                                                                  try
                                                                  {
                                                                      doo.Remove("_experiment");
                                                                      doo.Add("_experiment", o["_experiment"].ToString(Formatting.Indented));
                                                                  }
                                                                  catch (Exception exp)
                                                                  {
                                                                      Console.Out.WriteLine("Live Donut Error: " + duri.ToString());
                                                                      Console.WriteLine(exp.Message);
                                                                      Console.WriteLine(exp.StackTrace);
                                                                  }

                                                                  kvp = new KeyValuePair<string, string>(dxTemplate, _template.Render(donut, doo.ToString(Formatting.None)));
                                                              }
                                                              catch (JsonReaderException)
                                                              {
                                                                  kvp = new KeyValuePair<string, string>(dxTemplate, tcontent.Result);
                                                              }

                                                              return kvp;
                                                              
                                                          });

                                            if (!t.IsFaulted)
                                            {
                                                lock (sync)
                                                {
                                                    tasks.Add(t);
                                                }
                                            }
                                        }
                                    }


                                try
                                {
                                    Task.WaitAll(tasks.ToArray());
                                }
                                catch (Exception exp)
                                {
                                    Console.WriteLine("Awaiting Donuts: " + exp.Message);
                                }
                            }
                        }

                        // go through the sucessful tasks
                        foreach (var task in tasks)
                        {
                            html.Replace("####donut:" + task.Result.Key + "####", task.Result.Value);
                        }

                        context.Response.ContentType = "text/html";

                        // make sure the temp partials are cleared
                        _handlebars.Clear();
                        await context.Response.WriteAsync(html.ToString());
                        Console.WriteLine("Request Complete: " + context.Request.Uri.ToString());
                        return;
                    }

                }
                catch (Exception exp)
                {
                    // output any errors
                    Console.Error.WriteLine(context.Request.Uri.ToString() +
                                            "\n" +
                                            exp.Message +
                                            "\n" +
                                            exp.StackTrace);

                    // send them to the client as well
                    Task.WaitAll(context.Response.WriteAsync(context.Request.Uri.ToString() +
                                                             "\n" +
                                                             exp.Message +
                                                             "\n" +
                                                             exp.StackTrace));

                    return;
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

        private string GetProxyUri(Uri uri)
        {
            var builder = new UriBuilder(uri);
            builder.Scheme = HandlebarsProxyConfiguration.Instance.Scheme;
            builder.Host = HandlebarsProxyConfiguration.Instance.Domain;
            builder.Port = HandlebarsProxyConfiguration.Instance.Scheme.ToLower() == "https" ? 443 : 80;

            if (string.IsNullOrEmpty(builder.Query))
                builder.Query = "x-format=json";
            else if (builder.Query.Contains("x-format=json"))
            { }
            else
                builder.Query = builder.Query.TrimStart('?') + "&x-format=json";
            
            return builder.Uri.PathAndQuery;
        }

        private string GetProxyUri(string path)
        {
            var builder = new UriBuilder();
            builder.Path = path;
            builder.Scheme = HandlebarsProxyConfiguration.Instance.Scheme;
            builder.Host = HandlebarsProxyConfiguration.Instance.Domain;
            builder.Port = HandlebarsProxyConfiguration.Instance.DomainPort;

            if (string.IsNullOrEmpty(builder.Query))
                builder.Query = "x-format=json";
            else if (builder.Query.Contains("x-format=json"))
            { }
            else
                builder.Query = builder.Query.TrimStart('?') + "&x-format=json";

            return builder.Uri.PathAndQuery;
        }

        public static CookieCollection GetAllCookiesFromHeader(string strHeader, string strHost)
        {
            ArrayList al = new ArrayList();
            CookieCollection cc = new CookieCollection();
            if (strHeader != string.Empty)
            {
                al = ConvertCookieHeaderToArrayList(strHeader);
                cc = ConvertCookieArraysToCookieCollection(al, strHost);
            }
            return cc;
        }


        private static ArrayList ConvertCookieHeaderToArrayList(string strCookHeader)
        {
            strCookHeader = strCookHeader.Replace("\r", "");
            strCookHeader = strCookHeader.Replace("\n", "");
            string[] strCookTemp = strCookHeader.Split(',');
            ArrayList al = new ArrayList();
            int i = 0;
            int n = strCookTemp.Length;
            while (i < n)
            {
                if (strCookTemp[i].IndexOf("expires=", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    al.Add(strCookTemp[i] + "," + strCookTemp[i + 1]);
                    i = i + 1;
                }
                else
                {
                    al.Add(strCookTemp[i]);
                }
                i = i + 1;
            }
            return al;
        }


        private static CookieCollection ConvertCookieArraysToCookieCollection(ArrayList al, string strHost)
        {
            CookieCollection cc = new CookieCollection();

            int alcount = al.Count;
            string strEachCook;
            string[] strEachCookParts;
            for (int i = 0; i < alcount; i++)
            {
                strEachCook = al[i].ToString();
                strEachCookParts = strEachCook.Split(';');
                int intEachCookPartsCount = strEachCookParts.Length;
                string strCNameAndCValue = string.Empty;
                string strPNameAndPValue = string.Empty;
                string strDNameAndDValue = string.Empty;
                string[] NameValuePairTemp;
                Cookie cookTemp = new Cookie();

                for (int j = 0; j < intEachCookPartsCount; j++)
                {
                    if (j == 0)
                    {
                        strCNameAndCValue = strEachCookParts[j];
                        if (strCNameAndCValue != string.Empty)
                        {
                            int firstEqual = strCNameAndCValue.IndexOf("=");
                            string firstName = strCNameAndCValue.Substring(0, firstEqual);
                            string allValue = strCNameAndCValue.Substring(firstEqual + 1, strCNameAndCValue.Length - (firstEqual + 1));
                            cookTemp.Name = firstName;
                            cookTemp.Value = allValue;
                        }
                        continue;
                    }
                    if (strEachCookParts[j].IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        strPNameAndPValue = strEachCookParts[j];
                        if (strPNameAndPValue != string.Empty)
                        {
                            NameValuePairTemp = strPNameAndPValue.Split('=');
                            if (NameValuePairTemp[1] != string.Empty)
                            {
                                cookTemp.Path = NameValuePairTemp[1];
                            }
                            else
                            {
                                cookTemp.Path = "/";
                            }
                        }
                        continue;
                    }

                    if (strEachCookParts[j].IndexOf("domain", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        strPNameAndPValue = strEachCookParts[j];
                        if (strPNameAndPValue != string.Empty)
                        {
                            NameValuePairTemp = strPNameAndPValue.Split('=');

                            if (NameValuePairTemp[1] != string.Empty)
                            {
                                cookTemp.Domain = NameValuePairTemp[1];
                            }
                            else
                            {
                                cookTemp.Domain = strHost;
                            }
                        }
                        continue;
                    }
                }

                if (cookTemp.Path == string.Empty)
                {
                    cookTemp.Path = "/";
                }
                if (cookTemp.Domain == string.Empty)
                {
                    cookTemp.Domain = strHost;
                }
                cc.Add(cookTemp);
            }
            return cc;
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
                    // client.Headers["User-Agent"] = context.Request.Headers["User-Agent"];
                    // client.Headers["Cookie"] = context.Request.Headers["Cookie"];

                    if (_authHeader != null)
                        client.Headers.Add("auth", _authHeader.Parameter);
                        
                    var data = client.DownloadData(GetProxyUri(new Uri(   HandlebarsProxyConfiguration.Instance.Scheme +
                                                                          "://" +
                                                                          HandlebarsProxyConfiguration.Instance.Domain + ":" + 
                                                                          HandlebarsProxyConfiguration.Instance.DomainPort + 
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
                    var templatePath = Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                    "\\template",
                                                    templateName.Replace("/", "\\") + ".handlebars");
                    
                    if (!File.Exists(templatePath))
                        templatePath = Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                    "\\template",
                                                    templateName.Replace("/", "\\") + ".hbs");
                    
                    var templateData = File.ReadAllText(templatePath);

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
                        var partialPath = Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                       "\\template",
                                                       name.Replace("/", "\\") + ".handlebars");

                        if (!File.Exists(partialPath))
                            partialPath = Path.Combine(HandlebarsProxyConfiguration.Instance.Directory +
                                                       "\\template",
                                                       name.Replace("/", "\\") + ".hbs");

                        var partialTemplate =  File.ReadAllText(partialPath);

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
