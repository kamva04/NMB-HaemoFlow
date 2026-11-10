using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class DoctorCreateViewModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Surname { get; set; } = string.Empty;

        [Required, Display(Name = "HPCSA Number")]
        public string HPCSANumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email (Username)")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }
    }
}
