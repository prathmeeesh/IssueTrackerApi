using System.ComponentModel.DataAnnotations;

namespace IssueTrackerApi.DTOs
{
    public class CreateProjectDto
    {
        [Required]
        [MinLength(1)]
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";
    }
}
