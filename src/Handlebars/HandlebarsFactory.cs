using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ninject;
using Ninject.Modules;

namespace Handlebars
{
    /// <summary>
    /// A tiny wrapper around ninject for those who don't want to inherit it...
    /// </summary>
    public sealed class HandlebarsFactory
    {
        static HandlebarsFactory() 
        {
            kernel = new StandardKernel();

            // doesn't need anything
            kernel.Bind<IHandlebarsResourceProvider>()
                  .To<LocalResourceProvider>()
                  .InSingletonScope();

            // requires resource loading
            kernel.Load(HandlebarsConfiguration.Instance.Engine + ".dll");

            // requires engine and resource loading
            kernel.Bind<IHandlebarsTemplate>()
                  .To<DevelopmentHandlebarsTemplate>()
                  // .To<StandardHandlebarsTemplate>()
                  .InSingletonScope();

        }

        private static StandardKernel kernel;

        public static IHandlebarsEngine CreateEngine()
        {
            return kernel.Get<IHandlebarsEngine>();
        }

        public static IHandlebarsTemplate CreateTemplate()
        {
            return kernel.Get<IHandlebarsTemplate>();
        }

        public static IHandlebarsEngine Recreate()
        {
            var k = new StandardKernel();
            k.Load(HandlebarsConfiguration.Instance.Engine + ".dll");
            kernel = k;
            return CreateEngine();
        }
    }
}
