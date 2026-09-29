using System.Threading.Tasks;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Employees;

public enum EmployeeResult
{
    Success,
    NotFound,
    DuplicateNationalId,
    UsernameCollision,
    NicknameRequired,
    NicknameCollision,
    InvalidType,
    SalaryRequired,
    SalaryForbidden,
    AlreadyActive,
    AlreadyInactive
}

/// <summary>
/// Manages Employee lifecycle: creation, deactivation, and reactivation.
/// Enforces Decision 12 (Owner/SuperAdmin never have Employee records),
/// Decision 13 (username = name, nickname on collision),
/// Decision 15 (reactivation preserves username + password_hash unchanged).
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Creates an Employee of the given type.
    /// For ADMINISTRATOR type, also creates a linked User (username = name, password = BCrypt(nationalId)).
    /// On username collision, returns UsernameCollision/NicknameRequired without creating the User.
    /// </summary>
    Task<(EmployeeResult Result, Employee? Employee)> CreateEmployeeAsync(
        string name,
        string employeeType,
        string nationalId,
        string? phone,
        decimal? monthlySalary,
        string? nickname,
        int createdByUserId);

    /// <summary>
    /// Deactivates an Employee (sets Status=INACTIVE, DeactivatedAt=now).
    /// If the employee has a linked User, also deactivates that User.
    /// </summary>
    Task<EmployeeResult> DeactivateEmployeeAsync(int employeeId, int actorUserId);

    /// <summary>
    /// Reactivates an Employee (Status=ACTIVE, DeactivatedAt=NULL).
    /// If the employee has a linked User, restores is_active=TRUE, deactivated_at=NULL.
    /// Username and password_hash are preserved unchanged (Decision 15).
    /// Audits USER_REACTIVATED.
    /// </summary>
    Task<EmployeeResult> ReactivateEmployeeAsync(int employeeId, int actorUserId);

    Task<Employee?> GetEmployeeByIdAsync(int employeeId);

    Task<IReadOnlyList<Employee>> GetAllEmployeesAsync(string? typeFilter = null, string? statusFilter = null);
}
