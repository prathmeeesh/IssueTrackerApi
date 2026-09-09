using IssueTrackerApi.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace IssueTrackerApi.Tests;

[Trait("Category", "Integration")]
public class ConfigurationTests
{
    [Theory]
    [InlineData("Jwt:Key", "", "Jwt:Key")]
    [InlineData("Jwt:Key", "short", "Jwt:Key")]
    [InlineData("Jwt:Issuer", "", "Jwt:Issuer")]
    [InlineData("Jwt:Audience", "", "Jwt:Audience")]
    public void Startup_InvalidJwtConfiguration_IsRejected(string setting, string value, string expectedSetting)
    {
        using var app = new IssueTrackerApiFactory(new() { [setting] = value });

        var error = Assert.Throws<OptionsValidationException>(() => app.CreateApiClient());

        Assert.Contains(expectedSetting, error.Message);
    }

    [Fact]
    public void Startup_MissingDatabaseConfiguration_IsRejected()
    {
        using var app = new IssueTrackerApiFactory(new() { ["ConnectionStrings:Default"] = "" });

        var error = Assert.Throws<InvalidOperationException>(() => app.CreateApiClient());

        Assert.Contains("ConnectionStrings:Default", error.Message);
    }
}
