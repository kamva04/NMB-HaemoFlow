using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class MedicalConditionCategory
    {
        [Key]
        public int CategoryId { get; set; }
        [Required]
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
