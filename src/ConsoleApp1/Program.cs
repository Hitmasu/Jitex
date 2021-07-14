using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Confluent.Kafka;
using Jitex;
using Jitex.JIT.Context;
using Jitex.Utils;

namespace ConsoleApp1
{

    public static class Person
    {
        public static string Name => "Teste nome";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetNamePublic() => Name;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetNamePrivate() => Name;
    }

    interface IReflection
    {
        [Entryction(typeof(Headers), 0x06000204)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static object CtorHeaders() => default;
    }

    class MyClass
    {
        public MyClass()
        {
            Console.WriteLine("asdasdasdsa");
        }

        [Entryction(typeof(Headers), 0x06000204)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Headers GetHeaders() => default;
    }

    class Program
    {
        static Program()
        {
            JitexManager.LoadModule<EntryctionModule>();
        }

        static void Main(string[] args)
        {
            var nx = new Headers();
            Headers headers = new MyClass().GetHeaders();
            Debugger.Break();
            //AutoMapperBenchmark amp = new AutoMapperBenchmark();
            //BenchmarkRunner.Run<AutoMapperBenchmark>();
            //Console.WriteLine(lp);
            //string name = IReflection.GetNamePrivate();
        }
    }

    public class AutoMapperBenchmark
    {
        private static readonly Type HeadersType = typeof(Headers);
        private static readonly ConstructorInfo Ctor = HeadersType.GetConstructor(Type.EmptyTypes);
        private readonly Func<object> _dynamicMethodActivator;
        private readonly Func<object> _expression;

        public AutoMapperBenchmark()
        {
            DynamicMethod createHeadersMethod = new DynamicMethod(
                $"KafkaDynamicMethodHeaders",
                HeadersType,
                null,
                typeof(AutoMapperBenchmark).Module,
                false);

            ILGenerator il = createHeadersMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, Ctor);
            il.Emit(OpCodes.Ret);

            _dynamicMethodActivator = (Func<object>)createHeadersMethod.CreateDelegate(typeof(Func<object>));
            _expression = Expression.Lambda<Func<object>>(Expression.New(HeadersType)).Compile();  

            RuntimeHelpers.PrepareMethod(Ctor.MethodHandle);
        }


        [Benchmark(Baseline = true)]
        public object Direct() => new Headers();

        [Benchmark]
        public object Reflection() => Ctor.Invoke(null);

        [Benchmark]
        public object ActivatorCreateInstance() => Activator.CreateInstance(HeadersType);

        [Benchmark]
        public object CompiledExpression() => _expression();

        [Benchmark]
        public object ReflectionEmit() => _dynamicMethodActivator();

        //private static MethodBase method;
        static AutoMapperBenchmark()
        {
            JitexManager.LoadModule<EntryctionModule>();
        }

        //[Benchmark]
        //public string Entryction()
        //{
        //    return IReflection.GetNamePrivate();
        //}

        //[Benchmark(Baseline = true)]
        //public string Normal()
        //{
        //    return Person.GetNamePublic();
        //}

        //[Benchmark]
        //public string Reflection()
        //{
        //    return (string)method.Invoke(null, null);
        //}
    }


    
    class EntryctionAttribute : Attribute
    {
        public Type Type { get; set; }
        public string MethodName { get; set; }
        public int MetadataToken { get; set; }

        public EntryctionAttribute(Type type, int metadataToken)
        {
            Type = type;
            MetadataToken = metadataToken;
        }

        public EntryctionAttribute(Type type, string methodName)
        {
            Type = type;
            MethodName = methodName;
        }
    }

    class EntryctionModule : JitexModule
    {
        protected override void MethodResolver(MethodContext context)
        {
            EntryctionAttribute attribute = context.Method.GetCustomAttribute<EntryctionAttribute>();
            
            if (attribute != null)
            {
                MethodBase method;

                if (attribute.MetadataToken != 0)
                {
                    method = attribute.Type.Module.ResolveMethod(attribute.MetadataToken);
                }
                else
                {
                    method = attribute.Type.GetMethod(attribute.MethodName, BindingFlags.Static | BindingFlags.NonPublic);
                }
                
                context.ResolveEntry(method);
            }
        }

        protected override void TokenResolver(TokenContext context)
        {

        }
    }
}
