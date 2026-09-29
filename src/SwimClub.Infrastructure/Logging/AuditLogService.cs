using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Logging;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task LogSuccessAsync(string action, string? entityType = null, int? entityId = null, string? details = null)
    {
        await WriteLogAsync(action, entityType, entityId?.ToString(), details);
    }

    public async Task LogFailureAsync(string action, string reason, string? entityType = null, int? entityId = null)
    {
        await WriteLogAsync(action, entityType, entityId?.ToString(), $"[FAILURE] {reason}");
    }

    public async Task LogSystemEventAsync(string action, bool isSuccess, string? details = null)
    {
        var description = isSuccess ? details : $"[FAILURE] {details}";
        await WriteLogAsync(action, null, null, description, systemEvent: true);
    }

    private async Task WriteLogAsync(string eventType, string? entityType, string? entityId, string? description, bool systemEvent = false)
    {
        int? actorId = systemEvent ? null : _currentUserService.CurrentUser?.UserId;

        var audit = new AuditLog
        {
            UserId = actorId,
            EventType = eventType,
            EntityType = entityType ?? "SYSTEM",
            EntityId = entityId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(audit);
        await _dbContext.SaveChangesAsync();
    }
}
