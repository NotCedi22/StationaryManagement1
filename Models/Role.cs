namespace StationaryManagement1.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        public int? ReportsToRoleId { get; set; }

        // Add this property for approval hierarchy
        public bool CanApprove { get; set; } = false;

        // Navigation properties
        public Role? ReportsTo { get; set; }
        public ICollection<Role> DirectReports { get; set; } = new List<Role>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public RoleThreshold? RoleThreshold { get; set; }
    }
}
