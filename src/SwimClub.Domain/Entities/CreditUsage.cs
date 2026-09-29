namespace SwimClub.Domain.Entities;

/// <summary>Partial or full credit usage against a Training Subscription or Package.</summary>
public class CreditUsage
{
    public int UsageId { get; set; }
    public int CreditId { get; set; }
    public string AppliedToEntityType { get; set; } = null!;
    public int AppliedToEntityId { get; set; }
    public decimal AmountUsed { get; set; }
    public DateTime UsedAt { get; set; }
    public int RecordedBy { get; set; }

    public Credit Credit { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
    public Transaction? RevenueTransaction { get; set; }
}
