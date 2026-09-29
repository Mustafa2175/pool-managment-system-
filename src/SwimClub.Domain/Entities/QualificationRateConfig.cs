namespace SwimClub.Domain.Entities;

/// <summary>
/// Versioned rate keyed purely by qualification (Decision 6).
/// Rate is resolved at payroll calculation time and snapshotted onto the payroll row.
/// </summary>
public class QualificationRateConfig
{
    public int RateConfigId { get; set; }
    public int QualificationId { get; set; }
    public decimal SessionRate { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public Qualification Qualification { get; set; } = null!;
}
