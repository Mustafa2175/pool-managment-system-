namespace SwimClub.Domain.Entities;

/// <summary>
/// Refund record. Once confirmed, permanently locked (Decision 24).
/// Corrections are new REFUND_ADJUSTMENT Transactions.
/// </summary>
public class Refund
{
    public int RefundId { get; set; }
    public string RelatedEntityType { get; set; } = null!;
    public int RelatedEntityId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public User RecordedByUser { get; set; } = null!;
    public Transaction? LinkedTransaction { get; set; }
}
