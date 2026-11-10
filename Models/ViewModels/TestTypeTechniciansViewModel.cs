using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels;

public class TestTypeTechniciansViewModel
{
    public int TestTypeId { get; set; }
    public string? TestName { get; set; }
    public bool IsActive { get; set; }
    public List<int> SelectedTechnicianIds { get; set; } = new();
    public List<Technician> Technicians { get; set; } = new();
}
