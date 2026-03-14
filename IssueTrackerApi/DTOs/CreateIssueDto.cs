namespace IssueTrackerApi.DTOs
{
    public class CreateIssueDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "Medium";
        public int ProjectId { get; set; }
    }
}
