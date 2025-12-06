namespace StationaryManagement1.Models
{
    public class RequestItem
    {
        public int RequestItemId { get; set; }
        public int RequestId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }

        // EF Core will calculate this for you in the DB
        public decimal LineTotal => Quantity * UnitCost;

        // Navigation
        public StationeryRequest? Request { get; set; }
        public StationeryItem? Item { get; set; }
    }
}
