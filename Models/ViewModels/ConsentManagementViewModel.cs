using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels
{
    public class ConsentManagementViewModel
    {
        public int PatientId { get; set; }

        public List<DoctorPatientConsent> ActiveConsents { get; set; }
            = new List<DoctorPatientConsent>();

        public List<Doctor> AllDoctors { get; set; }
            = new List<Doctor>();

        public List<TestRequest> TestRequests { get; set; }
            = new List<TestRequest>();
        public List<TestRequest> PatientRequests { get; set; } = new();

        public List<RequestTestType> TestTypes { get; set; } = new();

        public List<RequestSample> Samples { get; set; } = new();
    }
}