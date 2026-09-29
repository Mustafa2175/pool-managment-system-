using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Finance;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Training;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Training;

public class TrainingLifecycleService : ITrainingLifecycleService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ITrainingSubscriptionService _subscriptionService;
    private readonly IRefundService _refundService;

    public TrainingLifecycleService(
        AppDbContext context, 
        IAuditLogService auditLog, 
        ITrainingSubscriptionService subscriptionService,
        IRefundService refundService)
    {
        _context = context;
        _auditLog = auditLog;
        _subscriptionService = subscriptionService;
        _refundService = refundService;
    }

    public async Task<LifecycleResult> PauseSubscriptionAsync(int subscriptionId, int actorUserId)
    {
        var sub = await _context.TrainingSubscriptions
            .Include(s => s.Pauses)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

        if (sub == null) return LifecycleResult.SubscriptionNotFound;
        if (sub.Status != "ACTIVE") return LifecycleResult.InvalidStateTransition;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var tx = await _context.Database.BeginTransactionAsync();

        sub.Status = "PAUSED";
        
        var pause = new TrainingSubscriptionPause
        {
            SubscriptionId = subscriptionId,
            PauseDate = today,
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingSubscriptionPauses.Add(pause);

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("PAUSE_SUBSCRIPTION", "TrainingSubscription", subscriptionId, $"Paused on {today}");

        await tx.CommitAsync();
        return LifecycleResult.Success;
    }

    public async Task<LifecycleResult> ResumeSubscriptionAsync(int subscriptionId, int actorUserId)
    {
        var sub = await _context.TrainingSubscriptions
            .Include(s => s.Pauses)
            .Include(s => s.Period).ThenInclude(p => p.Schedules)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

        if (sub == null) return LifecycleResult.SubscriptionNotFound;
        if (sub.Status != "PAUSED") return LifecycleResult.InvalidStateTransition;

        var activePause = sub.Pauses.OrderByDescending(p => p.PauseDate).FirstOrDefault(p => p.ResumeDate == null);
        if (activePause == null) return LifecycleResult.InvalidStateTransition;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        activePause.ResumeDate = today;

        var pauseDurationDays = today.DayNumber - activePause.PauseDate.DayNumber;
        sub.EndDate = sub.EndDate.AddDays(pauseDurationDays);
        sub.Status = "ACTIVE";

        using var tx = await _context.Database.BeginTransactionAsync();

        // Regenerate future sessions:
        // Delete all un-attended SCHEDULED sessions in the future (or past, since it was paused)
        var scheduledSessions = await _context.Sessions
            .Where(s => s.SubscriptionId == subscriptionId && s.Status == "SCHEDULED")
            .ToListAsync();

        int sessionsToRegenerate = scheduledSessions.Count;
        _context.Sessions.RemoveRange(scheduledSessions);

        var daysList = sub.Period.Schedules.Select(s => s.DayOfWeek).ToList();
        var currentDate = today;

        int generated = 0;
        int safeguard = 0;
        while (generated < sessionsToRegenerate && safeguard < 365)
        {
            safeguard++;
            int dayOfWeek = (int)currentDate.DayOfWeek;
            if (daysList.Contains(dayOfWeek))
            {
                var sStart = currentDate.ToDateTime(sub.Period.StartTime).ToUniversalTime();
                var sEnd = currentDate.ToDateTime(sub.Period.EndTime).ToUniversalTime();

                _context.Sessions.Add(new Session
                {
                    PeriodId = sub.PeriodId,
                    SubscriptionId = subscriptionId,
                    ScheduledStartTime = sStart,
                    ScheduledEndTime = sEnd,
                    Status = "SCHEDULED",
                    CreatedAt = DateTime.UtcNow
                });
                
                generated++;
                if (generated == sessionsToRegenerate)
                {
                    // Align end date accurately based on the exact final generated session
                    sub.EndDate = currentDate; 
                }
            }
            currentDate = currentDate.AddDays(1);
        }

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("RESUME_SUBSCRIPTION", "TrainingSubscription", subscriptionId, $"Resumed on {today}, shifted {pauseDurationDays} days");

        await tx.CommitAsync();
        return LifecycleResult.Success;
    }

    public async Task<(LifecycleResult Result, int? NewSubscriptionId)> RenewSubscriptionAsync(
        int oldSubscriptionId, DateOnly? requestedStartDate, decimal paidAmount, int actorUserId)
    {
        var oldSub = await _context.TrainingSubscriptions
            .Include(s => s.Period).ThenInclude(p => p.Schedules)
            .FirstOrDefaultAsync(s => s.SubscriptionId == oldSubscriptionId);

        if (oldSub == null) return (LifecycleResult.SubscriptionNotFound, null);
        
        DateOnly newStart;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Check if there are remaining sessions
        var hasRemaining = await _context.Sessions
            .AnyAsync(s => s.SubscriptionId == oldSubscriptionId && s.Status == "SCHEDULED");

        if (oldSub.Status == "ACTIVE" && hasRemaining)
        {
            // Start after old last session
            var lastSession = await _context.Sessions
                .Where(s => s.SubscriptionId == oldSubscriptionId)
                .OrderByDescending(s => s.ScheduledStartTime)
                .FirstOrDefaultAsync();

            var searchStart = lastSession != null ? DateOnly.FromDateTime(lastSession.ScheduledStartTime).AddDays(1) : today;
            newStart = GetNextPeriodDate(oldSub.Period, searchStart);
        }
        else
        {
            // Start from requested date or today
            var searchStart = requestedStartDate ?? today;
            if (searchStart < today) searchStart = today;
            newStart = GetNextPeriodDate(oldSub.Period, searchStart);
        }

        var (res, newSubId) = await _subscriptionService.CreateSubscriptionAsync(
            oldSub.SwimmerId,
            oldSub.ProgramId,
            oldSub.PeriodId,
            newStart,
            paidAmount,
            null, null, null, null,
            actorUserId);

        if (res == SubscriptionResult.Success && newSubId.HasValue)
        {
            var newSub = await _context.TrainingSubscriptions.FindAsync(newSubId.Value);
            newSub!.RenewedFromSubscriptionId = oldSubscriptionId;
            await _context.SaveChangesAsync();
            await _auditLog.LogSuccessAsync("RENEW_SUBSCRIPTION", "TrainingSubscription", newSubId.Value, $"Renewed from {oldSubscriptionId}");
            return (LifecycleResult.Success, newSubId.Value);
        }

        // Map errors (simplified)
        return (LifecycleResult.InvalidStateTransition, null);
    }

    public async Task<LifecycleResult> CancelSubscriptionAsync(int subscriptionId, int actorUserId)
    {
        var sub = await _context.TrainingSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

        if (sub == null) return LifecycleResult.SubscriptionNotFound;
        if (sub.Status == "CANCELLED" || sub.Status == "COMPLETED") return LifecycleResult.InvalidStateTransition;

        var attendedCount = await _context.Sessions
            .CountAsync(s => s.SubscriptionId == subscriptionId && (s.Status == "COMPLETED" || s.Status == "PAUSED")); 

        using var tx = await _context.Database.BeginTransactionAsync();

        sub.Status = "CANCELLED";

        // Delete future scheduled sessions
        var futureSessions = await _context.Sessions
            .Where(s => s.SubscriptionId == subscriptionId && s.Status == "SCHEDULED")
            .ToListAsync();
        _context.Sessions.RemoveRange(futureSessions);

        if (attendedCount == 0)
        {
            // Full Refund
            if (sub.PaidAmount > 0)
            {
                var refund = new Refund
                {
                    RelatedEntityType = "TRAINING_SUBSCRIPTION",
                    RelatedEntityId = subscriptionId,
                    Amount = sub.PaidAmount,
                    Reason = "Cancelled before first session",
                    RecordedBy = actorUserId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Refunds.Add(refund);
                await _context.SaveChangesAsync();

                var refundRes = await _refundService.ConfirmRefundAsync(refund.RefundId);
                if (refundRes != RefundResult.Success)
                {
                    return LifecycleResult.RefundFailed;
                }
            }
        }
        else
        {
            // After first session: no refund, keep paid amount as revenue
        }

        // Auto-update swimmer status if no other active subs exist
        var swimmerId = sub.SwimmerId;
        var activeSubsCount = await _context.TrainingSubscriptions
            .CountAsync(s => s.SwimmerId == swimmerId && s.SubscriptionId != subscriptionId && (s.Status == "ACTIVE" || s.Status == "PAUSED"));
        
        var activePkgCount = await _context.Packages
            .CountAsync(p => p.SwimmerId == swimmerId && (p.Status == "ACTIVE" || p.Status == "PAUSED"));

        if (activeSubsCount == 0 && activePkgCount == 0)
        {
            var swimmer = await _context.Swimmers.FindAsync(swimmerId);
            if (swimmer != null) swimmer.Status = "INACTIVE";
        }

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("CANCEL_SUBSCRIPTION", "TrainingSubscription", subscriptionId, $"Attended: {attendedCount > 0}");

        await tx.CommitAsync();
        return LifecycleResult.Success;
    }

    private DateOnly GetNextPeriodDate(TrainingPeriod period, DateOnly fromDate)
    {
        var daysList = period.Schedules.Select(s => s.DayOfWeek).ToList();
        var d = fromDate;
        while (!daysList.Contains((int)d.DayOfWeek))
        {
            d = d.AddDays(1);
        }
        return d;
    }
}
