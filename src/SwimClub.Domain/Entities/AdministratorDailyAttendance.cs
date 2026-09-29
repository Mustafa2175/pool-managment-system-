namespace SwimClub.Domain.Entities;

/// <summary>
/// Monthly attendance record for Administrator employees (separate from session-based attendance).
/// </summary>
public class AdministratorDailyAttendance
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }

    /// <summary>PRESENT | ABSENT</summary>
    public string Status { get; set; } = null!;
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
