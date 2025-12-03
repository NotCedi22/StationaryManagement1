namespace StationaryManagement.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int EmployeeId { get; set; }
        public int? RelatedRequestId { get; set; }
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Employee? Employee { get; set; }
        public StationeryRequest? RelatedRequest { get; set; }
    }

}
