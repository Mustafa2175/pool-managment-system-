namespace SwimClub.Domain.Entities;

/// <summary>
/// Immutable financial transaction record.
/// transaction_type enum includes: TRAINING_PAYMENT, PACKAGE_PAYMENT, PRIVATE_PAYMENT,
///   RECREATIONAL_TICKET, EXPENSE, PAYROLL_PAYMENT, PAYROLL_ADJUSTMENT, REFUND, REFUND_ADJUSTMENT.
/// The ONE mutability exception: when a Payment is corrected (Decision 22), the linked
/// Transaction row's amount is updated in the same operation. All other corrections
/// use new adjustment Transactions.
/// </summary>
public class Transaction
{
    public int TransactionId { get; set; }
    public string TransactionType { get; set; } = null!;
    public decimal Amount { get; set; }

    /// <summary>References the parent entity (Payment, Refund, Payroll, etc.).</summary>
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    public string? Notes { get; set; }
    public int RecordedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public User RecordedByUser { get; set; } = null!;
}
