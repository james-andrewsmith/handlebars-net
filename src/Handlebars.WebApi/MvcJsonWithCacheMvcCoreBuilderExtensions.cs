// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Handlebars.WebApi
{
    public static class MvcJsonMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddJsonWithCacheFormatters(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddJsonWithCacheFormatterServices(builder.Services);
            return builder;
        }

        public static IMvcCoreBuilder AddJsonWithCacheFormatters(
            this IMvcCoreBuilder builder,
            Action<JsonSerializerSettings> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AddJsonWithCacheFormatterServices(builder.Services);

            builder.Services.Configure<MvcJsonOptions>((options) => setupAction(options.SerializerSettings));

            return builder;
        }
         

        // Internal for testing.
        internal static void AddJsonWithCacheFormatterServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcJsonWithCacheMvcOptionsSetup>());
            services.TryAddSingleton<JsonWithCacheResultExecutor>();
        }
    }
}