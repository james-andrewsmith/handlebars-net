using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Autofac;

namespace Handlebars
{
    /// <summary>
    /// A tiny wrapper around ninject for those who don't want to inherit it...
    /// </summary>
    public sealed class HandlebarsFactory
    {
        static HandlebarsFactory() 
        { 
            var builder = new ContainerBuilder();

            // doesn't need anything
            builder.RegisterType<LocalResourceProvider>()
                   .As<IHandlebarsResourceProvider>()
                   .SingleInstance();             

            // requires resource loading
            builder.RegisterAssemblyModules(Assembly.LoadFile(HandlebarsConfiguration.Instance.Engine + ".dll"));

            // requires engine and resource loading
            builder.RegisterType<DevelopmentHandlebarsTemplate>()
                   .As<IHandlebarsTemplate>()
                   .SingleInstance();    
             
            container = builder.Build();
        }

        private static IContainer container;        

        public static IHandlebarsEngine CreateEngine()
        {
            return container.Resolve<IHandlebarsEngine>();
        }

        public static IHandlebarsTemplate CreateTemplate()
        {
            return container.Resolve<IHandlebarsTemplate>();
        }

        public static IHandlebarsEngine Recreate()
        {
            var builder = new ContainerBuilder();

            // doesn't need anything
            builder.RegisterType<LocalResourceProvider>()
                   .As<IHandlebarsResourceProvider>()
                   .SingleInstance();

            // requires resource loading
            builder.RegisterAssemblyModules(Assembly.LoadFile(HandlebarsConfiguration.Instance.Engine + ".dll"));

            // requires engine and resource loading
            builder.RegisterType<DevelopmentHandlebarsTemplate>()
                   .As<IHandlebarsTemplate>()
                   .SingleInstance();

            // 
            container = builder.Build();

            return CreateEngine();
        }
    }
}
