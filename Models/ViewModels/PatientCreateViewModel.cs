using NMB_HLabSys.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class PatientCreateViewModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Surname { get; set; } = string.Empty;

        [Required, StringLength(13, MinimumLength = 13)]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "SA ID number must contain exactly 13 digits.")]
        [Display(Name = "SA ID Number")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required, DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cell number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Cell number must contain exactly 10 digits.")]
        [Display(Name = "Cell Number")]
        public string? CellNumber { get; set; }

        [Required, EmailAddress]
        [Display(Name = "Email (Username)")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Street { get; set; } = string.Empty;

        [Display(Name = "Street address line 2")]
        public string? Street2 { get; set; }

        [Required]
        public string Suburb { get; set; } = string.Empty;

        [Required]
        public string Town { get; set; } = string.Empty;

        [Required]
        public string Province { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Postal Code")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Postal code must be 4 digits.")]
        public string PostalCode { get; set; } = string.Empty;

        public List<int> SelectedConditionIds { get; set; } = new();
        public List<int> SelectedAllergyIds { get; set; } = new();
        public List<int> SelectedMedicationIds { get; set; } = new();

        public List<MedicalCondition> AvailableConditions { get; set; } = new();
        public List<Allergy> AvailableAllergies { get; set; } = new();
        public List<Medication> AvailableMedications { get; set; } = new();
    }
}
