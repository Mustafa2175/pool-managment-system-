using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Recreational;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Infrastructure.Recreational;
using SwimClub.Infrastructure.Security;
using SwimClub.Infrastructure.Logging;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class RecreationalServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private IRecreationalService _service = null!;
    private int _actorId;

    public async Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"swimclub-rec-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var currentUser = new CurrentUserService();
        var audit = new AuditLogService(_context, currentUser);
        _service = new RecreationalService(_context, audit);

        _actorId = await SeedBaseDataAsync();
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
        return user.UserId;
    }

    [Fact]
    public async Task CreatePeriod_Succeeds()
    {
        var periodId = await _service.CreatePeriodAsync("Morning Swim", new TimeOnly(8, 0), new TimeOnly(9, 0), 20, _actorId);
        
        var period = await _context.RecreationalPeriods.FindAsync(periodId);
        Assert.NotNull(period);
        Assert.Equal("Morning Swim", period.Name);
        Assert.Equal(20, period.Capacity);
    }

    [Fact]
    public async Task SingleEntry_WithinWindow_Succeeds_AndCreatesTransaction()
    {
        // 30 mins before to 30 mins after start time
        var now = DateTime.UtcNow;
        var startTime = TimeOnly.FromDateTime(now.AddMinutes(15)); // Starts in 15 mins (within the 30 min window before)
        
        var periodId = await _service.CreatePeriodAsync("Test", startTime, startTime.AddHours(1), 10, _actorId);

        var res = await _service.RecordSingleEntryAsync(periodId, "John Doe", "MEMBER", 150, "Cash", _actorId);
        
        Assert.Equal(RecreationalResult.Success, res.Result);
        Assert.NotNull(res.TicketId);

        var ticket = await _context.RecreationalTickets.FindAsync(res.TicketId);
        Assert.Equal("John Doe", ticket!.Name);
        Assert.Equal(150m, ticket.AmountPaid);

        var tx = await _context.Transactions.FirstAsync(t => t.RelatedEntityType == "RECREATIONAL_TICKET" && t.RelatedEntityId == res.TicketId);
        Assert.Equal("RECREATIONAL_TICKET_PAYMENT", tx.TransactionType);
        Assert.Equal(150m, tx.Amount);
    }

    [Fact]
    public async Task SingleEntry_OutsideWindow_Rejects()
    {
        var now = DateTime.UtcNow;
        var startTime = TimeOnly.FromDateTime(now.AddMinutes(40)); // Starts in 40 mins (outside the 30 min window before)
        
        var periodId = await _service.CreatePeriodAsync("Test", startTime, startTime.AddHours(1), 10, _actorId);

        var res = await _service.RecordSingleEntryAsync(periodId, "John Doe", "MEMBER", 150, "Cash", _actorId);
        
        Assert.Equal(RecreationalResult.OutsideCheckInWindow, res.Result);
    }

    [Fact]
    public async Task SingleEntry_DuplicateName_SameDay_Rejects()
    {
        var now = DateTime.UtcNow;
        var startTime = TimeOnly.FromDateTime(now);
        var periodId = await _service.CreatePeriodAsync("Test", startTime, startTime.AddHours(1), 10, _actorId);

        var res1 = await _service.RecordSingleEntryAsync(periodId, "John Doe", "MEMBER", 150, "Cash", _actorId);
        Assert.Equal(RecreationalResult.Success, res1.Result);

        var res2 = await _service.RecordSingleEntryAsync(periodId, "john doe", "MEMBER", 150, "Cash", _actorId); // case insensitive
        Assert.Equal(RecreationalResult.DuplicateCheckIn, res2.Result);
    }

    [Fact]
    public async Task Capacity_SharedBetween_SingleEntry_And_Package()
    {
        var now = DateTime.UtcNow;
        var startTime = TimeOnly.FromDateTime(now);
        var periodId = await _service.CreatePeriodAsync("Test", startTime, startTime.AddHours(1), 2, _actorId); // Capacity 2

        // 1 Single Entry
        var res1 = await _service.RecordSingleEntryAsync(periodId, "John Doe", "MEMBER", 150, "Cash", _actorId);
        Assert.Equal(RecreationalResult.Success, res1.Result);

        // 1 Package CheckIn
        var swimmer = new Swimmer { SwimmerId = "SW-1", Name = "S1", DateOfBirth = new DateOnly(2000,1,1), Gender = "MALE", MemberStatus = "MEMBER", QrToken = "QR_SW1", Status = "ACTIVE" };
        _context.Swimmers.Add(swimmer);
        
        var package = new Package 
        {
            SwimmerId = "SW-1",
            PackageType = "RECREATIONAL",
            RecreationalPeriodId = periodId,
            StartDate = DateOnly.FromDateTime(now.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(now.AddDays(30)),
            AvailableSessionsTotal = 10,
            AvailableSessionsRemaining = 10,
            QrToken = "PKG_QR",
            Status = "ACTIVE",
            CreatedBy = _actorId,
            CreatedAt = now
        };
        _context.Packages.Add(package);
        await _context.SaveChangesAsync();

        var res2 = await _service.RecordPackageCheckInAsync(periodId, "PKG_QR", _actorId);
        Assert.Equal(RecreationalResult.Success, res2);

        // Now capacity is full. Next single entry should fail.
        var res3 = await _service.RecordSingleEntryAsync(periodId, "Jane Doe", "NON_MEMBER", 200, "Cash", _actorId);
        Assert.Equal(RecreationalResult.CapacityFull, res3.Result);
    }

    [Fact]
    public async Task PackageCheckIn_DecrementsSessions()
    {
        var now = DateTime.UtcNow;
        var startTime = TimeOnly.FromDateTime(now);
        var periodId = await _service.CreatePeriodAsync("Test", startTime, startTime.AddHours(1), 10, _actorId);
        
        var swimmer = new Swimmer { SwimmerId = "SW-1", Name = "S1", DateOfBirth = new DateOnly(2000,1,1), Gender = "MALE", MemberStatus = "MEMBER", QrToken = "QR_SW1", Status = "ACTIVE" };
        _context.Swimmers.Add(swimmer);
        
        var package = new Package 
        {
            SwimmerId = "SW-1",
            PackageType = "RECREATIONAL",
            RecreationalPeriodId = periodId,
            StartDate = DateOnly.FromDateTime(now.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(now.AddDays(30)),
            AvailableSessionsTotal = 10,
            AvailableSessionsRemaining = 10,
            QrToken = "PKG_QR_2",
            Status = "ACTIVE",
            CreatedBy = _actorId,
            CreatedAt = now
        };
        _context.Packages.Add(package);
        await _context.SaveChangesAsync();

        var res = await _service.RecordPackageCheckInAsync(periodId, "PKG_QR_2", _actorId);
        Assert.Equal(RecreationalResult.Success, res);

        var updatedPkg = await _context.Packages.FindAsync(package.PackageId);
        Assert.Equal(9, updatedPkg!.AvailableSessionsRemaining);
    }

    [Fact]
    public async Task PackageCheckIn_WrongPeriod_Rejects()
    {
        var now = DateTime.UtcNow;
        var startTime = TimeOnly.FromDateTime(now);
        var periodId1 = await _service.CreatePeriodAsync("Test1", startTime, startTime.AddHours(1), 10, _actorId);
        var periodId2 = await _service.CreatePeriodAsync("Test2", startTime, startTime.AddHours(1), 10, _actorId);
        
        var swimmer = new Swimmer { SwimmerId = "SW-1", Name = "S1", DateOfBirth = new DateOnly(2000,1,1), Gender = "MALE", MemberStatus = "MEMBER", QrToken = "QR_SW1", Status = "ACTIVE" };
        _context.Swimmers.Add(swimmer);
        
        var package = new Package 
        {
            SwimmerId = "SW-1",
            PackageType = "RECREATIONAL",
            RecreationalPeriodId = periodId1, // Belongs to period 1
            StartDate = DateOnly.FromDateTime(now.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(now.AddDays(30)),
            AvailableSessionsTotal = 10,
            AvailableSessionsRemaining = 10,
            QrToken = "PKG_QR_3",
            Status = "ACTIVE",
            CreatedBy = _actorId,
            CreatedAt = now
        };
        _context.Packages.Add(package);
        await _context.SaveChangesAsync();

        // Check into period 2 -> Wrong period
        var res = await _service.RecordPackageCheckInAsync(periodId2, "PKG_QR_3", _actorId);
        Assert.Equal(RecreationalResult.WrongPeriod, res);
    }
}
