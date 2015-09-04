using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handlebars.Proxy
{
    public sealed class HandlebarsProxyConfiguration
    {
        #region // Singleton //
        static HandlebarsProxyConfiguration()
        {
            Instance = new HandlebarsProxyConfiguration
            {
                Directory = System.Environment.CurrentDirectory,
                Port = 8080, 
                Hostname = "localhost",
                ContentDeliveryNetwork = "cdn.archfashion.dev",
                Scheme = "http",
                DomainPort = 0
            };
        }

        public static HandlebarsProxyConfiguration Instance;
        #endregion

        #region // Properties //         

        /// <summary>
        /// HTTP vs HTTPS
        /// </summary>
        public string Scheme
        {
            get;
            set;
        }

        /// <summary>
        /// The domain we are setting up a proxy to, eg: www.archfashion.com.au
        /// </summary>
        public string Domain
        {
            get;
            set;
        }

        public int DomainPort
        {
            get;
            set;
        }


        /// <summary>
        /// Where on the local drive can the templates be found, will 
        /// default to the current working directory
        /// </summary>
        public string Directory
        {
            get;
            set;
        }

        public string Hostname
        {
            get;
            set;
        }

        /// <summary>
        /// If there is authentication in place for the proxy domain then 
        /// use this username.
        /// </summary>
        public string Username
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }         

        public int Port
        {
            get;
            set;
        } 
         
        public bool LocalCache
        {
            get;
            set;
        }

        public string ContentDeliveryNetwork
        {
            get;
            set;
        }

        #endregion

        public bool IsValid()
        {

            // domain is valid

            return true;
            // directory exists


        }
    }
}
