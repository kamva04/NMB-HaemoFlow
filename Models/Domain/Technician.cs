using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
    public class Technician
    {
        [Key]
        public int TechnicianId { get; set; }
        public int UserId { get; set; }
        public bool IsDeleted { get; set; }

        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Surname { get; set; } = string.Empty;

        [Required, StringLength(13, MinimumLength = 13)]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required] public string EmployeeNumber { get; set; } = string.Empty;
        public string? ContactNumber { get; set; }
        public string? ProfilePicturePath { get; set; }
        public User? User { get; set; }
		//public List<TestType> AssignedTestTypes { get; set; } = new();
		public List<TechnicianTestType> TechnicianTestTypes { get; set; } = new();
	}
}
