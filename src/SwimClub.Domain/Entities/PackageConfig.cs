namespace SwimClub.Domain.Entities;

/// <summary>Configuration entity for Training Packages.</summary>
public class PackageConfig
{
    public int PackageConfigId { get; set; }
    public int ProgramId { get; set; }
    public int DurationMonths { get; set; }

    /// <summary>NEW: sessions per month for Package session-balance calculation (Decision 5).</summary>
    public int SessionsPerMonth { get; set; }

    /// <summary>MEMBER | NON_MEMBER</summary>
    public string MemberStatus { get; set; } = null!;
    public decimal Price { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public Program Program { get; set; } = null!;
}
