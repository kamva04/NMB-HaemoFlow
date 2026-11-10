using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels;

public class TechnicianAssignmentsViewModel
{
    public int TechnicianId { get; set; }
    public bool IsArchived { get; set; }
    public string? TechnicianName { get; set; }
    public string? EmployeeNumber { get; set; }
    public List<int> SelectedTestTypeIds { get; set; } = new();
    public List<TestType> TestTypes { get; set; } = new();
}
