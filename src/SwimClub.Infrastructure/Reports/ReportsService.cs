using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Reports;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Reports;

public class ReportsService : IReportsService
{
    private readonly AppDbContext _context;

    // Based on Doc11 §10.1
    private static readonly string[] RevenueTransactionTypes =
    [
        "TRAINING_PAYMENT", "PACKAGE_PAYMENT", "PRIVATE_PAYMENT",
        "RECREATIONAL_TICKET_PAYMENT", "OUTSTANDING_PAYMENT",
        "CREDIT_USAGE", "REFUND", "REFUND_ADJUSTMENT"
    ];

    private static readonly string[] ExpenseTransactionTypes =
    [
        "EXPENSE", "PAYROLL_PAYMENT", "PAYROLL_ADJUSTMENT"
    ];

    public ReportsService(AppDbContext context)
    {
        _context = context;
    }

    private async Task<bool> IsOwnerOrSuperAdminAsync(int userId)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        return user?.Role?.Code == "SUPER_ADMIN" || user?.Role?.Code == "OWNER";
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        bool canViewFinancials = await IsOwnerOrSuperAdminAsync(requestingUserId);

        var financial = canViewFinancials 
            ? await GetFinancialReportAsync(requestingUserId, fromDate, toDate)
            : new FinancialReportDto();

        var swimmersQuery = _context.Swimmers.AsQueryable();
        var subscriptionsQuery = _context.TrainingSubscriptions.AsQueryable();
        
        var newSubsQuery = _context.TrainingSubscriptions.AsQueryable();
        var newPackagesQuery = _context.Packages.AsQueryable();
        var newPrivatesQuery = _context.PrivateBookings.AsQueryable();
        
        var ticketsQuery = _context.RecreationalTickets.AsQueryable();
        var cancellations = await GetCancellationReportAsync(requestingUserId, fromDate, toDate);

        if (fromDate.HasValue)
        {
            newSubsQuery = newSubsQuery.Where(x => x.CreatedAt >= fromDate.Value);
            newPackagesQuery = newPackagesQuery.Where(x => x.CreatedAt >= fromDate.Value);
            newPrivatesQuery = newPrivatesQuery.Where(x => x.CreatedAt >= fromDate.Value);
            ticketsQuery = ticketsQuery.Where(x => x.CheckedInAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            newSubsQuery = newSubsQuery.Where(x => x.CreatedAt <= toDate.Value);
            newPackagesQuery = newPackagesQuery.Where(x => x.CreatedAt <= toDate.Value);
            newPrivatesQuery = newPrivatesQuery.Where(x => x.CreatedAt <= toDate.Value);
            ticketsQuery = ticketsQuery.Where(x => x.CheckedInAt <= toDate.Value);
        }

        return new DashboardSummaryDto
        {
            CanViewFinancials = canViewFinancials,
            PeriodRevenue = financial.TotalRevenue,
            PeriodProfit = financial.NetProfit,
            ActiveSwimmers = await swimmersQuery.CountAsync(s => s.Status == "ACTIVE" && !s.IsDeleted),
            ActiveSubscriptions = await subscriptionsQuery.CountAsync(s => s.Status == "ACTIVE"),
            NewSubscriptionsCount = await newSubsQuery.CountAsync() + await newPackagesQuery.CountAsync() + await newPrivatesQuery.CountAsync(),
            RecreationalTicketsCount = await ticketsQuery.CountAsync(),
            CancellationsCount = cancellations.TotalCancellations
        };
    }

    public async Task<FinancialReportDto> GetFinancialReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Transactions.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var allTxs = await query.ToListAsync();

        var revenueTxs = allTxs.Where(t => RevenueTransactionTypes.Contains(t.TransactionType)).ToList();
        var expenseTxs = allTxs.Where(t => ExpenseTransactionTypes.Contains(t.TransactionType)).ToList();

        decimal totalRevenue = revenueTxs.Sum(t => t.Amount);
        decimal totalExpenses = Math.Abs(expenseTxs.Sum(t => t.Amount)); // Expenses are stored as negative (or pos/neg for adjustments), sum them and take absolute for display

        var revenueByCategory = revenueTxs
            .GroupBy(t => t.TransactionType)
            .Select(g => new TransactionSummaryDto
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount)
            }).ToList();

        var expensesByCategory = expenseTxs
            .GroupBy(t => t.TransactionType)
            .Select(g => new TransactionSummaryDto
            {
                Category = g.Key,
                Amount = Math.Abs(g.Sum(t => t.Amount))
            }).ToList();

        // Outstanding calculation
        var outSubsList = await _context.TrainingSubscriptions.Where(s => s.BalanceDue > 0).Select(s => s.BalanceDue).ToListAsync();
        var outPkgsList = await _context.Packages.Where(p => p.BalanceDue > 0).Select(p => p.BalanceDue).ToListAsync();
        var outPrivsList = await _context.PrivateBookings.Where(p => p.BalanceDue > 0).Select(p => p.BalanceDue).ToListAsync();

        var outSubs = outSubsList.Sum();
        var outPkgs = outPkgsList.Sum();
        var outPrivs = outPrivsList.Sum();

        return new FinancialReportDto
        {
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            NetProfit = totalRevenue - totalExpenses,
            TotalOutstanding = outSubs + outPkgs + outPrivs,
            RevenueByCategory = revenueByCategory,
            ExpensesByCategory = expensesByCategory
        };
    }

    public async Task<List<OutstandingItemDto>> GetOutstandingReportAsync(int requestingUserId)
    {
        var result = new List<OutstandingItemDto>();

        var subs = await _context.TrainingSubscriptions
            .Include(s => s.Swimmer)
            .Where(s => s.BalanceDue > 0 || s.OutstandingDeclaredAmount > 0)
            .ToListAsync();
            
        foreach (var s in subs)
        {
            result.Add(new OutstandingItemDto
            {
                EntityType = "SUBSCRIPTION",
                EntityId = s.SubscriptionId,
                CustomerName = s.Swimmer.Name,
                BalanceDue = s.BalanceDue,
                DeclaredOutstanding = s.OutstandingDeclaredAmount,
                Description = s.OutstandingDescription,
                CreatedAt = s.CreatedAt
            });
        }

        var pkgs = await _context.Packages
            .Include(p => p.Swimmer)
            .Where(p => p.BalanceDue > 0 || p.OutstandingDeclaredAmount > 0)
            .ToListAsync();
            
        foreach (var p in pkgs)
        {
            result.Add(new OutstandingItemDto
            {
                EntityType = "PACKAGE",
                EntityId = p.PackageId,
                CustomerName = p.Swimmer.Name,
                BalanceDue = p.BalanceDue,
                DeclaredOutstanding = p.OutstandingDeclaredAmount,
                Description = p.OutstandingDescription,
                CreatedAt = p.CreatedAt
            });
        }

        var privs = await _context.PrivateBookings
            .Include(p => p.CreatedByUser) // Using creator for now, or could pull primary participant
            .Where(p => p.BalanceDue > 0)
            .ToListAsync();

        foreach (var p in privs)
        {
            result.Add(new OutstandingItemDto
            {
                EntityType = "PRIVATE",
                EntityId = p.PrivateBookingId,
                CustomerName = "N/A", // Better to query PrivateBookingParticipants
                BalanceDue = p.BalanceDue,
                DeclaredOutstanding = null,
                Description = null,
                CreatedAt = p.CreatedAt
            });
        }

        return result.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<SwimmerReportDto> GetSwimmerReportAsync(int requestingUserId)
    {
        var all = await _context.Swimmers.ToListAsync();
        
        return new SwimmerReportDto
        {
            TotalSwimmers = all.Count(s => !s.IsDeleted),
            ActiveSwimmers = all.Count(s => s.Status == "ACTIVE" && !s.IsDeleted),
            InactiveSwimmers = all.Count(s => s.Status == "INACTIVE" && !s.IsDeleted),
            MemberSwimmers = all.Count(s => s.MemberStatus == "MEMBER" && !s.IsDeleted),
            NonMemberSwimmers = all.Count(s => s.MemberStatus == "NON_MEMBER" && !s.IsDeleted)
        };
    }

    public async Task<RecreationalTicketReportDto> GetRecreationalReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.RecreationalTickets.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(t => t.CheckedInAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.CheckedInAt <= toDate.Value);

        var list = await query.ToListAsync();

        return new RecreationalTicketReportDto
        {
            TotalTickets = list.Count,
            TotalRevenue = list.Sum(t => t.AmountPaid),
            MemberTickets = list.Count(t => t.MemberStatus == "MEMBER"),
            NonMemberTickets = list.Count(t => t.MemberStatus == "NON_MEMBER")
        };
    }

    public async Task<List<CoachDuesReportDto>> GetCoachDuesReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.CoachDues.Include(cd => cd.Employee).AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(cd => cd.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(cd => cd.CreatedAt <= toDate.Value);

        var list = await query.ToListAsync();
        
        var grouped = list.GroupBy(cd => cd.EmployeeId).ToList();

        var result = new List<CoachDuesReportDto>();
        foreach(var g in grouped)
        {
            var emp = g.First().Employee;
            var bySource = g.GroupBy(cd => cd.SourceType)
                            .Select(sg => new TransactionSummaryDto { Category = sg.Key, Amount = sg.Sum(x => x.Amount) })
                            .ToList();
            
            result.Add(new CoachDuesReportDto
            {
                EmployeeId = emp.EmployeeId,
                CoachName = emp.Name,
                ConsumedDues = g.Where(x => x.ConsumedInPayrollId != null).Sum(x => x.Amount),
                UnconsumedDues = g.Where(x => x.ConsumedInPayrollId == null).Sum(x => x.Amount),
                DuesBySource = bySource
            });
        }
        return result;
    }

    public async Task<CancellationReportDto> GetCancellationReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        // To accurately get cancellations we can count Refund transactions, OR check status updates, OR Audit Logs.
        // It's cleaner to query CANCELLED entities within the date range, assuming their last modified or created date (or we just use Refund created_at).
        // For accurate date ranges on cancellations, we could use Audit Logs. But we can also query Refunds directly for financial info.
        
        var refundQuery = _context.Refunds.AsQueryable();
        if (fromDate.HasValue) refundQuery = refundQuery.Where(r => r.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) refundQuery = refundQuery.Where(r => r.CreatedAt <= toDate.Value);
        
        var refunds = await refundQuery.ToListAsync();

        var duesQuery = _context.CoachDues.Where(cd => cd.SourceType == "CANCELLATION_FEE").AsQueryable();
        if (fromDate.HasValue) duesQuery = duesQuery.Where(cd => cd.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) duesQuery = duesQuery.Where(cd => cd.CreatedAt <= toDate.Value);
        
        var feesList = await duesQuery.Select(cd => cd.Amount).ToListAsync();
        var fees = feesList.Sum();

        // Cancellations count by entity (simplification: checking Audit logs or assuming CANCELLED entities)
        var logsQuery = _context.AuditLogs.Where(l => l.EventType == "TRAINING_SUBSCRIPTION_CANCELLED" || 
                                                      l.EventType == "PACKAGE_CANCELLED" || 
                                                      l.EventType == "PRIVATE_BOOKING_CANCELLED").AsQueryable();
        
        if (fromDate.HasValue) logsQuery = logsQuery.Where(l => l.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) logsQuery = logsQuery.Where(l => l.CreatedAt <= toDate.Value);

        var logs = await logsQuery.ToListAsync();

        return new CancellationReportDto
        {
            TotalCancellations = logs.Count,
            TotalRefunded = refunds.Sum(r => r.Amount),
            TotalCancellationFees = fees,
            TrainingCancellations = logs.Count(l => l.EventType == "TRAINING_SUBSCRIPTION_CANCELLED"),
            PackageCancellations = logs.Count(l => l.EventType == "PACKAGE_CANCELLED"),
            PrivateCancellations = logs.Count(l => l.EventType == "PRIVATE_BOOKING_CANCELLED")
        };
    }

    public async Task<PayrollReportDto> GetPayrollReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Payrolls.AsQueryable();
        if (fromDate.HasValue) query = query.Where(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(p => p.CreatedAt <= toDate.Value);

        var payrolls = await query.ToListAsync();
        var txQuery = _context.Transactions.Where(t => t.TransactionType == "PAYROLL_PAYMENT" || t.TransactionType == "PAYROLL_ADJUSTMENT");
        if (fromDate.HasValue) txQuery = txQuery.Where(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) txQuery = txQuery.Where(p => p.CreatedAt <= toDate.Value);
        var txs = await txQuery.ToListAsync();

        return new PayrollReportDto
        {
            TotalPayrolls = payrolls.Count,
            TotalCalculatedAmount = payrolls.Sum(p => p.CalculatedAmount),
            TotalPaidAmount = Math.Abs(txs.Sum(t => t.Amount)),
            PaidCount = payrolls.Count(p => p.Status == "PAID"),
            UnpaidCount = payrolls.Count(p => p.Status == "NOT_PAID")
        };
    }

    public async Task<AttendanceReportDto> GetAttendanceReportAsync(int requestingUserId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var eAttQuery = _context.EmployeeAttendances.AsQueryable();
        var pAttQuery = _context.PrivateSessionAttendances.AsQueryable();
        var pkgQuery = _context.PackageCheckIns.AsQueryable();
        var tktQuery = _context.RecreationalTickets.AsQueryable();

        if (fromDate.HasValue)
        {
            eAttQuery = eAttQuery.Where(a => a.CreatedAt >= fromDate.Value);
            pAttQuery = pAttQuery.Where(a => a.RecordedAt >= fromDate.Value);
            pkgQuery = pkgQuery.Where(a => a.CreatedAt >= fromDate.Value);
            tktQuery = tktQuery.Where(a => a.CheckedInAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            eAttQuery = eAttQuery.Where(a => a.CreatedAt <= toDate.Value);
            pAttQuery = pAttQuery.Where(a => a.RecordedAt <= toDate.Value);
            pkgQuery = pkgQuery.Where(a => a.CreatedAt <= toDate.Value);
            tktQuery = tktQuery.Where(a => a.CheckedInAt <= toDate.Value);
        }

        var eAtts = await eAttQuery.Include(a => a.Employee).ToListAsync();
        var pAtts = await pAttQuery.ToListAsync();
        var pkgs = await pkgQuery.CountAsync();
        var tkts = await tktQuery.CountAsync();

        var cAtts = eAtts.Where(a => a.Employee.EmployeeType == "COACH").ToList();
        var lAtts = eAtts.Where(a => a.Employee.EmployeeType == "LIFEGUARD").ToList();

        // Note: Swimmer attendance for training requires querying `Attendances` (which hasn't been explicitly separated into SwimmerAttendances vs others if the table is `Attendances`). Let's assume there's a DbSet `Attendances` for training.
        var sAttQuery = _context.Attendances.AsQueryable();
        if (fromDate.HasValue) sAttQuery = sAttQuery.Where(a => a.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) sAttQuery = sAttQuery.Where(a => a.CreatedAt <= toDate.Value);
        var sAtts = await sAttQuery.ToListAsync();

        return new AttendanceReportDto
        {
            SwimmerPresent = sAtts.Count,
            SwimmerAbsent = 0,
            CoachPresent = cAtts.Count(a => a.Status == "PRESENT"),
            CoachAbsent = cAtts.Count(a => a.Status == "ABSENT"),
            LifeguardPresent = lAtts.Count(a => a.Status == "PRESENT"),
            LifeguardAbsent = lAtts.Count(a => a.Status == "ABSENT"),
            PackageCheckIns = pkgs,
            PrivateParticipantCheckIns = pAtts.Count(a => a.Status == "PRESENT"),
            RecreationalCheckIns = tkts // Because tickets are equivalent to check-ins
        };
    }
}
