using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ExampleWebApi.Controllers
{
    public class TestController : ApiController
    {
        [HttpGet]
        [ActionName("header")]
        [Route("/test/header")]
        public HttpResponseMessage Header()
        {
            var data = new
            {
                cart = new { id = 123, product = "Some product", price = 1235 }
            };

            return Request.CreateResponse(HttpStatusCode.OK, data);         
        }

        [HttpGet]
        [ActionName("footer")]
        [Route("/test/footer")]
        public HttpResponseMessage Footer()
        {
            var data = new
            {
                cart = new { id = 123, product = "Some product", price = 1235 }
            };

            return Request.CreateResponse(HttpStatusCode.OK, data);
        }
    }
}
