using System;
using System.Threading.Tasks;

namespace SwimClub.Application.Packages;

public enum PackageResult
{
    Success,
    SwimmerNotFound,
    InvalidStartDate,
    PeriodNotActive,
    CapacityReached,
    DuplicatePackage,
    ScheduleOverlap,
    Overpayment,
    PackageNotFound,
    InvalidStateTransition,
    RefundFailed,
    InvalidPeriodType,
    ConfigNotFound,
    NoSessionsRemaining,
    PackageExpired
}

public interface IPackageConfigService
{
    Task<(decimal Price, int DurationMonths, int SessionsPerMonth)> GetActiveTrainingConfigAsync(int programId, string memberStatus);
    Task<(decimal Price, int DurationMonths, int SessionsPerMonth)> GetActiveRecreationalConfigAsync(string memberStatus);
    
    Task SetTrainingConfigAsync(int programId, int durationMonths, int sessionsPerMonth, decimal memberPrice, decimal nonMemberPrice, int actorUserId);
    Task SetRecreationalConfigAsync(int durationMonths, int sessionsPerMonth, decimal memberPrice, decimal nonMemberPrice, int actorUserId);
}
