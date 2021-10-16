## Problem

I want to mock static or extensions methods. What should I do?

Sometimes I have those static methods inside my library and sometime they are in an external library like [Dapper](https://github.com/DapperLib/Dapper)

Do you have any solution?

## Solution

A good, well-known solution is writing a wrapper.

Consider the following example:

```cs
public class MyClass
{
    public void LogMessage(string message)
    {
        Logger.Write(message);
    }
}

public class Logger
{
    public static void Write(string message)
    {
       //Write your code here to log data
    }
}
```

You can use [Moq](https://github.com/moq/moq4) to mock non-static methods but it cannot be used to mock static methods, so what should I do if I want to mock `Logger.Write`?

#### Create a wrapper class and interface

Make an interface just like the signature of the static method

```cs
public interface IWrapper
{
    void LogData(string message);
}
```

Implement that interface and call the static method (`Logger.Write`) into the same non-static method.

```cs
public class LogWrapper : IWrapper
{
    string _message = null;
    public LogWrapper(string message)
    {
        _message = message;
    }
    public void LogData(string message)
    {
        _message = message;
        Logger.Write(_message);
    }
}
```

Now you can use `IWrapper` and `LogWrapper` everywhere in your code and also make things mockable.

```cs
var mock = new Mock<IWrapper>();
mock.Setup(x => x.LogData(It.IsAny<string>()));
new ProductBL(mock.Object).LogMessage("Hello World!");
mock.VerifyAll();
```

#### What is the wrong part with this solution?

Of course, you can not do this for all static methods or for all external libraries. It is hard and tedious work.

## MockableStaticGenerator

`MockableStaticGenerator` is a C# code generator for making your static/extension methods even from an external library mockable just like above approach but automatically!

## How it works?

* For internal usage

If you have a class with static methods inside it, put `[MockableStatic]` on top of the class

```cs
// For your class with some static methods.
[MockableStatic]
public class Math
{
    public static int Add(int a, int b) { return a+b; }
    public static int Sub(int a, int b) { return a+b; }
}
```

* For external usage

If you have a class with static methods inside it but from somewhere else (an external library for example), you should introduce type of that class to the `[MockableStatic]`
 
 ```cs
 // For type of an external assembly with a lot of static methods, like 'Dapper.SqlMapper' class.
[MockableStatic(typeof(Dapper.SqlMapper))]
public class StudentRepositoryTest
{
    [Fact]
    public void StudentRepositoryMoqObject()
    {
        // Dapper.MockableGenerated.ISqlMapperWrapper

        var mockConn = new Mock<IDbConnection>();
        var sut = new StudentRepository(mockConn.Object);
        var stu = sut.GetStudents();

    }
}
 ```
 
#### How to call generated the classes and interfaces?

There are two naming convention you should follow:

* `I{CLASS_NAME}Wrapper` for the interface
* `{CLASS_NAME}Wrapper` for the class

and you can find them under 

* `{ASSEMBLY_NAME}.MockableGenerated.I{CLASS_NAME}Wrapper`
* `{ASSEMBLY_NAME}.MockableGenerated.{CLASS_NAME}Wrapper`


![image](https://user-images.githubusercontent.com/8418700/108617117-04539600-7429-11eb-804a-7c6e3241a799.png)
![image](https://user-images.githubusercontent.com/8418700/108617134-1a615680-7429-11eb-9de0-b49006a50b6f.png)

#### Attention

You **must** use the generated interfaces and classes **instead of** the originals to make your library, application testable and mockable.

## [Nuget](https://www.nuget.org/packages/MockableStaticGenerator)

```
Install-Package MockableStaticGenerator
dotnet add package MockableStaticGenerator
```

For more details read [this blog post](https://hamedfathi.me/the-dotnet-world-csharp-source-generator/).
