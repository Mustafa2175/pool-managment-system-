using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Reports;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Infrastructure.Reports;
using Xunit;

namespace SwimClub.Infrastructure.Tests.Reports;

public class ReportsServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private ReportsService _service = null!;
    private int _superAdminUserId;
    private int _adminUserId;

    public async Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"swimclub-reports-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _service = new ReportsService(_context);

        // Roles
        var saRole = new Role { Code = "SUPER_ADMIN", NameEn = "SA", NameAr = "SA" };
        var adminRole = new Role { Code = "ADMINISTRATOR", NameEn = "Admin", NameAr = "Admin" };
        _context.Roles.AddRange(saRole, adminRole);
        await _context.SaveChangesAsync();

        // Users
        var saUser = new User { Username = "sa", PasswordHash = "hash", RoleId = saRole.RoleId, IsActive = true };
        var adminUser = new User { Username = "admin", PasswordHash = "hash", RoleId = adminRole.RoleId, IsActive = true };
        _context.Users.AddRange(saUser, adminUser);
        await _context.SaveChangesAsync();

        _superAdminUserId = saUser.UserId;
        _adminUserId = adminUser.UserId;
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // Helper
    private async Task SeedFinancialDataAsync()
    {
        var jan1 = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var jan15 = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var feb1 = new DateTime(2025, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        // Revenue (+1000, +500)
        _context.Transactions.Add(new Transaction { TransactionType = "TRAINING_PAYMENT", Amount = 1000, CreatedAt = jan1, RelatedEntityId = 1, RecordedBy = _adminUserId });
        _context.Transactions.Add(new Transaction { TransactionType = "PRIVATE_PAYMENT", Amount = 500, CreatedAt = jan15, RelatedEntityId = 2, RecordedBy = _adminUserId });

        // Credit Usage (+200) -> Revenue
        _context.Transactions.Add(new Transaction { TransactionType = "CREDIT_USAGE", Amount = 200, CreatedAt = feb1, RelatedEntityId = 1, RecordedBy = _adminUserId });
        
        // Refund (-100) -> reduces revenue
        _context.Transactions.Add(new Transaction { TransactionType = "REFUND", Amount = -100, CreatedAt = feb1, RelatedEntityId = 1, RecordedBy = _adminUserId });

        // Expenses (-300, Payroll -400) -> 700 total expenses
        _context.Transactions.Add(new Transaction { TransactionType = "EXPENSE", Amount = -300, CreatedAt = jan15, RelatedEntityId = 1, RecordedBy = _adminUserId });
        _context.Transactions.Add(new Transaction { TransactionType = "PAYROLL_PAYMENT", Amount = -400, CreatedAt = feb1, RelatedEntityId = 1, RecordedBy = _adminUserId });

        // A payroll adjustment (+50 expense correction)
        _context.Transactions.Add(new Transaction { TransactionType = "PAYROLL_ADJUSTMENT", Amount = 50, CreatedAt = feb1, RelatedEntityId = 1, RecordedBy = _adminUserId });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetFinancialReport_RevenueClassification_CalculatesCorrectly()
    {
        await SeedFinancialDataAsync();

        var report = await _service.GetFinancialReportAsync(_superAdminUserId);

        // Revenue = 1000 + 500 + 200 - 100 = 1600
        Assert.Equal(1600m, report.TotalRevenue);

        // Expenses = |-300 + -400 + 50| = 650
        Assert.Equal(650m, report.TotalExpenses);

        // Profit = 1600 - 650 = 950
        Assert.Equal(950m, report.NetProfit);
    }

    [Fact]
    public async Task GetFinancialReport_DateFiltering_AppliesCorrectly()
    {
        await SeedFinancialDataAsync();

        // Filter for January only
        var fromDate = new DateTime(2025, 1, 1);
        var toDate = new DateTime(2025, 1, 31, 23, 59, 59);

        var report = await _service.GetFinancialReportAsync(_superAdminUserId, fromDate, toDate);

        // Jan Revenue = 1000 (TRAINING) + 500 (PRIVATE) = 1500
        Assert.Equal(1500m, report.TotalRevenue);

        // Jan Expenses = |-300 (EXPENSE)| = 300
        Assert.Equal(300m, report.TotalExpenses);

        Assert.Equal(1200m, report.NetProfit);
    }

    [Fact]
    public async Task Dashboard_RoleVisibility_SuperAdminSeesFinancials()
    {
        await SeedFinancialDataAsync();

        var dash = await _service.GetDashboardSummaryAsync(_superAdminUserId);
        
        Assert.True(dash.CanViewFinancials);
        Assert.Equal(1600m, dash.PeriodRevenue);
        Assert.Equal(950m, dash.PeriodProfit);
    }

    [Fact]
    public async Task Dashboard_RoleVisibility_AdminDoesNotSeeFinancials()
    {
        await SeedFinancialDataAsync();

        var dash = await _service.GetDashboardSummaryAsync(_adminUserId);
        
        Assert.False(dash.CanViewFinancials);
        Assert.Equal(0m, dash.PeriodRevenue);
        Assert.Equal(0m, dash.PeriodProfit);
    }

    [Fact]
    public async Task FinancialReconciliation_ReportTotalsMatchTransactionTypes()
    {
        await SeedFinancialDataAsync();

        var report = await _service.GetFinancialReportAsync(_superAdminUserId);

        var allTxs = await _context.Transactions.ToListAsync();
        
        decimal manualRevenue = allTxs.Where(t => new[] { "TRAINING_PAYMENT", "PACKAGE_PAYMENT", "PRIVATE_PAYMENT", "RECREATIONAL_TICKET_PAYMENT", "OUTSTANDING_PAYMENT", "CREDIT_USAGE", "REFUND", "REFUND_ADJUSTMENT" }.Contains(t.TransactionType)).Sum(t => t.Amount);
        
        decimal manualExpenses = Math.Abs(allTxs.Where(t => new[] { "EXPENSE", "PAYROLL_PAYMENT", "PAYROLL_ADJUSTMENT" }.Contains(t.TransactionType)).Sum(t => t.Amount));

        Assert.Equal(manualRevenue, report.TotalRevenue);
        Assert.Equal(manualExpenses, report.TotalExpenses);
        Assert.Equal(manualRevenue - manualExpenses, report.NetProfit);
    }

    [Fact]
    public async Task OutstandingReport_ShowsOnlyBalancesGreaterThanZero_OrDeclared()
    {
        var swimmer = new Swimmer { SwimmerId = "S1", Name = "S1", DateOfBirth = new DateOnly(2000,1,1), Gender = "MALE", MemberStatus = "MEMBER", QrToken = "123", Status = "ACTIVE" };
        _context.Swimmers.Add(swimmer);
        await _context.SaveChangesAsync();

        var program = new Program { Name = "Main", ProgramType = "REGULAR" };
        _context.Programs.Add(program);
        await _context.SaveChangesAsync();

        var period = new TrainingPeriod { ProgramId = program.ProgramId, StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(10,0), Capacity = 10 };
        _context.TrainingPeriods.Add(period);
        await _context.SaveChangesAsync();

        // Subscription 1: Balance = 0, but Declared = 500
        var sub1 = new TrainingSubscription { SwimmerId = "S1", ProgramId = program.ProgramId, PeriodId = period.PeriodId, StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,2,1), TotalPrice = 1000, PaidAmount = 1000, OutstandingDeclaredAmount = 500, CreatedBy = _adminUserId, CreatedAt = DateTime.UtcNow };
        
        // Subscription 2: Balance = 200, Declared = null
        var sub2 = new TrainingSubscription { SwimmerId = "S1", ProgramId = program.ProgramId, PeriodId = period.PeriodId, StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,2,1), TotalPrice = 1000, PaidAmount = 800, CreatedBy = _adminUserId, CreatedAt = DateTime.UtcNow };

        // Subscription 3: Balance = 0, Declared = null (should not appear)
        var sub3 = new TrainingSubscription { SwimmerId = "S1", ProgramId = program.ProgramId, PeriodId = period.PeriodId, StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,2,1), TotalPrice = 1000, PaidAmount = 1000, CreatedBy = _adminUserId, CreatedAt = DateTime.UtcNow };

        _context.TrainingSubscriptions.AddRange(sub1, sub2, sub3);
        await _context.SaveChangesAsync();

        var outstanding = await _service.GetOutstandingReportAsync(_superAdminUserId);

        Assert.Equal(2, outstanding.Count);
        Assert.Contains(outstanding, o => o.EntityId == sub1.SubscriptionId && o.DeclaredOutstanding == 500);
        Assert.Contains(outstanding, o => o.EntityId == sub2.SubscriptionId && o.BalanceDue == 200);
    }
}
