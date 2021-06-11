## Resolvendo por IL

Dentro do resolver, é possível modificar o IL do método antes dele ser compilado para código nativo.

#### Resolvendo IL puro

Para resolver o IL, basta chamar o método ResolveIL passando o novo código IL que deseja executar:

```csharp
static void Main(string[] args)
{
    JitexManager.AddMethodResolver(MethodResolver);

    int result = Sum(5, 5);
    Console.WriteLine("Result: " + result);
}

static int Sum(int n1, int n2) => n1 + n2;

private static void MethodResolver(MethodContext context)
{
    if (context.Method.Name == "Sum")
    {
        byte[] il =
        {
            (byte) OpCodes.Ldarg_0.Value,
            (byte) OpCodes.Ldarg_1.Value,
            (byte) OpCodes.Mul.Value, //replace + by *
            (byte) OpCodes.Ret.Value,
        };

        context.ResolveIL(il);
    }
}
```

No código acima apenas substituímos o corpo do método para fazer a multiplicação dos parâmetros ao invés da soma.

O resultado deverá ser:

```
Result: 25
```

Este cenário é simples e não requer grandes modificações do método, pois o método já está preparado para o nosso novo corpo (IL). Porém, nem sempre o método está preparado para aceitar o corpo. Vamos tornar o cenário um pouco mais complexo.

Ao invés de substituir o sinal de + pelo sinal de * no corpo do método, vamos pegar o cubo da soma dos parâmetros. De forma direta:

Substituir isso:

```csharp
static int Sum(int n1, int n2) => n1 + n2;
```

Por isto:

```csharp
static int Sum(int n1, int n2)
{
    int sum = n1 + n2;
    return sum * sum * sum;
}
```

Utilizando o [SharpLab](https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYGYAE9MGFMDeqmJ2WMCAbJmAHbCYDKArgLYAUdDtCANDfUy04ASkLFSkrpgDObTAF4hCANTCA3BMkkYAdllsAVHNbG2mlJIC+qK0A===), este é o IL que deveremos aplicar:

```csharp
private static void MethodResolver(MethodContext context)
{
    if (context.Method.Name == "Sum")
    {
        byte[] il =
        {
            (byte) OpCodes.Ldarg_0.Value,
            (byte) OpCodes.Ldarg_1.Value,
            (byte) OpCodes.Add.Value,
            (byte) OpCodes.Stloc_0.Value,
            (byte) OpCodes.Ldloc_0.Value,
            (byte) OpCodes.Ldloc_0.Value,
            (byte) OpCodes.Mul.Value,
            (byte) OpCodes.Ldloc_0.Value,
            (byte) OpCodes.Mul.Value,
            (byte) OpCodes.Ret.Value,
        };

        context.ResolveIL(il);
    }
}
```

Ao executar, teremos o seguinte erro:

```
Unhandled exception. System.InvalidProgramException: Common Language Runtime detected an invalid program.
   at MyApplication.Program.Sum(Int32 n1, Int32 n2)
```

O JIT não foi capaz de traduzir o nosso MSIL. 

Isto ocorreu por causa das instruções Stloc_0 e Ldloc_0 no corpo do nosso método. Utilizando o próprio SharpLab novamente, podemos ver que da forma que queremos fazer, é necessário que o método local tenha uma variável local do tipo int para armazenar a soma dos parâmetros.

Neste cenário, o ResolveIL já não é o mais o suficiente, e é necessário utilizar o MethodBody.

#### Resolvendo com MethodBody

O Jitex possui o seu próprio MethodBody e é utilizando para fornecer mais detalhes sobre o corpo de um método. Ele é útil quando o corpo que você deseja escrever, contém mais informações que apenas código IL, como também quantidades de exceções, variáveis locais, ...

Utilizando o exemplo acima do cubo da soma, podemos resolver o problema que tivemos com o seguinte trecho de código:

```csharp
static void Main(string[] args)
{
    JitexManager.AddMethodResolver(MethodResolver);

    int result = Sum(5, 5);
    Console.WriteLine("Result: " + result);
}

static int Sum(int n1, int n2) => n1 + n2;

private static void MethodResolver(MethodContext context)
{
    if (context.Method.Name == "Sum")
    {
        byte[] il =
        {
            (byte) OpCodes.Ldarg_0.Value,
            (byte) OpCodes.Ldarg_1.Value,
            (byte) OpCodes.Add.Value,
            (byte) OpCodes.Stloc_0.Value,
            (byte) OpCodes.Ldloc_0.Value,
            (byte) OpCodes.Ldloc_0.Value,
            (byte) OpCodes.Mul.Value,
            (byte) OpCodes.Ldloc_0.Value,
            (byte) OpCodes.Mul.Value,
            (byte) OpCodes.Ret.Value,
        };
        
		//Set quantity and types of local variables from method
        Type[] localVariables =
        {
            typeof(int)
        };

        MethodBody body = new MethodBody(il, localVariables);

        context.ResolveBody(body);
    }
}
```

Observe que agora criamos o nosso corpo do método passando os tipos de variáveis locais que ele tem, e também utilizamos o ResolveBody.

Ao executarmos a aplicação, teremos o resultado:

```
Result: 1000
```

#### Lendo o corpo de um método

O MethodBody serve também para ler o corpo de um método. Diferente do método GetILAsByteArray do System.Reflection.MethodBody, ele retornará o IL mais legível ao invés de apenas um array de bytes. Por exemplo:

```csharp
static void Main(string[] args)
{
    MethodInfo sumMethod = typeof(Program).GetMethod("Sum");
    MethodBody body = new MethodBody(sumMethod);

    foreach (Operation operation in body.ReadIL())
    {
        Console.WriteLine($"{operation.Index} - {operation.OpCode} {operation.Instance}");
    }
}

public static int Sum(int n1, int n2) => n1 + n2;
```

O resultado será:

```
0 - ldarg.0
1 - ldarg.1
2 - add
3 - ret
```

> Observe que o Jitex não foi inicializado neste exemplo, isto porque o MethodBody não precisa do Jitex instalado para poder funcionar.

A propriedade Instance é o valor do tipo da operação. Por exemplo: se o operação for um call, a Instance vai ser o MethodBase do método que está sendo chamado.

O seguinte método método:

```csharp
public static int Sum(int n1, int n2)
{
    return Math.Max(n1, n2) + 1000;
}
```

Terá o seguinte resultado em Debug:

```
0 - nop
1 - ldarg.0
2 - ldarg.1
3 - call Int32 Max(Int32, Int32) -- Instance value: Method Max
4 - ldc.i4 1000 -- Instance value: 1000
5 - add
6 - stloc.0
7 - br.s 0
8 - ldloc.0
9 - ret
```

E em Release:

```
0 - ldarg.0
1 - ldarg.1
2 - call Int32 Max(Int32, Int32)
3 - ldc.i4 1000
4 - add
5 - ret
```

> O MethodContext possui a propriedade Body, que é o corpo do método que será compilado.

> Caso você apenas quer uma forma fácil de ler o corpo do método, você não precisa da biblioteca Jitex por completa, apenas a [Jitex.Builder](https://www.nuget.org/packages/Jitex.Builder/).
