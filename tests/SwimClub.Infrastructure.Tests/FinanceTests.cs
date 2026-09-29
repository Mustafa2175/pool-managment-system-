using Microsoft.EntityFrameworkCore;
using Moq;
using SwimClub.Application.Finance;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Finance;
using SwimClub.Infrastructure.Persistence;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class FinanceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly AppDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IAuditLogService> _auditLogMock;

    private readonly PaymentService _paymentService;
    private readonly RefundService _refundService;
    private readonly ExpenseService _expenseService;
    private readonly CreditService _creditService;
    private readonly TransactionService _transactionService;
    private readonly UnitOfWork _unitOfWork;

    public FinanceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"swimclub-fin-test-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _context = new AppDbContext(options);

        _currentUserMock = new Mock<ICurrentUserService>();
        _auditLogMock = new Mock<IAuditLogService>();

        _unitOfWork = new UnitOfWork(_context);
        _transactionService = new TransactionService(_context);
        _paymentService = new PaymentService(_context, _currentUserMock.Object, _auditLogMock.Object);
        _refundService = new RefundService(_context, _currentUserMock.Object);
        _expenseService = new ExpenseService(_context, _currentUserMock.Object);
        _creditService = new CreditService(_context, _currentUserMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    private async Task SeedPrerequisitesAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        // Seed role & user
        var role = await _context.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var employee = new Employee
        {
            Name = "Fin Admin",
            EmployeeType = "ADMINISTRATOR",
            NationalId = $"FIN-ADM-{Guid.NewGuid():N}",
            MonthlySalary = 5000,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Username = $"finadmin_{Guid.NewGuid():N}",
            PasswordHash = "hash",
            RoleId = role.RoleId,
            EmployeeId = employee.EmployeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _currentUserMock.Setup(c => c.CurrentUser).Returns(user);

        // Seed Swimmer
        var swimmer = new Swimmer
        {
            SwimmerId = "SW-FIN-001",
            Name = "Fin Swimmer",
            DateOfBirth = new DateOnly(2012, 1, 1),
            Gender = "MALE",
            MemberStatus = "MEMBER",
            QrToken = $"qr-fin-{Guid.NewGuid():N}",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Swimmers.Add(swimmer);

        // Seed Program & Period
        var program = new Domain.Entities.Program
        {
            Name = "Fin Program",
            ProgramType = "REGULAR",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Programs.Add(program);
        await _context.SaveChangesAsync();

        var period = new TrainingPeriod
        {
            ProgramId = program.ProgramId,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Capacity = 15,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingPeriods.Add(period);
        await _context.SaveChangesAsync();

        // Seed Subscription: TotalPrice = 1000, PaidAmount = 0 -> BalanceDue = 1000
        var sub = new TrainingSubscription
        {
            SwimmerId = swimmer.SwimmerId,
            ProgramId = program.ProgramId,
            PeriodId = period.PeriodId,
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 12, 31),
            UnitPriceSnapshot = 1000,
            ConfiguredSessionCountSnapshot = 12,
            TotalPrice = 1000,
            PaidAmount = 0,
            Status = "ACTIVE",
            CreatedBy = user.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingSubscriptions.Add(sub);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task RecordPayment_ValidAmount_CreatesPaymentAndTransaction()
    {
        await SeedPrerequisitesAsync();
        var sub = await _context.TrainingSubscriptions.FirstAsync();

        // Act: Record payment of 400
        var result = await _paymentService.RecordPaymentAsync(
            "TRAINING_SUBSCRIPTION",
            sub.SubscriptionId,
            400,
            "CASH",
            "Initial payment"
        );

        // Assert
        Assert.Equal(PaymentResult.Success, result);

        // Verify updated subscription balance
        _context.ChangeTracker.Clear();
        var updatedSub = await _context.TrainingSubscriptions.FindAsync(sub.SubscriptionId);
        Assert.Equal(400, updatedSub!.PaidAmount);
        Assert.Equal(600, updatedSub.BalanceDue);

        // Verify created payment & transaction
        var payment = await _context.Payments.FirstOrDefaultAsync();
        Assert.NotNull(payment);
        Assert.Equal(400, payment!.Amount);

        var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.RelatedEntityType == "TRAINING_SUBSCRIPTION");
        Assert.NotNull(tx);
        Assert.Equal("TRAINING_PAYMENT", tx!.TransactionType);
        Assert.Equal(400, tx.Amount);
    }

    [Fact]
    public async Task RecordPayment_Overpayment_ReturnsOverpaymentRejected_AndLogsAuditFailure()
    {
        await SeedPrerequisitesAsync();
        var sub = await _context.TrainingSubscriptions.FirstAsync();

        // Act: Attempt to pay 1500 when TotalPrice / BalanceDue is 1000
        var result = await _paymentService.RecordPaymentAsync(
            "TRAINING_SUBSCRIPTION",
            sub.SubscriptionId,
            1500,
            "CASH",
            "Overpayment attempt"
        );

        // Assert
        Assert.Equal(PaymentResult.OverpaymentRejected, result);
        _auditLogMock.Verify(a => a.LogFailureAsync("PAYMENT_OVERPAYMENT_REJECTED", It.IsAny<string>(), null, null), Times.Once);
    }

    [Fact]
    public async Task PaymentCorrection_MutatesPaymentAndLinkedTransaction_AndAudits()
    {
        await SeedPrerequisitesAsync();
        var sub = await _context.TrainingSubscriptions.FirstAsync();

        // Record initial payment of 500
        await _paymentService.RecordPaymentAsync(
            "TRAINING_SUBSCRIPTION",
            sub.SubscriptionId,
            500,
            "CASH",
            null
        );

        var payment = await _context.Payments.FirstAsync();

        // Act: Correct payment from 500 to 450
        var result = await _paymentService.CorrectPaymentAsync(
            payment.PaymentId,
            450,
            "Typo correction"
        );

        // Assert
        Assert.Equal(PaymentResult.Success, result);

        _context.ChangeTracker.Clear();
        var updatedPayment = await _context.Payments.FindAsync(payment.PaymentId);
        Assert.Equal(450, updatedPayment!.Amount);

        var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.RelatedEntityType == "TRAINING_SUBSCRIPTION");
        Assert.NotNull(tx);
        Assert.Equal(450, tx!.Amount);

        // Verify audit log call
        _auditLogMock.Verify(a => a.LogSuccessAsync("PAYMENT_CORRECTED", "Payment", payment.PaymentId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RefundService_ConfirmAndAdjust_UpdatesTransactions()
    {
        await SeedPrerequisitesAsync();
        var user = await _context.Users.FirstAsync();

        var refund = new Refund
        {
            RelatedEntityType = "TRAINING_SUBSCRIPTION",
            RelatedEntityId = 1,
            Amount = 200,
            Reason = "Customer request",
            RecordedBy = user.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Refunds.Add(refund);
        await _context.SaveChangesAsync();

        // Act 1: Confirm refund
        var confirmResult = await _refundService.ConfirmRefundAsync(refund.RefundId);
        Assert.Equal(RefundResult.Success, confirmResult);

        var refundTx = await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionType == "REFUND");
        Assert.NotNull(refundTx);
        Assert.Equal(-200, refundTx!.Amount); // Refunds are negative in the ledger

        // Act 2: Adjust refund
        var adjustResult = await _refundService.AdjustRefundAsync(refund.RefundId, 50, "Adjustment fee");
        Assert.Equal(RefundResult.Success, adjustResult);

        var adjustTx = await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionType == "REFUND_ADJUSTMENT");
        Assert.NotNull(adjustTx);
        Assert.Equal(50, adjustTx!.Amount);
    }

    [Fact]
    public async Task CreditService_GrantCreatesNoTransaction_UsageCreatesTransaction()
    {
        await SeedPrerequisitesAsync();
        var sub = await _context.TrainingSubscriptions.FirstAsync();

        // Act 1: Grant 300 credit from subscription
        var creditId = await _creditService.GrantCreditAsync(
            "TRAINING_SUBSCRIPTION",
            sub.SubscriptionId,
            300,
            "Cancellation credit"
        );

        // Assert 1: Credit record created, but NO transaction created (Decision 1)
        Assert.True(creditId > 0);

        var grantTxCount = await _context.Transactions.CountAsync();
        Assert.Equal(0, grantTxCount);

        // Act 2: Use 100 credit on package/subscription
        var usageResult = await _creditService.UseCreditAsync(
            creditId,
            100,
            "TRAINING_SUBSCRIPTION",
            sub.SubscriptionId
        );

        // Assert 2: Credit usage creates a positive revenue transaction
        Assert.Equal(CreditResult.Success, usageResult);

        var credit = await _context.Credits.FindAsync(creditId);
        Assert.Equal(200, credit!.RemainingAmount);

        var usageTx = await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionType == "CREDIT_USAGE");
        Assert.NotNull(usageTx);
        Assert.Equal(100, usageTx!.Amount);
    }

    [Fact]
    public async Task ExpenseService_RecordsExpenseAndTransaction()
    {
        await SeedPrerequisitesAsync();

        // Act: Record maintenance expense
        var expenseId = await _expenseService.RecordExpenseAsync(
            450,
            "Pool Chemicals & Chlorine",
            "CASH",
            DateTime.UtcNow
        );

        // Assert
        Assert.True(expenseId > 0);

        var expense = await _context.Expenses.FindAsync(expenseId);
        Assert.NotNull(expense);
        Assert.Equal(450, expense!.Amount);

        var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionType == "EXPENSE");
        Assert.NotNull(tx);
        Assert.Equal(-450, tx!.Amount); // Expense is negative in transaction ledger
    }

    [Fact]
    public async Task TransactionService_FormulaBasedRevenueAndProfitCalculation()
    {
        await SeedPrerequisitesAsync();
        var sub = await _context.TrainingSubscriptions.FirstAsync();
        var user = await _context.Users.FirstAsync();

        var today = DateTime.UtcNow;
        var start = today.AddDays(-1);
        var end = today.AddDays(1);

        // 1. Subscription payment: +500 (Revenue)
        await _paymentService.RecordPaymentAsync("TRAINING_SUBSCRIPTION", sub.SubscriptionId, 500, "CASH", null);

        // 2. Credit Grant + Usage: Grant +200, Use 150 (Revenue)
        var creditId = await _creditService.GrantCreditAsync("TRAINING_SUBSCRIPTION", sub.SubscriptionId, 200, "Bonus");
        await _creditService.UseCreditAsync(creditId, 150, "TRAINING_SUBSCRIPTION", sub.SubscriptionId);

        // 3. Refund: -100 (Deducted from Revenue)
        var refund = new Refund { RelatedEntityType = "TRAINING_SUBSCRIPTION", RelatedEntityId = sub.SubscriptionId, Amount = 100, RecordedBy = user.UserId, CreatedAt = DateTime.UtcNow };
        _context.Refunds.Add(refund);
        await _context.SaveChangesAsync();
        await _refundService.ConfirmRefundAsync(refund.RefundId);

        // 4. Expense: 200 (Excluded from Revenue, deducted from Net Profit)
        await _expenseService.RecordExpenseAsync(200, "Utilities", "CASH", today);

        // Expected Revenue: 500 (Subscription) + 150 (Credit Usage) - 100 (Refund) = 550
        var revenue = await _transactionService.GetRevenueAsync(start, end);
        Assert.Equal(550, revenue);

        // Expected Expenses: 200
        var expenses = await _transactionService.GetExpensesAsync(start, end);
        Assert.Equal(200, expenses);

        // Expected Net Profit: 550 (Revenue) - 200 (Expense) = 350
        var profit = await _transactionService.GetProfitAsync(start, end);
        Assert.Equal(350, profit);
    }

    [Fact]
    public async Task UnitOfWork_TransactionRollback_DiscardsChanges()
    {
        await SeedPrerequisitesAsync();

        // Act: Start transaction, insert transaction record, then rollback
        await _unitOfWork.BeginTransactionAsync();

        _context.Transactions.Add(new Transaction
        {
            TransactionType = "RECREATIONAL_TICKET_PAYMENT",
            Amount = 100,
            CreatedAt = DateTime.UtcNow,
            RecordedBy = 1
        });
        await _unitOfWork.SaveChangesAsync();

        await _unitOfWork.RollbackTransactionAsync();

        // Assert: No transaction saved to database after rollback
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(0, count);
    }
}
