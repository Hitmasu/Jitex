using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        public void ModifyObjectAndPrimitiveParametersTest(string name1, string name2, string separator)
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

            CallsIntercepted.TryRemove(nameof(ModifyObjectAndPrimitiveParametersTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyObjectAndPrimitiveParametersTest), out _);
        }

        [Theory]
        [InlineData("Pedro", 20)]
        [InlineData("Jorge", 30)]
        [InlineData("Patricia", 99)]
        public void ModifyNonPrimitiveReturnTest(string name, int age)
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

            CallsIntercepted.TryRemove(nameof(ModifyNonPrimitiveReturnTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyNonPrimitiveReturnTest), out _);
        }

        private static string ReverseText(string text) => new(text.Reverse().ToArray());

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SimpleSum(int n1, int n2)
        {
            AddMethodCall(nameof(SimpleSum));
            return n1 + n2;
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
            return new (person1.Name, person1.Age);
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
            else if (testSource.Name == nameof(ModifyObjectAndPrimitiveParametersTest) && context.Method.Name == nameof(ConcatNamePersons))
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
            else if (testSource.Name == nameof(ModifyNonPrimitiveReturnTest) && context.Method.Name == nameof(MakeNewPerson))
            {
                InterceptPerson person = context.Parameters.GetParameterValue<InterceptPerson>(0);

                string newName = ReverseText(person.Name);
                int newAge = person.Age * person.Age;

                context.ReturnValue = new InterceptPerson(newName, newAge);
            }
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(SimpleSum) ||
                context.Method.Name == nameof(InterceptPerson.GetAgeAfter10Years) ||
                context.Method.Name == nameof(SumAge) ||
                context.Method.Name == nameof(ConcatNamePersons) ||
                context.Method.Name == nameof(MakeNewPerson)
            )
                context.InterceptCall();
        }

        #region Utils

        private static int CountCalls(string methodName, [CallerMemberName] string caller = "")
        {
            if (MethodsCalled.TryGetValue(caller, out ConcurrentBag<string> methods))
                return methods.Count(w => w == methodName);

            return -1;
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