using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;


namespace HandlebarsViewEngine
{
    /// <summary>
    /// Takes a request/viewbag and turns it into a fully formed JSON object
    /// including detected device, additional application specific information like the user
    /// their vitals, split testing and any other app data from their session.
    /// </summary>
    public class SampleRequestFormatter
    {
        private string GetContextJson(HtmlHelper helper, object context)
        {
            string protocol = "http",
                   hostname = helper.ViewContext.HttpContext.Request.Url.DnsSafeHost;

            if (!string.IsNullOrEmpty(helper.ViewContext.HttpContext.Request.Headers["x-arr-ssl"]))
            {
                protocol = "https";
            }

            // get the context
            var json = JsonHelper.ToJson(context);
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
#if DEBUG
            sb.Append("debug:true,");
#else
            sb.Append("debug:false,");
#endif
            sb.Append("gzip: \".gz\"");
            //sb.Append("_device: {");
            //sb.Append("type: 'ipad',");
            //sb.Append("retina: true");
            //sb.Append("},");
            sb.Append("_request: {");
            sb.Append("host: '" + hostname + "',");
            sb.Append("protocol: '" + protocol + "',");
            sb.Append("fqdn: '" + protocol + "://" + hostname + "'");
            sb.Append("path: '" + helper.ViewContext.RequestContext.HttpContext.Request.Path.Trim('/') + "',");

            sb.Append("},");
            sb.Append("_config: {");
            // sb.Append("cdn: '" + Configuration.Instance.CDN + "',");
            // sb.Append("git: '" + Configuration.Instance.Git + "'");
            // sb.Append("git: '" + Configuration.Instance.Git + "'");
            sb.Append("},");
            // sb.Append("_user: {");
            // sb.Append("authenticated: true,");
            // sb.Append("name: 'James Andrews',");
            // sb.Append("first_name: 'James',");
            // sb.Append("last_name: 'Andrews',");
            // sb.Append("gender: 'male',");
            // sb.Append("group: 'A',"); // vs B,C,D
            // sb.Append("geoip: 'Sydney, Australia',");
            // sb.Append("vitals: {");
            // sb.Append("women: true,");
            // sb.Append("age: 29,");
            // sb.Append("style: 'grunge'");
            // sb.Append("}");
            // sb.Append("},");
            // sb.Append("_testing: {");
            // sb.Append("splits: {");
            // sb.Append("'product-layout-test' : 'control',");
            // sb.Append("'search-facet-dropdown' : 'A'");
            // sb.Append("}");
            // sb.Append("}");
            // actual context
            // sb.Append("data: [],");
            sb.Append("data: " + json);
            sb.Append("}");

            json = sb.ToString();
            return json;
        }
    }

}
