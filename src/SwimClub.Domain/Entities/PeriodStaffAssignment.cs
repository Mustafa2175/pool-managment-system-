namespace SwimClub.Domain.Entities;

/// <summary>
/// Assignment of a Coach or Lifeguard to a Training Period.
/// No employee may be assigned to two time-overlapping Periods.
/// </summary>
public class PeriodStaffAssignment
{
    public int AssignmentId { get; set; }
    public int PeriodId { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>COACH | LIFEGUARD</summary>
    public string Role { get; set; } = null!;
    public DateTime AssignedAt { get; set; }

    public TrainingPeriod Period { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
