using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FormBuilderAPI.Helpers
{
    public static class JwtHelper
    {
        public static string IssueToken(
            string userId, string username, IEnumerable<string> roles,
            string key, string issuer, string audience, int minutes, out DateTime expiresAt)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim("uid", userId)
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            expiresAt = DateTime.UtcNow.AddMinutes(minutes);

            var token = new JwtSecurityToken(
                issuer: issuer, audience: audience, claims: claims,
                expires: expiresAt, signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

