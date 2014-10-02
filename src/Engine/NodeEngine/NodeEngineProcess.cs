using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

using Handlebars;

namespace Handlebars
{
    public sealed partial class NodeEngine : IHandlebarsEngine
    {         
        private ProcessStartInfo GetStartInfo()
        {
            var si = new ProcessStartInfo();
            si.CreateNoWindow = false;
            // si.RedirectStandardInput = true;
            // si.RedirectStandardOutput = true;
            // si.RedirectStandardError = true;
            si.UseShellExecute = false;
            si.FileName = "node.exe";
            si.Arguments = "hb-server.js";

            // si.CreateNoWindow = true;
            // si.RedirectStandardInput = true;
            // si.RedirectStandardOutput = true;
            // si.RedirectStandardError = true;
            // si.UseShellExecute = false;
            // si.FileName = "node.exe";
            // si.Arguments = "-i";

            return si;
        } 
    }
}
