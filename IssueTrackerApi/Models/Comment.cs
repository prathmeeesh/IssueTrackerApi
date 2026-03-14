namespace IssueTrackerApi.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int IssueId { get; set; }

        public int UserId { get; set; }

        public string Message { get; set; } = "";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}