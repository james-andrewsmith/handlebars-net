using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Handlebars.WebApi
{
    public class DonutHttpRequest : DefaultHttpRequest
    { 
        
        public DonutHttpRequest(HttpContext context) : base(context)
        {            
        }                  
         
        public override PathString PathBase
        {
            get;
            set;
        }

        public override PathString Path
        {
            get;
            set;
        }
                 
        public override IHeaderDictionary Headers
        {
            get { return _headers; }
        }

        private IHeaderDictionary _headers;
        public void SetHeaders(IHeaderDictionary headers)
        {
            _headers = headers;
        }             
         
    }
}
