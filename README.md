# Jitex

[![Build status](https://ci.appveyor.com/api/projects/status/2h0y08mk82iwmyfr/branch/master?svg=true)](https://ci.appveyor.com/project/Hitmasu/jitex/branch/master) [![Nuget](https://img.shields.io/nuget/vpre/Jitex)](https://www.nuget.org/packages/Jitex/)

A framework to modify MSIL/Native code at runtime.

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

- Modify normal and generic methods
- Inject native code (ASM)
- Inject MSIL code (IL)
- Inject variables
- Inject custom metadatatoken



## Inject MSIL

```c#
private static void MethodResolver (MethodContext context) 
{
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

## Inject custom metadata

You can inject a custom metadata too, in this way, you can "execute" metadatatoken not referenced in compile-time:

```c#
/// <summary>
///     Example of a external library to replace SimpleSum.
/// </summary>
/// <remarks>
///     We replace SimpleSum to return the PID of process. To do this, normally we need
///     reference assembly (System.Diagnostics.Process) and class Process.
///     In this case, the original module, dont have any reference to Diagnostics and class Process.
///     As we pass a MetadataToken from Process.GetCurrentProcess().Id, its necessary resolve that manually,
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



## Debugging

We don't support debug (C#/VB.NET) in code generated/injected using Jitex. Somes IDEs (like Visual Studio and Rider), dont offer possibility to reload/update PDB at runtime.

Disassembly debug in Visual Studio and Windbg works fine!



## Proof of Concept

[AutoMapper Patcher](https://github.com/Hitmasu/AutoMapper.Patcher) - A simple remove AutoMapper at runtime.


## Thanks

Replace methods was an idea to increase performance in .NET Applications. Searching a way to do that, i've found this hook implementation from @xoofx [Writing a Managed JIT in C# with CoreCLR](https://xoofx.com/blog/2018/04/12/writing-managed-jit-in-csharp-with-coreclr/), which became core of Jitex.



###### *I'm trying be better in english language too, so probably you will see some grammatical errors... Feel free to notify me.*

