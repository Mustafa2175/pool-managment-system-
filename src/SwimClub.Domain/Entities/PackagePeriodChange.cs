namespace SwimClub.Domain.Entities;

/// <summary>
/// Period change for a Package. Remaining sessions carry forward; start/end dates unchanged.
/// </summary>
public class PackagePeriodChange
{
    public int ChangeId { get; set; }
    public int PackageId { get; set; }
    public int OldPeriodId { get; set; }
    public int NewPeriodId { get; set; }
    public DateOnly ChangeDate { get; set; }
    public int SessionsCarriedOver { get; set; }
    public int ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Package Package { get; set; } = null!;
    public TrainingPeriod OldPeriod { get; set; } = null!;
    public TrainingPeriod NewPeriod { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
