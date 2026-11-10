using System.ComponentModel.DataAnnotations;
using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels;

public class ReportStockProblemViewModel
{
    [Required]
    public int ConsumableId { get; set; }

    [Required]
    public string ProblemType { get; set; } = "Damaged"; // Damaged | Expired | Missing

    [Required, StringLength(500, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    public List<Consumable> AvailableConsumables { get; set; } = new();
}