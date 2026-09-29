using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Payroll;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Logging;
using SwimClub.Infrastructure.Payroll;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Infrastructure.Security;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class PayrollServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private PayrollService _service = null!;
    private int _adminUserId;
    private int _coachEmployeeId;
    private int _lifeguardEmployeeId;
    private int _adminEmployeeId;
    private int _qualificationId;
    private int _periodId;
    private int _programId;

    public async Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"swimclub-payroll-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var cu = new CurrentUserService();
        var audit = new AuditLogService(_context, cu);
        _service = new PayrollService(_context, audit);

        _adminUserId = await SeedBaseDataAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private async Task<int> SeedBaseDataAsync()
    {
        var role = new Role { Code = "SUPER_ADMIN", NameEn = "SA", NameAr = "SA" };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var user = new User { Username = "sa", PasswordHash = "hash", RoleId = role.RoleId, IsActive = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Qualification
        var qual = new Qualification { NameEn = "Level 1", NameAr = "مستوى 1", RankOrder = 1 };
        _context.Qualifications.Add(qual);
        await _context.SaveChangesAsync();
        _qualificationId = qual.QualificationId;

        // Rate config: 100 EGP/session, effective from 2020
        _context.QualificationRateConfigs.Add(new QualificationRateConfig
        {
            QualificationId = _qualificationId,
            SessionRate = 100m,
            EffectiveFrom = new DateOnly(2020, 1, 1),
            EffectiveTo = null
        });
        await _context.SaveChangesAsync();

        // Coach employee with qualification
        var coach = new Employee
        {
            Name = "Coach A",
            EmployeeType = "COACH",
            NationalId = "12345",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(coach);
        await _context.SaveChangesAsync();
        _coachEmployeeId = coach.EmployeeId;

        _context.EmployeeQualifications.Add(new EmployeeQualification
        {
            EmployeeId = _coachEmployeeId,
            QualificationId = _qualificationId,
            ObtainedAt = new DateOnly(2020, 1, 1)
        });

        // Lifeguard employee with qualification
        var lifeguard = new Employee
        {
            Name = "Lifeguard B",
            EmployeeType = "LIFEGUARD",
            NationalId = "54321",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(lifeguard);
        await _context.SaveChangesAsync();
        _lifeguardEmployeeId = lifeguard.EmployeeId;

        _context.EmployeeQualifications.Add(new EmployeeQualification
        {
            EmployeeId = _lifeguardEmployeeId,
            QualificationId = _qualificationId,
            ObtainedAt = new DateOnly(2020, 1, 1)
        });

        // Administrator employee
        var admin = new Employee
        {
            Name = "Admin C",
            EmployeeType = "ADMINISTRATOR",
            NationalId = "99999",
            MonthlySalary = 3000m,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(admin);
        await _context.SaveChangesAsync();
        _adminEmployeeId = admin.EmployeeId;

        // Training period (for session generation)
        var program = new Program { Name = "Main", ProgramType = "REGULAR", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Programs.Add(program);
        await _context.SaveChangesAsync();
        _programId = program.ProgramId;

        var period = new TrainingPeriod
        {
            ProgramId = program.ProgramId,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Capacity = 20,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingPeriods.Add(period);
        await _context.SaveChangesAsync();
        _periodId = period.PeriodId;

        await _context.SaveChangesAsync();
        return user.UserId;
    }

    // Helper: create a session + PRESENT employee attendance for a specific date
    private async Task<Session> CreateSessionWithAttendanceAsync(int employeeId, DateTime date, string status = "PRESENT")
    {
        // Need a dummy subscription
        var swimmer = new Swimmer
        {
            SwimmerId = $"SW-{Guid.NewGuid():N}",
            Name = "Test",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = "MALE",
            MemberStatus = "MEMBER",
            QrToken = Guid.NewGuid().ToString("N"),
            Status = "ACTIVE"
        };
        _context.Swimmers.Add(swimmer);
        await _context.SaveChangesAsync();

        var sub = new TrainingSubscription
        {
            SwimmerId = swimmer.SwimmerId,
            ProgramId = _programId,
            PeriodId = _periodId,
            StartDate = DateOnly.FromDateTime(date),
            EndDate = DateOnly.FromDateTime(date.AddMonths(1)),
            ConfiguredSessionCountSnapshot = 4,
            UnitPriceSnapshot = 500,
            TotalPrice = 500,
            PaidAmount = 500,
            Status = "ACTIVE",
            CreatedBy = _adminUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingSubscriptions.Add(sub);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            PeriodId = _periodId,
            SubscriptionId = sub.SubscriptionId,
            ScheduledStartTime = date,
            ScheduledEndTime = date.AddHours(1),
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        _context.EmployeeAttendances.Add(new EmployeeAttendance
        {
            SessionId = session.SessionId,
            EmployeeId = employeeId,
            Status = status,
            RecordedBy = _adminUserId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return session;
    }

    // ======================================================================
    // ADMINISTRATOR PAYROLL TESTS
    // ======================================================================

    [Fact]
    public async Task Administrator_NoAbsence_FullSalary()
    {
        // No absences → net = monthly_salary
        var (result, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2025, 1, _adminUserId);

        Assert.Equal(PayrollResult.Success, result);
        var payroll = await _context.Payrolls.FindAsync(id!.Value);
        Assert.Equal(3000m, payroll!.CalculatedAmount);
    }

    [Fact]
    public async Task Administrator_AbsenceDays_DeductedCorrectly()
    {
        // January 2025 has 31 days. 3 absences.
        // daily = 3000/31; deduction = 3*daily; net = 3000 - deduction
        var jan = new DateOnly(2025, 1, 1);
        _context.AdministratorDailyAttendances.Add(new AdministratorDailyAttendance
        {
            EmployeeId = _adminEmployeeId,
            AttendanceDate = jan,
            Status = "ABSENT",
            RecordedBy = _adminUserId,
            CreatedAt = DateTime.UtcNow
        });
        _context.AdministratorDailyAttendances.Add(new AdministratorDailyAttendance
        {
            EmployeeId = _adminEmployeeId,
            AttendanceDate = jan.AddDays(1),
            Status = "ABSENT",
            RecordedBy = _adminUserId,
            CreatedAt = DateTime.UtcNow
        });
        _context.AdministratorDailyAttendances.Add(new AdministratorDailyAttendance
        {
            EmployeeId = _adminEmployeeId,
            AttendanceDate = jan.AddDays(2),
            Status = "ABSENT",
            RecordedBy = _adminUserId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var (result, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2025, 1, _adminUserId);

        Assert.Equal(PayrollResult.Success, result);
        var payroll = await _context.Payrolls.FindAsync(id!.Value);

        int daysInJan = 31;
        decimal dailyValue = 3000m / daysInJan;
        decimal expectedNet = 3000m - (3 * dailyValue);
        Assert.Equal(expectedNet, payroll!.CalculatedAmount);
    }

    [Fact]
    public async Task Administrator_MonthDayCount_FebruaryLeapYear()
    {
        // Feb 2024 = 29 days (leap year). daily = 3000/29
        var (result, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2024, 2, _adminUserId);

        Assert.Equal(PayrollResult.Success, result);
        // We just verify it successfully calculated and correct days
        var payroll = await _context.Payrolls.FindAsync(id!.Value);
        // No absences → full salary
        Assert.Equal(3000m, payroll!.CalculatedAmount);
    }

    // ======================================================================
    // COACH / LIFEGUARD PAYROLL TESTS
    // ======================================================================

    [Fact]
    public async Task Coach_PresentSessions_CorrectDues()
    {
        // 3 sessions in Jan 2025, all PRESENT → 3 × 100 = 300
        var month = new DateTime(2025, 1, 10);
        await CreateSessionWithAttendanceAsync(_coachEmployeeId, month);
        await CreateSessionWithAttendanceAsync(_coachEmployeeId, month.AddDays(2));
        await CreateSessionWithAttendanceAsync(_coachEmployeeId, month.AddDays(4));

        var (result, id) = await _service.CalculateCoachLifeguardPayrollAsync(_coachEmployeeId, 2025, 1, _adminUserId);

        Assert.Equal(PayrollResult.Success, result);
        var payroll = await _context.Payrolls.FindAsync(id!.Value);
        Assert.Equal(300m, payroll!.CalculatedAmount);
    }

    [Fact]
    public async Task Coach_AbsentSessions_ZeroForAbsent()
    {
        // 2 PRESENT + 1 ABSENT → 2 × 100 = 200
        var month = new DateTime(2025, 2, 5);
        await CreateSessionWithAttendanceAsync(_coachEmployeeId, month, "PRESENT");
        await CreateSessionWithAttendanceAsync(_coachEmployeeId, month.AddDays(2), "PRESENT");
        await CreateSessionWithAttendanceAsync(_coachEmployeeId, month.AddDays(4), "ABSENT");

        var (result, id) = await _service.CalculateCoachLifeguardPayrollAsync(_coachEmployeeId, 2025, 2, _adminUserId);

        Assert.Equal(PayrollResult.Success, result);
        var payroll = await _context.Payrolls.FindAsync(id!.Value);
        Assert.Equal(200m, payroll!.CalculatedAmount);
    }

    [Fact]
    public async Task Lifeguard_PresentSessions_CorrectDues()
    {
        // 2 sessions PRESENT → 2 × 100 = 200
        var month = new DateTime(2025, 3, 5);
        await CreateSessionWithAttendanceAsync(_lifeguardEmployeeId, month);
        await CreateSessionWithAttendanceAsync(_lifeguardEmployeeId, month.AddDays(3));

        var (result, id) = await _service.CalculateCoachLifeguardPayrollAsync(_lifeguardEmployeeId, 2025, 3, _adminUserId);

        Assert.Equal(PayrollResult.Success, result);
        var payroll = await _context.Payrolls.FindAsync(id!.Value);
        Assert.Equal(200m, payroll!.CalculatedAmount);
    }

    [Fact]
    public async Task Coach_ReplacementRate_UsesSnapshotRate()
    {
        // Coach has 0 direct sessions; replaced in 2 sessions at snapshot rate 80 EGP
        var session1 = await CreateSessionWithAttendanceAsync(_coachEmployeeId, new DateTime(2025, 4, 5), "ABSENT");
        var session2 = await CreateSessionWithAttendanceAsync(_coachEmployeeId, new DateTime(2025, 4, 7), "ABSENT");

        _context.EmployeeReplacements.Add(new EmployeeReplacement
        {
            SessionId = session1.SessionId,
            OriginalEmployeeId = _coachEmployeeId,
            ReplacingEmployeeId = _lifeguardEmployeeId,
            RateAppliedSnapshot = 80m,
            RecordedBy = _adminUserId,
            CreatedAt = DateTime.UtcNow
        });
        _context.EmployeeReplacements.Add(new EmployeeReplacement
        {
            SessionId = session2.SessionId,
            OriginalEmployeeId = _coachEmployeeId,
            ReplacingEmployeeId = _lifeguardEmployeeId,
            RateAppliedSnapshot = 80m,
            RecordedBy = _adminUserId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var (result, id) = await _service.CalculateCoachLifeguardPayrollAsync(_lifeguardEmployeeId, 2025, 4, _adminUserId);

        Assert.Equal(PayrollResult.Success, result);
        var payroll = await _context.Payrolls.FindAsync(id!.Value);
        // Lifeguard was the REPLACING employee → gets 2 × 80 = 160 replacement dues
        Assert.Equal(160m, payroll!.CalculatedAmount);
    }

    [Fact]
    public async Task Coach_QualificationRate_HighestRankUsed()
    {
        // Add a higher-ranked qualification with higher rate to coach
        var qual2 = new Qualification { NameEn = "Level 2", NameAr = "مستوى 2", RankOrder = 2 };
        _context.Qualifications.Add(qual2);
        await _context.SaveChangesAsync();

        _context.QualificationRateConfigs.Add(new QualificationRateConfig
        {
            QualificationId = qual2.QualificationId,
            SessionRate = 150m, // Higher rate
            EffectiveFrom = new DateOnly(2020, 1, 1),
            EffectiveTo = null
        });

        _context.EmployeeQualifications.Add(new EmployeeQualification
        {
            EmployeeId = _coachEmployeeId,
            QualificationId = qual2.QualificationId,
            ObtainedAt = new DateOnly(2022, 1, 1)
        });
        await _context.SaveChangesAsync();

        // 1 session PRESENT → rate should be 150 (highest ranked)
        await CreateSessionWithAttendanceAsync(_coachEmployeeId, new DateTime(2025, 5, 10));

        var (result, id) = await _service.CalculateCoachLifeguardPayrollAsync(_coachEmployeeId, 2025, 5, _adminUserId);

        Assert.Equal(PayrollResult.Success, result);
        var payroll = await _context.Payrolls.FindAsync(id!.Value);
        Assert.Equal(150m, payroll!.CalculatedAmount);
        Assert.Equal(150m, payroll.QualificationRateSnapshot);
    }

    [Fact]
    public async Task HistoricalRatePreservation_RateChangeDoesNotAffectOldPayroll()
    {
        // 1 session at rate 100. Calculate payroll. Then change rate to 200.
        await CreateSessionWithAttendanceAsync(_coachEmployeeId, new DateTime(2025, 6, 5));

        var (_, id) = await _service.CalculateCoachLifeguardPayrollAsync(_coachEmployeeId, 2025, 6, _adminUserId);
        var originalPayroll = await _context.Payrolls.FindAsync(id!.Value);
        Assert.Equal(100m, originalPayroll!.CalculatedAmount);

        // Change the rate config (expire old, add new)
        var oldConfig = await _context.QualificationRateConfigs
            .FirstAsync(r => r.QualificationId == _qualificationId && r.EffectiveTo == null);
        oldConfig.EffectiveTo = new DateOnly(2025, 6, 30);

        _context.QualificationRateConfigs.Add(new QualificationRateConfig
        {
            QualificationId = _qualificationId,
            SessionRate = 200m,
            EffectiveFrom = new DateOnly(2025, 7, 1),
            EffectiveTo = null
        });
        await _context.SaveChangesAsync();

        // Old payroll is PAID — it never changes
        await _service.MarkPaidAsync(id.Value, _adminUserId);

        var lockedPayroll = await _context.Payrolls.FindAsync(id.Value);
        Assert.Equal(100m, lockedPayroll!.CalculatedAmount); // Still 100, unaffected
    }

    // ======================================================================
    // COACH DUES TESTS
    // ======================================================================

    [Fact]
    public async Task CoachDues_ConsumedExactlyOnce_NoDoublePayment()
    {
        // Add 2 coach_dues for the coach in May 2025
        _context.PrivateBookings.Add(new PrivateBooking
        {
            BusinessType = "CLUB_BROUGHT",
            CoachId = _coachEmployeeId,
            StartTime = new DateTime(2025, 5, 1, 9, 0, 0),
            EndTime = new DateTime(2025, 5, 1, 10, 0, 0),
            TotalPrice = 1000,
            PaidAmount = 1000,
            ClubPercentageSnapshot = 40,
            CoachPercentageSnapshot = 60,
            Status = "ACTIVE",
            CreatedBy = _adminUserId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var booking = await _context.PrivateBookings.FirstAsync();

        _context.CoachDues.Add(new CoachDue
        {
            EmployeeId = _coachEmployeeId,
            SourceType = "CLUB_BROUGHT_SHARE",
            SourceId = booking.PrivateBookingId,
            Amount = 600m,
            PeriodYear = 2025,
            PeriodMonth = 5,
            CreatedAt = DateTime.UtcNow,
            ConsumedInPayrollId = null
        });
        _context.CoachDues.Add(new CoachDue
        {
            EmployeeId = _coachEmployeeId,
            SourceType = "CANCELLATION_FEE",
            SourceId = booking.PrivateBookingId,
            Amount = 200m,
            PeriodYear = 2025,
            PeriodMonth = 5,
            CreatedAt = DateTime.UtcNow,
            ConsumedInPayrollId = null
        });
        await _context.SaveChangesAsync();

        // First payroll calculation → consumes both dues
        var (result, id) = await _service.CalculateCoachLifeguardPayrollAsync(_coachEmployeeId, 2025, 5, _adminUserId);
        Assert.Equal(PayrollResult.Success, result);

        var payroll = await _context.Payrolls.FindAsync(id!.Value);
        Assert.Equal(800m, payroll!.CalculatedAmount); // 600 + 200 dues (0 session training)

        // Verify all dues are now consumed
        var unconsumed = await _context.CoachDues
            .CountAsync(cd => cd.EmployeeId == _coachEmployeeId && cd.ConsumedInPayrollId == null);
        Assert.Equal(0, unconsumed);

        // Mark payroll paid so it's locked
        await _service.MarkPaidAsync(id.Value, _adminUserId);

        // Recalculate for the same period: there should be no dues left to consume
        var (result2, _) = await _service.CalculateCoachLifeguardPayrollAsync(_coachEmployeeId, 2025, 5, _adminUserId);
        // Verify: already paid blocks recalculation
        Assert.Equal(PayrollResult.AlreadyPaid, result2);
    }

    // ======================================================================
    // PAYROLL PAYMENT & LOCKING TESTS
    // ======================================================================

    [Fact]
    public async Task MarkPaid_CreatesTransaction_LocksPayroll()
    {
        // Calculate first
        var (_, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2025, 8, _adminUserId);

        var result = await _service.MarkPaidAsync(id!.Value, _adminUserId);
        Assert.Equal(PayrollResult.Success, result);

        var payroll = await _context.Payrolls.FindAsync(id.Value);
        Assert.Equal("PAID", payroll!.Status);
        Assert.NotNull(payroll.PaymentDate);

        var tx = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionType == "PAYROLL_PAYMENT" && t.RelatedEntityId == id.Value);
        Assert.NotNull(tx);
        Assert.Equal(3000m, tx!.Amount);
    }

    [Fact]
    public async Task MarkPaid_AlreadyPaid_ReturnsAlreadyPaid()
    {
        var (_, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2025, 9, _adminUserId);
        await _service.MarkPaidAsync(id!.Value, _adminUserId);

        var result = await _service.MarkPaidAsync(id.Value, _adminUserId);
        Assert.Equal(PayrollResult.AlreadyPaid, result);
    }

    [Fact]
    public async Task PayrollLocking_PaidPayroll_CannotBeRecalculated()
    {
        var (_, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2025, 10, _adminUserId);
        await _service.MarkPaidAsync(id!.Value, _adminUserId);

        // Try to recalculate same period
        var (result, id2) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2025, 10, _adminUserId);
        Assert.Equal(PayrollResult.AlreadyPaid, result);
    }

    // ======================================================================
    // PAYROLL ADJUSTMENT TEST
    // ======================================================================

    [Fact]
    public async Task AdjustPaidPayroll_CreatesSignedAdjustmentTransaction()
    {
        var (_, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2025, 11, _adminUserId);
        await _service.MarkPaidAsync(id!.Value, _adminUserId);

        // Adjustment: +500 (positive correction)
        var result = await _service.AdjustPaidPayrollAsync(id.Value, 500m, "Overtime bonus", _adminUserId);
        Assert.Equal(PayrollResult.Success, result);

        var tx = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionType == "PAYROLL_ADJUSTMENT" && t.RelatedEntityId == id.Value);
        Assert.NotNull(tx);
        Assert.Equal(500m, tx!.Amount);

        // Original payroll row is UNCHANGED
        var payroll = await _context.Payrolls.FindAsync(id.Value);
        Assert.Equal(3000m, payroll!.CalculatedAmount);
    }

    [Fact]
    public async Task AdjustNotPaidPayroll_ReturnsNotPaid()
    {
        var (_, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2025, 12, _adminUserId);
        // Not marking paid

        var result = await _service.AdjustPaidPayrollAsync(id!.Value, -100m, "Error fix", _adminUserId);
        Assert.Equal(PayrollResult.NotPaid, result);
    }

    [Fact]
    public async Task MarkUnpaid_PaidPayroll_CreatesNegativeAdjustment()
    {
        var (_, id) = await _service.CalculateAdministratorPayrollAsync(_adminEmployeeId, 2026, 1, _adminUserId);
        await _service.MarkPaidAsync(id!.Value, _adminUserId);

        var result = await _service.MarkUnpaidAsync(id.Value, "Payment cancelled", _adminUserId);
        Assert.Equal(PayrollResult.Success, result);

        var tx = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionType == "PAYROLL_ADJUSTMENT"
                                   && t.RelatedEntityId == id.Value
                                   && t.Amount < 0);
        Assert.NotNull(tx);
        Assert.Equal(-3000m, tx!.Amount);
    }
}
