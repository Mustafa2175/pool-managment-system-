using System.Threading.Tasks;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Employees;

public enum UserManagementResult
{
    Success,
    NotFound,
    EmployeeNotFound,
    EmployeeNotAdministrator,
    UserAlreadyExists,
    UsernameCollision,
    NicknameRequired,
    Unauthorized
}

public enum PasswordResetResult
{
    Success,
    TargetNotFound,
    NationalIdMismatch,
    NotAnAdministrator
}

/// <summary>
/// Manages User lifecycle distinct from Employee lifecycle:
/// linking, deactivation, reactivation, and password reset flows.
/// Enforces Decision 15 (reactivation preserves credentials) and
/// Decision 16 (password reset by National ID validation).
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Creates a User linked to an ADMINISTRATOR Employee.
    /// Username = employee.Name. On collision, if employee.Nickname is set,
    /// username = "{Name} {Nickname}". If still collides, returns NicknameRequired.
    /// Initial password = BCrypt(employee.NationalId).
    /// </summary>
    Task<(UserManagementResult Result, User? User)> CreateAdminUserAsync(
        int employeeId,
        int createdByUserId);

    /// <summary>
    /// Deactivates a User: is_active=FALSE, deactivated_at=now.
    /// </summary>
    Task<UserManagementResult> DeactivateUserAsync(int userId, int actorUserId);

    /// <summary>
    /// Reactivates a User: is_active=TRUE, deactivated_at=NULL.
    /// Username and password_hash are preserved unchanged (Decision 15).
    /// Audits USER_REACTIVATED.
    /// </summary>
    Task<UserManagementResult> ReactivateUserAsync(int userId, int actorUserId);

    /// <summary>
    /// Owner/Super Admin path: resets an Administrator's password using their National ID.
    /// Validates the National ID against the linked Employee record.
    /// Reseeds hash to BCrypt(NationalId). Audits PASSWORD_RESET (success and failure).
    /// </summary>
    Task<PasswordResetResult> ResetPasswordByNationalIdAsync(
        string targetNationalId,
        int actorUserId);

    /// <summary>
    /// Administrator self-reset: validates own Username + own National ID.
    /// Both must match the caller's own User + Employee records.
    /// Reseeds hash to BCrypt(NationalId). Audits PASSWORD_RESET.
    /// </summary>
    Task<PasswordResetResult> AdminSelfResetPasswordAsync(
        string username,
        string nationalId);
}
