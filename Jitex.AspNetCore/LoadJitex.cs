using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jitex.AspNetCore
{
    public static class LoadJitex
    {
        public static void UseJitex(this IApplicationBuilder app)
        {
            IHostApplicationLifetime applicationLifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        }
    }
}