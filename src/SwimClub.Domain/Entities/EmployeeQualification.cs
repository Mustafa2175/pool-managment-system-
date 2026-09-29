namespace SwimClub.Domain.Entities;

/// <summary>
/// Many-to-many: an employee may hold multiple qualifications (Decision 6).
/// </summary>
public class EmployeeQualification
{
    public int EmployeeId { get; set; }
    public int QualificationId { get; set; }
    public DateOnly ObtainedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public Qualification Qualification { get; set; } = null!;
}
