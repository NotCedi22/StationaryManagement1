namespace StationaryManagement1.Models.ViewModels
{
    public class ItemCostReportViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public decimal UnitCost { get; set; }
        public int TotalRequested { get; set; }
        public int HeadCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal PercentOfTotal { get; set; }
        public decimal CumulativeCost { get; set; }
    }
}

