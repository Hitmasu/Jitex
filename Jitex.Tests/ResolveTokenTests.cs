using Jitex.JIT;
using Jitex.Tests.Context;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jitex.JIT.Context;
using Xunit;
using static Jitex.Tests.Utils;

namespace Jitex.Tests
{
    public class ResolveTokenTests
    {
        public ResolveTokenTests()
        {
            ManagedJit jit = ManagedJit.GetInstance();
            jit.AddCompileResolver(CompileResolver);
            jit.AddTokenResolver(TokenResolver);
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
            return new Caller().GetIdade() + 20;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int ResolveWithModuleReplace()
        {
            return new Caller().GetIdade() + 20;
        }

        private void TokenResolver(TokenContext context)
        {
            if (context.Source != null)
            {
                if (context.Source.Name == nameof(ResolveTokenReplace))
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
        }

        private void CompileResolver(CompileContext context)
        {
            if (context.Method == GetMethod<ResolveTokenTests>(nameof(ResolveTokenReplace)))
            {
                MethodInfo methodToReplace = GetMethod<Caller>(nameof(Caller.GetIdade));
                context.ResolveMethod(methodToReplace);
            }
            else if (context.Method == GetMethod<ResolveTokenTests>(nameof(ResolveWithModuleReplace)))
            {
                MethodInfo methodToReplace = GetMethod<Caller>(nameof(Caller.GetIdade));
                context.ResolveMethod(methodToReplace);
            }
        }
    }
}
