using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class StockOrderItem
    {
        [Key]
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ConsumableId { get; set; }
        public int QuantityOrdered { get; set; }
        public string Status { get; set; } = "Ordered";
        public DateTime? DateReceived { get; set; }
        public DateTime? DateCancelled { get; set; }
        public string? CancelReason { get; set; }
        public Consumable? Consumable { get; set; }
		//Added
		public StockOrder? StockOrder { get; set; }
	}
}
