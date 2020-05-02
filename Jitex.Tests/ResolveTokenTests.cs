using Jitex.JIT;
using Jitex.Tests.Context;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using static Jitex.Tests.Utils;

namespace Jitex.Tests
{
    public class ResolveTokenTests
    {
        public ResolveTokenTests()
        {
            ManagedJit jit = JitexInstance.GetInstance();
            jit.AddCompileResolver(OnResolveCompile);
            jit.AddTokenResolver(OnResolveToken);
        }

        [Fact]
        public void ResolveTokenTest()
        {
            int number = ResolveTokenReplace();
            Assert.True(number == 100, "Body not injected!");
        }

        [Fact]
        public void ResolveWithModuleTest()
        {
            int number = ResolveWithModuleReplace();
            Assert.True(number == 100, "Body not injected!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int ResolveTokenReplace()
        {
            return -2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int ResolveWithModuleReplace()
        {
            return -3;
        }

        private void OnResolveToken(TokenContext context)
        {
            if (context.Source == GetMethod<ResolveTokenTests>(nameof(ResolveTokenReplace)))
            {
                Type personType = typeof(Caller).Module.GetType("Jitex.Tests.Context.Person");

                switch (context.MetadataToken)
                {
                    case 0x06000005:
                        ConstructorInfo ctor = personType.GetConstructor(Type.EmptyTypes);
                        context.ResolveConstructor(ctor);
                        break;

                    case 0x06000003:
                        MethodBase get_Idade = personType.GetMethod("get_Idade");
                        context.ResolveMethod(get_Idade);
                        break;
                }
            }
            else if (context.Source == GetMethod<ResolveTokenTests>(nameof(ResolveWithModuleReplace)))
            {
                context.ResolveFromModule(typeof(Caller).Module);
            }
        }

        private void OnResolveCompile(CompileContext context)
        {
            if (context.Method == GetMethod<ResolveTokenTests>("ResolveWithModuleReplace"))
            {
                MethodInfo methodToReplace = GetMethod<Caller>("GetIdade");
                context.ResolveMethod(methodToReplace);
            }
            else if (context.Method == GetMethod<ResolveTokenTests>("ResolveTokenReplace"))
            {
                MethodInfo methodToReplace = GetMethod<Caller>("GetIdade");
                context.ResolveMethod(methodToReplace);
            }
        }
    }
}
