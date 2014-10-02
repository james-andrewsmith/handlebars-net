using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Handlebars
{
    public interface IHandlebarsTemplate
    {
        string Render(string name, string json);
    }
}
