using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace HandlebarsViewEngine.Web.Controllers
{
    public class HomeController : Controller
    {

        #region // Async & Content Type //

        public void IndexAsync()
        {
            // Handlebars view
            // -> Device master 
            // -> Templates which need rendering
            // -> How will they be combined?
            // -> Git version to use

            // -> Request info (protocol)
            // -> 

            // master template?
            // -> Device Master
            // -> 
             

        }

        public ActionResult IndexCompleted()
        {
            // bind data to view
            ViewBag.Title = "Test Title";
            
            /*
            // if for some reason the JSON data needs to vary from the HTML mapping
            var data = new
            {
                success = true,
                data = new
                {
                    order = ViewBag.Order
                }
            };
            if (!this.TryXFormatResponse(data, out result)) result = View();           
            */
            return View();
        }

        #endregion

        #region // Razor Fallback //

        public ActionResult Razor()
        {
            return View();
        }

        #endregion

        #region // Donut Caching //

        [DonutOutputCache(Location = OutputCacheLocation.Server,
                          Duration = 86400,
                          VaryByParam = "id",
                          VaryByHeader = "accept-encoding",
                          VaryByCustom = "user;scheme",
                          VaryByContentEncoding = "gzip;deflate")]
        public ActionResult Donut()
        {
            ViewBag.Time = DateTime.Now.Millisecond;
            return View();
        }

        public ActionResult GetRandomMessage()
        {
            string message = RandomMessage();

            ViewBag.Message = message + " (" + DateTime.Now.Millisecond + ")";
            return View();
        }

        /*
        [DonutOutputCache(Location = OutputCacheLocation.Server,
                          Duration = 10,
                          VaryByParam = "id",
                          VaryByHeader = "accept-encoding",
                          VaryByCustom = "user;scheme",
                          VaryByContentEncoding = "gzip;deflate")]*/
        [OutputCache(Duration = 10)]
        public ActionResult GetRandomMessageWithCache()
        {
            string message = RandomMessage();

            ViewBag.Message = message + " (" + DateTime.Now.Millisecond + ")";
            return View("getrandommessage");
        }

        private static string RandomMessage()
        {
            var random = new Random();
            var what = random.Next(0, 5);
            string message = "";
            switch (what)
            {
                case 0:
                    message = "I Love Lamp";
                    break;
                case 1:
                    message = "It's so hot, milk was a bad choice";
                    break;
                case 2:
                    message = "Dorthey Mantooth was a saint";
                    break;
                case 3:
                    message = "Let's leave the mothers out of this";
                    break;
                case 4:
                    message = "Brick are you just saying you love things you see in the room.";
                    break;
                case 5:
                    message = "60% of the time it works every time.";
                    break;
            }
            return message;
        }

        #endregion

        #region // Template Master & Sections //

        public ActionResult Masterful()
        { 
            ViewBag.Title = "I am the title of this page, awesome.";
            return View();
        }

        #endregion
    }
}
