namespace SwimClub.Domain.Entities;

/// <summary>
/// Attendance record for one participant in one private session.
/// No QR path — manual recording only (Admin records based on coach's verbal report).
/// </summary>
public class PrivateSessionAttendance
{
    public int AttendanceId { get; set; }
    public int PrivateSessionId { get; set; }
    public int ParticipantId { get; set; }

    /// <summary>PRESENT | ABSENT</summary>
    public string Status { get; set; } = "PRESENT";
    public int RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; }

    public PrivateSession PrivateSession { get; set; } = null!;
    public PrivateBookingParticipant Participant { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
