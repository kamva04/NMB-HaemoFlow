using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels;

public class ManagerProfileViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Surname")]
    public string Surname { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Email / username")]
    public string Email { get; set; } = string.Empty;
}
