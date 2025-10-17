using System;
using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Data;

namespace FormBuilderAPI.UnitTests.Common;

public sealed class InMemoryDbFixture : IDisposable
{
    public SqlDbContext Db { get; }

    public InMemoryDbFixture()
    {
        var options = new DbContextOptionsBuilder<SqlDbContext>()
            .UseInMemoryDatabase($"fbapi_unit_{Guid.NewGuid():N}")
            .Options;

        Db = new SqlDbContext(options);
        Db.Database.EnsureCreated();
    }

    public void Dispose() => Db.Dispose();
}