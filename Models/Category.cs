namespace StationaryManagement.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation property for items in this category
    public ICollection<StationeryItem> Items { get; set; } = new List<StationeryItem>();
}