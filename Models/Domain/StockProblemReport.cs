using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models.Domain;

public class StockProblemReport
{
    [Key]
    public int ReportId { get; set; }

    public int ConsumableId { get; set; }
    public int TechnicianId { get; set; }

    [Required]
    public string IssueType { get; set; } = string.Empty; // Damaged, Expired, Missing, IncorrectQuantity

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? Status { get; set; } = "Open"; // Open, Investigating, Resolved

    public DateTime ReportedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey("ConsumableId")]
    public Consumable? Consumable { get; set; }

    [ForeignKey("TechnicianId")]
    public Technician? Technician { get; set; }
}