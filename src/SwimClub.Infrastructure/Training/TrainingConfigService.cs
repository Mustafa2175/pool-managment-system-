using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Training;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Training;

public class TrainingConfigService : ITrainingConfigService
{
    private readonly AppDbContext _context;
    private readonly ISystemConfigurationService _sysConfig;
    private readonly IAuditLogService _auditLog;

    private const string GlobalSessionCountKey = "TrainingSessionCount";

    public TrainingConfigService(AppDbContext context, ISystemConfigurationService sysConfig, IAuditLogService auditLog)
    {
        _context = context;
        _sysConfig = sysConfig;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<Program>> GetProgramsAsync()
    {
        return await _context.Programs.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<decimal> GetActivePriceAsync(int programId, string memberStatus)
    {
        var config = await _context.TrainingPriceConfigs
            .Where(c => c.ProgramId == programId && c.MemberStatus == memberStatus)
            .Where(c => c.EffectiveFrom <= DateOnly.FromDateTime(DateTime.UtcNow) && (c.EffectiveTo == null || c.EffectiveTo >= DateOnly.FromDateTime(DateTime.UtcNow)))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();

        return config?.Price ?? 0m;
    }

    public async Task UpdatePriceAsync(int programId, string memberStatus, decimal newPrice)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Find currently active configs and expire them yesterday
        var activeConfigs = await _context.TrainingPriceConfigs
            .Where(c => c.ProgramId == programId && c.MemberStatus == memberStatus && c.EffectiveTo == null)
            .ToListAsync();

        foreach (var active in activeConfigs)
        {
            active.EffectiveTo = today.AddDays(-1);
        }

        // Add new config
        var newConfig = new TrainingPriceConfig
        {
            ProgramId = programId,
            MemberStatus = memberStatus,
            Price = newPrice,
            EffectiveFrom = today,
            EffectiveTo = null
        };

        _context.TrainingPriceConfigs.Add(newConfig);
        await _context.SaveChangesAsync();
        await _auditLog.LogSystemEventAsync("TRAINING_PRICE_CHANGED", true, $"Program {programId} for {memberStatus} is now {newPrice}");
    }

    public async Task<int> GetGlobalSessionCountAsync()
    {
        var val = await _sysConfig.GetSettingAsync(GlobalSessionCountKey);
        if (int.TryParse(val, out int count) && count > 0)
        {
            return count;
        }
        return 12; // Default if not configured
    }

    public async Task SetGlobalSessionCountAsync(int count)
    {
        await _sysConfig.SetSettingAsync(GlobalSessionCountKey, count.ToString());
        await _auditLog.LogSystemEventAsync("TRAINING_SESSION_COUNT_CHANGED", true, $"New count: {count}");
    }
}
