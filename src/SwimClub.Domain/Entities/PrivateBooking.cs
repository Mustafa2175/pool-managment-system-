namespace SwimClub.Domain.Entities;

/// <summary>
/// Private Booking — LANE_RENTAL, COACH_BROUGHT, or CLUB_BROUGHT.
/// Status: ACTIVE | COMPLETED | CANCELLED (NEW removed — Decision 28).
/// club_percentage_snapshot and coach_percentage_snapshot are frozen at creation (Decision 8).
/// </summary>
public class PrivateBooking
{
    public int PrivateBookingId { get; set; }

    /// <summary>LANE_RENTAL | COACH_BROUGHT | CLUB_BROUGHT</summary>
    public string BusinessType { get; set; } = null!;
    public int? CoachId { get; set; }
    public int? LaneId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    /// <summary>For COACH_BROUGHT: fixed club fee per swimmer.</summary>
    public decimal? ClubFeePerSwimmerSnapshot { get; set; }

    /// <summary>For CLUB_BROUGHT: club % (frozen at creation).</summary>
    public decimal? ClubPercentageSnapshot { get; set; }

    /// <summary>For CLUB_BROUGHT: coach % (frozen at creation).</summary>
    public decimal? CoachPercentageSnapshot { get; set; }

    public decimal TotalPrice { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal BalanceDue { get; private set; }

    public string Status { get; set; } = "ACTIVE";
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employee? Coach { get; set; }
    public Lane? Lane { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<PrivateBookingParticipant> Participants { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<CoachDue> CoachDues { get; set; } = [];
    public ICollection<PrivateSession> Sessions { get; set; } = [];
}
