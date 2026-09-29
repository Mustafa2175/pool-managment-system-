namespace SwimClub.Domain.Entities;

/// <summary>
/// Participant in a Private Booking — either a registered Swimmer or an unregistered Guest (Decision 9).
/// Exactly one of swimmer_id or guest_name is populated per row.
/// </summary>
public class PrivateBookingParticipant
{
    public int ParticipantId { get; set; }
    public int PrivateBookingId { get; set; }

    /// <summary>SWIMMER | GUEST</summary>
    public string ParticipantType { get; set; } = null!;

    /// <summary>Populated when type = SWIMMER.</summary>
    public string? SwimmerId { get; set; }

    /// <summary>Populated when type = GUEST.</summary>
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }

    public PrivateBooking PrivateBooking { get; set; } = null!;
    public Swimmer? Swimmer { get; set; }
}
