namespace SwimClub.Domain.Entities;

/// <summary>Days of the week a Training Period runs.</summary>
public class TrainingPeriodSchedule
{
    public int ScheduleId { get; set; }
    public int PeriodId { get; set; }

    /// <summary>0=Sunday, 1=Monday, ... 6=Saturday</summary>
    public int DayOfWeek { get; set; }

    public TrainingPeriod Period { get; set; } = null!;
}
