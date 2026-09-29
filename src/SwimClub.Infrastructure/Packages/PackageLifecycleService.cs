using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Finance;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Packages;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Packages;

public class PackageLifecycleService : IPackageLifecycleService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IPackageConfigService _configService;
    private readonly IRefundService _refundService;

    public PackageLifecycleService(
        AppDbContext context,
        IAuditLogService auditLog,
        IPackageConfigService configService,
        IRefundService refundService)
    {
        _context = context;
        _auditLog = auditLog;
        _configService = configService;
        _refundService = refundService;
    }

    public async Task<(PackageResult Result, int? PackageId)> CreatePackageAsync(
        string swimmerId,
        string packageType,
        int? programId,
        int? trainingPeriodId,
        int? recreationalPeriodId,
        DateOnly startDate,
        decimal paidAmount,
        decimal? creditGrantedAmount,
        string? creditDescription,
        decimal? outstandingDeclaredAmount,
        string? outstandingDescription,
        int actorUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (startDate < today) return (PackageResult.InvalidStartDate, null);

        var swimmer = await _context.Swimmers.FindAsync(swimmerId);
        if (swimmer == null || swimmer.IsDeleted) return (PackageResult.SwimmerNotFound, null);

        decimal price = 0;
        int durationMonths = 0;
        int sessionsPerMonth = 0;
        int capacity = 0;

        if (packageType == "TRAINING")
        {
            if (!programId.HasValue || !trainingPeriodId.HasValue) return (PackageResult.InvalidPeriodType, null);
            var period = await _context.TrainingPeriods.Include(p => p.Schedules).FirstOrDefaultAsync(p => p.PeriodId == trainingPeriodId.Value);
            if (period == null || period.Status != "ACTIVE" || period.ProgramId != programId.Value) return (PackageResult.PeriodNotActive, null);
            
            capacity = period.Capacity;

            var config = await _configService.GetActiveTrainingConfigAsync(programId.Value, swimmer.MemberStatus);
            price = config.Price;
            durationMonths = config.DurationMonths;
            sessionsPerMonth = config.SessionsPerMonth;

            var activeSubCount = await _context.TrainingSubscriptions
                .CountAsync(s => s.PeriodId == trainingPeriodId.Value && (s.Status == "ACTIVE" || s.Status == "PAUSED"));
            var activePkgCount = await _context.Packages
                .CountAsync(p => p.TrainingPeriodId == trainingPeriodId.Value && p.Status == "ACTIVE");
            
            if (activeSubCount + activePkgCount >= capacity) return (PackageResult.CapacityReached, null);

            var activePkgs = await _context.Packages
                .Include(p => p.TrainingPeriod).ThenInclude(tp => tp!.Schedules)
                .Where(p => p.SwimmerId == swimmerId && p.PackageType == "TRAINING" && p.Status == "ACTIVE")
                .ToListAsync();

            var endDate = startDate.AddMonths(durationMonths);

            foreach (var activePkg in activePkgs)
            {
                if (startDate <= activePkg.EndDate && endDate >= activePkg.StartDate)
                {
                    if (activePkg.TrainingPeriodId == trainingPeriodId.Value) return (PackageResult.DuplicatePackage, null);
                    
                    var p = activePkg.TrainingPeriod!;
                    bool timesOverlap = period.StartTime < p.EndTime && period.EndTime > p.StartTime;
                    if (timesOverlap)
                    {
                        bool daysOverlap = p.Schedules.Any(s => period.Schedules.Select(ps => ps.DayOfWeek).Contains(s.DayOfWeek));
                        if (daysOverlap) return (PackageResult.ScheduleOverlap, null);
                    }
                }
            }
            
            var activeSubs = await _context.TrainingSubscriptions
                .Include(s => s.Period).ThenInclude(tp => tp.Schedules)
                .Where(s => s.SwimmerId == swimmerId && (s.Status == "ACTIVE" || s.Status == "PAUSED"))
                .ToListAsync();
            foreach (var sub in activeSubs)
            {
                if (startDate <= sub.EndDate && endDate >= sub.StartDate)
                {
                    var p = sub.Period;
                    bool timesOverlap = period.StartTime < p.EndTime && period.EndTime > p.StartTime;
                    if (timesOverlap)
                    {
                        bool daysOverlap = p.Schedules.Any(s => period.Schedules.Select(ps => ps.DayOfWeek).Contains(s.DayOfWeek));
                        if (daysOverlap) return (PackageResult.ScheduleOverlap, null);
                    }
                }
            }
        }
        else if (packageType == "RECREATIONAL")
        {
            if (!recreationalPeriodId.HasValue) return (PackageResult.InvalidPeriodType, null);
            var period = await _context.RecreationalPeriods.Include(p => p.Schedules).FirstOrDefaultAsync(p => p.RecreationalPeriodId == recreationalPeriodId.Value);
            if (period == null || period.Status != "ACTIVE") return (PackageResult.PeriodNotActive, null);
            
            var config = await _configService.GetActiveRecreationalConfigAsync(swimmer.MemberStatus);
            price = config.Price;
            durationMonths = config.DurationMonths;
            sessionsPerMonth = config.SessionsPerMonth;

            var activePkgs = await _context.Packages
                .Include(p => p.RecreationalPeriod).ThenInclude(tp => tp!.Schedules)
                .Where(p => p.SwimmerId == swimmerId && p.PackageType == "RECREATIONAL" && p.Status == "ACTIVE")
                .ToListAsync();

            var endDate = startDate.AddMonths(durationMonths);

            foreach (var activePkg in activePkgs)
            {
                if (startDate <= activePkg.EndDate && endDate >= activePkg.StartDate)
                {
                    if (activePkg.RecreationalPeriodId == recreationalPeriodId.Value) return (PackageResult.DuplicatePackage, null);
                    
                    var p = activePkg.RecreationalPeriod!;
                    bool timesOverlap = period.StartTime < p.EndTime && period.EndTime > p.StartTime;
                    if (timesOverlap)
                    {
                        bool daysOverlap = p.Schedules.Any(s => period.Schedules.Select(ps => ps.DayOfWeek).Contains(s.DayOfWeek));
                        if (daysOverlap) return (PackageResult.ScheduleOverlap, null);
                    }
                }
            }
        }
        else
        {
            return (PackageResult.InvalidPeriodType, null);
        }

        if (paidAmount > price) return (PackageResult.Overpayment, null);

        decimal refTrainingPrice = 0;
        try 
        {
            var regularConfig = await _configService.GetActiveTrainingConfigAsync(1, swimmer.MemberStatus);
            refTrainingPrice = regularConfig.Price;
        } 
        catch { }

        using var tx = await _context.Database.BeginTransactionAsync();

        var endDateCalc = startDate.AddMonths(durationMonths);
        var totalSessions = durationMonths * sessionsPerMonth;

        var package = new Package
        {
            SwimmerId = swimmerId,
            PackageType = packageType,
            ProgramId = programId,
            TrainingPeriodId = trainingPeriodId,
            RecreationalPeriodId = recreationalPeriodId,
            StartDate = startDate,
            EndDate = endDateCalc,
            DurationSnapshotDays = durationMonths * 30,
            SessionsPerMonthSnapshot = sessionsPerMonth,
            AvailableSessionsTotal = totalSessions,
            AvailableSessionsRemaining = totalSessions,
            ReferenceTrainingPriceSnapshot = refTrainingPrice,
            TotalPrice = price,
            PaidAmount = paidAmount,
            CreditGrantedAmount = creditGrantedAmount,
            CreditDescription = creditDescription,
            OutstandingDeclaredAmount = outstandingDeclaredAmount,
            OutstandingDescription = outstandingDescription,
            Status = "ACTIVE",
            QrToken = $"PKG-{Guid.NewGuid():N}",
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Packages.Add(package);
        await _context.SaveChangesAsync();

        if (creditGrantedAmount.HasValue && creditGrantedAmount.Value > 0)
        {
            _context.Credits.Add(new Credit
            {
                SwimmerId = swimmerId,
                Amount = creditGrantedAmount.Value,
                RemainingAmount = creditGrantedAmount.Value,
                Description = creditDescription,
                GeneratedFromType = "PACKAGE",
                GeneratedFromId = package.PackageId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (paidAmount > 0)
        {
            _context.Payments.Add(new Payment
            {
                Amount = paidAmount,
                RelatedEntityType = "PACKAGE",
                RelatedEntityId = package.PackageId,
                PaymentMethod = "CASH",
                RecordedBy = actorUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        swimmer.Status = "ACTIVE";
        
        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("CREATE_PACKAGE", "Package", package.PackageId, $"Type: {packageType}, Amount: {paidAmount}");
        
        await tx.CommitAsync();

        return (PackageResult.Success, package.PackageId);
    }

    public async Task<(PackageResult Result, int? NewPackageId)> RenewPackageAsync(
        int oldPackageId,
        DateOnly? requestedStartDate,
        string packageType,
        int? programId,
        int? trainingPeriodId,
        int? recreationalPeriodId,
        decimal paidAmount,
        int actorUserId)
    {
        var oldPkg = await _context.Packages.FindAsync(oldPackageId);
        if (oldPkg == null) return (PackageResult.PackageNotFound, null);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly newStart;

        if (oldPkg.Status == "ACTIVE")
        {
            newStart = oldPkg.EndDate.AddDays(1);
        }
        else
        {
            newStart = requestedStartDate ?? today;
            if (newStart < today) newStart = today;
        }

        var (res, newPkgId) = await CreatePackageAsync(
            oldPkg.SwimmerId,
            packageType,
            programId,
            trainingPeriodId,
            recreationalPeriodId,
            newStart,
            paidAmount,
            null, null, null, null,
            actorUserId);

        if (res == PackageResult.Success && newPkgId.HasValue)
        {
            var newPkg = await _context.Packages.FindAsync(newPkgId.Value);
            newPkg!.RenewedFromPackageId = oldPackageId;
            await _context.SaveChangesAsync();
            await _auditLog.LogSuccessAsync("RENEW_PACKAGE", "Package", newPkgId.Value, $"Renewed from {oldPackageId}");
        }

        return (res, newPkgId);
    }

    public async Task<PackageResult> ChangePeriodAsync(
        int packageId,
        int? newTrainingPeriodId,
        int? newRecreationalPeriodId,
        int actorUserId)
    {
        var pkg = await _context.Packages.FindAsync(packageId);
        if (pkg == null) return PackageResult.PackageNotFound;
        if (pkg.Status != "ACTIVE") return PackageResult.InvalidStateTransition;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var tx = await _context.Database.BeginTransactionAsync();

        if (pkg.PackageType == "TRAINING" && newTrainingPeriodId.HasValue)
        {
            _context.PackagePeriodChanges.Add(new PackagePeriodChange
            {
                PackageId = packageId,
                ChangeDate = today,
                OldPeriodId = pkg.TrainingPeriodId ?? 0,
                NewPeriodId = newTrainingPeriodId.Value,
                SessionsCarriedOver = pkg.AvailableSessionsRemaining,
                ChangedBy = actorUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newTrainingPeriodId.HasValue) 
        {
            pkg.TrainingPeriodId = newTrainingPeriodId;
            pkg.RecreationalPeriodId = null;
        }
        else if (newRecreationalPeriodId.HasValue)
        {
            pkg.RecreationalPeriodId = newRecreationalPeriodId;
            pkg.TrainingPeriodId = null;
        }

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("CHANGE_PACKAGE_PERIOD", "Package", packageId, "Period changed");
        
        await tx.CommitAsync();

        return PackageResult.Success;
    }

    public async Task<PackageResult> CancelPackageAsync(int packageId, int actorUserId)
    {
        var pkg = await _context.Packages.FindAsync(packageId);
        if (pkg == null) return PackageResult.PackageNotFound;
        if (pkg.Status != "ACTIVE") return PackageResult.InvalidStateTransition;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstMonthBoundary = pkg.StartDate.AddMonths(1);

        using var tx = await _context.Database.BeginTransactionAsync();

        pkg.Status = "CANCELLED";
        decimal refundAmount = 0;

        if (today < pkg.StartDate)
        {
            refundAmount = pkg.PaidAmount;
        }
        else if (today <= firstMonthBoundary)
        {
            // Boundary definition logic: "during first month" => <= firstMonthBoundary (Inclusive boundary for now)
            var amountToKeep = pkg.ReferenceTrainingPriceSnapshot;
            refundAmount = Math.Max(0, pkg.PaidAmount - amountToKeep);
        }
        else
        {
            refundAmount = 0;
        }

        if (refundAmount > 0)
        {
            var refund = new Refund
            {
                RelatedEntityType = "PACKAGE",
                RelatedEntityId = packageId,
                Amount = refundAmount,
                Reason = "Package Cancellation",
                RecordedBy = actorUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Refunds.Add(refund);
            await _context.SaveChangesAsync();

            var refundRes = await _refundService.ConfirmRefundAsync(refund.RefundId);
            if (refundRes != RefundResult.Success)
            {
                return PackageResult.RefundFailed;
            }
        }

        // Swimmer inactive logic
        var activeSubsCount = await _context.TrainingSubscriptions
            .CountAsync(s => s.SwimmerId == pkg.SwimmerId && (s.Status == "ACTIVE" || s.Status == "PAUSED"));
        var activePkgCount = await _context.Packages
            .CountAsync(p => p.SwimmerId == pkg.SwimmerId && p.PackageId != packageId && p.Status == "ACTIVE");

        if (activeSubsCount == 0 && activePkgCount == 0)
        {
            var swimmer = await _context.Swimmers.FindAsync(pkg.SwimmerId);
            if (swimmer != null) swimmer.Status = "INACTIVE";
        }

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("CANCEL_PACKAGE", "Package", packageId, $"Refund: {refundAmount}");

        await tx.CommitAsync();

        return PackageResult.Success;
    }
}
