using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Jitex.JIT.Hooks.Token;

namespace Jitex.JIT.Hooks;

public abstract class Contextbase
{
    public bool IsResolved { get; set; }

    private MethodBase? _source;

    /// <summary>
    /// If context has source method from call.
    /// </summary>
    public bool HasSource { get; private set; }

    /// <summary>
    /// Method source from call
    /// </summary>
    public MethodBase? Source
    {
        get
        {
            if (!HasSource)
            {
                StackTrace trace = new(1, false);

                var methods = trace.GetFrames()
                    .Select(frame => frame.GetMethod())
                    .Where(method => method.DeclaringType.Assembly != typeof(Token.TokenHook).Assembly);

                _source = methods.FirstOrDefault();
                HasSource = _source != null;
            }

            return _source;
        }
    }

    protected Contextbase(MethodBase? source, bool hasSource)
    {
        _source = source;
        HasSource = hasSource;
    }
}