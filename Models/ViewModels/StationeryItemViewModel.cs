namespace StationaryManagement.Models.ViewModels
{
    public class StationeryItemViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public decimal UnitCost { get; set; }
        public int SelectedQuantity { get; set; }
    }
}