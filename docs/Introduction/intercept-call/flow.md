# Fluxo de interceptação

Ao interceptar um método, você pode desejar definir se você quer continuar o fluxo original da chamada ou se você quer dar um fim à ele. 

Para continuar com a chamada original do método, no contexto do interceptador, possuímos o método ContinueFlowAsync, que faz a chamada original do método sem sair da interceptação da chamada. 

Por exemplo:

```csharp
static void Main(string[] args)
{
    JitexManager.AddMethodResolver(context =>
    {
        if (context.Method.Name == "Sum")
            context.InterceptCall();
    });

    JitexManager.AddInterceptor(async context =>
    {
        Console.WriteLine("Before call");
        int result = await context.ContinueAsync<int>();
        Console.WriteLine($"Result from call: {result}");
    });

    int result = Sum(1, 1);
    Console.WriteLine($"Result from sum: {result}");
}

static int Sum(int n1, int n2)
{
    Console.WriteLine("Sum called!");
    return n1 + n2;
}
```

O resultado será:

```
Before call
Sum called!
Result from call: 2
Result from sum: 2
```

Neste caso, você consegue trabalhar no momento de antes e depois da chamada do método.

O método original pode ser chamado quantas vezes for necessário:

```csharp
JitexManager.AddInterceptor(async context =>
{
    await context.ContinueAsync<int>();
    await context.ContinueAsync<int>();
    await context.ContinueAsync<int>();
    await context.ContinueAsync<int>();
});
```

Mostrará no console:

```Sum called!
Sum called!
Sum called!
Sum called!
Sum called!
```



## Proceder com a chamada

Ao interceptar uma chamada, você tem a possibilidade de escolher se a chamada deve continuar com o fluxo original ou não. Isto pode ser feito alterando o valor da propriedade ProceedCall. Quando false, o fluxo não irá continuar, e quando true, a chamada vai continuar com o fluxo original.

```csharp
static void Main(string[] args)
{
    JitexManager.AddMethodResolver(context =>
    {
        if (context.Method.Name == "ShowText")
            context.InterceptCall();
    });

    JitexManager.AddInterceptor(async context =>
    {
        context.ProceedCall = false;
    });

    ShowText("Hello World");
    Console.WriteLine("End");
}

static void ShowText(string text)
{
    Console.WriteLine(text);
}
```

O resultado será apenas:

```
End
```

Porém, é necessário tomar cuidado atribuir ProceedCall como false.

Ao atribuir o ProceedCall, espera-se que a propriedade ReturnValue já tenha sido atribuída, caso contrário, a interceptação retornará null para quem a chamou. Se o fluxo original, não estiver preparado para receber nulo, a sua aplicação lançará NullReferenceException:

```csharp
JitexManager.AddInterceptor(async context =>
{
    context.ProceedCall = false;
});

string text = ShowText("Hello World");
//NullReferenceException, text is null here.
Console.WriteLine(text.Length);

static string ShowText(string text) => text;
```

O correto deveria ser:

```csharp
JitexManager.AddInterceptor(async context =>
{
    context.ReturnValue = "My text";
    context.ProceedCall = false; //Redundance
});

string text = ShowText("Hello World");
//text has value 'My Text'
Console.WriteLine(text.Length);

static string ShowText(string text) => text;
```

 Por padrão, o ProceedCall é true, porém há dois cenários que ele muda para false sem que seja explicitamente atribuído:

- Quando o valor de retorno é atribuído
- Quando o fluxo original é chamado (ContinueFlowAsync)

Ou seja, a interceptação do código acima, é o mesmo que esta:

```csharp
JitexManager.AddInterceptor(async context =>
{
    context.ReturnValue = "My text";
});
```

> Em ambos os cenários citados a cima, você pode forçar a execução original apenas atribuindo true para o ProceedCall.