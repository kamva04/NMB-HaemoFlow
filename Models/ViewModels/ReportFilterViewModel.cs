using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.ViewModels
{
    public class ReportFilterViewModel
    {
        [Required, DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime FromDate { get; set; } = DateTime.Today.AddMonths(-1);

        [Required, DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; } = DateTime.Today;
    }
}
