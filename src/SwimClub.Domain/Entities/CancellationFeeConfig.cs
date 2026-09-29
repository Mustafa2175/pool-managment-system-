namespace SwimClub.Domain.Entities;

/// <summary>Cancellation fee configuration (percentage of paid amount).</summary>
public class CancellationFeeConfig
{
    public int ConfigId { get; set; }
    public decimal FeePercentage { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
}
