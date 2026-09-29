namespace SwimClub.Domain.Entities;

/// <summary>
/// Training Subscription.
/// Status: ACTIVE | PAUSED | COMPLETED | CANCELLED (NEW/RENEWED removed — Decision 18 &amp; 28).
/// A subscription is ACTIVE immediately on creation.
/// balance_due = total_price - paid_amount (always computed).
/// credit_granted_amount and outstanding_declared_amount are mutually exclusive.
/// </summary>
public class TrainingSubscription
{
    public int SubscriptionId { get; set; }
    public string SwimmerId { get; set; } = null!;
    public int ProgramId { get; set; }
    public int PeriodId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Flat total price snapshot (Decision 3).</summary>
    public decimal UnitPriceSnapshot { get; set; }
    public int ConfiguredSessionCountSnapshot { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal PaidAmount { get; set; } = 0;

    // balance_due = TotalPrice - PaidAmount — computed column in DB, not mapped as write-able.
    public decimal BalanceDue { get; private set; }

    /// <summary>Manual credit grant at creation (Decision 1). Mutually exclusive with outstanding_declared_amount.</summary>
    public decimal? CreditGrantedAmount { get; set; }
    public string? CreditDescription { get; set; }

    /// <summary>Manual outstanding annotation (Decision 1). NOT a replacement for balance_due.</summary>
    public decimal? OutstandingDeclaredAmount { get; set; }
    public string? OutstandingDescription { get; set; }

    public string Status { get; set; } = "ACTIVE";

    /// <summary>Links renewal to predecessor (Decision 18). No forced status change on predecessor.</summary>
    public int? RenewedFromSubscriptionId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Swimmer Swimmer { get; set; } = null!;
    public Program Program { get; set; } = null!;
    public TrainingPeriod Period { get; set; } = null!;
    public TrainingSubscription? RenewedFromSubscription { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<TrainingSubscriptionPause> Pauses { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public Credit? GrantedCredit { get; set; }
}
