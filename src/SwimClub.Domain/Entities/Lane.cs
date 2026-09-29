namespace SwimClub.Domain.Entities;

/// <summary>
/// Lane — first-class managed entity (Decisions 30 &amp; 35).
/// Managed exclusively by Super Admin. Lane Rental uses lane capacity.
/// </summary>
public class Lane
{
    public int LaneId { get; set; }

    /// <summary>Unique label (e.g., "Lane 1").</summary>
    public string Label { get; set; } = null!;

    /// <summary>Maximum swimmers for a Lane Rental on this lane.</summary>
    public int Capacity { get; set; }

    /// <summary>ACTIVE | INACTIVE</summary>
    public string Status { get; set; } = "ACTIVE";

    public ICollection<PrivateBooking> PrivateBookings { get; set; } = [];
}
