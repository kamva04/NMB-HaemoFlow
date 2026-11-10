using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class TestCategory
    {
        [Key]
        public int CategoryId { get; set; }
        [Required]
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
