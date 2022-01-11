using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Jitex.Utils;

namespace Jitex.JIT.Context
{
    public abstract class ContextBase
    {
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

                    IEnumerable<MethodBase> methods = trace.GetFrames()
                        .Select(frame => frame.GetMethod())
                        .Where(method => method.DeclaringType.Assembly != typeof(ManagedJit).Assembly);

                    _source = methods.FirstOrDefault();
                    HasSource = _source != null;
                }

                return _source;
            }
        }

        protected ContextBase(MethodBase? source, bool hasSource)
        {
            _source = source;
            HasSource = hasSource;
        }
    }
}