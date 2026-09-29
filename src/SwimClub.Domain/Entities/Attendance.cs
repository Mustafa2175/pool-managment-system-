namespace SwimClub.Domain.Entities;

/// <summary>
/// Swimmer attendance for a Training Session.
/// Window: [session.scheduled_start_time - 30min, session.scheduled_start_time + 30min] (Decision 29).
/// </summary>
public class Attendance
{
    public int AttendanceId { get; set; }
    public int SessionId { get; set; }
    public string SwimmerId { get; set; } = null!;
    public DateTime CheckedInAt { get; set; }

    /// <summary>QR | MANUAL</summary>
    public string AttendanceMethod { get; set; } = null!;
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Session Session { get; set; } = null!;
    public Swimmer Swimmer { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
