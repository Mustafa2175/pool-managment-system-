using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;
using SwimClub.Application.Training;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Training;

public class TrainingPeriodService : ITrainingPeriodService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ICurrentUserService _currentUserService;

    public TrainingPeriodService(AppDbContext context, IAuditLogService auditLog, ICurrentUserService currentUserService)
    {
        _context = context;
        _auditLog = auditLog;
        _currentUserService = currentUserService;
    }

    public async Task<(TrainingPeriodResult Result, int? PeriodId)> CreatePeriodAsync(
        int programId,
        TimeOnly startTime,
        TimeOnly endTime,
        int capacity,
        IEnumerable<int> daysOfWeek,
        int actorUserId)
    {
        // 1. Validate Schedule and Capacity
        if (endTime <= startTime) return (TrainingPeriodResult.InvalidSchedule, null);
        if (capacity <= 0) return (TrainingPeriodResult.InvalidCapacity, null);

        var daysList = daysOfWeek.Distinct().ToList();
        if (!daysList.Any() || daysList.Any(d => d < 0 || d > 6)) return (TrainingPeriodResult.InvalidSchedule, null);

        var program = await _context.Programs.FindAsync(programId);
        if (program == null || !program.IsActive) return (TrainingPeriodResult.NotFound, null);

        // Period creation is Super Admin only (Decision 14)
        var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == actorUserId);
        if (actor == null || actor.Role.Code != "SUPER_ADMIN")
            return (TrainingPeriodResult.Unauthorized, null);

        using var tx = await _context.Database.BeginTransactionAsync();

        var period = new TrainingPeriod
        {
            ProgramId = programId,
            StartTime = startTime,
            EndTime = endTime,
            Capacity = capacity,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _context.TrainingPeriods.Add(period);
        await _context.SaveChangesAsync();

        foreach (var d in daysList)
        {
            _context.TrainingPeriodSchedules.Add(new TrainingPeriodSchedule
            {
                PeriodId = period.PeriodId,
                DayOfWeek = d
            });
        }

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("CREATE_TRAINING_PERIOD", "TrainingPeriod", period.PeriodId,
            $"Program: {program.Name}, Capacity: {capacity}");

        await tx.CommitAsync();

        return (TrainingPeriodResult.Success, period.PeriodId);
    }

    public async Task<TrainingPeriodResult> EditPeriodAsync(
        int periodId,
        TimeOnly startTime,
        TimeOnly endTime,
        int capacity,
        IEnumerable<int> daysOfWeek,
        IEnumerable<int> coachEmployeeIds,
        IEnumerable<int> lifeguardEmployeeIds,
        int actorUserId)
    {
        if (endTime <= startTime) return TrainingPeriodResult.InvalidSchedule;
        if (capacity <= 0) return TrainingPeriodResult.InvalidCapacity;

        var daysList = daysOfWeek.Distinct().ToList();
        if (!daysList.Any() || daysList.Any(d => d < 0 || d > 6)) return TrainingPeriodResult.InvalidSchedule;

        var period = await _context.TrainingPeriods
            .Include(p => p.Schedules)
            .Include(p => p.StaffAssignments)
            .FirstOrDefaultAsync(p => p.PeriodId == periodId);

        if (period == null) return TrainingPeriodResult.NotFound;

        var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == actorUserId);
        if (actor == null || (actor.Role.Code != "SUPER_ADMIN" && actor.Role.Code != "ADMINISTRATOR"))
            return TrainingPeriodResult.Unauthorized;

        var allStaff = coachEmployeeIds.Distinct().Select(id => new { Id = id, Role = "COACH" })
            .Concat(lifeguardEmployeeIds.Distinct().Select(id => new { Id = id, Role = "LIFEGUARD" })).ToList();

        // 2. Validate Staff Overlap (Doc 06)
        foreach (var staff in allStaff)
        {
            bool hasOverlap = await CheckStaffOverlapAsync(staff.Id, periodId, startTime, endTime, daysList);
            if (hasOverlap) return TrainingPeriodResult.StaffOverlap;
        }

        using var tx = await _context.Database.BeginTransactionAsync();

        // 3. Update Period Properties
        bool scheduleChanged = period.StartTime != startTime || period.EndTime != endTime || 
            !period.Schedules.Select(s => s.DayOfWeek).OrderBy(x => x).SequenceEqual(daysList.OrderBy(x => x));

        period.StartTime = startTime;
        period.EndTime = endTime;
        period.Capacity = capacity;

        // 4. Update Schedules
        _context.TrainingPeriodSchedules.RemoveRange(period.Schedules);
        foreach (var d in daysList)
        {
            _context.TrainingPeriodSchedules.Add(new TrainingPeriodSchedule
            {
                PeriodId = period.PeriodId,
                DayOfWeek = d
            });
        }

        // 5. Update Staff
        _context.PeriodStaffAssignments.RemoveRange(period.StaffAssignments);
        foreach (var staff in allStaff)
        {
            _context.PeriodStaffAssignments.Add(new PeriodStaffAssignment
            {
                PeriodId = period.PeriodId,
                EmployeeId = staff.Id,
                Role = staff.Role,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // 6. Handle Schedule Change Impact (Decision 14)
        if (scheduleChanged)
        {
            await RegenerateFutureSessionsAsync(periodId, startTime, endTime, daysList);
        }

        await _auditLog.LogSuccessAsync("EDIT_TRAINING_PERIOD", "TrainingPeriod", period.PeriodId,
            $"ScheduleChanged: {scheduleChanged}, Capacity: {capacity}");

        await tx.CommitAsync();

        return TrainingPeriodResult.Success;
    }

    public async Task<TrainingPeriodResult> SetPeriodStatusAsync(int periodId, string status, int actorUserId)
    {
        var period = await _context.TrainingPeriods.FindAsync(periodId);
        if (period == null) return TrainingPeriodResult.NotFound;

        var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == actorUserId);
        if (actor == null || (actor.Role.Code != "SUPER_ADMIN" && actor.Role.Code != "ADMINISTRATOR"))
            return TrainingPeriodResult.Unauthorized;

        if (status != "ACTIVE" && status != "INACTIVE") return TrainingPeriodResult.InvalidSchedule;

        period.Status = status;
        await _context.SaveChangesAsync();
        
        await _auditLog.LogSuccessAsync("SET_TRAINING_PERIOD_STATUS", "TrainingPeriod", period.PeriodId, $"Status set to {status}");
        return TrainingPeriodResult.Success;
    }

    public async Task<TrainingPeriod?> GetPeriodAsync(int periodId)
    {
        return await _context.TrainingPeriods
            .Include(p => p.Program)
            .Include(p => p.Schedules)
            .Include(p => p.StaffAssignments)
                .ThenInclude(sa => sa.Employee)
            .FirstOrDefaultAsync(p => p.PeriodId == periodId);
    }

    public async Task<IReadOnlyList<TrainingPeriod>> ListPeriodsAsync(int? programId = null, string? status = null)
    {
        var query = _context.TrainingPeriods
            .Include(p => p.Program)
            .Include(p => p.Schedules)
            .AsQueryable();

        if (programId.HasValue) query = query.Where(p => p.ProgramId == programId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.Status == status);

        return await query.OrderBy(p => p.Program.Name).ThenBy(p => p.StartTime).ToListAsync();
    }

    // --- Helpers ---

    private async Task<bool> CheckStaffOverlapAsync(int employeeId, int currentPeriodId, TimeOnly startTime, TimeOnly endTime, List<int> daysOfWeek)
    {
        // 1. Check other Training Periods
        var otherPeriods = await _context.PeriodStaffAssignments
            .Where(a => a.EmployeeId == employeeId && a.PeriodId != currentPeriodId)
            .Include(a => a.Period)
                .ThenInclude(p => p.Schedules)
            .Where(a => a.Period.Status == "ACTIVE")
            .Select(a => a.Period)
            .ToListAsync();

        foreach (var op in otherPeriods)
        {
            bool timesOverlap = startTime < op.EndTime && endTime > op.StartTime;
            if (timesOverlap)
            {
                bool daysOverlap = op.Schedules.Any(s => daysOfWeek.Contains(s.DayOfWeek));
                if (daysOverlap) return true;
            }
        }

        // 2. Check Private Bookings (Doc 06)
        // Since PrivateBookings have specific DateTime StartTime/EndTime, we just check if any active future booking 
        // falls on these days and times.
        var futureBookings = await _context.PrivateBookings
            .Where(pb => pb.CoachId == employeeId && pb.Status == "ACTIVE" && pb.EndTime > DateTime.UtcNow)
            .ToListAsync();

        foreach (var fb in futureBookings)
        {
            var fbDay = (int)fb.StartTime.DayOfWeek;
            if (daysOfWeek.Contains(fbDay))
            {
                var fbStart = TimeOnly.FromDateTime(fb.StartTime);
                var fbEnd = TimeOnly.FromDateTime(fb.EndTime);
                bool timesOverlap = startTime < fbEnd && endTime > fbStart;
                if (timesOverlap) return true;
            }
        }

        return false;
    }

    private async Task RegenerateFutureSessionsAsync(int periodId, TimeOnly newStart, TimeOnly newEnd, List<int> newDays)
    {
        // "historical/previous sessions remain unchanged, future sessions follow the new schedule"
        // Since we are only modifying the Period, the actual session records exist tied to Subscriptions.
        // We find all SCHEDULED sessions in the future for this period.
        
        var now = DateTime.UtcNow;
        var futureScheduledSessions = await _context.Sessions
            .Where(s => s.PeriodId == periodId && s.Status == "SCHEDULED" && s.ScheduledStartTime > now)
            .Include(s => s.Subscription)
            .ToListAsync();

        if (!futureScheduledSessions.Any()) return;

        // Group by Subscription so we can generate the right amount of replacement sessions.
        var subGroups = futureScheduledSessions.GroupBy(s => s.Subscription);

        foreach (var group in subGroups)
        {
            var sub = group.Key;
            if (sub.Status != "ACTIVE") continue; // We don't regenerate for cancelled/completed

            int sessionsToReplace = group.Count();
            
            // Delete the old future sessions
            _context.Sessions.RemoveRange(group);

            // Re-generate starting from today or the last un-deleted session's date, whichever is later
            var lastKeptSession = await _context.Sessions
                .Where(s => s.SubscriptionId == sub.SubscriptionId && s.PeriodId == periodId && s.Status != "SCHEDULED" || s.ScheduledStartTime <= now)
                .OrderByDescending(s => s.ScheduledStartTime)
                .FirstOrDefaultAsync();

            DateOnly generateFrom = DateOnly.FromDateTime(now);
            if (lastKeptSession != null && DateOnly.FromDateTime(lastKeptSession.ScheduledStartTime) > generateFrom)
            {
                generateFrom = DateOnly.FromDateTime(lastKeptSession.ScheduledStartTime).AddDays(1);
            }

            int generated = 0;
            DateOnly currentDate = generateFrom;

            // Generate exactly 'sessionsToReplace' new sessions
            // Max loop boundary to avoid infinite loop
            int safeguard = 0;
            while (generated < sessionsToReplace && safeguard < 365 * 2) // Max 2 years out
            {
                safeguard++;
                int dayOfWeek = (int)currentDate.DayOfWeek;
                if (newDays.Contains(dayOfWeek))
                {
                    var sStart = currentDate.ToDateTime(newStart).ToUniversalTime();
                    var sEnd = currentDate.ToDateTime(newEnd).ToUniversalTime();
                    
                    _context.Sessions.Add(new Session
                    {
                        PeriodId = periodId,
                        SubscriptionId = sub.SubscriptionId,
                        ScheduledStartTime = sStart,
                        ScheduledEndTime = sEnd,
                        Status = "SCHEDULED",
                        CreatedAt = DateTime.UtcNow
                    });
                    
                    generated++;
                }
                currentDate = currentDate.AddDays(1);
            }
        }
    }
}
