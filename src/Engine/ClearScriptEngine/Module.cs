using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autofac;

namespace Handlebars
{
    public sealed class ClearScriptModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ClearScriptEngine>()
                   .As<IHandlebarsEngine>()
                   .SingleInstance(); 
        }
    }
}
