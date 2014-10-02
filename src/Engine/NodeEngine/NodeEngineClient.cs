using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace Handlebars
{
    public sealed partial class NodeEngine : IHandlebarsEngine
    {
        public void Clear()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetServerUri("clear"));
            var t = _client.SendAsync(request);
            Task.WaitAll(t);
        }

        public void Compile(string name, string template)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetServerUri("compile"));
            var form = new List<KeyValuePair<string, string>>();
            form.Add(new KeyValuePair<string, string>("name", name));
            form.Add(new KeyValuePair<string, string>("template", template));
            request.Content = new FormUrlEncodedContent(form);
            var t = _client.SendAsync(request);
            Task.WaitAll(t);
        }

        public bool Exists(string name)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetServerUri("exists?name=" + name));
            var t = _client.SendAsync(request);
            Task.WaitAll(t);
            return t.Result.StatusCode == HttpStatusCode.OK;
        }

        public string ExportPrecompile()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetServerUri("precompile"));
            var t = _client.SendAsync(request);
            Task.WaitAll(t);
            var s = t.Result.Content.ReadAsStringAsync();
            Task.WaitAll(s);
            return s.Result;
        }

        public void ImportPrecompile(string js)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetServerUri("precompile-import"));
            var form = new List<KeyValuePair<string, string>>();
            form.Add(new KeyValuePair<string, string>("js", js));
            request.Content = new FormUrlEncodedContent(form);
            var t = _client.SendAsync(request);
            Task.WaitAll(t);
        }

        public void PartialCompile(string name, string template)
        {
            throw new NotImplementedException();
        }

        public bool PartialExists(string name)
        {
            throw new NotImplementedException();
        }

        public string Render(string name, string json)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetServerUri("render"));
            var form = new List<KeyValuePair<string, string>>();
            form.Add(new KeyValuePair<string, string>("name", name));
            form.Add(new KeyValuePair<string, string>("context", json));
            request.Content = new FormUrlEncodedContent(form);
            var t = _client.SendAsync(request);
            Task.WaitAll(t);
            var s = t.Result.Content.ReadAsStringAsync();
            Task.WaitAll(s);
            return s.Result;
        }

        public string Render(string name, object context)
        {
            string json = context is string ? ((string)context) : HandlebarsUtilities.ToJson(context);
            return Render(name, json);
        }

        public string Render(string name, string template, object context)
        {
            return Render(name, template, HandlebarsUtilities.ToJson(context));
        }

        public string Render(string name, string template, string json)
        {
            if (string.IsNullOrEmpty(json))
                json = "{}";
            
            var request = new HttpRequestMessage(HttpMethod.Post, GetServerUri("render"));
            var form = new List<KeyValuePair<string, string>>();
            form.Add(new KeyValuePair<string, string>("name", name));
            form.Add(new KeyValuePair<string, string>("template", template));
            form.Add(new KeyValuePair<string, string>("context", json));
            request.Content = new FormUrlEncodedContent(form);
            var t = _client.SendAsync(request);
            Task.WaitAll(t);
            var s = t.Result.Content.ReadAsStringAsync();
            Task.WaitAll(s);
            return s.Result;
        }

        public void Remove(string name)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetServerUri("remove"));
            var form = new List<KeyValuePair<string, string>>();
            form.Add(new KeyValuePair<string, string>("name", name));
            request.Content = new FormUrlEncodedContent(form);
            var t = _client.SendAsync(request);
            Task.WaitAll(t);
        }
    }
}
