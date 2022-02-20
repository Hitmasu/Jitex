using System.Threading.Tasks;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;
using Microsoft.Extensions.Logging;
using Serilog;
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
        }
    }
}