using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class PatientAllergy
    {
        [Key]
        public int PatientId { get; set; }
        public int AllergyId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDoctorAdded { get; set; }
        public Patient? Patient { get; set; }
        public Allergy? Allergy { get; set; }
    }
}
