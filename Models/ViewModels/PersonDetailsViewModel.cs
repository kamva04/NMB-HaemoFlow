namespace NMB_HLabSys.Models.ViewModels;

public class PersonDetailsViewModel
{
    // Common
    public string PersonType { get; set; } = string.Empty; // "Patient" | "Doctor" | "Technician"
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }

    // Patient-specific
    public string? SouthAfricanId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public List<string> MedicalConditions { get; set; } = new();
    public List<string> Allergies { get; set; } = new();
    public List<string> Medications { get; set; } = new();

    // Doctor-specific
    public string? HpcsaNumber { get; set; }

    // Technician-specific
    public string? EmployeeNumber { get; set; }
    public List<string> AssignedTestTypes { get; set; } = new();
}
public class PersonDetailsButtonModel
{
    public string PersonType { get; set; } = "Patient";
    public int Id { get; set; }
}