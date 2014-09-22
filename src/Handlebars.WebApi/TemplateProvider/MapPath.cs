using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Handlebars.WebApi
{
    internal static class MapPath
    {
        private static Func<string, string> _mapPath;

        public static Func<string, string> Map
        {
            get { return _mapPath ?? (_mapPath = Load()); }
        }

        private static Func<string, string> Load()
        {
            var systemWeb = TryGetSystemWeb();
            if (systemWeb != null)
            {
                var hostingEnvironment = systemWeb.GetType("System.Web.Hosting.HostingEnvironment");
                if (hostingEnvironment != null)
                {
                    var method = hostingEnvironment.GetMethod("MapPath");
#if(NET40)
                    var func =
                        (Func<string, string>) Delegate.CreateDelegate(typeof(Func<string,string>), method);
#else
                    var func =
                        (Func<string, string>)
                            method.CreateDelegate(typeof(Func<string, string>));
#endif

                    return path => func(path) ?? FallbackMapPath(path);
                }
            }

            return FallbackMapPath;
        }

        private static Assembly TryGetSystemWeb()
        {
            return
                AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => assembly.FullName.StartsWith("System.Web,"));
        }

        private static string FallbackMapPath(string virtualPath)
        {
            var assembly = Assembly.GetEntryAssembly() ??
                           Assembly.GetCallingAssembly();
            var path = Path.GetDirectoryName(assembly.GetPath());

            if (path == null)
                throw new Exception("Unable to determine executing assembly path.");

            return Path.Combine(path, virtualPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }

        private static string GetPath(this Assembly assembly)
        {
            return new Uri(assembly.EscapedCodeBase).LocalPath;
        }
    }
}
