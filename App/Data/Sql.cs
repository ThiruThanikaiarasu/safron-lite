using System.Data;
using Microsoft.Data.SqlClient;

namespace Saffron.Data;

internal static class Sql
{
    public static SqlCommand Proc(SqlConnection conn, string name)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = name;
        return cmd;
    }

    public static object? NullIfDbNull(object value) => value == DBNull.Value ? null : value;
}
