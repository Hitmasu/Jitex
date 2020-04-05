using Jitex.Builder;
using Jitex.JIT;
using System;
using System.Reflection;
using Xunit;
using MethodBody = Jitex.Builder.MethodBody;

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
            if (method == GetMethod(nameof(EmptyBodyTest)))
            {
                MethodBody body = new MethodBody(GetMethod(nameof(EmptyBodyReplace)));
                return new ReplaceInfo(body);
            }

            if (method == GetMethod(nameof(BodyImpTest)))
            {
                MethodBody body = new MethodBody(GetMethod(nameof(BodyImpReplace)));
                return new ReplaceInfo(body);
            }

            if (method == GetMethod(nameof(ReturnSimpleInt)))
            {
                MethodBody body = new MethodBody(GetMethod(nameof(ReturnSimpleIntReplace)));
                return new ReplaceInfo(body);
            }

            if (method == GetMethod(nameof(ReturnSimpleDouble)))
            {
                MethodBody body = new MethodBody(GetMethod(nameof(ReturnSimpleDoubleReplace)));
                return new ReplaceInfo(body);
            }

            if (method == GetMethod(nameof(ReturnSimpleObj)))
            {
                MethodBody body = new MethodBody(GetMethod(nameof(ReturnSimpleObjReplace)));
                return new ReplaceInfo(body);
            }

            if (method == GetMethod(nameof(LocalVariableNativeType)))
            {
                MethodBody body = new MethodBody(GetMethod(nameof(LocalVariableNativeTypeReplace)));
                return new ReplaceInfo(body);
            }

            if (method == GetMethod(nameof(LocalVariableReferenceType)))
            {
                MethodBody body = new MethodBody(GetMethod(nameof(LocalVariableReferenceTypeReplace)));
                return new ReplaceInfo(body);
            }

            return null;
        }

        private MethodInfo GetMethod(string name)
        {
            return GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
        }
    }
}
