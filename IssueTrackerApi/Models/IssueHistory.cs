namespace IssueTrackerApi.Models
{
    public class IssueHistory
    {
        public int Id { get; set; }

        public int IssueId { get; set; }

        public string OldStatus { get; set; } = "";

        public string NewStatus { get; set; } = "";

        public int ChangedByUserId { get; set; }

        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    }
}