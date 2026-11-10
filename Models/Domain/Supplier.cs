using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }
        [Required]
        public string SupplierName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public string? ContactPerson { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
