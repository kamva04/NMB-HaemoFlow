using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class PatientCondition
    {
        [Key]
        public int PatientId { get; set; }
        public int ConditionId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDoctorAdded { get; set; }
        public Patient? Patient { get; set; }
        public MedicalCondition? Condition { get; set; }
    }
}
