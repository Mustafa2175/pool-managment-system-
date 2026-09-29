namespace SwimClub.Domain.Entities;

/// <summary>
/// Package check-in. Each successful check-in decrements Package.available_sessions_remaining by 1.
/// </summary>
public class PackageCheckIn
{
    public int CheckInId { get; set; }
    public int PackageId { get; set; }
    public DateTime CheckedInAt { get; set; }

    /// <summary>QR | MANUAL</summary>
    public string AttendanceMethod { get; set; } = null!;
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Package Package { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
