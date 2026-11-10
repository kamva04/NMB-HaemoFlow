using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        [Display(Name = "Your registered email address")]
        public string Email { get; set; } = string.Empty;
    }
}
