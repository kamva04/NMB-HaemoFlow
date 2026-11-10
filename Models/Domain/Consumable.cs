using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class Consumable
    {
        [Key]
        public int ConsumableId { get; set; }

        [Required]
        public string ConsumableName { get; set; } = string.Empty;

        public int ReorderLevel { get; set; }
        public int QuantityOnHand { get; set; }
        public int SupplierId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? TestTypeId { get; set; }
        public TestType? TestType { get; set; }


        // === NEW: Soft-removal audit ===
        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;
        public string? RemovalReason { get; set; }
        public DateTime? RemovedAt { get; set; }

        public Supplier? Supplier { get; set; }
    }
}