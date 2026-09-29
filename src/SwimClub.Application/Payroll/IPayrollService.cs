using System.Threading.Tasks;

namespace SwimClub.Application.Payroll;

public enum PayrollResult
{
    Success,
    EmployeeNotFound,
    AlreadyExists,
    PayrollNotFound,
    AlreadyPaid,
    NotPaid,
    InvalidAmount,
    InsufficientPermission
}

public interface IPayrollService
{
    /// <summary>
    /// Calculates (or recalculates if NOT_PAID) monthly payroll for a Coach or Lifeguard.
    /// training_dues + replacement_dues + coach_dues (consuming unclaimed coach_due rows).
    /// Rate snapshotted at calculation time; historical payrolls are NEVER retroactively changed.
    /// </summary>
    Task<(PayrollResult Result, int? PayrollId)> CalculateCoachLifeguardPayrollAsync(
        int employeeId, int year, int month, int actorUserId);

    /// <summary>
    /// Calculates (or recalculates if NOT_PAID) monthly payroll for an Administrator.
    /// net = monthly_salary - (absent_days × daily_value).
    /// </summary>
    Task<(PayrollResult Result, int? PayrollId)> CalculateAdministratorPayrollAsync(
        int employeeId, int year, int month, int actorUserId);

    /// <summary>
    /// Marks a NOT_PAID payroll as PAID.
    /// Creates a PAYROLL_PAYMENT Transaction and locks the payroll row.
    /// </summary>
    Task<PayrollResult> MarkPaidAsync(int payrollId, int actorUserId);

    /// <summary>
    /// Marks a PAID payroll back to NOT_PAID if business allows (reversal).
    /// Creates a PAYROLL_ADJUSTMENT transaction with negative amount.
    /// Audited.
    /// </summary>
    Task<PayrollResult> MarkUnpaidAsync(int payrollId, string reason, int actorUserId);

    /// <summary>
    /// Creates a signed PAYROLL_ADJUSTMENT Transaction against a PAID payroll.
    /// The original payroll row is permanently locked; this creates a new transaction.
    /// </summary>
    Task<PayrollResult> AdjustPaidPayrollAsync(int payrollId, decimal adjustmentAmount, string reason, int actorUserId);
}
