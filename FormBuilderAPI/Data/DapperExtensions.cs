using System.Data;

namespace FormBuilderAPI.Data
{
    public static class DapperExtensions
    {
        public static async Task<T> WithConn<T>(this IDbConnectionFactory factory, Func<IDbConnection, Task<T>> work)
        {
            using var conn = factory.Create();
            await (conn as System.Data.Common.DbConnection)!.OpenAsync();
            return await work(conn);
        }

        public static async Task WithConn(this IDbConnectionFactory factory, Func<IDbConnection, Task> work)
        {
            using var conn = factory.Create();
            await (conn as System.Data.Common.DbConnection)!.OpenAsync();
            await work(conn);
        }
    }
}
