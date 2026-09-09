using System.Net;
using System.Net.Http.Json;
using IssueTrackerApi.Models;
using IssueTrackerApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackerApi.Tests;

[Trait("Category", "Integration")]
public class ServerControlledFieldsTests
{
    [Fact]
    public async Task CreateIssue_BindsProjectButIgnoresClientStatusIdentityAssignmentAndTimestamp()
    {
        using var app = new IssueTrackerApiFactory();
        var actor = await app.SeedUserAsync();
        using var client = app.CreateApiClient(actor);
        using var context = app.CreateDbContext();
        var firstProject = new Project { Name = "Unrelated project" };
        var targetProject = new Project { Name = "Target project" };
        context.Projects.AddRange(firstProject, targetProject);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var started = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync("/api/issues", new
        {
            Id = 999,
            Title = "Preserve the project link",
            Description = "A new issue",
            Priority = "High",
            ProjectId = targetProject.Id,
            Status = "Closed",
            AssignedUserId = actor.Id,
            CreatedDate = DateTime.UnixEpoch
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<Issue>(TestContext.Current.CancellationToken);
        Assert.NotNull(returned);
        using var verification = app.CreateDbContext();
        var stored = await verification.Issues.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(returned.Id, stored.Id);
        Assert.NotEqual(999, stored.Id);
        Assert.Equal("Open", stored.Status);
        Assert.Null(stored.AssignedUserId);
        Assert.Equal(targetProject.Id, stored.ProjectId);
        Assert.Equal("Preserve the project link", stored.Title);
        Assert.Equal("A new issue", stored.Description);
        Assert.Equal("High", stored.Priority);
        Assert.InRange(stored.CreatedDate, started, DateTime.UtcNow);
        Assert.Empty(await verification.IssueHistories.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddComment_UsesAuthenticatedAuthorAndPreservesIssueLink()
    {
        using var app = new IssueTrackerApiFactory();
        var actor = await app.SeedUserAsync();
        var impersonated = await app.SeedUserAsync();
        var firstIssue = await app.SeedIssueAsync();
        var targetIssue = await app.SeedIssueAsync();
        using var client = app.CreateApiClient(actor);
        var started = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync("/api/comments", new
        {
            IssueId = targetIssue.Id,
            UserId = impersonated.Id,
            Message = "The author must come from the token",
            CreatedDate = DateTime.UnixEpoch
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<Comment>(TestContext.Current.CancellationToken);
        Assert.NotNull(returned);
        Assert.Equal(actor.Id, returned.UserId);
        using var context = app.CreateDbContext();
        var stored = await context.Comments.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(actor.Id, stored.UserId);
        Assert.Equal(targetIssue.Id, stored.IssueId);
        Assert.NotEqual(firstIssue.Id, stored.IssueId);
        Assert.Equal("The author must come from the token", stored.Message);
        Assert.InRange(stored.CreatedDate, started, DateTime.UtcNow);
    }

    [Fact]
    public async Task AssignUser_ExistingIssueAndUser_PersistsAssignmentWithoutChangingProjectOrStatus()
    {
        using var app = new IssueTrackerApiFactory();
        var actor = await app.SeedUserAsync();
        var assignee = await app.SeedUserAsync();
        var issue = await app.SeedIssueAsync();
        using var client = app.CreateApiClient(actor);

        using var response = await client.PutAsync(
            $"/api/issues/{issue.Id}/assign/{assignee.Id}", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var context = app.CreateDbContext();
        var stored = await context.Issues.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(assignee.Id, stored.AssignedUserId);
        Assert.Equal(issue.ProjectId, stored.ProjectId);
        Assert.Equal("Open", stored.Status);
        Assert.Empty(await context.IssueHistories.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateProject_UsesDtoAndIgnoresClientControlledIdentityAndTimestamp()
    {
        using var app = new IssueTrackerApiFactory();
        var admin = await app.SeedUserAsync("Admin");
        using var client = app.CreateApiClient(admin);
        var started = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync("/api/projects", new
        {
            Id = 777,
            Name = "Portfolio hardening",
            Description = "Server controls project identity",
            CreatedDate = DateTime.UnixEpoch
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<Project>(TestContext.Current.CancellationToken);
        Assert.NotNull(returned);
        Assert.NotEqual(777, returned.Id);
        Assert.Equal("Portfolio hardening", returned.Name);
        Assert.Equal("Server controls project identity", returned.Description);
        Assert.InRange(returned.CreatedDate, started, DateTime.UtcNow);

        using var context = app.CreateDbContext();
        var stored = await context.Projects.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(returned.Id, stored.Id);
        Assert.NotEqual(777, stored.Id);
        Assert.InRange(stored.CreatedDate, started, DateTime.UtcNow);
    }
}
