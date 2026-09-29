namespace SwimClub.Domain.Entities;

/// <summary>Recreational Period (e.g., a morning swim session).</summary>
public class RecreationalPeriod
{
    public int RecreationalPeriodId { get; set; }
    public string Name { get; set; } = null!;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Capacity { get; set; }

    /// <summary>ACTIVE | INACTIVE</summary>
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }

    public ICollection<RecreationalPeriodSchedule> Schedules { get; set; } = [];
    public ICollection<RecreationalTicket> Tickets { get; set; } = [];
    public ICollection<Package> Packages { get; set; } = [];
    public ICollection<PackageCheckIn> PackageCheckIns { get; set; } = [];
}
