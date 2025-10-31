using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Data;

namespace FormBuilderAPI.UnitTests.TestUtils;

public static class InMemorySql
{
    public static SqlDbContext NewDb(string? name = null)
    {
        var opts = new DbContextOptionsBuilder<SqlDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        return new SqlDbContext(opts);
    }
}