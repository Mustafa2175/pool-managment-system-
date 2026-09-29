namespace SwimClub.Domain.Entities;

/// <summary>
/// A single scheduled training session, generated from the Period schedule.
/// </summary>
public class Session
{
    public int SessionId { get; set; }
    public int PeriodId { get; set; }
    public int SubscriptionId { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>SCHEDULED | PAUSED | COMPLETED | CANCELLED</summary>
    public string Status { get; set; } = "SCHEDULED";
    public DateTime CreatedAt { get; set; }

    public TrainingPeriod Period { get; set; } = null!;
    public TrainingSubscription Subscription { get; set; } = null!;
    public Attendance? Attendance { get; set; }
    public ICollection<EmployeeAttendance> EmployeeAttendances { get; set; } = [];
    public ICollection<EmployeeReplacement> EmployeeReplacements { get; set; } = [];
}
