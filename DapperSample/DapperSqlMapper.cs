using Dapper;
using System.Collections.Generic;
using System.Data;

namespace DapperSample
{
    public class DapperSqlMapper : IDapperSqlMapper
    {
        public IEnumerable<T> Query<T>(IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return cnn.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType);
        }
    }
}
