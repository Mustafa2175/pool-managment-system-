namespace SwimClub.Domain.Entities;

/// <summary>
/// Versioned pricing for a Training Program by member status.
/// effectve_from/effective_to versioning pattern.
/// </summary>
public class TrainingPriceConfig
{
    public int PriceConfigId { get; set; }
    public int ProgramId { get; set; }

    /// <summary>MEMBER | NON_MEMBER</summary>
    public string MemberStatus { get; set; } = null!;
    public decimal Price { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public Program Program { get; set; } = null!;
}
