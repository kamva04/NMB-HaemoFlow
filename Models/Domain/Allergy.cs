using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class Allergy
    {
        [Key]
        public int AllergyId { get; set; }
        public int CategoryId { get; set; }
        [Required]
        public string AllergyName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public AllergyCategory? Category { get; set; }
    }
}
