namespace StationaryManagement.Models;
public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; } // <-- add this

    // Navigation property for items in this category
    public ICollection<StationeryItem> Items { get; set; } = new List<StationeryItem>();
}
//a