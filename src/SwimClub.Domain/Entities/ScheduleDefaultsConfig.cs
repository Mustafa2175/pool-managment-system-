namespace SwimClub.Domain.Entities;

/// <summary>Schedule defaults configuration — pre-fill defaults for Period-creation form (Decision 33).</summary>
public class ScheduleDefaultsConfig
{
    public int ConfigId { get; set; }
    public TimeOnly DefaultStartTime { get; set; }
    public TimeOnly DefaultEndTime { get; set; }
    public int DefaultCapacity { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UpdatedBy { get; set; }

    public User UpdatedByUser { get; set; } = null!;
}
