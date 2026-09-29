namespace SwimClub.Domain.Entities;

/// <summary>
/// Ranked catalogue of Coach/Lifeguard qualification tiers (Decision 6).
/// Managed exclusively by Super Admin.
/// </summary>
public class Qualification
{
    public int QualificationId { get; set; }
    public string NameEn { get; set; } = null!;
    public string NameAr { get; set; } = null!;

    /// <summary>Higher number = higher qualification. Must be unique.</summary>
    public int RankOrder { get; set; }

    public ICollection<EmployeeQualification> EmployeeQualifications { get; set; } = [];
    public ICollection<QualificationRateConfig> QualificationRateConfigs { get; set; } = [];
}
