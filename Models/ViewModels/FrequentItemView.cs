namespace StationaryManagement1.Models.ViewModels
{
    public class FrequentItemView
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public int TotalRequested { get; set; }
        public int RequestorCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
