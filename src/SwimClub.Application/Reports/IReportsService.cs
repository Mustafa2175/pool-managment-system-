using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SwimClub.Application.Reports;

// ==========================================================
// DTOs
// ==========================================================

public class FinancialReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<TransactionSummaryDto> RevenueByCategory { get; set; } = [];
    public List<TransactionSummaryDto> ExpensesByCategory { get; set; } = [];
}

public class TransactionSummaryDto
{
    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
}

public class DashboardSummaryDto
{
    // High-level cards
    public decimal PeriodRevenue { get; set; }
    public decimal PeriodProfit { get; set; }
    public int ActiveSwimmers { get; set; }
    public int ActiveSubscriptions { get; set; }

    // Recent activity
    public int NewSubscriptionsCount { get; set; }
    public int RecreationalTicketsCount { get; set; }
    public int CancellationsCount { get; set; }

    // Role specific flags (populated based on requesting user's role)
    public bool CanViewFinancials { get; set; }
}

public class OutstandingItemDto
{
    public string EntityType { get; set; } = null!; // SUBSCRIPTION, PACKAGE, PRIVATE
    public int EntityId { get; set; }
    public string CustomerName { get; set; } = null!;
    public decimal BalanceDue { get; set; }
    public decimal? DeclaredOutstanding { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SwimmerReportDto
{
    public int TotalSwimmers { get; set; }
    public int ActiveSwimmers { get; set; }
    public int InactiveSwimmers { get; set; }
    public int MemberSwimmers { get; set; }
    public int NonMemberSwimmers { get; set; }
}

public class RecreationalTicketReportDto
{
    public int TotalTickets { get; set; }
    public decimal TotalRevenue { get; set; }
    public int MemberTickets { get; set; }
    public int NonMemberTickets { get; set; }
}

public class CoachDuesReportDto
{
    public int EmployeeId { get; set; }
    public string CoachName { get; set; } = null!;
    public decimal UnconsumedDues { get; set; }
    public decimal ConsumedDues { get; set; }
    public List<TransactionSummaryDto> DuesBySource { get; set; } = [];
}

public class CancellationReportDto
{
    public int TotalCancellations { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal TotalCancellationFees { get; set; }
    public int TrainingCancellations { get; set; }
    public int PackageCancellations { get; set; }
    public int PrivateCancellations { get; set; }
}

public class PayrollReportDto
{
    public int TotalPayrolls { get; set; }
    public decimal TotalCalculatedAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }
}

public class AttendanceReportDto
{
    public int SwimmerPresent { get; set; }
    public int SwimmerAbsent { get; set; }
    public int CoachPresent { get; set; }
    public int CoachAbsent { get; set; }
    public int LifeguardPresent { get; set; }
    public int LifeguardAbsent { get; set; }
    public int PackageCheckIns { get; set; }
    public int PrivateParticipantCheckIns { get; set; }
    public int RecreationalCheckIns { get; set; }
}

// ==========================================================
// Interface
// ==========================================================
public interface IReportsService
{
    // Dashboard
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null);

    // Financial
    Task<FinancialReportDto> GetFinancialReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null);
    
    // Outstanding
    Task<List<OutstandingItemDto>> GetOutstandingReportAsync(int requestingUserId);

    // Swimmers
    Task<SwimmerReportDto> GetSwimmerReportAsync(int requestingUserId);

    // Recreational
    Task<RecreationalTicketReportDto> GetRecreationalReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null);

    // Coach Dues
    Task<List<CoachDuesReportDto>> GetCoachDuesReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null);

    // Cancellations
    Task<CancellationReportDto> GetCancellationReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null);

    // Payroll
    Task<PayrollReportDto> GetPayrollReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null);

    // Attendance
    Task<AttendanceReportDto> GetAttendanceReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null);
}
