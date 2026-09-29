using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Packages;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Packages;

public class PackageAttendanceService : IPackageAttendanceService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public PackageAttendanceService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<PackageResult> RecordAttendanceAsync(string swimmerId, int packageId, int actorUserId)
    {
        return await ProcessAttendanceAsync(packageId, actorUserId, "MANUAL", swimmerId);
    }

    public async Task<PackageResult> RecordAttendanceByQrAsync(string qrToken, int packageId, int actorUserId)
    {
        var pkg = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == packageId && p.QrToken == qrToken);
        if (pkg == null) return PackageResult.PackageNotFound;

        return await ProcessAttendanceAsync(packageId, actorUserId, "QR", pkg.SwimmerId);
    }

    private async Task<PackageResult> ProcessAttendanceAsync(int packageId, int actorUserId, string method, string expectedSwimmerId)
    {
        var pkg = await _context.Packages
            .Include(p => p.TrainingPeriod).ThenInclude(tp => tp!.Schedules)
            .Include(p => p.RecreationalPeriod).ThenInclude(rp => rp!.Schedules)
            .FirstOrDefaultAsync(p => p.PackageId == packageId);

        if (pkg == null || pkg.SwimmerId != expectedSwimmerId) return PackageResult.PackageNotFound;
        if (pkg.Status != "ACTIVE") return PackageResult.InvalidStateTransition;
        if (pkg.AvailableSessionsRemaining <= 0) return PackageResult.NoSessionsRemaining;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        if (today < pkg.StartDate || today > pkg.EndDate) return PackageResult.PackageExpired;

        // "Correct period" validation
        // Is it the correct day and time for the package's period?
        if (pkg.PackageType == "TRAINING" && pkg.TrainingPeriod != null)
        {
            var period = pkg.TrainingPeriod;
            var todayDOW = (int)today.DayOfWeek;
            if (!period.Schedules.Any(s => s.DayOfWeek == todayDOW)) return PackageResult.PeriodNotActive; // wrong day

            var pStart = today.ToDateTime(period.StartTime).ToUniversalTime();
            // Using same 30m window rule as training subscriptions
            if (now < pStart.AddMinutes(-30) || now > pStart.AddMinutes(30)) return PackageResult.PeriodNotActive; // outside attendance window
        }
        else if (pkg.PackageType == "RECREATIONAL" && pkg.RecreationalPeriod != null)
        {
            var period = pkg.RecreationalPeriod;
            var todayDOW = (int)today.DayOfWeek;
            if (!period.Schedules.Any(s => s.DayOfWeek == todayDOW)) return PackageResult.PeriodNotActive; // wrong day

            var pStart = today.ToDateTime(period.StartTime).ToUniversalTime();
            var pEnd = today.ToDateTime(period.EndTime).ToUniversalTime();
            if (now < pStart || now > pEnd) return PackageResult.PeriodNotActive; // out of operating hours
        }
        else
        {
            return PackageResult.InvalidPeriodType;
        }

        using var tx = await _context.Database.BeginTransactionAsync();

        pkg.AvailableSessionsRemaining--;

        _context.PackageCheckIns.Add(new PackageCheckIn
        {
            PackageId = packageId,
            CheckedInAt = now,
            AttendanceMethod = method,
            RecordedBy = actorUserId,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("PACKAGE_ATTENDANCE", "Package", packageId, $"Method: {method}, Remaining: {pkg.AvailableSessionsRemaining}");

        await tx.CommitAsync();

        return PackageResult.Success;
    }
}
