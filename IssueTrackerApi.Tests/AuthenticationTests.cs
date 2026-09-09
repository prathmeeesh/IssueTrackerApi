using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IssueTrackerApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackerApi.Tests;

[Trait("Category", "Integration")]
public class AuthenticationTests
{
    [Fact]
    public async Task Register_DefaultUser_CannotSelfAssignAdminRoleOrStoredIdentity()
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();
        var password = IssueTrackerApiFactory.CreatePassword();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "developer@example.invalid",
            Password = password,
            Role = "Admin",
            Id = 999,
            PasswordHash = "client-controlled"
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var context = app.CreateDbContext();
        var stored = await context.Users.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Developer", stored.Role);
        Assert.NotEqual(999, stored.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify(password, stored.PasswordHash));

        using var login = await client.PostAsJsonAsync("/api/auth/login",
            new { stored.Email, Password = password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var createProject = await client.PostAsJsonAsync("/api/projects",
            new { Name = "Attempted privileged operation" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, createProject.StatusCode);
        Assert.Empty(await context.Projects.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_SamePasswordForTwoUsers_StoresDistinctVerifiableHashes()
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();
        var password = IssueTrackerApiFactory.CreatePassword();
        foreach (var email in new[] { "first@example.invalid", "second@example.invalid" })
        {
            using var response = await client.PostAsJsonAsync("/api/auth/register",
                new { Email = email, Password = password }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain(password, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        using var context = app.CreateDbContext();
        var users = await context.Users.OrderBy(user => user.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, users.Count);
        Assert.NotEqual(users[0].PasswordHash, users[1].PasswordHash);
        Assert.All(users, user =>
        {
            Assert.NotEqual(password, user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
            Assert.False(BCrypt.Net.BCrypt.Verify(IssueTrackerApiFactory.CreatePassword(), user.PasswordHash));
        });
    }

    [Fact]
    public async Task Register_DuplicateEmail_DoesNotReplaceCredentialsOrRole()
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();
        var password = IssueTrackerApiFactory.CreatePassword();
        const string email = "existing@example.invalid";
        using var first = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var replacement = IssueTrackerApiFactory.CreatePassword();

        using var duplicate = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = replacement, Role = "Admin" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        using var context = app.CreateDbContext();
        var stored = await context.Users.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Developer", stored.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify(password, stored.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(replacement, stored.PasswordHash));
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("not-an-email", null)]
    [InlineData(null, "")]
    [InlineData(null, "7-chars")]
    public async Task Register_InvalidCredentials_AreRejectedBeforePersistence(string? invalidEmail, string? invalidPassword)
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = invalidEmail ?? "valid@example.invalid",
            Password = invalidPassword ?? IssueTrackerApiFactory.CreatePassword()
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var context = app.CreateDbContext();
        Assert.Empty(await context.Users.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsBearerTokenAcceptedByProtectedEndpoint()
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();
        var password = IssueTrackerApiFactory.CreatePassword()[..8];
        var credentials = new { Email = "login@example.invalid", Password = password };
        using var registration = await client.PostAsJsonAsync(
            "/api/auth/register", credentials, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        using var login = await client.PostAsJsonAsync("/api/auth/login", credentials, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(password, token);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var issues = await client.GetAsync("/api/issues", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, issues.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Login_WrongPasswordOrUnknownUser_ReturnsSameUnauthorizedResponse(bool unknownUser)
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();
        var credentials = new { Email = "known@example.invalid", Password = IssueTrackerApiFactory.CreatePassword() };
        using var registration = await client.PostAsJsonAsync(
            "/api/auth/register", credentials, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = unknownUser ? "unknown@example.invalid" : credentials.Email,
            Password = unknownUser ? credentials.Password : IssueTrackerApiFactory.CreatePassword()
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData(null, "")]
    public async Task Login_MissingCredentials_ReturnsBadRequest(string? invalidEmail, string? invalidPassword)
    {
        using var app = new IssueTrackerApiFactory();
        using var client = app.CreateApiClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = invalidEmail ?? "user@example.invalid",
            Password = invalidPassword ?? IssueTrackerApiFactory.CreatePassword()
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
