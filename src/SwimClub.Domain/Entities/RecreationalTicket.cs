namespace SwimClub.Domain.Entities;

/// <summary>
/// Recreational Ticket — Single Entry check-in (Decisions 10 &amp; 11).
/// No swimmer_id FK. Free-text name only. Full payment always required.
/// </summary>
public class RecreationalTicket
{
    public int TicketId { get; set; }
    public int RecreationalPeriodId { get; set; }

    /// <summary>Free text — NOT a swimmer_id FK.</summary>
    public string Name { get; set; } = null!;

    /// <summary>MEMBER | NON_MEMBER</summary>
    public string MemberStatus { get; set; } = null!;
    public DateTime CheckedInAt { get; set; }

    /// <summary>Always the full configured fee. CHECK > 0.</summary>
    public decimal AmountPaid { get; set; }
    public string? PaymentDescription { get; set; }
    public int RecordedBy { get; set; }

    public RecreationalPeriod RecreationalPeriod { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
