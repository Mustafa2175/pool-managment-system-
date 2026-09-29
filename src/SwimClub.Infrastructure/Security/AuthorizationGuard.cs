using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;

namespace SwimClub.Infrastructure.Security;

public class AuthorizationGuard : IAuthorizationGuard
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    // Roles
    private const string SA = "SUPER_ADMIN";
    private const string OW = "OWNER";
    private const string AD = "ADMINISTRATOR";

    private readonly Dictionary<string, string[]> _actionPermissions = new()
    {
        { AppActions.LOGIN, new[] { SA, OW, AD } }, // Base
        
        { AppActions.CREATE_SWIMMER, new[] { SA, AD } },
        { AppActions.EDIT_SWIMMER, new[] { SA, AD } },
        { AppActions.DELETE_SWIMMER, new[] { SA, AD } },
        { AppActions.VIEW_SWIMMER, new[] { SA, AD } },
        
        { AppActions.CREATE_TRAINING_PERIOD, new[] { SA } },
        { AppActions.EDIT_TRAINING_PERIOD, new[] { SA, AD } },
        { AppActions.ASSIGN_COACH, new[] { SA, AD } },
        { AppActions.ASSIGN_LIFEGUARD, new[] { SA, AD } },
        
        { AppActions.VIEW_EMPLOYEE, new[] { SA, OW, AD } },
        { AppActions.EDIT_EMPLOYEE, new[] { SA, AD } },
        
        { AppActions.RECORD_PAYMENT, new[] { SA, OW, AD } },
        { AppActions.RECORD_EXPENSE, new[] { SA, OW, AD } },
        { AppActions.MARK_PAYROLL_PAID, new[] { SA, OW, AD } },
        { AppActions.CORRECT_PAYMENT, new[] { SA, AD } },
        { AppActions.ADJUST_PAYROLL, new[] { SA, OW, AD } },
        { AppActions.ADJUST_REFUND, new[] { SA, OW, AD } },
        
        { AppActions.RESET_PASSWORD, new[] { SA, OW } },
        { AppActions.REACTIVATE_USER, new[] { SA, OW } },
        { AppActions.MANAGE_USERS, new[] { SA, OW } },
        
        { AppActions.EDIT_GENERAL_CONFIGURATION, new[] { SA } },
        { AppActions.EDIT_PRICING_RATES, new[] { SA } },
        { AppActions.MANAGE_QUALIFICATIONS_AND_RATES, new[] { SA } },
        { AppActions.EDIT_CANCELLATION_FEE, new[] { SA, OW } },
        { AppActions.BACKUP_RESTORE, new[] { SA } },
        { AppActions.VIEW_AUDIT_LOG, new[] { SA, OW } },
        { AppActions.MANAGE_LANES, new[] { SA } }
    };

    public AuthorizationGuard(ICurrentUserService currentUserService, IAuditLogService auditLogService)
    {
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public void Authorize(string action, string? targetRoleCode = null)
    {
        if (!HasPermission(action, targetRoleCode))
        {
            var user = _currentUserService.CurrentUser;
            // Fire-and-forget the async audit log (sync method boundary in desktop app).
            // Use Task.Run to avoid deadlocking WPF SynchronizationContext.
            Task.Run(async () =>
            {
                try
                {
                    await _auditLogService.LogFailureAsync(
                        "UNAUTHORIZED_ACTION_ATTEMPT",
                        $"Action: {action}, TargetRole: {targetRoleCode ?? "none"}, ActorRole: {user?.Role?.Code ?? "Unauthenticated"}");
                }
                catch { /* Audit must never throw */ }
            });
            
            throw new UnauthorizedAccessException("You do not have permission to perform this action.");
        }
    }

    public async Task AuthorizeAsync(string action, string? targetRoleCode = null)
    {
        if (!HasPermission(action, targetRoleCode))
        {
            var user = _currentUserService.CurrentUser;
            await _auditLogService.LogFailureAsync(
                "UNAUTHORIZED_ACTION_ATTEMPT",
                $"Action: {action}, TargetRole: {targetRoleCode ?? "none"}, ActorRole: {user?.Role?.Code ?? "Unauthenticated"}");
            throw new UnauthorizedAccessException("You do not have permission to perform this action.");
        }
    }

    public bool HasPermission(string action, string? targetRoleCode = null)
    {
        var user = _currentUserService.CurrentUser;
        if (user == null || user.Role == null) return false;

        var userRole = user.Role.Code;

        if (!_actionPermissions.TryGetValue(action, out var allowedRoles))
        {
            return false; // Fail secure if action not registered
        }

        bool isAllowed = Array.Exists(allowedRoles, r => r == userRole);

        // Special Context Checks
        if (isAllowed && userRole == OW)
        {
            // Owner can only manage/reactivate/reset ADMINISTRATOR users
            if ((action == AppActions.MANAGE_USERS || action == AppActions.REACTIVATE_USER || action == AppActions.RESET_PASSWORD) 
                && targetRoleCode != AD)
            {
                return false;
            }
        }

        return isAllowed;
    }
}
