using System;
using System.Runtime.CompilerServices;
using Jitex.Builder.Method;
using Jitex.JIT.Context;
using Jitex.Tests.Context;
using Xunit;
using static Jitex.Tests.Utils;

namespace Jitex.Tests.Resolvers
{
    [Collection("Manager")]
    public class ResolveMethodTests
    {
        public ResolveMethodTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        #region EmptyBody

        [Fact]
        public void EmptyBodyTest()
        {
            Assert.True(false, "IL not replaced");
        }

        public void EmptyBodyReplace()
        {
            Assert.True(true);
        }

        #endregion

        #region Body Implementation

        [Fact]
        public void BodyImpTest()
        {
            double a = 99999;
            double b = 999999;
            double max = Math.Max(a, b);

            Assert.True(max == a, "Body not replaced.");
        }

        public void BodyImpReplace()
        {
            int a = 10;
            int b = 20;
            int max = Math.Max(b, a);

            Assert.True(max == b);
        }

        #endregion

        #region Return

        [Fact]
        public void ReturnNativeTypeTest()
        {
            int returnInt = ReturnSimpleInt();
            Assert.True(returnInt == 4321, $"Body not replaced. Return type {nameof(Int32)}.");

            double returnDouble = ReturnSimpleDouble();
            Assert.True(returnDouble == 1.5d, $"Body not replaced. Return type {nameof(Double)}.");
        }

        [Fact]
        public void ReturnReferenceTypeTest()
        {
            object ob = ReturnSimpleObj();
            Assert.True(ob is Caller, $"Body not replaced. Return type {nameof(Object)}.");
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public int ReturnSimpleInt()
        {
            return 1234;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int ReturnSimpleIntReplace()
        {
            return 4321;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public double ReturnSimpleDouble()
        {
            return 0.5d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public double ReturnSimpleDoubleReplace()
        {
            return 0.5d + 1.0d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public object ReturnSimpleObj()
        {
            return new object();
        }

        public object ReturnSimpleObjReplace()
        {
            object ob = new Caller();
            return ob;
        }

        #endregion

        #region Local Variables

        [Fact]
        public void LocalVariableNativeTypeTest()
        {
            string expected = "Boolean | Int32 | Double | Decimal | String";
            string actual = LocalVariableNativeType();

            Assert.True(expected == actual, $"\nVariable not inserted. \n {expected} \n {actual}");
        }

        [Fact]
        public void LocalVariableReferenceTypeTest()
        {
            string expected = $"{nameof(ResolveMethodTests)} | Random | CorElementType";
            string actual = LocalVariableReferenceType();

            Assert.True(expected == actual, "\nVariable not inserted.");
        }

        public string LocalVariableNativeTypeReplace()
        {
            bool type1 = default;
            int type2 = default;
            double type3 = default;
            decimal type4 = default;
            string type5 = string.Empty;

            return $"{type1.GetType().Name} | {type2.GetType().Name} | {type3.GetType().Name} | {type4.GetType().Name} | {type5.GetType().Name}";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string LocalVariableNativeType()
        {
            return nameof(Object);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]

        public string LocalVariableReferenceType()
        {
            return nameof(DateTime);
        }

        public string LocalVariableReferenceTypeReplace()
        {
            ResolveMethodTests type2 = this;
            Random type3 = new Random();
            CorElementType type4 = CorElementType.ELEMENT_TYPE_OBJECT;
            return $"{type2.GetType().Name} | {type3.GetType().Name} | {type4.GetType().Name}";
        }

        #endregion

        private void MethodResolver(MethodContext context)
        {
            if (context.Method == GetMethod<ResolveMethodTests>(nameof(EmptyBodyTest)))
            {
                context.ResolveMethod(GetMethod<ResolveMethodTests>(nameof(EmptyBodyReplace)));
            }
            else if (context.Method == GetMethod<ResolveMethodTests>(nameof(BodyImpTest)))
            {
                context.ResolveMethod(GetMethod<ResolveMethodTests>(nameof(BodyImpReplace)));
            }
            else if (context.Method == GetMethod<ResolveMethodTests>(nameof(ReturnSimpleInt)))
            {
                context.ResolveMethod(GetMethod<ResolveMethodTests>(nameof(ReturnSimpleIntReplace)));
            }
            else if (context.Method == GetMethod<ResolveMethodTests>(nameof(ReturnSimpleDouble)))
            {
                context.ResolveMethod(GetMethod<ResolveMethodTests>(nameof(ReturnSimpleDoubleReplace)));
            }
            else if (context.Method == GetMethod<ResolveMethodTests>(nameof(ReturnSimpleObj)))
            {
                context.ResolveMethod(GetMethod<ResolveMethodTests>(nameof(ReturnSimpleObjReplace)));
            }
            else if (context.Method == GetMethod<ResolveMethodTests>(nameof(LocalVariableNativeType)))
            {
                context.ResolveMethod(GetMethod<ResolveMethodTests>(nameof(LocalVariableNativeTypeReplace)));
            }
            else if (context.Method == GetMethod<ResolveMethodTests>(nameof(LocalVariableReferenceType)))
            {
                context.ResolveMethod(GetMethod<ResolveMethodTests>(nameof(LocalVariableReferenceTypeReplace)));
            }
        }
    }
}