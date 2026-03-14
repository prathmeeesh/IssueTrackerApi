namespace IssueTrackerApi.Models
{
    public class Issue
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string Priority { get; set; } = "Medium";

        public string Status { get; set; } = "Open";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int ProjectId { get; set; }

        public int? AssignedUserId { get; set; }
    }
}