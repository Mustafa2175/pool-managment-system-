namespace SwimClub.Domain.Entities;

/// <summary>
/// Audit log — insert-only. Records all significant system events.
/// </summary>
public class AuditLog
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string EventType { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
