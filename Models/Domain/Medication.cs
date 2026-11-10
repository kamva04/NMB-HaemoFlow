using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class Medication
    {
        [Key]
        public int MedicationId { get; set; }
        public int CategoryId { get; set; }
        [Required] 
        public string MedicationName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public MedicationCategory? Category { get; set; }
    }
}
