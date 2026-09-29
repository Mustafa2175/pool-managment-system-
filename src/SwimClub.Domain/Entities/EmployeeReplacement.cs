namespace SwimClub.Domain.Entities;

/// <summary>
/// Replacement Coach/Lifeguard for a specific Session.
/// Rate snapshot at time of replacement feeds into payroll formula.
/// </summary>
public class EmployeeReplacement
{
    public int ReplacementId { get; set; }
    public int SessionId { get; set; }
    public int OriginalEmployeeId { get; set; }
    public int ReplacingEmployeeId { get; set; }

    /// <summary>Qualification rate snapshot at time of replacement.</summary>
    public decimal RateAppliedSnapshot { get; set; }
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Session Session { get; set; } = null!;
    public Employee OriginalEmployee { get; set; } = null!;
    public Employee ReplacingEmployee { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
