using Jitex.Builder;
using Jitex.JIT;
using System;
using System.Reflection;
using Xunit;
using static Jitex.Tests.Utils;

namespace Jitex.Tests
{
    public class ReplaceILTests
    {
        public ReplaceILTests()
        {
            ManagedJit jit = ManagedJit.GetInstance();
            jit.OnPreCompile = OnPreCompile;
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
            Assert.True(ob is ReplaceILTests, $"Body not replaced. Return type {typeof(object).Name}.");
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
            return new ReplaceILTests();
        }

        public object ReturnSimpleObjReplace()
        {
            object ob = new ReplaceILTests();
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

        public string LocalVariableNativeType()
        {
            return typeof(object).Name;
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

        public string LocalVariableReferenceType()
        {
            return typeof(DateTime).Name;
        }

        public string LocalVariableReferenceTypeReplace()
        {
            ManagedJit type1 = ManagedJit.GetInstance();
            ReplaceILTests type2 = this;
            Random type3 = new Random();
            CorElementType type4 = CorElementType.ELEMENT_TYPE_OBJECT;
            return $"{type1.GetType().Name} | {type2.GetType().Name} | {type3.GetType().Name} | {type4.GetType().Name}";
        }

        #endregion

        private ReplaceInfo OnPreCompile(MethodBase method)
        {
            if (method == GetMethod<ReplaceILTests>(nameof(EmptyBodyTest)))
            {
                return new ReplaceInfo(GetMethod<ReplaceILTests>(nameof(EmptyBodyReplace)));
            }

            if (method == GetMethod<ReplaceILTests>(nameof(BodyImpTest)))
            {
                return new ReplaceInfo(GetMethod<ReplaceILTests>(nameof(BodyImpReplace)));
            }

            if (method == GetMethod<ReplaceILTests>(nameof(ReturnSimpleInt)))
            {
                return new ReplaceInfo(GetMethod<ReplaceILTests>(nameof(ReturnSimpleIntReplace)));
            }

            if (method == GetMethod<ReplaceILTests>(nameof(ReturnSimpleDouble)))
            {
                return new ReplaceInfo(GetMethod<ReplaceILTests>(nameof(ReturnSimpleDoubleReplace)));
            }

            if (method == GetMethod<ReplaceILTests>(nameof(ReturnSimpleObj)))
            {
                return new ReplaceInfo(GetMethod<ReplaceILTests>(nameof(ReturnSimpleObjReplace)));
            }

            if (method == GetMethod<ReplaceILTests>(nameof(LocalVariableNativeType)))
            {
                return new ReplaceInfo(GetMethod<ReplaceILTests>(nameof(LocalVariableNativeTypeReplace)));
            }

            if (method == GetMethod<ReplaceILTests>(nameof(LocalVariableReferenceType)))
            {
                return new ReplaceInfo(GetMethod<ReplaceILTests>(nameof(LocalVariableReferenceTypeReplace)));
            }

            return null;
        }

    }
}
