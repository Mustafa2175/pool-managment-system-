namespace SwimClub.Domain.Entities;

/// <summary>
/// Swimmer — the primary customer entity.
/// status is ACTIVE iff at least one linked TrainingSubscription or Package is ACTIVE/PAUSED.
/// Private Bookings never contribute to ACTIVE status (Decision 20).
/// </summary>
public class Swimmer
{
    /// <summary>SW-000125 format.</summary>
    public string SwimmerId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }

    /// <summary>MALE | FEMALE</summary>
    public string Gender { get; set; } = null!;
    public string? ParentName { get; set; }
    public string? Phone { get; set; }

    /// <summary>MEMBER | NON_MEMBER</summary>
    public string MemberStatus { get; set; } = null!;
    public string QrToken { get; set; } = null!;

    /// <summary>ACTIVE | INACTIVE — derived from active Training/Package records.</summary>
    public string Status { get; set; } = "INACTIVE";
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<TrainingSubscription> TrainingSubscriptions { get; set; } = [];
    public ICollection<Package> Packages { get; set; } = [];
    public ICollection<PrivateBookingParticipant> PrivateBookingParticipants { get; set; } = [];
    public ICollection<Credit> Credits { get; set; } = [];
}
