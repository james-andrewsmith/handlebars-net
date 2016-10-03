using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Handlebars.WebApi
{
    public interface IRequestFormatter
    {

        string GetContext(HttpRequest request, object context);

    }
}
