using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Training;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Configuration;
using SwimClub.Infrastructure.Logging;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Infrastructure.Security;
using SwimClub.Infrastructure.Training;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class TrainingTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private ITrainingConfigService _configService = null!;
    private ITrainingPeriodService _periodService = null!;
    private ITrainingSubscriptionService _subscriptionService = null!;

    private AppDbContext CreateContext()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"swimclub-tr-test-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public async Task InitializeAsync()
    {
        _context = CreateContext();
        var sysConfig = new SystemConfigurationService(_context);
        var auditLog = new AuditLogService(_context, new CurrentUserService());

        _configService = new TrainingConfigService(_context, sysConfig, auditLog);
        _periodService = new TrainingPeriodService(_context, auditLog, new CurrentUserService());
        _subscriptionService = new TrainingSubscriptionService(_context, _configService, auditLog);

        await SeedBaseDataAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private async Task SeedBaseDataAsync()
    {
        // Roles
        _context.Roles.AddRange(
            new Role { Code = "SUPER_ADMIN", NameEn = "Super Admin", NameAr = "Super Admin" },
            new Role { Code = "ADMINISTRATOR", NameEn = "Admin", NameAr = "Admin" }
        );
        await _context.SaveChangesAsync();

        var saRole = await _context.Roles.FirstAsync(r => r.Code == "SUPER_ADMIN");
        var adminRole = await _context.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");

        // Users
        _context.Users.Add(new User { UserId = 1, Username = "sa", PasswordHash = "x", RoleId = saRole.RoleId });
        _context.Users.Add(new User { UserId = 2, Username = "admin", PasswordHash = "x", RoleId = adminRole.RoleId });
        await _context.SaveChangesAsync();

        // Employees (Coach)
        _context.Employees.Add(new Employee { EmployeeId = 1, Name = "Coach A", EmployeeType = "COACH", Status = "ACTIVE", NationalId = "1" });
        _context.Employees.Add(new Employee { EmployeeId = 2, Name = "Coach B", EmployeeType = "COACH", Status = "ACTIVE", NationalId = "2" });
        await _context.SaveChangesAsync();

        // Programs
        _context.Programs.Add(new Program { ProgramId = 1, Name = "Regular", ProgramType = "REGULAR" });
        await _context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Period Creation & Validation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePeriod_RequiresSuperAdmin()
    {
        var (res, _) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 1 }, 2); // user 2 is Admin
        Assert.Equal(TrainingPeriodResult.Unauthorized, res);

        var (res2, pid) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 1 }, 1); // user 1 is SA
        Assert.Equal(TrainingPeriodResult.Success, res2);
        Assert.NotNull(pid);
    }

    [Fact]
    public async Task CreatePeriod_ValidatesSchedule()
    {
        // End time before start
        var (res, _) = await _periodService.CreatePeriodAsync(1, new TimeOnly(11,0), new TimeOnly(10,0), 10, new[] { 1 }, 1);
        Assert.Equal(TrainingPeriodResult.InvalidSchedule, res);

        // Invalid day
        var (res2, _) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 8 }, 1);
        Assert.Equal(TrainingPeriodResult.InvalidSchedule, res2);

        // Zero capacity
        var (res3, _) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 0, new[] { 1 }, 1);
        Assert.Equal(TrainingPeriodResult.InvalidCapacity, res3);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Staff Overlap Validation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EditPeriod_PreventsCoachOverlap()
    {
        var (_, p1) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(12,0), 10, new[] { 1, 3 }, 1); // Mon, Wed 10-12
        var (_, p2) = await _periodService.CreatePeriodAsync(1, new TimeOnly(11,0), new TimeOnly(13,0), 10, new[] { 1 }, 1); // Mon 11-13 (overlaps)

        // Assign Coach 1 to P1
        await _periodService.EditPeriodAsync(p1!.Value, new TimeOnly(10,0), new TimeOnly(12,0), 10, new[] { 1, 3 }, new[] { 1 }, Array.Empty<int>(), 1);

        // Try assign Coach 1 to P2
        var res = await _periodService.EditPeriodAsync(p2!.Value, new TimeOnly(11,0), new TimeOnly(13,0), 10, new[] { 1 }, new[] { 1 }, Array.Empty<int>(), 1);
        Assert.Equal(TrainingPeriodResult.StaffOverlap, res);

        // But P3 on Tuesday is fine
        var (_, p3) = await _periodService.CreatePeriodAsync(1, new TimeOnly(11,0), new TimeOnly(13,0), 10, new[] { 2 }, 1);
        var res3 = await _periodService.EditPeriodAsync(p3!.Value, new TimeOnly(11,0), new TimeOnly(13,0), 10, new[] { 2 }, new[] { 1 }, Array.Empty<int>(), 1);
        Assert.Equal(TrainingPeriodResult.Success, res3);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Pricing
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pricing_HistoricalResolution()
    {
        await _configService.UpdatePriceAsync(1, "MEMBER", 500m);
        var price1 = await _configService.GetActivePriceAsync(1, "MEMBER");
        Assert.Equal(500m, price1);

        await _configService.UpdatePriceAsync(1, "MEMBER", 600m);
        var price2 = await _configService.GetActivePriceAsync(1, "MEMBER");
        Assert.Equal(600m, price2);

        // Non-member should be 0 because we didn't set it yet
        var price3 = await _configService.GetActivePriceAsync(1, "NON_MEMBER");
        Assert.Equal(0m, price3);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Subscription Creation & Validation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Subscription_RequiresFutureOrTodayStartDateMatchingSchedule()
    {
        var (_, p1) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 1, 3 }, 1); // Mon(1), Wed(3)

        _context.Swimmers.Add(new Swimmer { SwimmerId = "SW-1", Name = "A", Gender = "MALE", MemberStatus = "MEMBER", QrToken = "Q1" });
        await _context.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Find a future Monday (DayOfWeek = 1)
        var futureMonday = today;
        while ((int)futureMonday.DayOfWeek != 1) futureMonday = futureMonday.AddDays(1);

        var (res, _) = await _subscriptionService.CreateSubscriptionAsync("SW-1", 1, p1!.Value, futureMonday, 0, null, null, null, null, 1);
        Assert.Equal(SubscriptionResult.Success, res);

        // Find a future Tuesday (DayOfWeek = 2) - Should fail matching
        var futureTuesday = today;
        while ((int)futureTuesday.DayOfWeek != 2) futureTuesday = futureTuesday.AddDays(1);

        var (res2, _) = await _subscriptionService.CreateSubscriptionAsync("SW-1", 1, p1!.Value, futureTuesday, 0, null, null, null, null, 1);
        Assert.Equal(SubscriptionResult.InvalidStartDate, res2);
    }

    [Fact]
    public async Task Subscription_PreventsOverpayment()
    {
        var (_, p1) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 1, 3, 5 }, 1);
        _context.Swimmers.Add(new Swimmer { SwimmerId = "SW-2", Name = "B", Gender = "MALE", MemberStatus = "MEMBER", QrToken = "Q2" });
        await _context.SaveChangesAsync();

        await _configService.UpdatePriceAsync(1, "MEMBER", 500m);

        var nextValidDay = GetNextValidDay(new[] { 1, 3, 5 });
        var (res, _) = await _subscriptionService.CreateSubscriptionAsync("SW-2", 1, p1!.Value, nextValidDay, 600m, null, null, null, null, 1);
        Assert.Equal(SubscriptionResult.Overpayment, res);
    }

    [Fact]
    public async Task Subscription_PreventsDuplicateOnSamePeriod()
    {
        var (_, p1) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 1, 3, 5 }, 1);
        _context.Swimmers.Add(new Swimmer { SwimmerId = "SW-3", Name = "C", Gender = "MALE", MemberStatus = "MEMBER", QrToken = "Q3" });
        await _context.SaveChangesAsync();

        var nextValidDay = GetNextValidDay(new[] { 1, 3, 5 });
        await _subscriptionService.CreateSubscriptionAsync("SW-3", 1, p1!.Value, nextValidDay, 0, null, null, null, null, 1);
        
        var (res, _) = await _subscriptionService.CreateSubscriptionAsync("SW-3", 1, p1!.Value, nextValidDay, 0, null, null, null, null, 1);
        Assert.Equal(SubscriptionResult.DuplicateSubscription, res);
    }

    [Fact]
    public async Task Subscription_PreventsScheduleOverlap()
    {
        // Period 1: Mon/Wed 10-12
        var (_, p1) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(12,0), 10, new[] { 1, 3 }, 1);
        // Period 2: Mon/Wed 11-13
        var (_, p2) = await _periodService.CreatePeriodAsync(1, new TimeOnly(11,0), new TimeOnly(13,0), 10, new[] { 1, 3 }, 1);
        
        _context.Swimmers.Add(new Swimmer { SwimmerId = "SW-4", Name = "D", Gender = "MALE", MemberStatus = "MEMBER", QrToken = "Q4" });
        await _context.SaveChangesAsync();

        var nextMon = GetNextValidDay(new[] { 1 });
        await _subscriptionService.CreateSubscriptionAsync("SW-4", 1, p1!.Value, nextMon, 0, null, null, null, null, 1);
        
        var (res, _) = await _subscriptionService.CreateSubscriptionAsync("SW-4", 1, p2!.Value, nextMon, 0, null, null, null, null, 1);
        Assert.Equal(SubscriptionResult.ScheduleOverlap, res);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Session Generation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Subscription_GeneratesSessionsCorrectly()
    {
        await _configService.SetGlobalSessionCountAsync(4); // 4 sessions for easy math
        var (_, p1) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 1, 3 }, 1); // Mon, Wed
        _context.Swimmers.Add(new Swimmer { SwimmerId = "SW-5", Name = "E", Gender = "MALE", MemberStatus = "MEMBER", QrToken = "Q5" });
        await _context.SaveChangesAsync();

        var nextMon = GetNextValidDay(new[] { 1 });
        var (_, subId) = await _subscriptionService.CreateSubscriptionAsync("SW-5", 1, p1!.Value, nextMon, 0, null, null, null, null, 1);

        var sessions = await _context.Sessions.Where(s => s.SubscriptionId == subId).OrderBy(s => s.ScheduledStartTime).ToListAsync();
        Assert.Equal(4, sessions.Count);

        // Should be Mon, Wed, Mon, Wed
        Assert.Equal(DayOfWeek.Monday, sessions[0].ScheduledStartTime.DayOfWeek);
        Assert.Equal(DayOfWeek.Wednesday, sessions[1].ScheduledStartTime.DayOfWeek);
        Assert.Equal(DayOfWeek.Monday, sessions[2].ScheduledStartTime.DayOfWeek);
        Assert.Equal(DayOfWeek.Wednesday, sessions[3].ScheduledStartTime.DayOfWeek);

        // The subscription EndDate should match the last session's date
        var sub = await _context.TrainingSubscriptions.FindAsync(subId);
        Assert.Equal(DateOnly.FromDateTime(sessions[3].ScheduledStartTime), sub!.EndDate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Future Schedule Change
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EditPeriod_RegeneratesFutureSessions()
    {
        await _configService.SetGlobalSessionCountAsync(4);
        var (_, p1) = await _periodService.CreatePeriodAsync(1, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 1 }, 1); // Mon
        _context.Swimmers.Add(new Swimmer { SwimmerId = "SW-6", Name = "F", Gender = "MALE", MemberStatus = "MEMBER", QrToken = "Q6" });
        await _context.SaveChangesAsync();

        var nextMon = GetNextValidDay(new[] { 1 });
        var (_, subId) = await _subscriptionService.CreateSubscriptionAsync("SW-6", 1, p1!.Value, nextMon, 0, null, null, null, null, 1);

        // Simulate 1 session completed in the past
        var sessions = await _context.Sessions.Where(s => s.SubscriptionId == subId).OrderBy(s => s.ScheduledStartTime).ToListAsync();
        sessions[0].Status = "COMPLETED";
        sessions[0].ScheduledStartTime = DateTime.UtcNow.AddDays(-7);
        await _context.SaveChangesAsync();

        // Admin changes period to Tuesday (Day 2)
        await _periodService.EditPeriodAsync(p1!.Value, new TimeOnly(10,0), new TimeOnly(11,0), 10, new[] { 2 }, Array.Empty<int>(), Array.Empty<int>(), 1);

        // Fetch new sessions
        var updatedSessions = await _context.Sessions.Where(s => s.SubscriptionId == subId).OrderBy(s => s.ScheduledStartTime).ToListAsync();
        
        Assert.Equal(4, updatedSessions.Count);
        // The first one is completed and should still be DayOfWeek.Monday (or whatever we modified it to in the past)
        Assert.Equal("COMPLETED", updatedSessions[0].Status);
        
        // The remaining 3 should be re-scheduled to Tuesday
        Assert.Equal(DayOfWeek.Tuesday, updatedSessions[1].ScheduledStartTime.DayOfWeek);
        Assert.Equal(DayOfWeek.Tuesday, updatedSessions[2].ScheduledStartTime.DayOfWeek);
        Assert.Equal(DayOfWeek.Tuesday, updatedSessions[3].ScheduledStartTime.DayOfWeek);
    }

    private DateOnly GetNextValidDay(int[] allowedDays)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)); // Start from tomorrow to be safe
        while (!allowedDays.Contains((int)date.DayOfWeek))
        {
            date = date.AddDays(1);
        }
        return date;
    }
}
