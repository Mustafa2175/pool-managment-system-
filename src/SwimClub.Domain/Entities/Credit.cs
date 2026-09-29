namespace SwimClub.Domain.Entities;

/// <summary>
/// Credit for a Swimmer — sole generation path is manual credit_granted_amount at
/// TrainingSubscription or Package creation (Decision 1).
/// Partial usage is allowed; remaining_amount tracks balance.
/// Usable only for Training/Package — never Private or Recreational.
/// Revenue recognized on date of use, not grant.
/// </summary>
public class Credit
{
    public int CreditId { get; set; }
    public string SwimmerId { get; set; } = null!;

    /// <summary>TRAINING_SUBSCRIPTION | PACKAGE</summary>
    public string GeneratedFromType { get; set; } = null!;
    public int GeneratedFromId { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public Swimmer Swimmer { get; set; } = null!;
    public ICollection<CreditUsage> Usages { get; set; } = [];
}
