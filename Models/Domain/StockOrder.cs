using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class StockOrder
    {
       [Key]
        public int OrderId { get; set; }
        [Required] 
        public string OrderNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Ordered";
        public DateTime? DateCompleted { get; set; }
        public DateTime? DateCancelled { get; set; }
        public string? CancelReason { get; set; }
        public Supplier? Supplier { get; set; }
        public List<StockOrderItem> Items { get; set; } = new();
		
	}
}
