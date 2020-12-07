using System.Collections.Generic;
using System.Data;
using System;

namespace DapperSample
{

    //public interface AAA { }
    //public class BBB { }

    //[MockableStatic]

    //public class X
    //{

    //}

    //[MockableStatic]
    //public class Sample<T, U, V> : BBB, AAA where U : struct, IConvertible
    //{
    //    public static void q() { }

    //    public static IEnumerable<Student> Get<T, V>(V v, out string ty, T t = default, int? y = 2, Nullable<float> u = 2.1f) where T : class, ICloneable, new() where V : struct
    //    {
    //        ty = "";
    //        return null;
    //    }

    //    [ObsoleteAttribute("dfasfasdfasdf", true)]

    //    public static int A() { return 1; }

    //    [Obsolete("", false)]
    //    public static int B() { return 1; }
    //}

    [MockableStatic(typeof(Dapper.SqlMapper))]
    public class StudentRepository : IStudentRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDapperSqlMapper _dapperSqlMapper;

        public StudentRepository(IDbConnection dbConnection, IDapperSqlMapper dapperSqlMapper)
        {
            _dbConnection = dbConnection;
            _dapperSqlMapper = dapperSqlMapper;
        }

        public IEnumerable<Student> GetStudents()
        {
            return _dapperSqlMapper.Query<Student>(_dbConnection, "SELECT * FROM STUDENT");
        }
    }
}
