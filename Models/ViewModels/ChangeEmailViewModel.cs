using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class ChangeEmailViewModel
    {
        [Required, EmailAddress]
        [Display(Name = "New Email / Username")]
        public string NewEmail { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Display(Name = "Current Password (to confirm)")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Compare("NewEmail", ErrorMessage = "The new email and confirmation email do not match.")]
        [Display(Name = "Confirm New Email")]
        public string ConfirmEmail { get; set; } = string.Empty;
    }
}