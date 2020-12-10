# MockableStaticGenerator

```cs
// For whole classes of an external assembly with a lot of static methods, like Dapper.
[MockableStatic(typeof(Dapper.DynamicParameters))]
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

// For your class with some static methods.
[MockableStatic]
public class Math
{
    public static int Add(int a, int b) { return a+b; }
    public static int Sub(int a, int b) { return a+b; }
}
```

### [Nuget](https://www.nuget.org/packages/MockableStaticGenerator)

```
Install-Package MockableStaticGenerator
dotnet add package MockableStaticGenerator
```

For more information read [this blog post](https://hamedfathi.me/the-dotnet-world-csharp-source-generator/).
