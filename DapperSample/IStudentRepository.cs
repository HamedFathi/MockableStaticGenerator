using System.Collections.Generic;

namespace DapperSample
{
    public interface IStudentRepository
    {
        IEnumerable<Student> GetStudents();
    }
}
