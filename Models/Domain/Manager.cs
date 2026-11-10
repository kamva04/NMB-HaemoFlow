using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class Manager
    {
        [Key]
        public int ManagerId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}
