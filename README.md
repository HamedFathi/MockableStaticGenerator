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