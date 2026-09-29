using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Payroll;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Payroll;

public class PayrollService : IPayrollService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public PayrollService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    // -----------------------------------------------------------------
    // Rate resolution: highest-ranked qualification effective on date
    // -----------------------------------------------------------------
    private async Task<decimal?> ResolveRateAsync(int employeeId, DateOnly asOfDate)
    {
        // All qualifications this employee holds
        var qualIds = await _context.EmployeeQualifications
            .Where(eq => eq.EmployeeId == employeeId)
            .Select(eq => eq.QualificationId)
            .ToListAsync();

        if (!qualIds.Any()) return null;

        // Highest-ranked qualification the employee holds
        var highestQual = await _context.Qualifications
            .Where(q => qualIds.Contains(q.QualificationId))
            .OrderByDescending(q => q.RankOrder)
            .FirstOrDefaultAsync();

        if (highestQual == null) return null;

        // Rate config effective on asOfDate
        var rateConfig = await _context.QualificationRateConfigs
            .Where(r => r.QualificationId == highestQual.QualificationId
                     && r.EffectiveFrom <= asOfDate
                     && (r.EffectiveTo == null || r.EffectiveTo >= asOfDate))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        return rateConfig?.SessionRate;
    }

    // -----------------------------------------------------------------
    // Coach / Lifeguard payroll calculation
    // -----------------------------------------------------------------
    public async Task<(PayrollResult Result, int? PayrollId)> CalculateCoachLifeguardPayrollAsync(
        int employeeId, int year, int month, int actorUserId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return (PayrollResult.EmployeeNotFound, null);
        if (employee.EmployeeType != "COACH" && employee.EmployeeType != "LIFEGUARD")
            return (PayrollResult.EmployeeNotFound, null);

        // If an existing PAID payroll row exists → cannot recalculate
        var existingPaid = await _context.Payrolls
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId
                                   && p.PeriodYear == year
                                   && p.PeriodMonth == month
                                   && p.Status == "PAID");
        if (existingPaid != null) return (PayrollResult.AlreadyPaid, existingPaid.PayrollId);

        // Remove any existing NOT_PAID row (recalculation allowed)
        var existingNotPaid = await _context.Payrolls
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId
                                   && p.PeriodYear == year
                                   && p.PeriodMonth == month
                                   && p.Status == "NOT_PAID");
        if (existingNotPaid != null)
        {
            // Un-claim any coach_dues that were consumed by the old calculation
            var oldDues = await _context.CoachDues
                .Where(cd => cd.ConsumedInPayrollId == existingNotPaid.PayrollId)
                .ToListAsync();
            foreach (var d in oldDues) d.ConsumedInPayrollId = null;

            _context.Payrolls.Remove(existingNotPaid);
            await _context.SaveChangesAsync();
        }

        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
        var periodStartDate = DateOnly.FromDateTime(periodStart);
        var periodEndDate = DateOnly.FromDateTime(periodEnd);

        // ---- Training attendance dues ----
        // Sessions in this month where this employee is marked PRESENT
        var presentAttendances = await _context.EmployeeAttendances
            .Include(ea => ea.Session)
            .Where(ea => ea.EmployeeId == employeeId
                      && ea.Status == "PRESENT"
                      && ea.Session.ScheduledStartTime >= periodStart
                      && ea.Session.ScheduledStartTime <= periodEnd)
            .ToListAsync();

        decimal trainingDues = 0;
        decimal? snapshotRate = null;

        foreach (var att in presentAttendances)
        {
            var sessionDate = DateOnly.FromDateTime(att.Session.ScheduledStartTime);
            var rate = await ResolveRateAsync(employeeId, sessionDate);
            if (rate.HasValue)
            {
                trainingDues += rate.Value;
                snapshotRate ??= rate.Value; // snapshot the first resolved rate
            }
        }

        // ---- Replacement dues ----
        // Sessions this month where this employee was the REPLACEMENT
        var replacements = await _context.EmployeeReplacements
            .Include(er => er.Session)
            .Where(er => er.ReplacingEmployeeId == employeeId
                      && er.Session.ScheduledStartTime >= periodStart
                      && er.Session.ScheduledStartTime <= periodEnd)
            .ToListAsync();

        decimal replacementDues = replacements.Sum(r => r.RateAppliedSnapshot);

        // ---- Coach dues (Private revenue share + cancellation fees) ----
        var unconsumedDues = await _context.CoachDues
            .Where(cd => cd.EmployeeId == employeeId
                      && cd.PeriodYear == year
                      && cd.PeriodMonth == month
                      && cd.ConsumedInPayrollId == null)
            .ToListAsync();

        decimal privateDues = unconsumedDues.Sum(cd => cd.Amount);

        decimal calculatedAmount = trainingDues + replacementDues + privateDues;

        await using var tx = await _context.Database.BeginTransactionAsync();

        var payroll = new Domain.Entities.Payroll
        {
            EmployeeId = employeeId,
            PeriodYear = year,
            PeriodMonth = month,
            CalculatedAmount = calculatedAmount,
            QualificationRateSnapshot = snapshotRate,
            Status = "NOT_PAID",
            CreatedAt = DateTime.UtcNow
        };
        _context.Payrolls.Add(payroll);
        await _context.SaveChangesAsync();

        // Mark dues as consumed
        foreach (var due in unconsumedDues)
            due.ConsumedInPayrollId = payroll.PayrollId;

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("PAYROLL_CALCULATED", "Payroll", payroll.PayrollId,
            $"Employee={employeeId}, Period={year}/{month}, Training={trainingDues}, Replacement={replacementDues}, Private={privateDues}, Total={calculatedAmount}");

        await tx.CommitAsync();
        return (PayrollResult.Success, payroll.PayrollId);
    }

    // -----------------------------------------------------------------
    // Administrator payroll calculation
    // -----------------------------------------------------------------
    public async Task<(PayrollResult Result, int? PayrollId)> CalculateAdministratorPayrollAsync(
        int employeeId, int year, int month, int actorUserId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || employee.EmployeeType != "ADMINISTRATOR")
            return (PayrollResult.EmployeeNotFound, null);
        if (!employee.MonthlySalary.HasValue)
            return (PayrollResult.EmployeeNotFound, null);

        var existingPaid = await _context.Payrolls
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId
                                   && p.PeriodYear == year
                                   && p.PeriodMonth == month
                                   && p.Status == "PAID");
        if (existingPaid != null) return (PayrollResult.AlreadyPaid, existingPaid.PayrollId);

        var existingNotPaid = await _context.Payrolls
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId
                                   && p.PeriodYear == year
                                   && p.PeriodMonth == month
                                   && p.Status == "NOT_PAID");
        if (existingNotPaid != null)
        {
            _context.Payrolls.Remove(existingNotPaid);
            await _context.SaveChangesAsync();
        }

        // Count days in the month
        var daysInMonth = DateTime.DaysInMonth(year, month);
        decimal dailyValue = employee.MonthlySalary.Value / daysInMonth;

        // Count absent days in the month
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = new DateOnly(year, month, daysInMonth);

        var absentDays = await _context.AdministratorDailyAttendances
            .CountAsync(a => a.EmployeeId == employeeId
                          && a.AttendanceDate >= periodStart
                          && a.AttendanceDate <= periodEnd
                          && a.Status == "ABSENT");

        decimal absenceDeduction = absentDays * dailyValue;
        decimal netSalary = employee.MonthlySalary.Value - absenceDeduction;

        var payroll = new Domain.Entities.Payroll
        {
            EmployeeId = employeeId,
            PeriodYear = year,
            PeriodMonth = month,
            CalculatedAmount = netSalary,
            QualificationRateSnapshot = null, // Not applicable for Administrators
            Status = "NOT_PAID",
            CreatedAt = DateTime.UtcNow
        };
        _context.Payrolls.Add(payroll);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("PAYROLL_CALCULATED", "Payroll", payroll.PayrollId,
            $"Employee={employeeId} (ADMIN), Period={year}/{month}, MonthlySalary={employee.MonthlySalary}, DaysInMonth={daysInMonth}, AbsentDays={absentDays}, Net={netSalary}");

        return (PayrollResult.Success, payroll.PayrollId);
    }

    // -----------------------------------------------------------------
    // Mark Paid — creates PAYROLL_PAYMENT transaction, locks payroll
    // -----------------------------------------------------------------
    public async Task<PayrollResult> MarkPaidAsync(int payrollId, int actorUserId)
    {
        var payroll = await _context.Payrolls.FindAsync(payrollId);
        if (payroll == null) return PayrollResult.PayrollNotFound;
        if (payroll.Status == "PAID") return PayrollResult.AlreadyPaid;

        await using var tx = await _context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        payroll.Status = "PAID";
        payroll.PaymentDate = now;
        payroll.PaidByUserId = actorUserId;

        var transaction = new Transaction
        {
            TransactionType = "PAYROLL_PAYMENT",
            Amount = payroll.CalculatedAmount,
            RelatedEntityType = "PAYROLL",
            RelatedEntityId = payroll.PayrollId,
            RecordedBy = actorUserId,
            CreatedAt = now
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("PAYROLL_MARKED_PAID", "Payroll", payrollId,
            $"Employee={payroll.EmployeeId}, Amount={payroll.CalculatedAmount}");

        await tx.CommitAsync();
        return PayrollResult.Success;
    }

    // -----------------------------------------------------------------
    // Mark Unpaid — only allowed if the payroll IS paid and business allows
    // Creates a negative PAYROLL_ADJUSTMENT
    // -----------------------------------------------------------------
    public async Task<PayrollResult> MarkUnpaidAsync(int payrollId, string reason, int actorUserId)
    {
        var payroll = await _context.Payrolls.FindAsync(payrollId);
        if (payroll == null) return PayrollResult.PayrollNotFound;
        if (payroll.Status != "PAID") return PayrollResult.NotPaid;

        // Per D23: paid payroll row is PERMANENTLY LOCKED — we do NOT flip status.
        // Instead, we issue a reversal PAYROLL_ADJUSTMENT transaction for the full amount (negative).
        var now = DateTime.UtcNow;
        var transaction = new Transaction
        {
            TransactionType = "PAYROLL_ADJUSTMENT",
            Amount = -payroll.CalculatedAmount,
            RelatedEntityType = "PAYROLL",
            RelatedEntityId = payrollId,
            Notes = $"MARK_UNPAID: {reason}",
            RecordedBy = actorUserId,
            CreatedAt = now
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("PAYROLL_MARKED_UNPAID", "Payroll", payrollId,
            $"Adjustment={-payroll.CalculatedAmount}, Reason={reason}");

        return PayrollResult.Success;
    }

    // -----------------------------------------------------------------
    // Adjust PAID payroll — signed PAYROLL_ADJUSTMENT transaction
    // -----------------------------------------------------------------
    public async Task<PayrollResult> AdjustPaidPayrollAsync(int payrollId, decimal adjustmentAmount, string reason, int actorUserId)
    {
        if (adjustmentAmount == 0) return PayrollResult.InvalidAmount;

        var payroll = await _context.Payrolls.FindAsync(payrollId);
        if (payroll == null) return PayrollResult.PayrollNotFound;
        if (payroll.Status != "PAID") return PayrollResult.NotPaid;

        var now = DateTime.UtcNow;
        var transaction = new Transaction
        {
            TransactionType = "PAYROLL_ADJUSTMENT",
            Amount = adjustmentAmount,
            RelatedEntityType = "PAYROLL",
            RelatedEntityId = payrollId,
            Notes = reason,
            RecordedBy = actorUserId,
            CreatedAt = now
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("PAYROLL_ADJUSTED", "Payroll", payrollId,
            $"AdjustmentAmount={adjustmentAmount}, Reason={reason}");

        return PayrollResult.Success;
    }
}
