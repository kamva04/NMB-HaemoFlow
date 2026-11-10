using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models.Domain;

public class ConsumableRequest
{
    [Key]
    public int RequestId { get; set; }

    [Required]
    public string RequestNumber { get; set; } = string.Empty; // e.g. SUP-2026-1001

    public int ConsumableId { get; set; }
    public int TechnicianId { get; set; }

    public int QuantityRequested { get; set; }
    public int? QuantityApproved { get; set; }
    public int? QuantityFulfilled { get; set; }

    [Required]
    public string Urgency { get; set; } = "Routine"; // Routine, Urgent, Critical

    [Required]
    public string Reason { get; set; } = string.Empty;

    public string? RelatedRequestNumber { get; set; } // Optional: link to a TestRequest

    [Required]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Fulfilled, PartiallyApproved, PartiallyFulfilled

    public string? ManagerDecisionNote { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.Now;
    public DateTime? DecisionAt { get; set; }

    // Navigation
    [ForeignKey("ConsumableId")]
    public Consumable? Consumable { get; set; }

    [ForeignKey("TechnicianId")]
    public Technician? Technician { get; set; }
}