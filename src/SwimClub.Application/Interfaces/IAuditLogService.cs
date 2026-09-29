using System.Threading.Tasks;

namespace SwimClub.Application.Interfaces;

public interface IAuditLogService
{
    /// <summary>
    /// Logs a successful operation to the audit log.
    /// </summary>
    Task LogSuccessAsync(string action, string? entityType = null, int? entityId = null, string? details = null);

    /// <summary>
    /// Logs a failed or unauthorized operation to the audit log.
    /// </summary>
    Task LogFailureAsync(string action, string reason, string? entityType = null, int? entityId = null);
    
    /// <summary>
    /// Logs a system event (actor_user_id is NULL) to the audit log.
    /// </summary>
    Task LogSystemEventAsync(string action, bool isSuccess, string? details = null);
}
