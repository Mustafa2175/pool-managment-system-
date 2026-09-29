using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Recreational;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Application.Interfaces;

namespace SwimClub.Infrastructure.Recreational;

public class RecreationalService : IRecreationalService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public RecreationalService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<int> CreatePeriodAsync(string name, TimeOnly startTime, TimeOnly endTime, int capacity, int actorUserId)
    {
        var period = new RecreationalPeriod
        {
            Name = name,
            StartTime = startTime,
            EndTime = endTime,
            Capacity = capacity,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.RecreationalPeriods.Add(period);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("RECREATIONAL_PERIOD_CREATED", "RecreationalPeriod", period.RecreationalPeriodId,
            $"Name={name}, Capacity={capacity}");

        return period.RecreationalPeriodId;
    }

    public async Task<RecreationalResult> EditPeriodAsync(int periodId, string name, TimeOnly startTime, TimeOnly endTime, int capacity, string status, int actorUserId)
    {
        var period = await _context.RecreationalPeriods.FindAsync(periodId);
        if (period == null) return RecreationalResult.PeriodNotFound;

        period.Name = name;
        period.StartTime = startTime;
        period.EndTime = endTime;
        period.Capacity = capacity;
        period.Status = status;

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("RECREATIONAL_PERIOD_UPDATED", "RecreationalPeriod", period.RecreationalPeriodId,
            $"Name={name}, Status={status}");

        return RecreationalResult.Success;
    }

    private async Task<(bool IsValid, RecreationalResult Error)> ValidateTimeWindowAsync(RecreationalPeriod period, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var startDateTime = today.ToDateTime(period.StartTime);
        
        // Window: 30 mins before through 30 mins after start time
        var windowStart = startDateTime.AddMinutes(-30);
        var windowEnd = startDateTime.AddMinutes(30);

        if (now < windowStart || now > windowEnd)
        {
            return (false, RecreationalResult.OutsideCheckInWindow);
        }

        return (true, RecreationalResult.Success);
    }

    private async Task<(bool HasCapacity, int CurrentCount)> CheckCapacityAsync(int periodId, DateTime date)
    {
        var period = await _context.RecreationalPeriods.FindAsync(periodId);
        if (period == null) return (false, 0);

        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        // Count single entry tickets for today
        var ticketsCount = await _context.RecreationalTickets
            .CountAsync(t => t.RecreationalPeriodId == periodId 
                          && t.CheckedInAt >= startOfDay 
                          && t.CheckedInAt < endOfDay);

        // Count package check-ins for packages assigned to this period
        var packagesCount = await _context.PackageCheckIns
            .Include(p => p.Package)
            .CountAsync(p => p.Package.RecreationalPeriodId == periodId 
                          && p.CheckedInAt >= startOfDay 
                          && p.CheckedInAt < endOfDay);

        var total = ticketsCount + packagesCount;
        return (total < period.Capacity, total);
    }

    // ISOLATED UNRESOLVED BEHAVIOR: Duplicate Identity Enforcement
    // The business rules state the ticket is anonymous free-text, but also forbid duplicate check-ins.
    // The exact string matching, override mechanics, and "leave and return" handling are unresolved.
    // We implement strict exact string match per day, with no override.
    private async Task<bool> IsDuplicateSingleEntryAsync(int periodId, string name, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.RecreationalTickets
            .AnyAsync(t => t.RecreationalPeriodId == periodId
                        && t.Name.ToLower() == name.ToLower()
                        && t.CheckedInAt >= startOfDay
                        && t.CheckedInAt < endOfDay);
    }

    public async Task<(RecreationalResult Result, int? TicketId)> RecordSingleEntryAsync(
        int periodId,
        string name,
        string memberStatus,
        decimal amountPaid,
        string? paymentDescription,
        int actorUserId)
    {
        var now = DateTime.UtcNow;

        var period = await _context.RecreationalPeriods.FindAsync(periodId);
        if (period == null) return (RecreationalResult.PeriodNotFound, null);
        if (period.Status != "ACTIVE") return (RecreationalResult.PeriodInactive, null);

        var (isWindowValid, windowErr) = await ValidateTimeWindowAsync(period, now);
        if (!isWindowValid)
        {
            await _auditLog.LogSuccessAsync("RECREATIONAL_CHECKIN_REJECTED", "RecreationalPeriod", periodId, "Reason: Outside window");
            return (windowErr, null);
        }

        var (hasCapacity, _) = await CheckCapacityAsync(periodId, now);
        if (!hasCapacity)
        {
            await _auditLog.LogSuccessAsync("RECREATIONAL_CHECKIN_REJECTED", "RecreationalPeriod", periodId, "Reason: Capacity full");
            return (RecreationalResult.CapacityFull, null);
        }

        var isDuplicate = await IsDuplicateSingleEntryAsync(periodId, name, now);
        if (isDuplicate)
        {
            await _auditLog.LogSuccessAsync("RECREATIONAL_DUPLICATE_CHECKIN_REJECTED", "RecreationalPeriod", periodId, $"Name={name}");
            // Based on requirements, if they leave and return, we do NOT create a second check-in.
            // But we also reject it here to avoid charging them twice/exceeding capacity limits falsely.
            return (RecreationalResult.DuplicateCheckIn, null);
        }

        // Must pay exactly full configured fee. (Assuming it's validated at controller or here we assume AmountPaid is the fee).
        // Since we don't have a configured fee directly on the Period in this schema, 
        // we assume the UI supplies the full configured fee and it must be > 0.
        if (amountPaid <= 0) return (RecreationalResult.InvalidPayment, null);

        await using var tx = await _context.Database.BeginTransactionAsync();

        var ticket = new RecreationalTicket
        {
            RecreationalPeriodId = periodId,
            Name = name,
            MemberStatus = memberStatus,
            CheckedInAt = now,
            AmountPaid = amountPaid,
            PaymentDescription = paymentDescription,
            RecordedBy = actorUserId
        };
        _context.RecreationalTickets.Add(ticket);
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            TransactionType = "RECREATIONAL_TICKET_PAYMENT",
            Amount = amountPaid,
            RelatedEntityType = "RECREATIONAL_TICKET",
            RelatedEntityId = ticket.TicketId,
            RecordedBy = actorUserId,
            CreatedAt = now
        };
        _context.Transactions.Add(transaction);
        
        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("RECREATIONAL_TICKET_RECORDED", "RecreationalTicket", ticket.TicketId, $"Amount={amountPaid}");
        
        await tx.CommitAsync();

        return (RecreationalResult.Success, ticket.TicketId);
    }

    public async Task<RecreationalResult> RecordPackageCheckInAsync(
        int periodId,
        string qrToken,
        int actorUserId)
    {
        var now = DateTime.UtcNow;

        var period = await _context.RecreationalPeriods.FindAsync(periodId);
        if (period == null) return RecreationalResult.PeriodNotFound;
        if (period.Status != "ACTIVE") return RecreationalResult.PeriodInactive;

        var (isWindowValid, windowErr) = await ValidateTimeWindowAsync(period, now);
        if (!isWindowValid)
        {
            await _auditLog.LogSuccessAsync("PACKAGE_CHECKIN_REJECTED", "RecreationalPeriod", periodId, "Reason: Outside window");
            return windowErr;
        }

        // Validate Package
        var package = await _context.Packages
            .FirstOrDefaultAsync(p => p.QrToken == qrToken && p.Status == "ACTIVE");

        if (package == null)
        {
            await _auditLog.LogSuccessAsync("PACKAGE_CHECKIN_REJECTED", "RecreationalPeriod", periodId, "Reason: Invalid or inactive package");
            return RecreationalResult.InvalidPackage;
        }

        if (package.RecreationalPeriodId != periodId)
        {
            await _auditLog.LogSuccessAsync("PACKAGE_CHECKIN_REJECTED", "RecreationalPeriod", periodId, "Reason: Wrong period");
            return RecreationalResult.WrongPeriod;
        }

        if (DateOnly.FromDateTime(now) < package.StartDate || DateOnly.FromDateTime(now) > package.EndDate)
        {
            await _auditLog.LogSuccessAsync("PACKAGE_CHECKIN_REJECTED", "RecreationalPeriod", periodId, "Reason: Expired package");
            return RecreationalResult.PackageExpired;
        }

        if (package.AvailableSessionsRemaining <= 0)
        {
            await _auditLog.LogSuccessAsync("PACKAGE_CHECKIN_REJECTED", "RecreationalPeriod", periodId, "Reason: No sessions remaining");
            return RecreationalResult.PackageNoSessions;
        }

        // Enforce duplicate package check-in for the SAME DAY per the business rules
        // "same person cannot check in twice for the same Recreational Period"
        var startOfDay = now.Date;
        var endOfDay = startOfDay.AddDays(1);
        var duplicatePackageCheckin = await _context.PackageCheckIns
            .AnyAsync(p => p.PackageId == package.PackageId
                        && p.CheckedInAt >= startOfDay
                        && p.CheckedInAt < endOfDay);
        
        if (duplicatePackageCheckin)
        {
            await _auditLog.LogSuccessAsync("RECREATIONAL_DUPLICATE_CHECKIN_REJECTED", "Package", package.PackageId, "Reason: Already checked in today");
            return RecreationalResult.DuplicateCheckIn;
        }

        var (hasCapacity, _) = await CheckCapacityAsync(periodId, now);
        if (!hasCapacity)
        {
            await _auditLog.LogSuccessAsync("PACKAGE_CHECKIN_REJECTED", "RecreationalPeriod", periodId, "Reason: Capacity full");
            return RecreationalResult.CapacityFull;
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        var checkIn = new PackageCheckIn
        {
            PackageId = package.PackageId,
            CheckedInAt = now,
            AttendanceMethod = "QR",
            RecordedBy = actorUserId,
            CreatedAt = now
        };
        _context.PackageCheckIns.Add(checkIn);

        package.AvailableSessionsRemaining -= 1;

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("PACKAGE_CHECKIN_RECORDED", "PackageCheckIn", checkIn.CheckInId, $"Remaining={package.AvailableSessionsRemaining}");
        
        await tx.CommitAsync();

        return RecreationalResult.Success;
    }
}
