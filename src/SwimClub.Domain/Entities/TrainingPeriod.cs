namespace SwimClub.Domain.Entities;

/// <summary>Training Period — a specific scheduled instance of a Program.</summary>
public class TrainingPeriod
{
    public int PeriodId { get; set; }
    public int ProgramId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Capacity { get; set; }

    /// <summary>ACTIVE | INACTIVE</summary>
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }

    public Program Program { get; set; } = null!;
    public ICollection<TrainingPeriodSchedule> Schedules { get; set; } = [];
    public ICollection<PeriodStaffAssignment> StaffAssignments { get; set; } = [];
    public ICollection<TrainingSubscription> TrainingSubscriptions { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
}
