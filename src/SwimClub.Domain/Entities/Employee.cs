namespace SwimClub.Domain.Entities;

/// <summary>
/// Coach, Lifeguard, or Administrator staff record.
/// Owner/Super Admin are never Employees (Decision 12).
/// </summary>
public class Employee
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>ADMINISTRATOR | COACH | LIFEGUARD</summary>
    public string EmployeeType { get; set; } = null!;
    public string NationalId { get; set; } = null!;
    public string? Phone { get; set; }

    /// <summary>Populated only on username collision (Decision 13).</summary>
    public string? Nickname { get; set; }

    /// <summary>Required for ADMINISTRATOR; null for COACH/LIFEGUARD.</summary>
    public decimal? MonthlySalary { get; set; }

    /// <summary>ACTIVE | INACTIVE</summary>
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    public User? User { get; set; }
    public ICollection<EmployeeQualification> EmployeeQualifications { get; set; } = [];
    public ICollection<PeriodStaffAssignment> PeriodStaffAssignments { get; set; } = [];
    public ICollection<EmployeeAttendance> EmployeeAttendances { get; set; } = [];
    public ICollection<EmployeeReplacement> ReplacingAs { get; set; } = [];
    public ICollection<EmployeeReplacement> ReplacedBy { get; set; } = [];
    public ICollection<CoachDue> CoachDues { get; set; } = [];
    public ICollection<Payroll> Payrolls { get; set; } = [];
    public ICollection<AdministratorDailyAttendance> AdministratorDailyAttendances { get; set; } = [];
}
