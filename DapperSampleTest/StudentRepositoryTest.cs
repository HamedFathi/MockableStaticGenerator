using DapperSample;
using Moq;
using System.Data;
using Xunit;

namespace DapperSampleTest
{
    public class StudentRepositoryTest
    {
        [Fact]
        public void STUDENT_REPOSITORY_TEST()
        {
            var mockConn = new Mock<IDbConnection>();
            var mockDapper = new Mock<IDapperSqlMapper>();
            var sut = new StudentRepository(mockConn.Object, mockDapper.Object);
            var stu = sut.GetStudents();
            Assert.NotNull(stu);
        }
    }
}
