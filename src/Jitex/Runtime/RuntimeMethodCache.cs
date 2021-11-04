using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jitex.Exceptions;
using Jitex.JIT.Handlers;
using Jitex.Utils;
using Jitex.Utils.Comparer;

namespace Jitex.Runtime
{
    internal static class RuntimeMethodCache
    {
        private static readonly ConcurrentBag<MethodCompiled> CompiledMethods = new();

        internal static void AddMethod(MethodCompiled methodCompiled)
        {
            CompiledMethods.Add(methodCompiled);
        }

        public static async Task<NativeCode> GetNativeCodeAsync(MethodBase method, CancellationToken cancellationToken)
        {
            if (MethodHelper.HasCanon(method))
                method = MethodHelper.GetBaseMethodGeneric(method);

            MethodCompiled? methodCompiled = GetMethodCompiledInfo(method);

            if (methodCompiled == null)
            {
                if (!JitexManager.IsEnabled)
                    throw new JitexNotEnabledException("Jitex is not enabled!");

                await RuntimeHelperExtension.InternalPrepareMethodAsync(method).ConfigureAwait(false);

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    methodCompiled = GetMethodCompiledInfo(method);
                } while (methodCompiled == null);
            }

            return new NativeCode(methodCompiled.NativeCode.Address, methodCompiled.NativeCode.Size);
        }

        public static MethodCompiled? GetMethodCompiledInfo(MethodBase method)
        {
            return CompiledMethods.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.Method, method));
        }
    }
}