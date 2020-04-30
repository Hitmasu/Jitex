using System;
using System.Diagnostics;
using Jitex.JIT;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jitex.Tests.Context;
using Xunit;
using static Jitex.Tests.Utils;

namespace Jitex.Tests
{
    public class ResolveTokenTests
    {
        public ResolveTokenTests()
        {
            ManagedJit managedJit = ManagedJit.GetInstance();
            managedJit.OnPreCompile = OnPreCompile;
            managedJit.OnResolveToken = OnResolveToken;
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

        private void OnResolveToken(TokenContext token)
        {
            //if(token.Source != null)
            //    Trace.WriteLine(token.Source.Name);

            if (token.Source == GetMethod<ResolveTokenTests>(nameof(ResolveTokenReplace)))
            {
                Type personType = typeof(Caller).Module.GetType("Jitex.Tests.Context.Person");

                switch (token.MetadataToken)
                {
                    case 0x06000005:
                        ConstructorInfo ctor = personType.GetConstructor(Type.EmptyTypes);
                        token.ResolveConstructor(ctor);
                        break;

                    case 0x06000003:
                        MethodBase get_Idade = personType.GetMethod("get_Idade");
                        token.ResolveMethod(get_Idade);
                        break;
                }
            }
            else if (token.Source == GetMethod<ResolveTokenTests>(nameof(ResolveWithModuleReplace)))
            {
                token.ResolveFromModule(typeof(Caller).Module);
            }
        }

        private ReplaceInfo OnPreCompile(MethodBase method)
        {
            if (method == GetMethod<ResolveTokenTests>("ResolveTokenReplace") || method == GetMethod<ResolveTokenTests>("ResolveWithModuleReplace"))
            {
                MethodInfo methodToReplace = GetMethod<Caller>("GetIdade");
                return new ReplaceInfo(methodToReplace);
            }

            return null;
        }
    }
}
