using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class PatientMedication
    {
        [Key]
        public int MedicationId { get; set; }
        public int PatientId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDoctorAdded { get; set; }
        public Patient? Patient { get; set; }
        public Medication? Medication { get; set; }
    }
}
