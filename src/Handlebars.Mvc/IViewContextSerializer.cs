using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Web;
using System.Web.Mvc;

namespace HandlebarsViewEngine
{
    public interface IViewContextSerializer
    {
        string Serialize(ViewContext viewContext);
    }

    public sealed class DefaultViewSerializer : IViewContextSerializer
    {
        public string Serialize(ViewContext viewContext)
        {
            return JsonHelper.ToJson(viewContext.ViewData);
        }
    }
}
