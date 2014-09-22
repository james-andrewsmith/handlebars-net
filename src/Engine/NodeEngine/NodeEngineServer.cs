using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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



        private Uri GetServerUri(string action)
        {
            return new Uri("http://127.0.0.1:" + _port + "/" + action);
        }

        void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                Console.WriteLine(e.Data);
        }

        void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                Console.WriteLine(e.Data);
        }
    }
}
