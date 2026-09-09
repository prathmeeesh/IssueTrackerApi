using System.Net.Http.Headers;
using System.Security.Cryptography;
using IssueTrackerApi.Configuration;
using IssueTrackerApi.Data;
using IssueTrackerApi.Models;
using IssueTrackerApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IssueTrackerApi.Tests.Infrastructure;

internal sealed class IssueTrackerApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> _databaseOptions;
    private readonly Dictionary<string, string?> _configuration;

    public JwtOptions Jwt { get; } = new()
    {
        Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        Issuer = "IssueTrackerApi.Tests",
        Audience = "IssueTrackerApi.Tests"
    };

    public IssueTrackerApiFactory(Dictionary<string, string?>? overrides = null)
    {
        _configuration = new()
        {
            [HostDefaults.EnvironmentKey] = "Testing",
            ["ConnectionStrings:Default"] = "Replaced by isolated SQLite in tests",
            ["Jwt:Key"] = Jwt.Key,
            ["Jwt:Issuer"] = Jwt.Issuer,
            ["Jwt:Audience"] = Jwt.Audience
        };
        if (overrides is not null)
        {
            foreach (var entry in overrides)
                _configuration[entry.Key] = entry.Value;
        }

        _connection.Open();
        _databaseOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options;
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Host settings reach Program's early validation without changing process environment variables.
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(_configuration));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public AppDbContext CreateDbContext() => new(_databaseOptions);

    public HttpClient CreateApiClient(User? user = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        if (user is not null)
        {
            using var scope = Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<TokenService>()
                .CreateToken(user.Id, user.Email, user.Role);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    public static string CreatePassword() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));

    public async Task<User> SeedUserAsync(string role = "Developer")
    {
        using var context = CreateDbContext();
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.invalid",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(CreatePassword()),
            Role = role
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    public async Task<Issue> SeedIssueAsync(string status = "Open")
    {
        using var context = CreateDbContext();
        var project = new Project { Name = "Test project" };
        context.Projects.Add(project);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var issue = new Issue { Title = "Test issue", ProjectId = project.Id, Status = status };
        context.Issues.Add(issue);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return issue;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
