using System.ComponentModel.DataAnnotations;

namespace IssueTrackerApi.DTOs
{
    public class AddCommentDto
    {
        [Range(1, int.MaxValue)]
        public int IssueId { get; set; }

        [Required]
        [MinLength(1)]
        public string Message { get; set; } = "";
    }
}
