using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class MedicalCondition
    {
        [Key]
        public int ConditionId { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        [Required]
        public string ConditionName { get; set; } = string.Empty;
        public MedicalConditionCategory? Category { get; set; }
    }
}
