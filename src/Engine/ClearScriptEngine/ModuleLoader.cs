using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ClearScript;

namespace Handlebars
{
    public class ModuleLoader
    {

        private readonly ScriptEngine _engine;

        public ModuleLoader(ScriptEngine engine)
        {
            _engine = engine;
        }

        public async Task LoadModuleAsync(dynamic context, string name, string url)
        {
            using (var reader = File.OpenText(url))
            {
                _engine.Execute(await reader.ReadToEndAsync());
                context.completeLoad();
            }
        }
    }
}
