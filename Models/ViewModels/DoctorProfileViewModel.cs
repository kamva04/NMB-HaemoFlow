using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class DoctorProfileViewModel
    {
        public int DoctorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string HPCSANumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? ContactNumber { get; set; }
    }

    public class DoctorChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and include an uppercase letter, a number, and a special character.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
