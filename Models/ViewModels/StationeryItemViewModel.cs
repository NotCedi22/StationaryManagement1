namespace StationaryManagement.ViewModels
{
    public class StationeryItemViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public int CurrentStock { get; set; }
        public decimal UnitCost { get; set; }
    }
}
