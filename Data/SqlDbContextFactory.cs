using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FormBuilderAPI.Data
{
    public class SqlDbContextFactory : IDesignTimeDbContextFactory<SqlDbContext>
    {
        public SqlDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqlDbContext>();

            // 🔑 Use your real connection string
            var connectionString = "server=localhost;port=3306;database=FormBuilderDB;user=root;password=Mvrvvijay@10";

            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new SqlDbContext(optionsBuilder.Options);
        }
    }
}

