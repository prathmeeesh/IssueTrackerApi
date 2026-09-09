using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IssueTrackerApi.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace IssueTrackerApi.Services
{
    public class TokenService
    {
        private readonly JwtOptions _jwt;

        public TokenService(IOptions<JwtOptions> options)
        {
            _jwt = options.Value;
        }

        public string CreateToken(int userId, string email, string role)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwt.Key)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
