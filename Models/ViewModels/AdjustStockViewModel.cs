using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class AdjustStockViewModel
    {
        public int ConsumableId { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }

        [Required, Display(Name = "Adjustment Type")]
        public string AdjustmentType { get; set; } = "Increase"; // Increase|Decrease|SetTo

        [Required, Range(0, int.MaxValue)]
        public int Amount { get; set; }
    }
}
