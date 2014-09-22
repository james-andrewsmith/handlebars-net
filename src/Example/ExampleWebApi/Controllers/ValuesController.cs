using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Web.Http;

using System.Threading;
using System.Threading.Tasks;

namespace ExampleWebApi.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        [Route("/values")]
        public async Task<HttpResponseMessage> Get()
        {
            var data = new
            {
                order = new { id = 123, product = "Some product", price = 1235 }
            };

            // Here is an example of us getting the donut action 

           

            return Request.CreateResponse(HttpStatusCode.OK, data);            
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}