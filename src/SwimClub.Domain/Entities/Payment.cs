namespace SwimClub.Domain.Entities;

/// <summary>
/// Payment record. payments.amount is directly correctable by an authorized user (Decision 22).
/// Correcting a payment also updates the linked Transaction amount in the same operation.
/// </summary>
public class Payment
{
    public int PaymentId { get; set; }

    /// <summary>TRAINING_SUBSCRIPTION | PACKAGE | PRIVATE_BOOKING</summary>
    public string RelatedEntityType { get; set; } = null!;
    public int RelatedEntityId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>CASH | CARD | TRANSFER | etc.</summary>
    public string PaymentMethod { get; set; } = null!;
    public string? Notes { get; set; }
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Set when payment amount is corrected (Decision 22).</summary>
    public int? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    public User RecordedByUser { get; set; } = null!;
    public User? LastModifiedByUser { get; set; }
    public Transaction? LinkedTransaction { get; set; }
}
