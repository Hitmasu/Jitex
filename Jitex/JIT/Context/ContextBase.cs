using System.Reflection;
using Jitex.Utils;

namespace Jitex.JIT.Context
{
    public abstract  class ContextBase
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
                    _source = StackHelper.GetSourceCall(typeof(ManagedJit).Assembly);
                    HasSource = true;
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