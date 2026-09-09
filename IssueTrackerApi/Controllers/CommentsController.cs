using IssueTrackerApi.Data;
using IssueTrackerApi.DTOs;
using IssueTrackerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace IssueTrackerApi.Controllers
{
    [ApiController]
    [Route("api/comments")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(AddCommentDto dto)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
                NumberStyles.None, CultureInfo.InvariantCulture, out var userId) || userId <= 0)
            {
                return Unauthorized();
            }

            var comment = new Comment
            {
                IssueId = dto.IssueId,
                Message = dto.Message,
                UserId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok(comment);
        }
    }
}
