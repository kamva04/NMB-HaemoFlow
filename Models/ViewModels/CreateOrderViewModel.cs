using NMB_HLabSys.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class CreateOrderViewModel
    {
        [Required, Display(Name = "Order Number")]
        public string OrderNumber { get; set; } = string.Empty;

        [Required, Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        public List<OrderLineViewModel> Lines { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Consumable> LowStockConsumables { get; set; } = new();
    }
}
