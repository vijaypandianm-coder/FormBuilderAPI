using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace FormBuilderAPI.Data
{
    public sealed class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connString;
        public MySqlConnectionFactory(IConfiguration cfg)
        {
            _connString = cfg.GetConnectionString("Sql")
                          ?? throw new InvalidOperationException("Missing ConnectionStrings:Sql");
        }

        public IDbConnection Create() => new MySqlConnection(_connString);
    }
}