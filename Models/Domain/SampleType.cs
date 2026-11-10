using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class SampleType
    {
        [Key] 
        public int SampleTypeId { get; set; }
        [Required]
        public string TypeName { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}
