using Jitex.Hook;
using Jitex.JIT;
using Jitex.PE;
using System.Reflection;

namespace FrameworkTest
{
    public class Framework
    {
        private readonly ManagedJit _managedJit;
        private readonly Module _module;
        private readonly MetadataInfo _metadata;
        private readonly Detour _detour;

        public Framework(Module module)
        {
            _module = module;
            _metadata = new MetadataInfo(module.Assembly);
            _managedJit = ManagedJit.GetInstance();
            _managedJit.OnPreCompile = OnPreCompile;
            _detour = new Detour(typeof(Framework).Module);
        }

        private ReplaceInfo OnPreCompile(MethodBase method)
        {
            return _detour.TryDetourMethod(method);
        }
    }
}
