namespace SwimClub.Domain.Entities;

/// <summary>Training Program — Regular, Star, or Team.</summary>
public class Program
{
    public int ProgramId { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>REGULAR | STAR | TEAM</summary>
    public string ProgramType { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<TrainingPeriod> TrainingPeriods { get; set; } = [];
    public ICollection<TrainingPriceConfig> TrainingPriceConfigs { get; set; } = [];
}
