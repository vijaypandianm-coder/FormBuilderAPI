using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FormBuilderAPI.Helpers
{
    // Keep it public static so tests can call it directly.
    public static class JwtHelper
    {
        public static string GenerateToken(
            string issuer,
            string audience,
            string signingKey,
            IEnumerable<Claim> claims,
            TimeSpan? lifetime = null)
        {
            var keyBytes = Encoding.UTF8.GetBytes(signingKey);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(lifetime ?? TimeSpan.FromHours(2)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal? ValidateToken(
            string token,
            string issuer,
            string audience,
            string signingKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(signingKey);
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var handler = new JwtSecurityTokenHandler();
            try
            {
                return handler.ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}