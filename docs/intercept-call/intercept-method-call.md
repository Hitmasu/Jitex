# Interceptando chamadas

Às vezes ao longo do desenvolvimento, você quer apenas fazer um controle sobre a chamada em cima de um método, não tendo a necessidade de modificar o corpo ou método por completo. Para este cenário, o Jitex fornece a possibilidade de interceptar chamadas de métodos através de interceptadores.

Um interceptador, é um método assíncrono que vai te fornecer todo o contexto daquela chamada, como por exemplo: Parâmetros, Instância, Retorno do Valor, etc. O comportamento de um interceptador, é parecido com os do resolvers (TokenResolver e MethodResolver).

O Interceptador pode ser passado utilizando o método JitexManager.AddInterceptor:

```csharp
//...
JitexManager.AddInterceptor(InterceptorCallAsync);
//...

private static async ValueTask InterceptorCallAsync(CallContext context)
{
    //...
}

```

Neste caso, todo método que desejamos interceptar, passará pelo nosso InterceptorCallAsync.

Para interceptar as chamadas de um método, você deve informar no MethodResolver que deseja interceptar a chamada daquele método e um interceptador.

```csharp
public static void MethodResolver(MethodContext context){
    if(context.Method == myMethod)
        context.InterceptCall();
}
```

Desta forma, toda vez que o myMethod for chamado, ele passará pelo nosso interceptador.

Abaixo um exemplo de como implementar um interceptador:

```csharp
static void Main(string[] args)
{
    JitexManager.AddMethodResolver(MethodResolver);
    JitexManager.AddInterceptor(InterceptorCallAsync);
    int sum = Sum(5, 5);
    Console.WriteLine(sum);
}

public static int Sum(int n1, int n2)
{
    Console.WriteLine("Method Sum was called!");
    return n1 + n2;
}

private static async ValueTask InterceptorCallAsync(CallContext context)
{
   	Console.WriteLine("Call intercepted!")
}

private static void MethodResolver(MethodContext context)
{
    MethodInfo sumMethod = typeof(Program).GetMethod(nameof(Sum));
    
    if(context.Method == sumMethod)
        context.InterceptCall();
}
```

 Ao executar o código acima, o resultado deve ser: 

```bash
Call intercepted!
Method Sum was called!
10
```

