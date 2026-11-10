using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain
{
	public class Doctor
	{
        [Key]
        public int DoctorId { get; set; }
		public int UserId { get; set; }
        public bool IsDeleted { get; set; }

		[Required]
		public string Name { get; set; } = string.Empty;

		[Required]
		public string Surname { get; set; } = string.Empty;

		[Required]
		public string HPCSANumber { get; set; } = string.Empty;

		public string? ContactNumber { get; set; }

		public User? User { get; set; }

		// Navigation properties
		public ICollection<TestRequest> TestRequests { get; set; } = new List<TestRequest>();

		public ICollection<DoctorPatientConsent> DoctorPatientConsents { get; set; } = new List<DoctorPatientConsent>();
	}
}
