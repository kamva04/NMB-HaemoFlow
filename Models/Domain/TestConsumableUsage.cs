using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models.Domain;

public class TestConsumableUsage
{
    [Key]
    public int UsageId { get; set; }

    public int RequestTestId { get; set; }      // FK → RequestTestType.RequestTestId
    public int ConsumableId { get; set; }
    public int QuantityUsed { get; set; }
    public int UsedByTechnicianId { get; set; } // Technician.TechnicianId
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    [StringLength(300)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(RequestTestId))]
    public RequestTestType? RequestTest { get; set; }

    [ForeignKey(nameof(ConsumableId))]
    public Consumable? Consumable { get; set; }
}