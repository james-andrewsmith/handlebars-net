using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Owin.Hosting;
using Microsoft.Owin;
using NDesk.Options;

using Ninject;

namespace Handlebars.Proxy
{    

    class Program
    {
       
        // todo: 
        // -> OUTPUT CACHE DOESN'T SAVE x-template HEADER when x-format=json
        // -> Use memory store instead
        // -> Add etag support  

        static void Main(string[] args)
        {
            
            Console.Write("\nHandlebars Command Line [Version {0}]\n",
                          System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            // arguments and their defaults
            bool showHelp = false;
            
            var p = new OptionSet() {
                { "directory=", 
                  "The root directory of the templates.", 
                  v => HandlebarsProxyConfiguration.Instance.Directory = v },
                { "hostname=", 
                  "The hostname or IP to identify the development proxy", 
                  v => HandlebarsProxyConfiguration.Instance.Hostname = v },
                { "domain=",  
                  "The domain to proxy requests to with JSON suffixes", 
                  (v) => HandlebarsProxyConfiguration.Instance.Domain = v },
                { "domainport=",  
                  "The domain to proxy requests to with JSON suffixes", 
                  (v) => HandlebarsProxyConfiguration.Instance.DomainPort = Convert.ToInt32(v) },
                { "cdn=",  
                  "The local replacement server for a Content Delivery Network", 
                  (v) => HandlebarsProxyConfiguration.Instance.ContentDeliveryNetwork = v },
                { "username=", 
                  "The username to authenticate with.", 
                  v => HandlebarsProxyConfiguration.Instance.Username = v },
                { "password=", 
                  "The password to authenticate with.", 
                  v => HandlebarsProxyConfiguration.Instance.Password = v },
                { "scheme=", 
                  "The scheme of the url", 
                  v => HandlebarsProxyConfiguration.Instance.Scheme = v },
                { "port=", 
                  "The port to run the webserver on", 
                  (int v) => HandlebarsProxyConfiguration.Instance.Port = v }, 
                { "h|help",  "show this message and exit",  v => showHelp = v != null },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);

                if (!HandlebarsProxyConfiguration.Instance.IsValid())
                    throw new Exception("There are missing variables");

                // set the proxy port
                if (HandlebarsProxyConfiguration.Instance.DomainPort == 0 ||
                    HandlebarsProxyConfiguration.Instance.DomainPort == 80)
                    HandlebarsProxyConfiguration.Instance.DomainPort = HandlebarsProxyConfiguration.Instance.Scheme.ToLower() == "https" ? 443 : 80;
            

            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `flow --help' for more information.");
                return;
            }

            IKernel kernel = new StandardKernel();
            
#if __MonoCS__
            kernel.Bind<IHandlebarsEngine>().To<NodeEngine>().InSingletonScope();
#else
            kernel.Bind<IHandlebarsEngine>()
                  .To<ClearScriptEngine>()
                  .InSingletonScope();       
#endif

            kernel.Bind<IHandlebarsResourceProvider>()
                  .To<LocalResourceProvider>()
                  .InSingletonScope();

            kernel.Bind<IHandlebarsTemplate>()
                  .To<DevelopmentHandlebarsTemplate>() 
                  .InSingletonScope();
            
            kernel.Bind<ProxyStartup>()
                  .To<ProxyStartup>()
                  .InSingletonScope();

            using (WebApp.Start(new StartOptions("http://" + HandlebarsProxyConfiguration.Instance.Hostname + ":" + HandlebarsProxyConfiguration.Instance.Port), builder =>
                   {
                       var webHost = kernel.Get<ProxyStartup>();
                       webHost.Configuration(builder);
                   }))
            {
                Console.WriteLine("All services started");
                bool running = true;
                do
                {
                    var command = Console.ReadLine();
                    running = ProcessCommand(command);
                }
                while (running);
            }


            // 1. Check blob storage for new version
            // 2. Get map.js from blog storage for this domain

            // 1. Watch directory
            // 2. wait for save on .js | .css | .liquid
            // 3. send reload signal to all windows
                       
            // -> in parallel request the child controller x-format=json
            //   -> session
            //      --> cart
            //      --> current account
            //      --> testing group
            // eg: /home/header

            // -> wrap the data response with the request            

            // 1. Get request
            // 2. Make same request with appended ?x-format=json
            // 3. Cache response
            // 4. Deserialize json into anonymous/dynamic object
            // 5. Send to dotliquid
            // 6. render result
            // 7. append the livereload script

        }

        private static bool ProcessCommand(string line)
        {
            line = line.ToLower();
            if (line == "quit" || line == "exit" || line == "!q")
                return false;

            // todo:
            // -> Clear the cache

            return true;
        }
    
    }
}
