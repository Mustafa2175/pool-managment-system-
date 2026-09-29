using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Private;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Infrastructure.Private;
using SwimClub.Infrastructure.Security;
using SwimClub.Infrastructure.Logging;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class PrivateBookingTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private IPrivateBookingService _service = null!;
    private int _actorId;

    public async Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"swimclub-pb-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var currentUser = new CurrentUserService();
        var audit = new AuditLogService(_context, currentUser);
        
        // Mock config
        var config = new SwimClub.Infrastructure.Configuration.SystemConfigurationService(_context);
        _service = new PrivateBookingService(_context, audit, config);

        _actorId = await SeedBaseDataAsync();

        // Seed system settings
        await config.SetSettingAsync("Private.SessionCount", "4");
        await config.SetSettingAsync("Private.LaneRentalFee", "1000");
        await config.SetSettingAsync("Private.CoachBroughtFeePerSwimmer", "200");
        await config.SetSettingAsync("Private.ClubBroughtClubPct", "40");
        await config.SetSettingAsync("Private.ClubBroughtCoachPct", "60");
        
        // Seed Cancellation fee config
        _context.CancellationFeeConfigs.Add(new CancellationFeeConfig
        {
            FeePercentage = 20,
            EffectiveFrom = DateOnly.MinValue
        });
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private async Task<int> SeedBaseDataAsync()
    {
        var role = new Role { Code = "SUPER_ADMIN", NameEn = "SA", NameAr = "SA" };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var user = new User { Username = "sa", PasswordHash = "hash", RoleId = role.RoleId, IsActive = true };
        _context.Users.Add(user);
        
        var coach = new Employee { Name = "C", EmployeeType = "COACH", Status = "ACTIVE", NationalId = "1" };
        _context.Employees.Add(coach);
        
        var lane = new Lane { Label = "Lane 1", Capacity = 5, Status = "ACTIVE" };
        _context.Lanes.Add(lane);

        var swimmer = new Swimmer { SwimmerId = "SW-1", Name = "S1", DateOfBirth = new DateOnly(2000,1,1), Gender = "MALE", MemberStatus = "MEMBER", Status = "ACTIVE", QrToken = "QR123" };
        _context.Swimmers.Add(swimmer);

        await _context.SaveChangesAsync();
        return user.UserId;
    }

    private async Task<int> GetCoachId() => (await _context.Employees.FirstAsync()).EmployeeId;
    private async Task<int> GetLaneId() => (await _context.Lanes.FirstAsync()).LaneId;

    [Fact]
    public async Task LaneRental_CreatesBooking_AndSessions()
    {
        var participants = new[] { new ParticipantDto("GUEST", null, "Guest", "123") };
        var start = DateTime.UtcNow.AddDays(1);
        
        var res = await _service.CreateLaneRentalAsync(
            await GetCoachId(), await GetLaneId(), start, start.AddHours(1), DateOnly.FromDateTime(start),
            1000, participants, _actorId);

        Assert.Equal(PrivateBookingResult.Success, res.Result);
        Assert.NotNull(res.BookingId);

        var booking = await _context.PrivateBookings.Include(b => b.Sessions).Include(b => b.Participants).FirstAsync(b => b.PrivateBookingId == res.BookingId);
        Assert.Equal("LANE_RENTAL", booking.BusinessType);
        Assert.Equal(1000m, booking.TotalPrice);
        Assert.Equal(4, booking.Sessions.Count);
        Assert.Single(booking.Participants);
    }

    [Fact]
    public async Task CoachBrought_ClubRevenue_PerSwimmer()
    {
        var participants = new[] { 
            new ParticipantDto("SWIMMER", "SW-1", null, null),
            new ParticipantDto("GUEST", null, "G2", "12")
        };
        var start = DateTime.UtcNow.AddDays(1);
        
        var res = await _service.CreateCoachBroughtAsync(
            await GetCoachId(), start, start.AddHours(1), DateOnly.FromDateTime(start),
            400, participants, _actorId);

        Assert.Equal(PrivateBookingResult.Success, res.Result);

        var booking = await _context.PrivateBookings.FirstAsync(b => b.PrivateBookingId == res.BookingId);
        Assert.Equal(400m, booking.TotalPrice); // 2 * 200
        Assert.Equal(200m, booking.ClubFeePerSwimmerSnapshot);
    }

    [Fact]
    public async Task ClubBrought_CreatesCoachDue()
    {
        var participants = new[] { new ParticipantDto("GUEST", null, "Guest", "123") };
        var start = DateTime.UtcNow.AddDays(1);
        
        var res = await _service.CreateClubBroughtAsync(
            await GetCoachId(), start, start.AddHours(1), DateOnly.FromDateTime(start),
            1000, 1000, participants, _actorId);

        Assert.Equal(PrivateBookingResult.Success, res.Result);

        var dues = await _context.CoachDues.Where(cd => cd.SourceId == res.BookingId).ToListAsync();
        Assert.Single(dues);
        Assert.Equal("CLUB_BROUGHT_SHARE", dues[0].SourceType);
        Assert.Equal(600m, dues[0].Amount); // 60% of 1000
    }

    [Fact]
    public async Task LaneConflict_ReturnsError()
    {
        var p = new[] { new ParticipantDto("GUEST", null, "G", "") };
        var start = DateTime.UtcNow.AddDays(1);
        var laneId = await GetLaneId();
        
        // Coach 1 uses Lane 1
        await _service.CreateLaneRentalAsync(await GetCoachId(), laneId, start, start.AddHours(1), DateOnly.FromDateTime(start), 1000, p, _actorId);

        // Coach 2 uses Lane 1 at same time
        var coach2 = new Employee { Name = "C2", EmployeeType = "COACH", Status = "ACTIVE", NationalId = "2" };
        _context.Employees.Add(coach2);
        await _context.SaveChangesAsync();

        var res2 = await _service.CreateLaneRentalAsync(coach2.EmployeeId, laneId, start, start.AddHours(1), DateOnly.FromDateTime(start), 1000, p, _actorId);
        Assert.Equal(PrivateBookingResult.LaneConflict, res2.Result);
    }

    [Fact]
    public async Task CoachConflict_ReturnsError()
    {
        var p = new[] { new ParticipantDto("GUEST", null, "G", "") };
        var start = DateTime.UtcNow.AddDays(1);
        var coachId = await GetCoachId();
        
        await _service.CreateCoachBroughtAsync(coachId, start, start.AddHours(1), DateOnly.FromDateTime(start), 200, p, _actorId);

        var lane2 = new Lane { Label = "L2", Capacity = 5, Status = "ACTIVE" };
        _context.Lanes.Add(lane2);
        await _context.SaveChangesAsync();

        var res2 = await _service.CreateLaneRentalAsync(coachId, lane2.LaneId, start, start.AddHours(1), DateOnly.FromDateTime(start), 1000, p, _actorId);
        Assert.Equal(PrivateBookingResult.CoachConflict, res2.Result);
    }

    [Fact]
    public async Task Attendance_MarksSessionCompleted()
    {
        var p = new[] { new ParticipantDto("GUEST", null, "G", "") };
        var start = DateTime.UtcNow.AddDays(1);
        var res = await _service.CreateCoachBroughtAsync(await GetCoachId(), start, start.AddHours(1), DateOnly.FromDateTime(start), 200, p, _actorId);

        var session = await _context.PrivateSessions.FirstAsync(s => s.PrivateBookingId == res.BookingId);
        var participant = await _context.PrivateBookingParticipants.FirstAsync(pa => pa.PrivateBookingId == res.BookingId);

        var attRes = await _service.RecordAttendanceAsync(session.PrivateSessionId, participant.ParticipantId.ToString(), true, _actorId);
        Assert.Equal(PrivateBookingResult.Success, attRes);

        var updatedSession = await _context.PrivateSessions.FindAsync(session.PrivateSessionId);
        Assert.Equal("COMPLETED", updatedSession!.Status);
    }

    [Fact]
    public async Task Cancel_BeforeFirstSession_FullRefund()
    {
        var p = new[] { new ParticipantDto("GUEST", null, "G", "") };
        var start = DateTime.UtcNow.AddDays(1);
        var res = await _service.CreateLaneRentalAsync(await GetCoachId(), await GetLaneId(), start, start.AddHours(1), DateOnly.FromDateTime(start), 1000, p, _actorId);

        await _service.CancelBookingAsync(res.BookingId!.Value, _actorId);

        var booking = await _context.PrivateBookings.FindAsync(res.BookingId.Value);
        Assert.Equal("CANCELLED", booking!.Status);

        var refund = await _context.Refunds.Include(r => r.LinkedTransaction).FirstAsync(r => r.RelatedEntityId == res.BookingId.Value);
        Assert.Equal(1000m, refund.Amount);
        Assert.NotNull(refund.LinkedTransaction);
        Assert.Equal(-1000m, refund.LinkedTransaction.Amount);
    }

    [Fact]
    public async Task Cancel_LaneRental_After1Session_50PercentRefund()
    {
        var p = new[] { new ParticipantDto("GUEST", null, "G", "") };
        var start = DateTime.UtcNow.AddDays(1);
        var res = await _service.CreateLaneRentalAsync(await GetCoachId(), await GetLaneId(), start, start.AddHours(1), DateOnly.FromDateTime(start), 1000, p, _actorId);

        var session = await _context.PrivateSessions.FirstAsync(s => s.PrivateBookingId == res.BookingId);
        var participant = await _context.PrivateBookingParticipants.FirstAsync();
        
        await _service.RecordAttendanceAsync(session.PrivateSessionId, participant.ParticipantId.ToString(), true, _actorId);

        await _service.CancelBookingAsync(res.BookingId!.Value, _actorId);

        var refund = await _context.Refunds.FirstAsync(r => r.RelatedEntityId == res.BookingId.Value);
        Assert.Equal(500m, refund.Amount);
    }

    [Fact]
    public async Task Cancel_ClubBrought_After1Session_FeeDeducted()
    {
        var p = new[] { new ParticipantDto("GUEST", null, "G", "") };
        var start = DateTime.UtcNow.AddDays(1);
        var res = await _service.CreateClubBroughtAsync(await GetCoachId(), start, start.AddHours(1), DateOnly.FromDateTime(start), 1000, 1000, p, _actorId);

        var session = await _context.PrivateSessions.FirstAsync(s => s.PrivateBookingId == res.BookingId);
        var participant = await _context.PrivateBookingParticipants.FirstAsync();
        
        await _service.RecordAttendanceAsync(session.PrivateSessionId, participant.ParticipantId.ToString(), true, _actorId);

        await _service.CancelBookingAsync(res.BookingId!.Value, _actorId);

        // Paid 1000. Fee is 20%. Refund = 800. Coach receives 200 via CoachDue(CANCELLATION_FEE).
        var refund = await _context.Refunds.FirstAsync(r => r.RelatedEntityId == res.BookingId.Value);
        Assert.Equal(800m, refund.Amount);

        var dues = await _context.CoachDues.Where(cd => cd.SourceId == res.BookingId).ToListAsync();
        Assert.Equal(3, dues.Count); // 1 original CLUB_BROUGHT_SHARE(600), 1 offsetting(-600), 1 CANCELLATION_FEE(200)

        var cancelDue = dues.First(d => d.SourceType == "CANCELLATION_FEE");
        Assert.Equal(200m, cancelDue.Amount);

        var offsets = dues.Where(d => d.SourceType == "CLUB_BROUGHT_SHARE").Sum(d => d.Amount);
        Assert.Equal(0m, offsets); // They cancel out (600 and -600)
    }
}
