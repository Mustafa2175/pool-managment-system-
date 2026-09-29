namespace SwimClub.Domain.Entities;

/// <summary>
/// Employee attendance for a Training Session.
/// Late-edit (after 1-hour post-Period deadline) is allowed and flagged (Decision 17).
/// </summary>
public class EmployeeAttendance
{
    public int AttendanceId { get; set; }
    public int SessionId { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>PRESENT | ABSENT</summary>
    public string Status { get; set; } = null!;

    /// <summary>True if edited after the 1-hour post-Period deadline (Decision 17).</summary>
    public bool IsLateEdit { get; set; } = false;
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public int? LastModifiedBy { get; set; }

    public Session Session { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
