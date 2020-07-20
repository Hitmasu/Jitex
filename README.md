# Jitex
A framework to modify MSIL/Native code at runtime.

Jitex can help you inject code at runtime with a simple way.

## Inject MSIL

To Inject a simple MSIL, you just need add a resolver and replace method who want:

```c#
class Program {
    static void Main (string[] args) {
        ManagedJit jit = ManagedJit.GetInstance ();
        
        //Custom resolver
        jit.AddCompileResolver (CompileResolver);
        
        int result = SimpleSum (5, 5);
        Console.WriteLine (result); //output is 25
    }

    ///<summary>
    ///Simple method to override.
    ///</summary>
    static int SimpleSum (int num1, int num2) {
        return num1 + num2;
    }

    private static void CompileResolver (CompileContext context) {
        //Verify with method to be compile is our method who we want modify.
        if (context.Method.Name == "SimpleSum") {
            //num1 * num2
            byte[] newIL = {
            (byte) OpCodes.Ldarg_0.Value, //parameter num1
            (byte) OpCodes.Ldarg_1.Value, //parameter num2
            (byte) OpCodes.Mul.Value,
            (byte) OpCodes.Ret.Value
            };

            MethodBody body = new MethodBody (newIL, context.Method.Module);
            context.ResolveBody (body);
        }
    }
}
```

## Inject Method

```c#
class Program {
    static void Main (string[] args) {
        ManagedJit jit = ManagedJit.GetInstance ();
        jit.AddCompileResolver (CompileResolver);
        int result = SimpleSum (5, 5);
        Console.WriteLine (result);
    }

    ///<summary>
    ///Simple method to override.
    ///</summary>
    static int SimpleSum (int num1, int num2) {
        return num1 + num2;
    }

    /// <summary>
    /// Take sum of 2 random numbers
    /// </summary>
    /// <returns></returns>
    public static int SimpleSumReplace () {
        const string url = "https://www.random.org/integers/?num=2&min=1&max=999&col=2&base=10&format=plain&rnd=new";
        using HttpClient client = new HttpClient ();
        using HttpResponseMessage response = client.GetAsync (url).Result;
        string content = response.Content.ReadAsStringAsync ().Result;
        string[] columns = content.Split ("\t");

        int num1 = int.Parse (columns[0]);
        int num2 = int.Parse (columns[1]);

        return num1 + num2;
    }

    private static void CompileResolver (CompileContext context) {
        if (context.Method.Name == "SimpleSum") {
            //Replace SimpleSum to our SimpleSumReplace
            MethodInfo replaceSumMethod = typeof (Program).GetMethod ("SimpleSumReplace");
            context.ResolveMethod (replaceSumMethod);
        }
    }
}
```

## Inject Native Code

To inject a native code, you just need pass a byte code you want:

```c#
class Program {
    static void Main (string[] args) {
        ManagedJit jit = ManagedJit.GetInstance ();
        jit.AddCompileResolver (CompileResolver);
        int result = SimpleSum (1, 7);
        Console.WriteLine (result);
    }

    static int SimpleSum (int num1, int num2) {
        return num1 + num2;
    }

    private static void CompileResolver(CompileContext context)
    {
        if (context.Method.Name == "SimpleSum")
        {
            //Replace with fatorial number:
            //int sum = num1+num2;
            //int fatorial = 1;
            //for(int i = 2; i <= sum; i++){
            //    fatorial *= i;
            //}
            //return fatorial;
            byte[] asm =
            {
                0x01, 0xCA,                     //add    edx,ecx
                0xB8, 0x01, 0x00, 0x00, 0x00,   //mov    eax,0x1
                0xB9, 0x02, 0x00, 0x00, 0x00,   //mov    ecx,0x2
                0x83, 0xFA, 0x02,               //cmp    edx,0x2
                0x7C, 0x09,                     //jl
                0x0F, 0xAF, 0xC1,               //imul   eax,ecx
                0xFF, 0xC1,                     //inc    ecx
                0x39, 0xD1,                     //cmp    ecx,edx
                0x7E, 0xF7,                     //jle
                0xC3                            //ret
            };
            context.ResolveByteCode(asm);
        }
    }
}
```

## Inject custom metadata

You can inject a custom metadata too, in this way, you can "execute" metadatatoken not referenced in compile-time:

```c#
    /// <summary>
    /// Example of a external library to replace SimpleSum.
    /// </summary>
    /// <remarks>
    /// We replace SimpleSum to return the PID of process. To do this, normally we need
    /// reference assembly (System.Diagnostics.Process) and class Process.
    /// In this case, the original module, dont have any reference to Diagnostics and class Process.
    /// As we pass a MetadataToken from Process.GetCurrentProcess().Id, its necessary resolve that manually,
    /// because CLR dont have any information about that in original module.
    /// </remarks>
public static class ExternLibrary {
    
    private static MethodInfo _getCurrentProcess;
    private static MethodInfo _getterId;

    static ExternLibrary () {
        LoadAssemblyDiagnostics ();
    }

    public static void Initialize () {
        ManagedJit jitex = ManagedJit.GetInstance ();

        jitex.AddCompileResolver (CompileResolve);
        jitex.AddTokenResolver (TokenResolve);
    }

    private static void LoadAssemblyDiagnostics () {
        string pathAssembly = Path.Combine (@"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\3.1.5", "System.Diagnostics.Process.dll");

        Assembly assemblyDiagnostics = Assembly.LoadFrom (pathAssembly);
        Type typeProcess = assemblyDiagnostics.GetType ("System.Diagnostics.Process");
        _getCurrentProcess = typeProcess.GetMethod ("GetCurrentProcess");
        _getterId = _getCurrentProcess.ReturnType.GetProperty ("Id", BindingFlags.Public | BindingFlags.Instance).GetGetMethod ();
    }

    private static void TokenResolve (TokenContext context) {
        if (context.TokenType == TokenKind.Method && context.Source.Name == "SimpleSum") {
            if (context.MetadataToken == _getCurrentProcess.MetadataToken) {
                context.ResolveMethod (_getCurrentProcess);
            } else if (context.MetadataToken == _getterId.MetadataToken) {
                context.ResolveMethod (_getterId);
            }
        }
    }

    private static void CompileResolve (CompileContext context) {
        if (context.Method.Name == "SimpleSum") {
            List<byte> newIl = new List<byte> ();

            newIl.Add ((byte) OpCodes.Call.Value);
            newIl.AddRange (BitConverter.GetBytes (_getCurrentProcess.MetadataToken));
            newIl.Add ((byte) OpCodes.Call.Value);
            newIl.AddRange (BitConverter.GetBytes (_getterId.MetadataToken));
            newIl.Add ((byte) OpCodes.Ret.Value);

            MethodBody methodBody = new MethodBody (newIl.ToArray ());

            context.ResolveBody (methodBody);
        }
    }
}
```

```c#
using System;

namespace JitexDocInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            ExternLibrary.Initialize();
            int result = SimpleSum(1, 7);
            Console.WriteLine(result);
        }

        static int SimpleSum(int num1, int num2)
        {
            return num1 + num2;
        }
    }
}
```

[![Build status](https://ci.appveyor.com/api/projects/status/2h0y08mk82iwmyfr/branch/master?svg=true)](https://ci.appveyor.com/project/Hitmasu/jitex/branch/master)

[![Nuget](https://img.shields.io/nuget/vpre/Jitex)](https://www.nuget.org/packages/Jitex/)