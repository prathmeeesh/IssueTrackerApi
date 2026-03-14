using IssueTrackerApi.Data;
using IssueTrackerApi.DTOs;
using IssueTrackerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IssueTrackerApi.Controllers
{
    [ApiController]
    [Route("api/issues")]
    [Authorize]
    public class IssuesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IssuesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateIssue(CreateIssueDto dto)
        {
            var issue = new Issue
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                ProjectId = dto.ProjectId,
                Status = "Open",
                CreatedDate = DateTime.UtcNow
            };

            _context.Issues.Add(issue);
            await _context.SaveChangesAsync();

            return Ok(issue);
        }

        [HttpGet]
        public async Task<IActionResult> GetIssues(string? status, int page = 1, int pageSize = 10)
        {
            var query = _context.Issues.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(i => i.Status == status);

            var issues = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(issues);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIssue(int id)
        {
            var issue = await _context.Issues.FindAsync(id);

            if (issue == null)
                return NotFound();

            return Ok(issue);
        }

        [HttpPut("{id}/assign/{userId}")]
        public async Task<IActionResult> AssignUser(int id, int userId)
        {
            var issue = await _context.Issues.FindAsync(id);

            if (issue == null)
                return NotFound();

            issue.AssignedUserId = userId;

            await _context.SaveChangesAsync();

            return Ok(issue);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, string newStatus)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null) return NotFound();

            var validTransitions = new Dictionary<string, string[]>
            {
                { "Open", new[] { "InProgress" } },
                { "InProgress", new[] { "Resolved" } },
                { "Resolved", new[] { "Closed" } },
                { "Closed", new string[] { } }
            };

            if (!validTransitions[issue.Status].Contains(newStatus))
                return BadRequest("Invalid status transition");

            var history = new IssueHistory
            {
                IssueId = issue.Id,
                OldStatus = issue.Status,
                NewStatus = newStatus,
                ChangedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0")
            };

            issue.Status = newStatus;

            _context.IssueHistories.Add(history);
            await _context.SaveChangesAsync();

            return Ok(issue);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetIssueHistory(int id)
        {
            var history = await _context.IssueHistories
                .Where(x => x.IssueId == id)
                .ToListAsync();

            return Ok(history);
        }
    }
}