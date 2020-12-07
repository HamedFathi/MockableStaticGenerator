using System.Collections.Generic;
using System.Data;
using System;
using Dapper.MockableGenerated;

namespace DapperSample
{
    [MockableStatic(typeof(Dapper.SqlMapper))]
    public class StudentRepository : IStudentRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly ISqlMapperWrapper _dapperSqlMapper;

        public StudentRepository(IDbConnection dbConnection, ISqlMapperWrapper dapperSqlMapper)
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
