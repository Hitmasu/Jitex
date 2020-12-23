﻿using System;
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
        public ResolveTokenTests()
        {
            JitexManager.AddMethodResolver(CompileResolver);
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

                switch (context.MetadataToken)
                {
                    case 0x06000006:
                        ConstructorInfo ctor = personType.GetConstructor(Type.EmptyTypes);
                        context.ResolveConstructor(ctor);
                        break;

                    case 0x06000004:
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

        private void CompileResolver(MethodContext context)
        {
            if (context.Method == GetMethod<ResolveTokenTests>(nameof(ResolveTokenReplace)))
            {
                MethodInfo methodToReplace = GetMethod<Caller>(nameof(Caller.GetIdade));
                var il = new Jitex.Builder.Method.MethodBody(methodToReplace).ReadIL();
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
