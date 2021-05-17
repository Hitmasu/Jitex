﻿using System.Threading.Tasks;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("Jitex.Tests.TestConfiguration", "Jitex.Tests")]

namespace Jitex.Tests
{
    
    public class TestConfiguration : XunitTestFramework
    {
        public TestConfiguration(IMessageSink messageSink) : base(messageSink)
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InterceptorCall);
        }

        private async ValueTask InterceptorCall(CallContext context)
        {
            // //When return of method is a ValueTask, DisposeTestClass will raise an exception "Internal CLR Error"
            // //I dont know why that happen in xunit, but preventing him to be called, resolve this problem.
            // //TODO: Need discover why Internal CLR Error is raised when returns is a ValueTask.
            if (context.Method.Name == "DisposeTestClass")
            {
                context.ProceedCall = false;
                return;
            }
        }

        private void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "DisposeTestClass")
                context.InterceptCall();
        }
    }
}