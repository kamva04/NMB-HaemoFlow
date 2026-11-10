using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }
        public int UserId { get; set; }

        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Surname { get; set; } = string.Empty;

        [Required, StringLength(13, MinimumLength = 13 )]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required] public DateTime DateOfBirth { get; set; }
        public string? CellNumber { get; set; }
        public string? Gender { get; set; }
        public string? Street { get; set; }
        public string? Street2 { get; set; }
        public string? Suburb { get; set; }
        public string? Town { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        // Retained for existing patient records created before structured addresses were introduced.
        public string? HomeAddress { get; set; }
        public User? User { get; set; }

        public ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
        public ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
        public ICollection<PatientMedication> PatientMedications { get; set; } = new List<PatientMedication>();
        public ICollection<DoctorPatientConsent> DoctorPatientConsents { get; set; } = new List<DoctorPatientConsent>();
        public ICollection<TestRequest> TestRequests { get; set; } = new List<TestRequest>();

        public List<MedicalCondition> Conditions { get; set; } = new();
        public List<Allergy> Allergies { get; set; } = new();
        public List<Medication> Medications { get; set; } = new();
		
	}
}
