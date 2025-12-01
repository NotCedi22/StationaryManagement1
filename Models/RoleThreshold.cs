using System.ComponentModel.DataAnnotations;

namespace StationaryManagement.Models
{
    public class RoleThreshold
    {
        [Key]
        public int RoleId { get; set; }  // Primary key
        public decimal MaxAmount { get; set; }

        // Navigation
        public Role? Role { get; set; }
    }
}
