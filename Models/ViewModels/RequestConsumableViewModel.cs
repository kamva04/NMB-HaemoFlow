using System.ComponentModel.DataAnnotations;
using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels
{
    public class RequestConsumableViewModel
    {
        [Required]
        public int ConsumableId { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
        public int QuantityRequested { get; set; }

        [Required]
        public string Urgency { get; set; } = "Routine"; // Routine | Urgent | Critical

        [Required]
        [StringLength(500, MinimumLength = 5)]
        public string Reason { get; set; } = string.Empty;

        // For dropdowns
        public List<Consumable> AvailableConsumables { get; set; } = new();

        // Populated after submit (confirmation)
        public string? GeneratedRequestNumber { get; set; }
    }
}