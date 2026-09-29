using System.Threading.Tasks;

namespace SwimClub.Application.Security;

public enum LoginResult
{
    Success,
    InvalidCredentials,
    Deactivated
}

public enum ResetPasswordResult
{
    Success,
    AdminNotFound,
    TargetNotFound,
    UnauthorizedTarget
}

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string username, string password);
    void Logout();
    Task<ResetPasswordResult> ResetPasswordAsync(string adminUsername, string adminNationalId, string targetNationalId);
}
