using NMB_HLabSys.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class TechnicianCreateViewModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Surname { get; set; } = string.Empty;

        [Required, StringLength(13, MinimumLength = 13)]
        [Display(Name = "SA ID Number")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required, Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email (Username)")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [Display(Name = "Assigned Test Types")]
        public List<int> SelectedTestTypeIds { get; set; } = new();
        public List<TestType> AvailableTestTypes { get; set; } = new();
    }
}
