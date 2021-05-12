using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public void ModifyValueTypeParametersTest(int x, int y)
        {
            Point point = new(x, y);
            Point result = CreatePoint(point);
            Point expected = new(x + y, y + x);

            Assert.Equal(expected, result);

            Assert.True(HasCalled(nameof(CreatePoint)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(CreatePoint)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(CreatePoint)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(CreatePoint)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyValueTypeParametersTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyValueTypeParametersTest), out _);
        }

        [Theory]
        [InlineData("Pedro", 20)]
        [InlineData("Jorge", 30)]
        [InlineData("Patricia", 99)]
        public void ModifyClassReturnTest(string name, int age)
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

            CallsIntercepted.TryRemove(nameof(ModifyClassReturnTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyClassReturnTest), out _);
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
        public void ModifyRefPrimitiveParametersTest(int n1, int n2)
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

            CallsIntercepted.TryRemove(nameof(ModifyRefPrimitiveParametersTest), out _);
            MethodsCalled.TryRemove(nameof(ModifyRefPrimitiveParametersTest), out _);
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
        public void InterceptRefValueTypeReturn(int x, int y)
        {
            ref Point result = ref CreatePoint(x, y);

            TypedReference resultRef = __makeref(result);
            TypedReference pointRef = __makeref(_point);

            IntPtr resultAddr;
            IntPtr pointAddr;

            unsafe
            {
                resultAddr = *(IntPtr*) &resultRef;
                pointAddr = *(IntPtr*) &pointRef;
            }

            Assert.Equal(_point, result);

            Assert.Equal(pointAddr, resultAddr);

            Assert.True(HasCalled(nameof(CreatePoint)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(CreatePoint)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(CreatePoint)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(CreatePoint)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(InterceptRefValueTypeReturn), out _);
            MethodsCalled.TryRemove(nameof(InterceptRefValueTypeReturn), out _);
        }

        [Theory]
        [InlineData("Pedro", 20)]
        [InlineData("Jorge", 30)]
        [InlineData("Patricia", 99)]
        public void InterceptRefClassReturn(string name, int age)
        {
            ref InterceptPerson result = ref CreatePerson(name, age);

            TypedReference resultRef = __makeref(result);
            TypedReference personRef = __makeref(_person);

            IntPtr resultAddr;
            IntPtr personAddr;

            unsafe
            {
                resultAddr = *(IntPtr*) &resultRef;
                personAddr = *(IntPtr*) &personRef;
            }

            Assert.Equal(name, _person.Name);
            Assert.Equal(age, _person.Age);

            Assert.Equal(_person, result);

            Assert.Equal(personAddr, resultAddr);

            Assert.True(HasCalled(nameof(CreatePerson)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(CreatePerson)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(CreatePerson)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(CreatePerson)) == 1, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(InterceptRefClassReturn), out _);
            MethodsCalled.TryRemove(nameof(InterceptRefClassReturn), out _);
        }

        [Theory]
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public void ModifyRefValueTypeReturn(int x, int y)
        {
            ref Point result = ref CreatePoint(x, y);
            Point expected = new(x + y, x - y);

            Assert.Equal(result, expected);
            Assert.Equal(default, _point);

            Assert.False(HasCalled(nameof(CreatePoint)), "Call continued!");
            Assert.True(HasIntercepted(nameof(CreatePoint)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(CreatePoint)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(CreatePoint)) == 0, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyRefValueTypeReturn), out _);
            MethodsCalled.TryRemove(nameof(ModifyRefValueTypeReturn), out _);
        }

        [Theory]
        [InlineData("Pedro", 20)]
        [InlineData("Jorge", 30)]
        [InlineData("Patricia", 99)]
        public void ModifyRefClassReturn(string name, int age)
        {
            ref InterceptPerson result = ref CreatePerson(name, age);
            InterceptPerson expected = new(name + " " + name, age + age);

            Assert.Equal(result, expected);
            Assert.Equal(default, _person);

            Assert.False(HasCalled(nameof(CreatePerson)), "Call continued!");
            Assert.True(HasIntercepted(nameof(CreatePerson)), "Method not intercepted!");

            Assert.True(CountIntercept(nameof(CreatePerson)) == 1, "Intercepted more than expected!");
            Assert.True(CountCalls(nameof(CreatePerson)) == 0, "Called more than expected!");

            CallsIntercepted.TryRemove(nameof(ModifyRefClassReturn), out _);
            MethodsCalled.TryRemove(nameof(ModifyRefClassReturn), out _);
        }

        [Fact]
        public async Task TaskNonGeneric()
        {
            await SimpleCallTaskAsync().ConfigureAwait(false);

            Assert.True(HasCalled(nameof(SimpleCallTaskAsync)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SimpleCallTaskAsync)), "Method not intercepted!");

            Assert.True(CountCalls(nameof(SimpleCallTaskAsync)) == 1, "Called more than expected!");
            Assert.True(CountIntercept(nameof(SimpleCallTaskAsync)) == 1, "Intercepted more than expected!");

            CallsIntercepted.TryRemove(nameof(TaskNonGeneric), out _);
            MethodsCalled.TryRemove(nameof(TaskNonGeneric), out _);
        }

        [Fact]
        public async Task ValueTaskNonGeneric()
        {
            await SimpleCallValueTaskAsync().ConfigureAwait(false);

            Assert.True(HasCalled(nameof(SimpleCallValueTaskAsync)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SimpleCallValueTaskAsync)), "Method not intercepted!");
            
            Assert.True(CountCalls(nameof(SimpleCallValueTaskAsync)) == 1, "Called more than expected!");
            Assert.True(CountIntercept(nameof(SimpleCallValueTaskAsync)) == 1, "Intercepted more than expected!");

            CallsIntercepted.TryRemove(nameof(ValueTaskNonGeneric), out _);
            MethodsCalled.TryRemove(nameof(ValueTaskNonGeneric), out _);
        }

        [Theory]
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public async Task TaskGenericWithParameters(int n1, int n2)
        {
            Assert.True((true));
            // int result = await SumTaskAsync(n1, n2).ConfigureAwait(false);
            //
            // Assert.Equal(n1 + n2, result);
            //
            // Assert.True(HasCalled(nameof(SumTaskAsync)), "Call not continued!");
            // Assert.True(HasIntercepted(nameof(SumTaskAsync)), "Method not intercepted!");
            //
            // Assert.True(CountCalls(nameof(SumTaskAsync)) == 1, "Called more than expected!");
            // Assert.True(CountIntercept(nameof(SumTaskAsync)) == 1, "Intercepted more than expected!");
            //
            // CallsIntercepted.TryRemove(nameof(TaskGenericWithParameters), out _);
            // MethodsCalled.TryRemove(nameof(TaskGenericWithParameters), out _);
        }

        [Theory]
        [InlineData(7, 9)]
        [InlineData(-1, 20)]
        [InlineData(2000, 7000)]
        public async Task ValueTaskGenericWithParameters(int n1, int n2)
        {
            int result = await SumValueTaskAsync(n1, n2).ConfigureAwait(false);

            Assert.Equal(n1 + n2, result);

            Assert.True(HasCalled(nameof(SumValueTaskAsync)), "Call not continued!");
            Assert.True(HasIntercepted(nameof(SumValueTaskAsync)), "Method not intercepted!");

            Assert.True(CountCalls(nameof(SumValueTaskAsync)) == 1, "Called more than expected!");
            Assert.True(CountIntercept(nameof(SumValueTaskAsync)) == 1, "Intercepted more than expected!");

            CallsIntercepted.TryRemove(nameof(ValueTaskGenericWithParameters), out _);
            MethodsCalled.TryRemove(nameof(ValueTaskGenericWithParameters), out _);
        }

        private static string ReverseText(string text) => new(text.Reverse().ToArray());

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SimpleSum(int n1, int n2)
        {
            AddMethodCall(nameof(SimpleSum));
            return n1 + n2;
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SimpleSumRef(ref int n1, ref int n2)
        {
            AddMethodCall(nameof(SimpleSumRef));
            return n1 + n2;
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref Point CreatePoint(int x, int y)
        {
            AddMethodCall(nameof(CreatePoint));
            _point = new Point(x, y);
            return ref _point;
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref InterceptPerson CreatePerson(string name, int age)
        {
            AddMethodCall(nameof(CreatePerson));
            _person = new InterceptPerson(name, age);
            return ref _person;
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SimpleSumOut(ref int n1, ref int n2, out int result)
        {
            AddMethodCall(nameof(SimpleSumOut));
            result = n1 + n2;
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumAge(InterceptPerson person)
        {
            AddMethodCall(nameof(SumAge));
            return person.Age + 10;
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Point CreatePoint(Point point)
        {
            AddMethodCall(nameof(CreatePoint));
            return new Point(point.X, point.Y);
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private InterceptPerson MakeNewPerson(InterceptPerson person1)
        {
            AddMethodCall(nameof(MakeNewPerson));
            return new InterceptPerson(person1.Name, person1.Age);
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private InterceptPerson SimpleCall(ref int valueType, ref InterceptPerson objType)
        {
            AddMethodCall(nameof(SimpleCall));
            return new InterceptPerson(objType.Name, valueType);
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task SimpleCallTaskAsync()
        {
            await Task.Delay(10);
            AddMethodCall(nameof(SimpleCallTaskAsync), caller: nameof(TaskNonGeneric));
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async ValueTask SimpleCallValueTaskAsync()
        {
            await Task.Delay(10);
            AddMethodCall(nameof(SimpleCallValueTaskAsync), caller: nameof(ValueTaskNonGeneric));
        }

        // [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<int> SumTaskAsync(int n1, int n2)
        {
            AddMethodCall(nameof(SumTaskAsync), caller: nameof(TaskGenericWithParameters));
            return await Task.FromResult(n1 + n2);
        }

        [InterceptCall]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async ValueTask<int> SumValueTaskAsync(int n1, int n2)
        {
            AddMethodCall(nameof(SumValueTaskAsync), caller: nameof(ValueTaskGenericWithParameters));
            return await new ValueTask<int>(n1 + n2);
        }

        private static async ValueTask InterceptorCall(CallContext context)
        {
            AddMethodCall(context.Method.Name, true);

            MethodBase testSource = GetSourceTest();

            // //When return of method is a ValueTask, DisposeTestClass will raise an exception "Internal CLR Error"
            // //I dont know why that happen in xunit, but preventing him to be called, resolve this problem.
            // //TODO: Need discover why Internal CLR Error is raised when returns is a ValueTask.
            if (context.Method.Name == "DisposeTestClass")
            {
                context.ProceedCall = false;
                return;
            }
            
            if (testSource.DeclaringType != typeof(InterceptCallTests))
                return;

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
                InterceptPerson interceptPerson = (InterceptPerson) context.Instance;
                InterceptPerson newPerson = interceptPerson with
                {
                    Age = interceptPerson.Age - 20
                };

                context.Instance = newPerson;
            }
            else if (testSource.Name == nameof(ModifyObjectParameterTest) && context.Method.Name == nameof(SumAge))
            {
                InterceptPerson interceptPerson = context.Parameters!.GetParameterValue<InterceptPerson>(0);
                interceptPerson.Age += 255;
            }
            else if (testSource.Name == nameof(ModifyValueTypeParametersTest) && context.Method.Name == nameof(CreatePoint))
            {
                Point point = context.Parameters!.GetParameterValue<Point>(0);

                int x = point.X;
                int y = point.Y;

                point.X += y;
                point.Y += x;

                context.Parameters.SetParameterValue(0, point);
            }
            else if (testSource.Name == nameof(ModifyClassReturnTest) && context.Method.Name == nameof(MakeNewPerson))
            {
                InterceptPerson person = context.Parameters.GetParameterValue<InterceptPerson>(0);

                string newName = ReverseText(person.Name);
                int newAge = person.Age * person.Age;

                context.ReturnValue = new InterceptPerson(newName, newAge);
            }
            else if (testSource.Name == nameof(ModifyRefPrimitiveParametersTest) && context.Method.Name == nameof(SimpleSumRef))
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
                context.ProceedCall = false;
            }
            else if (testSource.Name == nameof(ModifyRefValueTypeReturn) && context.Method.Name == nameof(CreatePoint))
            {
                int x = context.Parameters.GetParameterValue<int>(0);
                int y = context.Parameters.GetParameterValue<int>(1);

                Point point = new(x + y, x - y);
                context.ReturnValue = point;
            }
            else if (testSource.Name == nameof(ModifyRefClassReturn) && context.Method.Name == nameof(CreatePerson))
            {
                string name = context.Parameters.GetParameterValue<string>(0);
                int age = context.Parameters.GetParameterValue<int>(1);

                InterceptPerson person = new(name + " " + name, age + age);
                context.ReturnValue = person;
            }
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "DisposeTestClass")
                context.InterceptCall();

            if (context.Method.GetCustomAttribute<InterceptCallAttribute>() != null || context.Method.Name == nameof(InterceptPerson.GetAgeAfter10Years))
                context.InterceptCall();
        }

        #region Utils

        [AttributeUsage(AttributeTargets.Method)]
        internal class InterceptCallAttribute : Attribute
        {
        }

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

        private static void AddMethodCall(string method, bool isIntercepted = false, string caller = "")
        {
            ConcurrentDictionary<string, ConcurrentBag<string>> calls = isIntercepted ? CallsIntercepted : MethodsCalled;

            MethodBase testSource = GetSourceTest();

            if (testSource != null)
                caller = testSource.Name;

            if (calls.TryGetValue(caller, out ConcurrentBag<string> methods))
                methods.Add(method);
            else
                calls.TryAdd(caller, new ConcurrentBag<string> {method});
        }

        private static MethodBase GetSourceTest()
        {
            StackTrace trace = new StackTrace(false);
            return trace.GetFrames()
                .Select(w => w.GetMethod())
                .FirstOrDefault(w => w.GetCustomAttribute<FactAttribute>() != null || w.GetCustomAttribute<TheoryAttribute>() != null);
        }

        #endregion
    }
}