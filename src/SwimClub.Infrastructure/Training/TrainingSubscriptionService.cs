using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Training;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Training;

public class TrainingSubscriptionService : ITrainingSubscriptionService
{
    private readonly AppDbContext _context;
    private readonly ITrainingConfigService _configService;
    private readonly IAuditLogService _auditLog;

    public TrainingSubscriptionService(AppDbContext context, ITrainingConfigService configService, IAuditLogService auditLog)
    {
        _context = context;
        _configService = configService;
        _auditLog = auditLog;
    }

    public async Task<(SubscriptionResult Result, int? SubscriptionId)> CreateSubscriptionAsync(
        string swimmerId,
        int programId,
        int periodId,
        DateOnly startDate,
        decimal paidAmount,
        decimal? creditGrantedAmount,
        string? creditDescription,
        decimal? outstandingDeclaredAmount,
        string? outstandingDescription,
        int actorUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Validate Start Date >= today
        if (startDate < today)
            return (SubscriptionResult.InvalidStartDate, null);

        // 2. Validate Credit / Outstanding mutual exclusivity
        if (creditGrantedAmount.HasValue && outstandingDeclaredAmount.HasValue)
            return (SubscriptionResult.CreditAndOutstandingMutuallyExclusive, null);

        var period = await _context.TrainingPeriods
            .Include(p => p.Schedules)
            .FirstOrDefaultAsync(p => p.PeriodId == periodId);

        if (period == null || period.Status != "ACTIVE")
            return (SubscriptionResult.PeriodNotActive, null);

        if (period.ProgramId != programId)
            return (SubscriptionResult.PeriodNotActive, null);

        // 3. Validate Start Date matches a scheduled day
        var startDayOfWeek = (int)startDate.DayOfWeek;
        if (!period.Schedules.Any(s => s.DayOfWeek == startDayOfWeek))
            return (SubscriptionResult.InvalidStartDate, null);

        // 4. Validate Capacity
        var activeSubCount = await _context.TrainingSubscriptions
            .CountAsync(s => s.PeriodId == periodId && (s.Status == "ACTIVE" || s.Status == "PAUSED"));
        
        if (activeSubCount >= period.Capacity)
            return (SubscriptionResult.CapacityReached, null);

        var swimmer = await _context.Swimmers.FindAsync(swimmerId);
        if (swimmer == null || swimmer.IsDeleted)
            return (SubscriptionResult.PeriodNotActive, null); // Generic fail

        // 5. Validate Duplicate / Schedule Overlap
        var allActiveSubs = await _context.TrainingSubscriptions
            .Include(s => s.Period).ThenInclude(p => p.Schedules)
            .Where(s => s.SwimmerId == swimmerId && (s.Status == "ACTIVE" || s.Status == "PAUSED"))
            .ToListAsync();

        foreach (var sub in allActiveSubs)
        {
            if (startDate <= sub.EndDate)
            {
                if (sub.PeriodId == periodId)
                    return (SubscriptionResult.DuplicateSubscription, null);

                var p = sub.Period;
                bool timesOverlap = period.StartTime < p.EndTime && period.EndTime > p.StartTime;
                if (timesOverlap)
                {
                    bool daysOverlap = p.Schedules.Any(s => period.Schedules.Select(ps => ps.DayOfWeek).Contains(s.DayOfWeek));
                    if (daysOverlap) return (SubscriptionResult.ScheduleOverlap, null);
                }
            }
        }
        
        // Check Packages for overlap
        var activePackages = await _context.Packages
            .Include(p => p.TrainingPeriod).ThenInclude(tp => tp!.Schedules)
            .Where(p => p.SwimmerId == swimmerId && p.PackageType == "TRAINING" && (p.Status == "ACTIVE" || p.Status == "PAUSED"))
            .ToListAsync();

        foreach(var pkg in activePackages)
        {
            if (startDate <= pkg.EndDate)
            {
                var p = pkg.TrainingPeriod;
                if (p != null)
                {
                    bool timesOverlap = period.StartTime < p.EndTime && period.EndTime > p.StartTime;
                    if (timesOverlap)
                    {
                        bool daysOverlap = p.Schedules.Any(s => period.Schedules.Select(ps => ps.DayOfWeek).Contains(s.DayOfWeek));
                        if (daysOverlap) return (SubscriptionResult.ScheduleOverlap, null);
                    }
                }
            }
        }

        // 6. Pricing
        decimal price = await _configService.GetActivePriceAsync(programId, swimmer.MemberStatus);
        
        if (paidAmount > price)
            return (SubscriptionResult.Overpayment, null);

        int sessionCount = await _configService.GetGlobalSessionCountAsync();

        using var tx = await _context.Database.BeginTransactionAsync();

        var subscription = new TrainingSubscription
        {
            SwimmerId = swimmerId,
            ProgramId = programId,
            PeriodId = periodId,
            StartDate = startDate,
            // EndDate will be computed after session generation
            UnitPriceSnapshot = price,
            ConfiguredSessionCountSnapshot = sessionCount,
            TotalPrice = price,
            PaidAmount = paidAmount,
            CreditGrantedAmount = creditGrantedAmount,
            CreditDescription = creditDescription,
            OutstandingDeclaredAmount = outstandingDeclaredAmount,
            OutstandingDescription = outstandingDescription,
            Status = "ACTIVE",
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TrainingSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(); // To get ID for Sessions

        // 7. Handle Credit Grant
        if (creditGrantedAmount.HasValue && creditGrantedAmount.Value > 0)
        {
            _context.Credits.Add(new Credit
            {
                SwimmerId = swimmerId,
                Amount = creditGrantedAmount.Value,
                RemainingAmount = creditGrantedAmount.Value,
                Description = creditDescription,
                GeneratedFromType = "TRAINING_SUBSCRIPTION",
                GeneratedFromId = subscription.SubscriptionId,
                CreatedAt = DateTime.UtcNow
            });
            await _auditLog.LogSuccessAsync("CREDIT_ISSUED", "Credit", null, $"Issued {creditGrantedAmount.Value} to {swimmerId}");
        }

        // 8. Generate Sessions
        var daysList = period.Schedules.Select(s => s.DayOfWeek).ToList();
        int generated = 0;
        DateOnly currentDate = startDate;
        
        while (generated < sessionCount)
        {
            int dayOfWeek = (int)currentDate.DayOfWeek;
            if (daysList.Contains(dayOfWeek))
            {
                var sStart = currentDate.ToDateTime(period.StartTime).ToUniversalTime();
                var sEnd = currentDate.ToDateTime(period.EndTime).ToUniversalTime();
                
                _context.Sessions.Add(new Session
                {
                    PeriodId = periodId,
                    SubscriptionId = subscription.SubscriptionId,
                    ScheduledStartTime = sStart,
                    ScheduledEndTime = sEnd,
                    Status = "SCHEDULED",
                    CreatedAt = DateTime.UtcNow
                });
                
                generated++;
                if (generated == sessionCount)
                {
                    subscription.EndDate = currentDate; // Set end date based on last session
                }
            }
            currentDate = currentDate.AddDays(1);
        }

        // 9. Payment recording (simplified for this module since Phase 3 handles deep finance)
        if (paidAmount > 0)
        {
            // Normally would route via Finance module `IPaymentService`
            var payment = new Payment
            {
                Amount = paidAmount,
                RelatedEntityType = "TRAINING_SUBSCRIPTION",
                RelatedEntityId = subscription.SubscriptionId,
                PaymentMethod = "CASH",
                RecordedBy = actorUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
        }

        // Auto-update Swimmer status
        swimmer.Status = "ACTIVE";

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("CREATE_TRAINING_SUBSCRIPTION", "TrainingSubscription", subscription.SubscriptionId, $"Price: {price}, Paid: {paidAmount}");

        await tx.CommitAsync();
        return (SubscriptionResult.Success, subscription.SubscriptionId);
    }
}
