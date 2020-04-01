using Jitex.Attributes;
using Jitex.JIT;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using MethodBody = Jitex.Builder.MethodBody;

namespace Jitex.Hook
{
    public class Detour
    {
        private readonly IList<DetourInfo> _methodsLoaded = new List<DetourInfo>();

        public Detour(Module module)
        {
            foreach (MethodInfo method in module.GetTypes().SelectMany(t => t.GetMethods()))
            {
                DetourAttribute attribute = method.GetCustomAttribute<DetourAttribute>(true);

                if (attribute != null)
                {
                    DetourInfo detourInfo = new DetourInfo(attribute, method);
                    _methodsLoaded.Add(detourInfo);
                }
            }
        }

        private DetourInfo GetDetour(MethodBase method)
        {
            return _methodsLoaded.FirstOrDefault(m => m.DetourAttribute.Equals(method));
        }

        public ReplaceInfo TryDetourMethod(MethodBase method)
        {
            DetourInfo detourInfo = GetDetour(method);

            if (detourInfo == null)
                return null;

            MethodInfo methodToInject = detourInfo.Method;

            DynamicMethod dm = new DynamicMethod("teste", typeof(int), null);
            var generator = dm.GetILGenerator();
            generator.Emit(OpCodes.Ldftn, methodToInject);
            generator.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, typeof(int), null);
            generator.Emit(OpCodes.Ret);

            dm.Invoke(null, null);
            return new ReplaceInfo(new MethodBody(dm));
        }
    }

    internal class DetourInfo
    {
        public DetourAttribute DetourAttribute { get; }

        /// <summary>
        ///     Method to Replace.
        /// </summary>
        public MethodInfo Method { get; }

        public DetourInfo(DetourAttribute detourAttribute, MethodBase method)
        {
            DetourAttribute = detourAttribute;
            Method = (MethodInfo) method;
        }
    }
}