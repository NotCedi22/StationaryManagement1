using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StationaryManagement.Models
{
    // Enum for request status
    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Withdrawn,
        Cancelled
    }

    public class StationeryRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int EmployeeId { get; set; }       // Who requested
        public int SuperiorId { get; set; }       // Manager/Superior
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal TotalCost { get; set; }
        public string? Reason { get; set; }
        public DateTime? LastStatusChangedAt { get; set; }

        // Use enum instead of string for Status
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        // Navigation properties
        public Employee? Employee { get; set; }
        public Employee? Superior { get; set; }
        public ICollection<RequestItem> RequestItems { get; set; } = new List<RequestItem>();
    }
}
