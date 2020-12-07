using System.Collections.Generic;
using System.Data;

namespace DapperSample
{

    public interface IDapperSqlMapper
    {
        IEnumerable<T> Query<T>(IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null);
    }
}
