using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class Admin
    {
        [Key]
        public int AdminId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Surname { get; set; } = string.Empty;
    }
}
