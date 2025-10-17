using System;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.UnitTests.Common;

public static class TestHelpers
{
    public static User MakeUser(long id = 1, string email = "user@example.com")
        => new()
        {
            Id = id,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd"),
            Role = "Learner"
        };
}
