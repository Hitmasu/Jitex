using System;
using System.Threading.Tasks;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;
using Xunit.Sdk;
using ILogger = Serilog.ILogger;

[assembly: Xunit.TestFramework("Jitex.Tests.TestConfiguration", "Jitex.Tests")]

namespace Jitex.Tests
{
    public class TestConfiguration : XunitTestFramework
    {
        public TestConfiguration(IMessageSink messageSink) : base(messageSink)
        {
            ILogger logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .CreateLogger();
    
            ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(logger);

            JitexLogger.SetLogger(loggerFactory);

            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InterceptorCall);
        }

        private async ValueTask InterceptorCall(CallContext context)
        {
            // //When return of method is a ValueTask, DisposeTestClass will raise an exception "Internal CLR Error"
            // //I dont know why that happen in xunit, but preventing him to be called, resolve this problem.
            // //TODO: Need discover why Internal CLR Error is raised when returns is a ValueTask.
            if (context.Method.Name == "DisposeTestClass")
                context.ProceedCall = false;
        }

        private void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "DisposeTestClass")
                context.InterceptCall();
        }
    }
}