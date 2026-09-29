namespace SwimClub.Domain.Entities;

/// <summary>
/// Package — TRAINING or RECREATIONAL.
/// Sessions are a finite consumable balance on top of the calendar-duration model (Decision 5).
/// Status: ACTIVE | EXPIRED | CANCELLED (NEW removed — Decision 28).
/// balance_due = total_price - paid_amount (computed column).
/// </summary>
public class Package
{
    public int PackageId { get; set; }
    public string SwimmerId { get; set; } = null!;

    /// <summary>TRAINING | RECREATIONAL</summary>
    public string PackageType { get; set; } = null!;

    /// <summary>Training only; must reference a Regular program.</summary>
    public int? ProgramId { get; set; }
    public int? TrainingPeriodId { get; set; }
    public int? RecreationalPeriodId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DurationSnapshotDays { get; set; }

    /// <summary>Frozen at creation from PackageConfig (Decision 5).</summary>
    public int SessionsPerMonthSnapshot { get; set; }

    /// <summary>duration_months × sessions_per_month_snapshot, frozen at creation.</summary>
    public int AvailableSessionsTotal { get; set; }

    /// <summary>Decremented per check-in. CHECK >= 0.</summary>
    public int AvailableSessionsRemaining { get; set; }

    /// <summary>Regular Training price at creation time (Decision 19), for cancellation refund formula.</summary>
    public decimal ReferenceTrainingPriceSnapshot { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal PaidAmount { get; set; } = 0;

    // balance_due — computed column in DB, not write-able from application.
    public decimal BalanceDue { get; private set; }

    public decimal? CreditGrantedAmount { get; set; }
    public string? CreditDescription { get; set; }
    public decimal? OutstandingDeclaredAmount { get; set; }
    public string? OutstandingDescription { get; set; }
    public string? QrToken { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int? RenewedFromPackageId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Swimmer Swimmer { get; set; } = null!;
    public Program? Program { get; set; }
    public TrainingPeriod? TrainingPeriod { get; set; }
    public RecreationalPeriod? RecreationalPeriod { get; set; }
    public Package? RenewedFromPackage { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<PackageCheckIn> CheckIns { get; set; } = [];
    public ICollection<PackageQrHistory> QrHistory { get; set; } = [];
    public ICollection<PackagePeriodChange> PeriodChanges { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public Credit? GrantedCredit { get; set; }
}
