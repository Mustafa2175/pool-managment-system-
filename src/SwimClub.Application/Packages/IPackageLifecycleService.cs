using System;
using System.Threading.Tasks;

namespace SwimClub.Application.Packages;

public interface IPackageLifecycleService
{
    Task<(PackageResult Result, int? PackageId)> CreatePackageAsync(
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
        int actorUserId);

    Task<(PackageResult Result, int? NewPackageId)> RenewPackageAsync(
        int oldPackageId, 
        DateOnly? requestedStartDate, 
        string packageType, 
        int? programId, 
        int? trainingPeriodId, 
        int? recreationalPeriodId, 
        decimal paidAmount, 
        int actorUserId);

    Task<PackageResult> ChangePeriodAsync(
        int packageId, 
        int? newTrainingPeriodId, 
        int? newRecreationalPeriodId, 
        int actorUserId);

    Task<PackageResult> CancelPackageAsync(int packageId, int actorUserId);
}
