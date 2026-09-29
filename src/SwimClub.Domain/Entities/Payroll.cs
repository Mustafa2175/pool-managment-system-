namespace SwimClub.Domain.Entities;

/// <summary>
/// Payroll record for an employee for a specific month.
/// Status: NOT_PAID | PAID. Once PAID, permanently locked (Decision 23).
/// Corrections are new PAYROLL_ADJUSTMENT Transactions, never edits to this row.
/// </summary>
public class Payroll
{
    public int PayrollId { get; set; }
    public int EmployeeId { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public decimal CalculatedAmount { get; set; }

    /// <summary>Qualification rate snapshot used for Coach/Lifeguard calculation.</summary>
    public decimal? QualificationRateSnapshot { get; set; }

    /// <summary>NOT_PAID | PAID</summary>
    public string Status { get; set; } = "NOT_PAID";
    public DateTime? PaymentDate { get; set; }
    public int? PaidByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public User? PaidByUser { get; set; }
    public ICollection<CoachDue> ConsumedCoachDues { get; set; } = [];
    public Transaction? PayrollPaymentTransaction { get; set; }
}
