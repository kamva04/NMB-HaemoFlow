using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class PatientRegisterViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
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
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Cell number can't be less or more than 10 digits.")]
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

        [Required, DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters with one uppercase letter, one number, and one special character.")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
