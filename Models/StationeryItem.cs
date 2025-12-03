namespace StationaryManagement.Models;
using StationaryManagement.Models;
using System.ComponentModel.DataAnnotations;
public class StationeryItem
{
    [Key]
    public int ItemId { get; set; }
    public string ItemName { get; set; } = null!;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public decimal UnitCost { get; set; }
    public int CurrentStock { get; set; } = 0;
    public string? ImagePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    // Navigation
    public Category? Category { get; set; }
    public ICollection<RequestItem> RequestItems { get; set; } = new List<RequestItem>();
}
