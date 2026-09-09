using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IssueTrackerApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace IssueTrackerApi.Tests;

[Trait("Category", "Integration")]
public class JwtAuthorizationTests
{
    [Theory]
    [InlineData("GET", "/api/issues")]
    [InlineData("POST", "/api/projects")]
    [InlineData("POST", "/api/comments")]
    public async Task ProtectedEndpoint_WithoutBearerToken_ReturnsUnauthorized(string method, string path)
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("wrong-signature")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("expired")]
    [InlineData("wrong-algorithm")]
    [InlineData("missing-user-id")]
    [InlineData("zero-user-id")]
    [InlineData("nonnumeric-user-id")]
    public async Task ProtectedEndpoint_InvalidBearerToken_IsRejected(string fault)
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();
        var key = fault == "wrong-signature"
            ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            : app.Jwt.Key;
        var claims = new List<Claim> { new(ClaimTypes.Role, "Admin") };
        if (fault != "missing-user-id")
        {
            var userId = fault switch
            {
                "zero-user-id" => "0",
                "nonnumeric-user-id" => "invalid",
                _ => "7"
            };
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }
        var jwt = new JwtSecurityToken(
            issuer: fault == "wrong-issuer" ? "DifferentIssuer" : app.Jwt.Issuer,
            audience: fault == "wrong-audience" ? "DifferentAudience" : app.Jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(fault == "expired" ? -10 : 5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                fault == "wrong-algorithm" ? SecurityAlgorithms.HmacSha512 : SecurityAlgorithms.HmacSha256));
        var token = fault == "malformed" ? "invalid-token" : new JwtSecurityTokenHandler().WriteToken(jwt);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/issues", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("error_description", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task CreateProject_AdminToken_SucceedsAndUsesServerTimestamp()
    {
        using var app = new IssueTrackerApiFactory();
        var admin = await app.SeedUserAsync("Admin");
        using var client = app.CreateApiClient(admin);
        var started = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Authorised project",
            Description = "Created by an administrator",
            CreatedDate = DateTime.UnixEpoch
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var context = app.CreateDbContext();
        var project = await context.Projects.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Authorised project", project.Name);
        Assert.Equal("Created by an administrator", project.Description);
        Assert.InRange(project.CreatedDate, started, DateTime.UtcNow);
    }
}
