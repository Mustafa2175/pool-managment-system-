namespace SwimClub.Domain.Entities;

/// <summary>
/// Coach Due — Private income accrual for a Coach (Decisions 2, 4 &amp; 8).
/// Consumed by monthly payroll run.
/// source_type: CLUB_BROUGHT_SHARE | CANCELLATION_FEE.
/// </summary>
public class CoachDue
{
    public int CoachDueId { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>CLUB_BROUGHT_SHARE | CANCELLATION_FEE</summary>
    public string SourceType { get; set; } = null!;

    /// <summary>FK to the originating PrivateBooking.</summary>
    public int SourceId { get; set; }
    public decimal Amount { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Set once a payroll run consumes this due.</summary>
    public int? ConsumedInPayrollId { get; set; }

    public Employee Employee { get; set; } = null!;
    public PrivateBooking PrivateBooking { get; set; } = null!;
    public Payroll? ConsumedInPayroll { get; set; }
}
