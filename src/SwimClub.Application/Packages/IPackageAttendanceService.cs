using System.Threading.Tasks;

namespace SwimClub.Application.Packages;

public interface IPackageAttendanceService
{
    Task<PackageResult> RecordAttendanceAsync(string swimmerId, int packageId, int actorUserId);
    Task<PackageResult> RecordAttendanceByQrAsync(string qrToken, int packageId, int actorUserId);
}
