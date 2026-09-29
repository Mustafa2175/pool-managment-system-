namespace SwimClub.Application.Security;

public interface IAuthorizationGuard
{
    /// <summary>
    /// Checks if the current user has permission to perform the specified action.
    /// If not, throws an UnauthorizedAccessException and logs the failure to the Audit Log.
    /// </summary>
    /// <param name="action">The action to authorize (from AppActions).</param>
    /// <param name="targetRoleCode">Optional. If the action is MANAGE_USERS, specifies the role being managed.</param>
    void Authorize(string action, string? targetRoleCode = null);
    
    /// <summary>
    /// Checks if the current user has permission to perform the specified action without throwing.
    /// </summary>
    bool HasPermission(string action, string? targetRoleCode = null);
}
