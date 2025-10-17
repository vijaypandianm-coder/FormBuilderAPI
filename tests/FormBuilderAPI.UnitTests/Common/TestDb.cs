using System;
using FormBuilderAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilderAPI.UnitTests.Common;

public static class TestDb
{
    public static SqlDbContext Create(string? name = null)
    {
        var opts = new DbContextOptionsBuilder<SqlDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;

        return new SqlDbContext(opts);
    }
}
