using System.Reflection;

namespace Jitex.JIT.Hooks.Exception;

public class ExceptionContext : Contextbase
{
    public ExceptionContext(MethodBase? source, bool hasSource) : base(source, hasSource)
    {
    }
}