using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Handlebars.WebApi
{
    public interface IRequestFormatter
    {

        string GetContext(HttpRequestMessage request, object context);

    }
}
