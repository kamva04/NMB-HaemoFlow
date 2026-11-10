using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class TechnicianTestType
    {
        
       
        [Key]
        public int TestTypeId { get; set; }
        public int TechnicianId { get; set; }
        public Technician? Technician { get; set; }
        public TestType? TestType { get; set; }
    }
}
