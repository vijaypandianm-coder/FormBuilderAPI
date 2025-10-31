using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;

public class SqlDbContextModelTests
{
    private static SqlDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<SqlDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;

        return new SqlDbContext(opts);
    }

    [Fact]
    public async Task Can_Create_And_Save_Minimum_Entities()
    {
        await using var db = NewDb();

        db.Users.Add(new User
        {
            Email = "u@example.com",
            PasswordHash = "hash",
            Role = "Admin",
            IsActive = true,
            Username = "admin",
            CreatedAt = System.DateTime.UtcNow
        });

        db.FormResponses.Add(new FormResponse
        {
            FormId = "abc123",
            FormKey = 7,
            UserId = 1,
            SubmittedAt = System.DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        db.Users.Should().HaveCount(1);
        db.FormResponses.Should().HaveCount(1);
    }

    [Fact]
    public void OnModelCreating_Configures_Sets()
    {
        using var db = NewDb();

        db.Users.EntityType.Should().NotBeNull();
        db.AuditLogs.EntityType.Should().NotBeNull();
        db.FormResponseAnswers.EntityType.Should().NotBeNull();
        db.FormResponseFiles.Should().NotBeNull();
    }
}