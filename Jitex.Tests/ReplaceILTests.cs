using Jitex.Tests.Context;
using System;
using Jitex.Builder.Method;
using Jitex.JIT.Context;
using Xunit;
using static Jitex.Tests.Utils;

namespace Jitex.Tests
{
    public class ReplaceILTests
    {
        public ReplaceILTests()
        {
            Jitex.AddCompileResolver(CompileResolver);
        }

        #region EmptyBody

        [Fact]
        public void EmptyBodyTest()
        {
            Assert.True(false, "IL not replaced.");
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
            Assert.True(returnInt == 4321, $"Body not replaced. Return type {typeof(int).Name}.");

            double returnDouble = ReturnSimpleDouble();
            Assert.True(returnDouble == 1.5d, $"Body not replaced. Return type {typeof(double).Name}.");
        }

        [Fact]
        public void ReturnReferenceTypeTest()
        {
            object ob = ReturnSimpleObj();
            Assert.True(ob is Caller, $"Body not replaced. Return type {typeof(object).Name}.");
        }

        public int ReturnSimpleInt()
        {
            return 1234;
        }

        public int ReturnSimpleIntReplace()
        {
            return 4321;
        }

        public double ReturnSimpleDouble()
        {
            return 0.5d;
        }

        public double ReturnSimpleDoubleReplace()
        {
            return 0.5d + 1.0d;
        }

        public object ReturnSimpleObj()
        {
            return new Caller();
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

            Assert.True(expected == actual, "\nVariable not inserted.");
        }

        [Fact]
        public void LocalVariableReferenceTypeTest()
        {
            string expected = "ManagedJit | ReplaceILTests | Random | CorElementType";
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


        public string LocalVariableNativeType()
        {
            return nameof(Object);
        }


        public string LocalVariableReferenceType()
        {
            return nameof(DateTime);
        }

        public string LocalVariableReferenceTypeReplace()
        {
            ReplaceILTests type2 = this;
            Random type3 = new Random();
            CorElementType type4 = CorElementType.ELEMENT_TYPE_OBJECT;
            return $"{type2.GetType().Name} | {type3.GetType().Name} | {type4.GetType().Name}";
        }

        #endregion

        private void CompileResolver(CompileContext context)
        {
            if (context.Method == GetMethod<ReplaceILTests>(nameof(EmptyBodyTest)))
            {
                context.ResolveMethod(GetMethod<ReplaceILTests>(nameof(EmptyBodyReplace)));
            }
            else if (context.Method == GetMethod<ReplaceILTests>(nameof(BodyImpTest)))
            {
                context.ResolveMethod(GetMethod<ReplaceILTests>(nameof(BodyImpReplace)));
            }
            else if (context.Method == GetMethod<ReplaceILTests>(nameof(ReturnSimpleInt)))
            {
                context.ResolveMethod(GetMethod<ReplaceILTests>(nameof(ReturnSimpleIntReplace)));
            }
            else if (context.Method == GetMethod<ReplaceILTests>(nameof(ReturnSimpleDouble)))
            {
                context.ResolveMethod(GetMethod<ReplaceILTests>(nameof(ReturnSimpleDoubleReplace)));
            }
            else if (context.Method == GetMethod<ReplaceILTests>(nameof(ReturnSimpleObj)))
            {
                context.ResolveMethod(GetMethod<ReplaceILTests>(nameof(ReturnSimpleObjReplace)));
            }
            else if (context.Method == GetMethod<ReplaceILTests>(nameof(LocalVariableNativeType)))
            {
                context.ResolveMethod(GetMethod<ReplaceILTests>(nameof(LocalVariableNativeTypeReplace)));
            }
            else if (context.Method == GetMethod<ReplaceILTests>(nameof(LocalVariableReferenceType)))
            {
                context.ResolveMethod(GetMethod<ReplaceILTests>(nameof(LocalVariableReferenceTypeReplace)));
            }
        }
    }
}