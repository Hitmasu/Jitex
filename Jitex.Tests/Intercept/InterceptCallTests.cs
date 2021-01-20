using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Tests.Context;
using Xunit;

namespace Jitex.Tests.Intercept
{
    [Collection("Manager")]
    public class InterceptCallTests
    {
        private Point _point;
        private InterceptPerson _person;

        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> CallsIntercepted = new();
        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> MethodsCalled = new();

        static InterceptCallTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InterceptorCall);
        }

        /// <summary>
        /// A simple call test.
        /// </summary>
        /// <remarks>
        /// Intercept call but does nothing.
        /// 
        /// Expected:
        /// • Normal result.
        /// • Method original should be called.
        /// • Method should be intercepted.
        /// </remarks>
        [Theory]
        [InlineData(1, 1)]
        [InlineData(-1, 20)]
        [InlineData(short.MaxValue, short.MaxValue)]
        public void SimpleCallTest(int n1, int n2)
        {
            int result = SimpleSum(n1, n2);
            int expected = n1 + n2;

            Assert.Equal(expected, result);

            Assert.True(HasCalled(nameof(SimpleSum)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SimpleSum)), "Method not intercepted!");

            Assert.True(CountCalls(nameof(SimpleSum)) == 1, "Called more than expected!");
            Assert.True(CountIntercept(nameof(SimpleSum)) == 1, "Intercepted more than expected!");

            CallsIntercepted.TryRemove(nameof(SimpleCallTest), out _);
            MethodsCalled.TryRemove(nameof(SimpleCallTest), out _);
        }

        [Fact]
        public void ModifyPrimitiveReturnTest()
        {
            int result = SimpleSum(1, 1);
            int expected = 11;

            Assert.Equal(expected, result);

            Assert.False(HasCalled(nameof(SimpleSum)), "Call continued!");
            Assert.True(HasIntercepted(nameof(SimpleSum)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(SimpleSum)) == 1, "Intercepted more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyPrimitiveReturnTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyPrimitiveReturnTest), out _);
        }

        [Theory]
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public void ModifyPrimitiveParametersTest(int n1, int n2)
        {
            int result = SimpleSum(n1, n2);
            int expected = n1 + n2 + n2 * n1;

            Assert.Equal(expected, result);

            Assert.True(HasCalled(nameof(SimpleSum)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SimpleSum)), "Method not intercepted!");

            Assert.True(CountCalls(nameof(SimpleSum)) == 1, "Called more than expected!");
            Assert.True(CountIntercept(nameof(SimpleSum)) == 1, "Intercepted more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyPrimitiveParametersTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyPrimitiveParametersTest), out _);
        }

        [Theory]
        [InlineData("Michael", 26)]
        [InlineData("Brenda", 32)]
        [InlineData("Felipe", 48)]
        public void ModifyInstanceTest(string name, int age)
        {
            InterceptPerson person = new(name, age);

            int result = person.GetAgeAfter10Years();
            int expected = person.Age - 10;

            Assert.Equal(expected, result);
            Assert.Equal(name, person.Name);
            Assert.Equal(age, person.Age);

            Assert.True(HasIntercepted(nameof(InterceptPerson.GetAgeAfter10Years)), "Method not intercepted!");
            Assert.True(CountIntercept(nameof(InterceptPerson.GetAgeAfter10Years)) == 1, "Intercepted more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyInstanceTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyInstanceTest), out _);
        }

        [Theory]
        [InlineData("Michael", 26)]
        [InlineData("Brenda", 32)]
        [InlineData("Felipe", 48)]
        public void ModifyObjectParameterTest(string name, int age)
        {
            InterceptPerson person = new(name, age);

            int result = SumAge(person);
            int expected = age + 255 + 10;

            Assert.Equal(expected, result);
            Assert.Equal(name, person.Name);
            Assert.Equal(age + 255, person.Age);

            Assert.True(HasCalled(nameof(SumAge)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SumAge)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(SumAge)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(SumAge)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyObjectParameterTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyObjectParameterTest), out _);
        }

        [Theory]
        [InlineData("Michael", "Robert", " ")]
        [InlineData("Brenda", "John", "-")]
        [InlineData("Felipe", "Souza", ",")]
        public void ModifyNonPrimitivePrametersTest(string name1, string name2, string separator)
        {
            InterceptPerson person1 = new(name1, 20);
            InterceptPerson person2 = new(name2, 30);

            string result = ConcatNamePersons(person1, separator, person2);
            string expected = $"{ReverseText(name1)}{separator}{ReverseText(name2)}";

            Assert.Equal(expected, result);
            Assert.Equal(name1, person1.Name);
            Assert.Equal(name2, person2.Name);

            Assert.True(HasCalled(nameof(ConcatNamePersons)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(ConcatNamePersons)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(ConcatNamePersons)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(ConcatNamePersons)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyNonPrimitivePrametersTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyNonPrimitivePrametersTest), out _);
        }

        [Theory]
        [InlineData("Pedro", 20)]
        [InlineData("Jorge", 30)]
        [InlineData("Patricia", 99)]
        public void ModifyObjectReturnTest(string name, int age)
        {
            InterceptPerson person = new(name, age);

            InterceptPerson result = MakeNewPerson(person);
            InterceptPerson expected = new(ReverseText(name), age * age);

            Assert.Equal(expected.Name, result.Name);
            Assert.Equal(expected.Age, result.Age);

            Assert.Equal(name, person.Name);
            Assert.Equal(age, person.Age);

            Assert.False(HasCalled(nameof(MakeNewPerson)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(MakeNewPerson)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(MakeNewPerson)) == 1, "Intercepted more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyObjectReturnTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyObjectReturnTest), out _);
        }

        [Fact]
        public void InterceptRefParametersTest()
        {
            int valueType = 50;
            string name = "Lucia";
            InterceptPerson person = new(name, valueType);

            InterceptPerson result = SimpleCall(ref valueType, ref person);

            Assert.Equal(name, result.Name);
            Assert.Equal(valueType, result.Age);

            Assert.Equal(50, valueType);

            Assert.True(HasCalled(nameof(SimpleCall)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SimpleCall)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(SimpleCall)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(SimpleCall)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(InterceptRefParametersTest), out _);
            MethodsCalled.TryRemove(nameof(InterceptRefParametersTest), out _);
        }

        [Theory]
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public void ModifyRefParametersTest(int n1, int n2)
        {
            int n1Expected = n1 * n2;
            int n2Expected = n1 + n2;
            int resultExpected = n1Expected + n2Expected;

            int result = SimpleSumRef(ref n1, ref n2);

            Assert.Equal(resultExpected, result);
            Assert.Equal(n1Expected, n1);
            Assert.Equal(n2Expected, n2);

            Assert.True(HasCalled(nameof(SimpleSumRef)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SimpleSumRef)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(SimpleSumRef)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(SimpleSumRef)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyRefParametersTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyRefParametersTest), out _);
        }

        [Theory]
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public void ModifyOutParametersTest(int n1, int n2)
        {
            int resultExpected = n1 * n2 + n2 + n1;
            SimpleSumOut(ref n1, ref n2, out int result);

            Assert.Equal(resultExpected, result);
            Assert.Equal(n1, n1);
            Assert.Equal(n2, n2);

            Assert.False(HasCalled(nameof(SimpleSumOut)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SimpleSumOut)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(SimpleSumOut)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(SimpleSumOut)) == 0, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyOutParametersTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyOutParametersTest), out _);
        }

        [Theory]
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public void InterceptValueTypeRefReturn(int x, int y)
        {
            ref Point result = ref CreatePoint(x, y);

            TypedReference resultRef = __makeref(result);
            TypedReference pointRef = __makeref(_point);

            IntPtr resultAddr;
            IntPtr pointAddr;

            unsafe
            {
                resultAddr = *(IntPtr*)&resultRef;
                pointAddr = *(IntPtr*)&pointRef;
            }

            Assert.Equal(_point, result);

            Assert.Equal(pointAddr, resultAddr);

            Assert.True(HasCalled(nameof(CreatePoint)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(CreatePoint)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(CreatePoint)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(CreatePoint)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(InterceptValueTypeRefReturn), out _);
            MethodsCalled.TryRemove(nameof(InterceptValueTypeRefReturn), out _);
        }

        [Theory]
        [InlineData("Pedro", 20)]
        [InlineData("Jorge", 30)]
        [InlineData("Patricia", 99)]
        public void InterceptObjectRefReturn(string name, int age)
        {
            ref InterceptPerson result = ref CreatePerson(name, age);

            TypedReference resultRef = __makeref(result);
            TypedReference personRef = __makeref(_person);

            IntPtr resultAddr;
            IntPtr personAddr;

            unsafe
            {
                resultAddr = *(IntPtr*)&resultRef;
                personAddr = *(IntPtr*)&personRef;
            }

            Assert.Equal(name, _person.Name);
            Assert.Equal(age, _person.Age);

            Assert.Equal(_person, result);

            Assert.Equal(personAddr, resultAddr);

            Assert.True(HasCalled(nameof(CreatePerson)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(CreatePerson)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(CreatePerson)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(CreatePerson)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(InterceptObjectRefReturn), out _);
            MethodsCalled.TryRemove(nameof(InterceptObjectRefReturn), out _);
        }

        [Theory]
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public void ModifyValueTypeRefReturn(int x, int y)
        {
            ref Point result = ref CreatePoint(x, y);
            Point expected = new(x + y, x - y);

            Assert.Equal(result, expected);

            Assert.False(HasCalled(nameof(CreatePoint)), "Call continued!");
            Assert.True(HasIntercepted(nameof(CreatePoint)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(CreatePoint)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(CreatePoint)) == 0, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyValueTypeRefReturn), out _);
            MethodsCalled.TryRemove(nameof(ModifyValueTypeRefReturn), out _);
        }


        private static string ReverseText(string text) => new(text.Reverse().ToArray());

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SimpleSum(int n1, int n2)
        {
            AddMethodCall(nameof(SimpleSum));
            return n1 + n2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SimpleSumRef(ref int n1, ref int n2)
        {
            AddMethodCall(nameof(SimpleSumRef));
            return n1 + n2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref Point CreatePoint(int x, int y)
        {
            AddMethodCall(nameof(CreatePoint));
            _point = new Point(x, y);
            return ref _point;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref InterceptPerson CreatePerson(string name, int age)
        {
            AddMethodCall(nameof(CreatePerson));
            _person = new(name, age);
            return ref _person;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SimpleSumOut(ref int n1, ref int n2, out int result)
        {
            AddMethodCall(nameof(SimpleSumOut));
            result = n1 + n2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumAge(InterceptPerson person)
        {
            AddMethodCall(nameof(SumAge));
            return person.Age + 10;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string ConcatNamePersons(InterceptPerson person1, string separator, InterceptPerson person2)
        {
            AddMethodCall(nameof(ConcatNamePersons));

            string name1 = person1.Name;
            string name2 = person2.Name;

            return name1 + separator + name2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InterceptPerson MakeNewPerson(InterceptPerson person1)
        {
            AddMethodCall(nameof(MakeNewPerson));
            return new InterceptPerson(person1.Name, person1.Age);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InterceptPerson SimpleCall(ref int valueType, ref InterceptPerson objType)
        {
            AddMethodCall(nameof(SimpleCall));
            return new InterceptPerson(objType.Name, valueType);
        }

        private static void InterceptorCall(CallContext context)
        {
            AddMethodCall(context.Method.Name, true);

            MethodBase testSource = GetSourceTest();

            if (testSource.Name == nameof(ModifyPrimitiveReturnTest))
            {
                context.ReturnValue = 11;
            }
            else if (testSource.Name == nameof(ModifyPrimitiveParametersTest))
            {
                int n1 = context.Parameters.GetParameterValue<int>(0);
                int n2 = context.Parameters.GetParameterValue<int>(1);

                context.Parameters.SetParameterValue(0, n1 + n2);
                context.Parameters.SetParameterValue(1, n1 * n2);
            }
            else if (testSource.Name == nameof(ModifyInstanceTest) && context.Method.Name == nameof(InterceptPerson.GetAgeAfter10Years))
            {
                InterceptPerson interceptPerson = (InterceptPerson)context.Instance;
                InterceptPerson newPerson = interceptPerson with
                {
                    Age = interceptPerson.Age - 20
                };

                context.Instance = newPerson;
            }
            else if (testSource.Name == nameof(ModifyObjectParameterTest) && context.Method.Name == nameof(SumAge))
            {
                InterceptPerson interceptPerson = context.Parameters.GetParameterValue<InterceptPerson>(0);
                interceptPerson.Age += 255;
            }
            else if (testSource.Name == nameof(ModifyNonPrimitivePrametersTest) && context.Method.Name == nameof(ConcatNamePersons))
            {
                InterceptPerson person1 = context.Parameters!.GetParameterValue<InterceptPerson>(0);
                InterceptPerson person2 = context.Parameters.GetParameterValue<InterceptPerson>(2);

                InterceptPerson newPerson1 = person1 with
                {
                    Name = ReverseText(person1.Name)
                };

                InterceptPerson newPerson2 = person2 with
                {
                    Name = ReverseText(person2.Name)
                };

                context.Parameters.SetParameterValue(0, newPerson1);
                context.Parameters.SetParameterValue(2, newPerson2);
            }
            else if (testSource.Name == nameof(ModifyObjectReturnTest) && context.Method.Name == nameof(MakeNewPerson))
            {
                InterceptPerson person = context.Parameters.GetParameterValue<InterceptPerson>(0);

                string newName = ReverseText(person.Name);
                int newAge = person.Age * person.Age;

                context.ReturnValue = new InterceptPerson(newName, newAge);
            }
            else if (testSource.Name == nameof(ModifyRefParametersTest) && context.Method.Name == nameof(SimpleSumRef))
            {
                int n1 = context.Parameters.GetParameterValue<int>(0);
                int n2 = context.Parameters.GetParameterValue<int>(1);

                int newN1 = n1 * n2;
                int newN2 = n2 + n1;

                context.Parameters.OverrideParameterValue(0, newN1);
                context.Parameters.OverrideParameterValue(1, newN2);
            }
            else if (testSource.Name == nameof(ModifyOutParametersTest) && context.Method.Name == nameof(SimpleSumOut))
            {
                int n1 = context.Parameters.GetParameterValue<int>(0);
                int n2 = context.Parameters.GetParameterValue<int>(1);

                int newN1 = n1 * n2;
                int newN2 = n2 + n1;
                int result = newN1 + newN2;

                context.Parameters.OverrideParameterValue(2, result);
                context.ContinueCall = false;
            }
            else if (testSource.Name == nameof(ModifyValueTypeRefReturn) && context.Method.Name == nameof(CreatePoint))
            {
                int x = context.Parameters.GetParameterValue<int>(0);
                int y = context.Parameters.GetParameterValue<int>(1);

                Point point = new(x + y, x - y);
                context.ReturnValue = point;
            }
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(SimpleSum) ||
                context.Method.Name == nameof(InterceptPerson.GetAgeAfter10Years) ||
                context.Method.Name == nameof(SumAge) ||
                context.Method.Name == nameof(ConcatNamePersons) ||
                context.Method.Name == nameof(MakeNewPerson) ||
                context.Method.Name == nameof(SimpleCall) ||
                context.Method.Name == nameof(SimpleSumRef) ||
                context.Method.Name == nameof(SimpleSumOut) ||
                context.Method.Name == nameof(CreatePoint) ||
                context.Method.Name == nameof(CreatePerson)
            )
            {
                context.InterceptCall();
            }

        }

        #region Utils

        private static int CountCalls(string methodName, [CallerMemberName] string caller = "")
        {
            if (MethodsCalled.TryGetValue(caller, out ConcurrentBag<string> methods))
                return methods.Count(w => w == methodName);

            return 0;
        }

        private static int CountIntercept(string methodName, [CallerMemberName] string caller = "")
        {
            if (CallsIntercepted.TryGetValue(caller, out ConcurrentBag<string> methods))
                return methods.Count(w => w == methodName);

            return -1;
        }

        private static bool HasCalled(string methodName, [CallerMemberName] string caller = "")
        {
            if (MethodsCalled.TryGetValue(caller, out ConcurrentBag<string> methods))
                return methods.Contains(methodName);

            return false;
        }

        private static bool HasIntercepted(string methodName, [CallerMemberName] string caller = "")
        {
            if (CallsIntercepted.TryGetValue(caller, out ConcurrentBag<string> methods))
                return methods.Contains(methodName);

            return false;
        }

        private static void AddMethodCall(string method, bool isIntercepted = false)
        {
            ConcurrentDictionary<string, ConcurrentBag<string>> calls = isIntercepted ? CallsIntercepted : MethodsCalled;

            MethodBase testSource = GetSourceTest();
            string caller = testSource.Name;

            if (calls.TryGetValue(caller, out ConcurrentBag<string> methods))
                methods.Add(method);
            else
                calls.TryAdd(caller, new ConcurrentBag<string> { method });
        }

        private static MethodBase GetSourceTest()
        {
            StackTrace trace = new();
            return trace.GetFrames()
                .Select(w => w.GetMethod())
                .FirstOrDefault(w => w.GetCustomAttribute<FactAttribute>() != null);
        }

        #endregion
    }
}