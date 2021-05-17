using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jitex.JIT.Context;
using Jitex.Tests.Context;
using Xunit;
using static Jitex.Tests.Utils;

namespace Jitex.Tests.Resolvers
{
    [Collection("Manager")]
    public class ResolveTokenTests
    {
        private static readonly int _ctorPersonToken = 0;
        private static readonly int _getAgePersonPropertyToken = 0;

        static ResolveTokenTests()
        {
            Type personType = typeof(Caller).Module.GetType("Jitex.Tests.Context.Person");
            ConstructorInfo ctor = personType.GetConstructor(Type.EmptyTypes);
            MethodBase getAge = personType.GetMethod("get_Age");

            _ctorPersonToken = ctor.MetadataToken;
            _getAgePersonPropertyToken = getAge.MetadataToken;
        }

        public ResolveTokenTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddTokenResolver(TokenResolver);
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
            return new Caller().GetWrong();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int ResolveWithModuleReplace()
        {
            return new Caller().GetWrong();
        }

        private void TokenResolver(TokenContext context)
        {
            if (context.Source == GetMethod<ResolveTokenTests>(nameof(ResolveTokenReplace)))
            {
                Type personType = typeof(Caller).Module.GetType("Jitex.Tests.Context.Person");
                
                if (context.MetadataToken == _ctorPersonToken)
                {
                    ConstructorInfo ctor = personType.GetConstructor(Type.EmptyTypes);
                    context.ResolveConstructor(ctor);
                }
                else if (context.MetadataToken == _getAgePersonPropertyToken)
                {
                    MethodBase getAge = personType.GetMethod("get_Age");
                    context.ResolveMethod(getAge);
                }
            }
            else if (context.Source == GetMethod<ResolveTokenTests>(nameof(ResolveWithModuleReplace)) && (context.MetadataToken == _ctorPersonToken || context.MetadataToken == _getAgePersonPropertyToken))
            {
                context.ResolveFromModule(typeof(Caller).Module);
            }
        }

        private void MethodResolver(MethodContext context)
        {
            if (context.Method == GetMethod<ResolveTokenTests>(nameof(ResolveTokenReplace)))
            {
                MethodInfo methodToReplace = GetMethod<Caller>(nameof(Caller.GetAge));
                context.ResolveMethod(methodToReplace);
            }
            else if (context.Method == GetMethod<ResolveTokenTests>(nameof(ResolveWithModuleReplace)))
            {
                MethodInfo methodToReplace = GetMethod<Caller>(nameof(Caller.GetAge));
                context.ResolveMethod(methodToReplace);
            }
        }
    }
}