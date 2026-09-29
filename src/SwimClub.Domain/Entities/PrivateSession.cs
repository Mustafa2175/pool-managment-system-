namespace SwimClub.Domain.Entities;

/// <summary>
/// A single scheduled session for a Private Booking.
/// Generated at booking creation using configured session count and start date.
/// </summary>
public class PrivateSession
{
    public int PrivateSessionId { get; set; }
    public int PrivateBookingId { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>SCHEDULED | COMPLETED | CANCELLED</summary>
    public string Status { get; set; } = "SCHEDULED";
    public DateTime CreatedAt { get; set; }

    public PrivateBooking PrivateBooking { get; set; } = null!;
    public ICollection<PrivateSessionAttendance> Attendances { get; set; } = [];
}
