namespace StationaryManagement.Models;
using StationaryManagement.Models;
using System.ComponentModel.DataAnnotations;

public class StationeryRequest
{
    [Key]
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }
    public int SuperiorId { get; set; }
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal TotalCost { get; set; }
    public string? Reason { get; set; }
    public DateTime? LastStatusChangedAt { get; set; }

    // Navigation
    public Employee? Employee { get; set; }
    public Employee? Superior { get; set; }
    public ICollection<RequestItem> RequestItems { get; set; } = new List<RequestItem>();
}
