using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jitex.AspNetCore
{
    public static class LoadJitex
    {
        public static void UseModule(this IApplicationBuilder app, params Type[] modules)
        {
            IHostApplicationLifetime applicationLifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            if (applicationLifetime.ApplicationStarted.IsCancellationRequested)
            {
                LoadModules(modules);
            }

            applicationLifetime.ApplicationStarted.Register(() =>
            {
                LoadModules(modules);
            });
        }

        public static void UseModule<TModule>(this IApplicationBuilder app) where TModule : JitexModule, new()
        {
            UseModule(app, typeof(TModule));
        }

        private static void LoadModules(params Type[] modules)
        {
            foreach (Type module in modules)
            {
                JitexManager.LoadModule(module);
            }
        }
    }
}