using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IssueTrackerApi.Configuration;
using IssueTrackerApi.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IssueTrackerApi.Tests;

[Trait("Category", "Unit")]
public class TokenServiceTests
{
    [Fact]
    public void CreateToken_ValidUser_ProducesSignedIdentityRoleAndTwoHourExpiry()
    {
        var settings = NewSettings();
        var service = new TokenService(Options.Create(settings));
        var earliestExpiry = DateTime.UtcNow.AddHours(2).AddSeconds(-1);

        var encoded = service.CreateToken(42, "developer@example.invalid", "Developer");

        var principal = new JwtSecurityTokenHandler().ValidateToken(encoded, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        }, out var validated);
        var jwt = Assert.IsType<JwtSecurityToken>(validated);
        Assert.Equal("42", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("developer@example.invalid", principal.Identity?.Name);
        Assert.True(principal.IsInRole("Developer"));
        Assert.False(principal.IsInRole("Admin"));
        Assert.InRange(jwt.ValidTo, earliestExpiry, DateTime.UtcNow.AddHours(2));
        Assert.DoesNotContain(settings.Key, jwt.Payload.SerializeToJson());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateToken_NonpositiveUserId_IsRejected(int userId)
    {
        var service = new TokenService(Options.Create(NewSettings()));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.CreateToken(userId, "developer@example.invalid", "Developer"));
    }

    private static JwtOptions NewSettings() => new()
    {
        Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        Issuer = "TokenServiceTests",
        Audience = "TokenServiceTests"
    };
}
