using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Employees;
using SwimClub.Application.Interfaces;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using BCrypt.Net;

namespace SwimClub.Infrastructure.Employees;

public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public UserManagementService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<(UserManagementResult Result, User? User)> CreateAdminUserAsync(
        int employeeId,
        int createdByUserId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return (UserManagementResult.EmployeeNotFound, null);
        if (employee.EmployeeType != "ADMINISTRATOR") return (UserManagementResult.EmployeeNotAdministrator, null);

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (existingUser != null) return (UserManagementResult.UserAlreadyExists, null);

        string targetUsername = employee.Name;
        if (!string.IsNullOrWhiteSpace(employee.Nickname))
        {
            targetUsername = $"{employee.Name} {employee.Nickname}";
        }

        bool usernameExists = await _context.Users.AnyAsync(u => u.Username == targetUsername);
        if (usernameExists)
        {
            return string.IsNullOrWhiteSpace(employee.Nickname)
                ? (UserManagementResult.NicknameRequired, null)
                : (UserManagementResult.UsernameCollision, null);
        }

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "ADMINISTRATOR");
        if (adminRole == null)
            throw new InvalidOperationException("ADMINISTRATOR role not found.");

        string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(employee.NationalId);

        var user = new User
        {
            Username = targetUsername,
            PasswordHash = hashedPassword,
            RoleId = adminRole.RoleId,
            EmployeeId = employeeId,
            IsActive = employee.Status == "ACTIVE",
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("CREATE_USER", "User", user.UserId, $"Created user {targetUsername}");

        return (UserManagementResult.Success, user);
    }

    public async Task<UserManagementResult> DeactivateUserAsync(int userId, int actorUserId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return UserManagementResult.NotFound;
        
        user.IsActive = false;
        user.DeactivatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("DEACTIVATE_USER", "User", userId);
        
        return UserManagementResult.Success;
    }

    public async Task<UserManagementResult> ReactivateUserAsync(int userId, int actorUserId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return UserManagementResult.NotFound;
        
        // Reactivation restores is_active and clears deactivated_at.
        // Username and password_hash are preserved unchanged (Decision 15).
        user.IsActive = true;
        user.DeactivatedAt = null;
        
        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("REACTIVATE_USER", "User", userId);
        
        return UserManagementResult.Success;
    }

    public async Task<PasswordResetResult> ResetPasswordByNationalIdAsync(
        string targetNationalId,
        int actorUserId)
    {
        var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == actorUserId);
        if (actor == null || (actor.Role.Code != "SUPER_ADMIN" && actor.Role.Code != "OWNER"))
            return PasswordResetResult.NotAnAdministrator;

        var targetEmployee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.NationalId == targetNationalId && e.EmployeeType == "ADMINISTRATOR");

        if (targetEmployee == null || targetEmployee.User == null)
        {
            await _auditLog.LogFailureAsync("PASSWORD_RESET", "National ID not found or not an Administrator");
            return PasswordResetResult.TargetNotFound; // Technically NationalIdMismatch from UI perspective
        }

        targetEmployee.User.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(targetEmployee.NationalId);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("PASSWORD_RESET", "User", targetEmployee.User.UserId, 
            "Password reset by Super Admin/Owner");

        return PasswordResetResult.Success;
    }

    public async Task<PasswordResetResult> AdminSelfResetPasswordAsync(
        string username,
        string nationalId)
    {
        var targetUser = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (targetUser == null || targetUser.Employee == null || targetUser.Employee.EmployeeType != "ADMINISTRATOR")
        {
            await _auditLog.LogFailureAsync("PASSWORD_RESET", "User not found or not Administrator");
            return PasswordResetResult.TargetNotFound;
        }

        if (targetUser.Employee.NationalId != nationalId)
        {
            await _auditLog.LogFailureAsync("PASSWORD_RESET", "National ID mismatch", "User", targetUser.UserId);
            return PasswordResetResult.NationalIdMismatch;
        }

        targetUser.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(targetUser.Employee.NationalId);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("PASSWORD_RESET", "User", targetUser.UserId, "Self reset");

        return PasswordResetResult.Success;
    }
}
