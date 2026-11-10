using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NMB_HLabSys.Models.ViewModels;

public class TechnicianProfileViewModel
{
    public int TechnicianId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string SouthAfricanId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePicturePath { get; set; }

    [StringLength(20)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? ProfilePictureFile { get; set; }
}

public class TechnicianChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}