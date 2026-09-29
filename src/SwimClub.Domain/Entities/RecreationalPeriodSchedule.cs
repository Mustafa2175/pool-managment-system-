namespace SwimClub.Domain.Entities;

/// <summary>Days of the week a Recreational Period runs.</summary>
public class RecreationalPeriodSchedule
{
    public int ScheduleId { get; set; }
    public int RecreationalPeriodId { get; set; }

    /// <summary>0=Sunday, 1=Monday, ... 6=Saturday</summary>
    public int DayOfWeek { get; set; }

    public RecreationalPeriod RecreationalPeriod { get; set; } = null!;
}
