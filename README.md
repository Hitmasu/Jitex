# Jitex

![Jitex Build](https://github.com/Hitmasu/Jitex/workflows/Jitex%20Build/badge.svg) [![Nuget](https://img.shields.io/nuget/vpre/Jitex)](https://www.nuget.org/packages/Jitex/)

------

A library to modify MSIL/Native code at runtime.

It's a library built in .NET Standard 2.0, works on all version >=.NET Core 2.0. 

|             | .NET Core (2.0 ~ 3.1) | .NET 5             | .NET Framework (4.6.1 ~ 4.8)           | Mono              |
| ----------- | --------------------- | ------------------ | -------------------------------------- | ----------------- |
| **Windows** | :heavy_check_mark:    | :heavy_check_mark: | :building_construction: In development | :x: Not supported |
| **Linux**   | :heavy_check_mark:    | :heavy_check_mark: | :x: Not supported                      | :x: Not supported |
| **MacOS**   | :heavy_check_mark:    | :heavy_check_mark: | :x: Not supported​                      | :x: Not supported |

:green_heart:  *.NET Framework as soon.*

------

Jitex can help you inject code at runtime easily.

```c#
class Program {
  static void Main (string[] args) {
    JitexManager.AddMethodResolver (MethodResolver);
    int result = SimpleSum (5, 5);
    Console.WriteLine (result); //output is 25
  }

  static int SimpleSum (int num1, int num2) 
  {
    return num1 + num2;
  }

  public static int SimpleMul (int num1, int num2) 
  {
    return num1 * num2;
  }

  private static void MethodResolver (MethodContext context) 
  {
    if (context.Method.Name == "SimpleSum") {
      //Replace SimpleSum to SimpleMul
      MethodInfo replaceSumMethod = typeof (Program).GetMethod ("SimpleMul");
      context.ResolveMethod (replaceSumMethod);
    }
  }
}
```



## Support

- [Modify normal and generic methods](#Inject-Method)
- [Detour method](#Detour-Method)
- [Inject MSIL code (IL)](#Inject-MSIL)
- [Inject native code (ASM)](#Inject-Native-Code)
- [Inject custom metadatatoken](#Inject-custom-metadatatoken)
- [Inject custom string](#Inject-custom-string)
- [Modules](#Modules)



## Intercept call

```c#
public static void Main(){
	JitexManager.AddMethodResolver (MethodResolver);
    JitexManager.AddInterceptor (InteceptorCallAsync);
}

private static int SimpleSum(int n1, int n2) => n1+n2;

private static async ValueTask InteceptorCallAsync(CallContext context)
{
    //Get parameters passed to method
    int n1 = context.Parameters.GetParameterValue<int>(0);
    int n2 = context.Parameters.GetParameterValue<int>(1);
    
    //Set new parameters value to call
    context.Parameters.SetParameterValue(0,999);
    context.Parameters.SetParameterValue(1,1)
    //Set return value;
    context.ReturnValue = 50;
    
    //Prevent method original to be called
    context.ProceedCall = false;
    
    //Continue original call
    int result = await context.ContinueAsync<int>();
}

private static void MethodResolver(MethodContext context)
{
    if (context.Method.Name == nameof(SimpleSum))
        context.InterceptCall(); //every call from SimpleSum will be intercepted
}
```

## Inject Method

```c#
/// <summary>
///     Take sum of 2 random numbers
/// </summary>
/// <returns></returns>
public static int SimpleSumReplace () 
{
  const string url = "https://www.random.org/integers/?num=2&min=1&max=999&col=2&base=10&format=plain&rnd=new";
  using HttpClient client = new HttpClient ();
  using HttpResponseMessage response = client.GetAsync (url).Result;
  string content = response.Content.ReadAsStringAsync ().Result;
    
  string[] columns = content.Split ("\t");
    
  int num1 = int.Parse (columns[0]);
  int num2 = int.Parse (columns[1]);
    
  return num1 + num2;
}

private static void MethodResolver (MethodContext context) 
{
  if (context.Method.Name == "SimpleSum") {
    //Replace SimpleSum to our SimpleSumReplace
    MethodInfo replaceSumMethod = typeof (Program).GetMethod (nameof (SimpleSumReplace));
    context.ResolveMethod (replaceSumMethod);
  }
}
```

## Detour Method

```c#
private static void MethodResolver (MethodContext context) {
  if (context.Method.Name == "SimpleSum") {
    //Detour by MethodInfo
    MethodInfo detourMethod = typeof (Program).GetMethod (nameof (SimpleSumDetour));
    context.ResolveDetour (detourMethod);
    //or
    context.ResolveDetour<Action> (SimpleSumDetour);

    //Detour by Action or Func
    Action<int, int> detourAction = (n1, n2) => {
      Console.WriteLine ("Detoured");
      Console.WriteLine (n1 + n2);
    };
    context.ResolveDetour (detourAction);

    //Detour by Address
    IntPtr addressMethod = default; //Address of method to execute.
    context.ResolveDetour (addressMethod);
  }
}
```

## Inject MSIL

```c#
private static void MethodResolver (MethodContext context) 
{
  //Verify if method to compiled is our method SimpleSum.
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
```

## Inject Native Code

```c#
private static void MethodResolver (MethodContext context) 
{
  if (context.Method.Name == "SimpleSum") {
    Assembler assembler = new Assembler (64);

    //Replace with fatorial number:
    //int sum = num1+num2;
    //int fatorial = 1;
    //for(int i = 2; i <= sum; i++){
    //    fatorial *= i;
    //}
    //return fatorial;
    assembler.add (edx, ecx);
    assembler.mov (eax, 1);
    assembler.mov (ecx, 2);
    assembler.cmp (edx, 0x02);
    assembler.jl (assembler.@F);
    assembler.AnonymousLabel ();
    assembler.imul (eax, ecx);
    assembler.inc (ecx);
    assembler.cmp (ecx, edx);
    assembler.jle (assembler.@B);
    assembler.AnonymousLabel ();
    assembler.ret ();
      
    using MemoryStream ms = new MemoryStream ();
    assembler.Assemble (new StreamCodeWriter (ms), 0);
      
    byte[] asm = ms.ToArray ();
      
    context.ResolveNative (asm);
  }
}
```

## Inject custom MetadataToken

You can inject a custom metadata too, in this way, you can "execute" metadatatoken not referenced in compile-time:

```c#
/// <summary>
///     Example of a external library to replace SimpleSum.
/// </summary>
/// <remarks>
///     We replace SimpleSum to return the PID of process running. To do this, normally we need
///     reference assembly (System.Diagnostics.Process) and class Process.
///     In this case, the original module, dont have any reference to namespace System.Diagnostics.Process.
///     As we pass the MetadataToken from Process.GetCurrentProcess().Id, its necessary resolve that manually,
///     because CLR dont have any information about that in original module.
/// </remarks>
public static class ExternLibrary
{
    private static MethodInfo _getCurrentProcess;
    private static MethodInfo _getterId;

    static ExternLibrary()
    {
        LoadAssemblyDiagnostics();
    }

    public static void Initialize()
    {
        JitexManager.AddMethodResolver(MethodResolver);
        JitexManager.AddTokenResolver(TokenResolver);
    }

    private static void LoadAssemblyDiagnostics()
    {
        string pathAssembly = Path.Combine(Directory.GetCurrentDirectory(), "../../../../", "System.Diagnostics.Process.dll");
        Assembly assemblyDiagnostics = Assembly.LoadFrom(pathAssembly);
        Type typeProcess = assemblyDiagnostics.GetType("System.Diagnostics.Process");
        _getCurrentProcess = typeProcess.GetMethod("GetCurrentProcess");
        _getterId = _getCurrentProcess.ReturnType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
    }

    private static void TokenResolver(TokenContext context)
    {
        if (context.TokenType == TokenKind.Method && context.Source.Name == "SimpleSum")
        {
            if (context.MetadataToken == _getCurrentProcess.MetadataToken)
            {
                context.ResolveMethod(_getCurrentProcess);
            }
            else if (context.MetadataToken == _getterId.MetadataToken)
            {
                context.ResolveMethod(_getterId);
            }
        }
    }
    
    private static void MethodResolver(MethodContext context)
    {
        if (context.Method.Name == "SimpleSum")
        {
            List<byte> newIl = new List<byte>();
            newIl.Add((byte)OpCodes.Call.Value);
            newIl.AddRange(BitConverter.GetBytes(_getCurrentProcess.MetadataToken));
            newIl.Add((byte)OpCodes.Call.Value);
            newIl.AddRange(BitConverter.GetBytes(_getterId.MetadataToken));
            newIl.Add((byte)OpCodes.Ret.Value);
            MethodBody methodBody = new MethodBody(newIl.ToArray(), _getCurrentProcess.Module);
            context.ResolveBody(methodBody);
        }
    }
}
```

```c#
static void Main (string[] args) {
    ExternLibrary.Initialize ();
    int result = SimpleSum (1, 7);
    Console.WriteLine (result); //output is PID
}
```

## Inject custom string

```c#
private static void TokenResolver (TokenContext context) {
  if (context.TokenType == TokenKind.String && context.Content == "Hello World!")
    context.ResolveString ("H3110 W0RLD!");
}
```

```c#
static void Main (string[] args) {
    ExternLibrary.Initialize ();
    HelloWorld (); //output is H3110 W0RLD!
}

static void HelloWorld () {
    Console.WriteLine ("Hello World!");
}
```

## Modules

Jitex can support modules. To create your own module, just extend JitexModule:

```c#
public class ModuleJitex : JitexModule
{
    protected override void MethodResolver(MethodContext context)
    {
        //...
    }

    protected override void TokenResolver(TokenContext context)
    {
        //...
    }
}
```

And load module:

```c#
JitexManager.LoadModule<ModuleJitex>();
//or...
JitexManager.LoadModule(typeof(ModuleJitex));
```
## ASP.NET Core support

To load module in ASP.NET Core, just call UseModule in Configure (Startup.cs):

```c#
app.UseModule<ModuleJitex>();
app.UseModule<ModuleJitex1>();
app.UseModule<ModuleJitex2>();
//or
app.UseModule(typeof(ModuleJitex);
```



## Proof of Concept

[AutoMapper Patcher](https://github.com/Hitmasu/AutoMapper.Patcher) - A simple remover AutoMapper at runtime.

[InAsm](https://github.com/Hitmasu/InAsm) - Run assembly directly from a method.


## Thanks

Replace methods was an idea to increase performance in .NET Applications. Searching a way to do that, i've found this hook implementation from @xoofx [Writing a Managed JIT in C# with CoreCLR](https://xoofx.com/blog/2018/04/12/writing-managed-jit-in-csharp-with-coreclr/), which became core of Jitex.



###### *I'm trying be better in english language too, so probably you will see some grammatical errors... Feel free to notify me.*

