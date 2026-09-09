using System.Net;
using System.Net.Http.Json;
using IssueTrackerApi.Models;
using IssueTrackerApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackerApi.Tests;

[Trait("Category", "Integration")]
public class IssueWorkflowTests
{
    [Fact]
    public async Task ChangeStatus_FullWorkflow_PersistsTransitionsAndAuthenticatedActor()
    {
        using var app = new IssueTrackerApiFactory();
        var actor = await app.SeedUserAsync();
        var issue = await app.SeedIssueAsync();
        using var client = app.CreateApiClient(actor);
        var started = DateTime.UtcNow;

        foreach (var nextStatus in new[] { "InProgress", "Resolved", "Closed" })
        {
            using var response = await client.PutAsync(
                $"/api/issues/{issue.Id}/status?newStatus={nextStatus}", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<Issue>(TestContext.Current.CancellationToken);
            Assert.NotNull(updated);
            Assert.Equal(nextStatus, updated.Status);
        }

        using var context = app.CreateDbContext();
        var stored = await context.Issues.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Closed", stored.Status);
        Assert.Equal(issue.ProjectId, stored.ProjectId);
        var history = await context.IssueHistories.OrderBy(row => row.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(
            new[] { ("Open", "InProgress"), ("InProgress", "Resolved"), ("Resolved", "Closed") },
            history.Select(row => (row.OldStatus, row.NewStatus)));
        Assert.All(history, row =>
        {
            Assert.Equal(issue.Id, row.IssueId);
            Assert.Equal(actor.Id, row.ChangedByUserId);
            Assert.InRange(row.ChangedDate, started, DateTime.UtcNow);
        });
    }

    [Theory]
    [InlineData("Open", "Open")]
    [InlineData("Open", "Resolved")]
    [InlineData("Open", "Closed")]
    [InlineData("InProgress", "Open")]
    [InlineData("InProgress", "InProgress")]
    [InlineData("InProgress", "Closed")]
    [InlineData("Resolved", "Open")]
    [InlineData("Resolved", "InProgress")]
    [InlineData("Resolved", "Resolved")]
    [InlineData("Closed", "Open")]
    [InlineData("Closed", "InProgress")]
    [InlineData("Closed", "Resolved")]
    [InlineData("Closed", "Closed")]
    [InlineData("Open", "Unknown")]
    [InlineData("Unknown", "InProgress")]
    public async Task ChangeStatus_InvalidTransition_DoesNotChangeIssueOrAppendHistory(
        string currentStatus, string requestedStatus)
    {
        using var app = new IssueTrackerApiFactory();
        var actor = await app.SeedUserAsync();
        var issue = await app.SeedIssueAsync(currentStatus);
        using var client = app.CreateApiClient(actor);

        using var response = await client.PutAsync(
            $"/api/issues/{issue.Id}/status?newStatus={requestedStatus}", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid status transition", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var context = app.CreateDbContext();
        Assert.Equal(currentStatus, (await context.Issues.SingleAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Empty(await context.IssueHistories.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ChangeStatus_MissingIssue_ReturnsNotFoundWithoutHistory()
    {
        using var app = new IssueTrackerApiFactory();
        var actor = await app.SeedUserAsync();
        using var client = app.CreateApiClient(actor);

        using var response = await client.PutAsync(
            "/api/issues/999/status?newStatus=InProgress", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var context = app.CreateDbContext();
        Assert.Empty(await context.IssueHistories.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetIssueHistory_ReturnsOnlyTheRequestedIssuesHistory()
    {
        using var app = new IssueTrackerApiFactory();
        var actor = await app.SeedUserAsync();
        var first = await app.SeedIssueAsync();
        var second = await app.SeedIssueAsync();
        using var client = app.CreateApiClient(actor);
        foreach (var issue in new[] { first, second })
        {
            using var change = await client.PutAsync(
                $"/api/issues/{issue.Id}/status?newStatus=InProgress", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        }

        using var response = await client.GetAsync(
            $"/api/issues/{first.Id}/history", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<List<IssueHistory>>(TestContext.Current.CancellationToken);
        Assert.NotNull(history);
        var row = Assert.Single(history);
        Assert.Equal(first.Id, row.IssueId);
        Assert.Equal(actor.Id, row.ChangedByUserId);
        Assert.Equal("Open", row.OldStatus);
        Assert.Equal("InProgress", row.NewStatus);
    }
}
