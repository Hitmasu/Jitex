# Parâmetros de uma chamada

É possível durante a chamada, obtermos os valores dos parâmetros passados nela. Por exemplo, o método abaixo:

```csharp
public static int Sum(int n1, int n2) => n1+n2;
```

Podemos obter os valores de `n1` e `n2` dentro do interceptador através de sua posição na chamada:

```csharp
private static async ValueTask InterceptorCallAsync(CallContext context)
{
    int n1 = context.Parameters.GetParameterValue<int>(0); //Index: 0 (first parameter)
    int n2 = context.Parameters.GetParameterValue<int>(1); //Index: 1 (second parameter)
}
```

Além de obtermos, podemos modificar os valores também:

```csharp
private static async ValueTask InterceptorCallAsync(CallContext context)
{
    int n1 = context.Parameters.GetParameterValue<int>(0); //Index: 0 (first parameter)
    int n2 = context.Parameters.GetParameterValue<int>(1); //Index: 1 (second parameter)
    
    context.Parameters.SetParameterValue(0,n1); //Index: 0 (first parameter)
    context.Parameters.SetParameterValue(1,n2); //Index: 1 (second parameter)
}
```

### Parâmetros por referência

Caso você esteja trabalhando com um método que contém parâmetros por referência, é possível também obter e sobrescrever os parâmetros. A chamada abaixo:

```csharp
public static int Sum(int n1, int n2, ref int max)
{
    max = Math.Max(n1, n2);
    return n1 + n2;
}
```

Você pode obter o parâmetro por referência utilizando a sobrecarga não genérica:

```csharp
private static ValueTask InterceptorCallAsync(CallContext context)
{
    int n1 = context.Parameters.GetParameterValue<int>(0);
    int n2 = context.Parameters.GetParameterValue<int>(1);
    ref object max = ref context.Parameters.GetParameterValue(2);
    
    return ValueTask.CompletedTask;
}
```

Neste caso, o retorno é um object para previnir boxing do valor.

Para escrever por referência (não é porque um parâmetro é por referência que é necessário escrever por referência), é possível usar também o SetParameterValue passando um objeto por referência:

```csharp
ref object max = ref context.Parameters.GetParameterValue(2);
context.Parameters.SetParameterValue(2, ref max);
```

Este modo serve em cenários em que o parâmetro é um ReferenceType, o que não é o nosso caso, já que int é um ValueType. Neste caso, podemos passar a variável com o tipo correto e utilizar expressão `__makeref` para facilitar a criação da referência:

```csharp
int max = context.Parameters.GetParameterValue<int>(2);
context.Parameters.SetParameterValue(2, __makeref(max));
```

 Se boxing não é uma opção para você, é possível passar também um IntPtr  contendo o endereço do valor.

```csharp
context.Parameters.SetParameterValue(2,addressVariable);
```

#### Sobrescrevendo uma referência

É possível sobrescrever a referência passada por parâmetro, utilizando o método OverrideParameterValue:

```csharp
ref object max = ref context.Parameters.GetParameterValue(2);
context.Parameters.OverrideParameterValue(2, ref max); //or (2,__makeref(max)), (2, 0x000) 
```

Desta forma, a variável de origem também apontará para a referência passada por parâmetro.



## Parâmetros originais

Quando uma chamada é interceptada no Jitex, é feito um trabalho para tornar os parâmetros mais "abstratos" e fáceis de serem utilizados. Porém, sabemos que muitas vezes há a necessidade de ver como o parâmetro foi passado originalmente. É possível obter os parâmetros passado em uma chamada em sua pura forma, utilizando a propriedade `RawParameters` do contexto:

```csharp
class Person
{
    public int Age { get; set; }

    public int SumAge<T>(int num)
    {
        Console.WriteLine(typeof(T).Name);
        return Age + num;
    }
}
```

```csharp
JitexManager.AddMethodResolver(context =>
{
    if (context.Method.Name == "SumAge")
        context.InterceptCall();
});

JitexManager.AddInterceptor(async context =>
{
    IEnumerable<Parameter> parameters =  context.RawParameters;
});

Person person = new Person {Age = 10};
person.SumAge<Program>(20);
```


> Ignore a funcionalidade do código, é apenas demonstrativo

No exemplo acima, temos o método SumAge, que é de instância, genérico e recebe um parâmetro de inteiro. Chamando o RawParameters, você terá de retorno uma lista com 3 itens:


```
[0] = (IntPtr) Valor da instância
[1] = (IntPtr) Handle do método
[2] = (int) Parâmetro num do método
```

Esses três valores são os que são passados para o método em tempo de execução.
