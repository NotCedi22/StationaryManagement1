namespace StationaryManagement1.Models.ViewModels
{
    public class StationeryItemViewModel
    {
        // Original properties for request creation
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public decimal UnitCost { get; set; }
        public int SelectedQuantity { get; set; }

        // New properties for availability display
        public StationeryItem? Item { get; set; }
        public int AvailableStock { get; set; }
        public int ReservedStock { get; set; }
    }
}