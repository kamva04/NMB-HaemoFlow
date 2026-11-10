using System;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class PatientEditProfileViewModel
    {
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [Display(Name = "First Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required.")]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "SA ID Number is required.")]
        [StringLength(13, MinimumLength = 13,
            ErrorMessage = "SA ID Number must be exactly 13 digits.")]
        [RegularExpression(@"^\d{13}$",
            ErrorMessage = "SA ID Number must contain only numbers.")]
        [Display(Name = "SA ID Number")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cell number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Cell number must contain exactly 10 digits.")]
        [Display(Name = "Cell Number")]
        public string? CellNumber { get; set; }

        [Required(ErrorMessage = "Street is required.")]
        public string Street { get; set; } = string.Empty;

        [Display(Name = "Street address line 2")]
        public string? Street2 { get; set; }

        [Required(ErrorMessage = "Suburb is required.")]
        public string Suburb { get; set; } = string.Empty;

        [Required(ErrorMessage = "Town is required.")]
        public string Town { get; set; } = string.Empty;

        [Required(ErrorMessage = "Province is required.")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Postal code must be 4 digits.")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;
    }
}