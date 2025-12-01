using System;
using System.Collections.Generic;

namespace StationaryManagement.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; } // auto-increment in DB if configured
        public string Name { get; set; } = null!;
        public int RoleId { get; set; }
        public string Email { get; set; } = null!;
        public string? PasswordHash { get; set; } = null!;
        public string? Location { get; set; }
        public string? Grade { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public int? SuperiorId { get; set; }

        // Navigation properties
        public Role? Role { get; set; }
        public Employee? Superior { get; set; }
        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
        public ICollection<StationeryRequest> StationeryRequests { get; set; } = new List<StationeryRequest>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
