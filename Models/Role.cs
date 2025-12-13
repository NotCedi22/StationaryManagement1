namespace StationaryManagement1.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        
        // Foreign key for role hierarchy (which role this role reports to)
        public int? ReportsToRoleId { get; set; }

        // Approval hierarchy - whether this role can approve requests
        public bool CanApprove { get; set; } = false;

        // Navigation properties
        // The role that this role reports to (navigation property for ReportsToRoleId)
        public Role? ReportsTo { get; set; }
        
        // Collection of roles that report to this role
        public ICollection<Role> DirectReports { get; set; } = new List<Role>();
        
        // Employees with this role
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        
        // Role threshold for approval limits
        public RoleThreshold? RoleThreshold { get; set; }
    }
}
