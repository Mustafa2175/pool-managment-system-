using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Security;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public AuthService(AppDbContext dbContext, ICurrentUserService currentUserService, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        // Always query DB first — then verify hash — generic error prevents user-enumeration
        var user = await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == username);

        // Timing-safe: hash verify even when user is null (with a dummy hash) to prevent timing attacks
        string hashToVerify = user?.PasswordHash ?? BCrypt.Net.BCrypt.EnhancedHashPassword("dummy-prevent-timing");
        bool passwordValid = BCrypt.Net.BCrypt.EnhancedVerify(password, hashToVerify);

        if (user == null || !passwordValid)
        {
            // Generic message — never leak whether username exists
            await _auditLogService.LogSystemEventAsync("LOGIN_FAILED", false, $"Attempted username: {username}");
            return LoginResult.InvalidCredentials;
        }

        if (!user.IsActive)
        {
            // Log deactivated attempt but return same InvalidCredentials enum to UI,
            // which must display a generic "Invalid username or password" message
            await _auditLogService.LogSystemEventAsync("LOGIN_FAILED", false, $"Deactivated account attempted: {username}");
            return LoginResult.Deactivated;
        }

        _currentUserService.SetCurrentUser(user);
        await _auditLogService.LogSuccessAsync("LOGIN_SUCCESS", "User", user.UserId);
        
        return LoginResult.Success;
    }

    public void Logout()
    {
        _currentUserService.Clear();
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(string adminUsername, string adminNationalId, string targetNationalId)
    {
        // Must be called by a logged in SuperAdmin or Owner, enforced by Auth Guard before this method, 
        // but this verifies the identity requirement: adminNationalId
        var adminUser = await _dbContext.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == adminUsername);

        if (adminUser?.Employee == null || adminUser.Employee.NationalId != adminNationalId)
        {
            await _auditLogService.LogFailureAsync("PASSWORD_RESET_ATTEMPT", "Admin Identity Verification Failed");
            return ResetPasswordResult.AdminNotFound;
        }

        // Find the target employee
        var targetEmployee = await _dbContext.Employees
            .Include(e => e.User)
            .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(e => e.NationalId == targetNationalId);

        if (targetEmployee?.User == null)
        {
            await _auditLogService.LogFailureAsync("PASSWORD_RESET_ATTEMPT", $"Target NationalId {targetNationalId} not found");
            return ResetPasswordResult.TargetNotFound;
        }

        // Owners can only reset Administrators (Role Code "ADMINISTRATOR")
        // Super Admins can reset anyone.
        if (adminUser.Role!.Code == "OWNER" && targetEmployee.User.Role!.Code != "ADMINISTRATOR")
        {
            await _auditLogService.LogFailureAsync("PASSWORD_RESET_ATTEMPT", $"Owner attempted to reset non-Administrator {targetNationalId}");
            return ResetPasswordResult.UnauthorizedTarget;
        }

        // Seed password hash to the employee's National ID
        string newHash = BCrypt.Net.BCrypt.HashPassword(targetNationalId);
        targetEmployee.User.PasswordHash = newHash;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogSuccessAsync("PASSWORD_RESET", "User", targetEmployee.User.UserId, "Password reset to National ID");
        
        return ResetPasswordResult.Success;
    }
}
