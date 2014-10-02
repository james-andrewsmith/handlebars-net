using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace Handlebars
{
    public sealed partial class NodeEngine : IHandlebarsEngine
    {
        #region // Constructor //
        public NodeEngine()
        {
            _port = 1337;             

            var sb = new StringBuilder(File.ReadAllText("server.js"));

            // add included scripts
            sb.AppendLine("var Handlebars = require('./Script/handlebars-1.0.0.js');");
            foreach (var script in HandlebarsConfiguration.Instance.Include)
            { 
                if (!string.IsNullOrEmpty(script.Name))
                {
                    sb.AppendLine("var " + script.Name + " = require('./" + script.Source + "');");
                }
                else
                {
                    sb.AppendLine("require('./" + script.Source + "');");
                } 
            }

            File.WriteAllText("hb-server.js", sb.ToString());
            _server = new Process();
            _server.StartInfo = GetStartInfo();
            _server.Start();
            
            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.AutomaticDecompression = DecompressionMethods.None;
            _client = new HttpClient(handler);
        }
        #endregion

        #region // Properties //
        private readonly int _port;
        private readonly Process _server;
        private readonly HttpClient _client;
        #endregion

        #region // Dispose //
        public void Dispose()
        {
            _server.Kill();
            _server.Dispose();
        }
        #endregion
    }
}
