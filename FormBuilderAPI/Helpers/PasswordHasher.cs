using BCrypt.Net;

namespace FormBuilderAPI.Helpers
{
    public static class PasswordHasher
    {
        public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
        public static bool Verify(string password, string hashed) => BCrypt.Net.BCrypt.Verify(password, hashed);
    }
}