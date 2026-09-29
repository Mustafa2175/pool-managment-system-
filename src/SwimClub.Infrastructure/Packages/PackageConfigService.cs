using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Packages;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Configuration;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Packages;

public class PackageConfigService : IPackageConfigService
{
    private readonly AppDbContext _context;
    private readonly ISystemConfigurationService _sysConfig;

    public PackageConfigService(AppDbContext context, ISystemConfigurationService sysConfig)
    {
        _context = context;
        _sysConfig = sysConfig;
    }

    public async Task<(decimal Price, int DurationMonths, int SessionsPerMonth)> GetActiveTrainingConfigAsync(int programId, string memberStatus)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var config = await _context.PackageConfigs
            .FirstOrDefaultAsync(c => c.ProgramId == programId && c.MemberStatus == memberStatus &&
                                      c.EffectiveFrom <= today && (c.EffectiveTo == null || c.EffectiveTo > today));
                                      
        if (config == null)
            throw new Exception("Config not found");

        return (config.Price, config.DurationMonths, config.SessionsPerMonth);
    }

    public async Task<(decimal Price, int DurationMonths, int SessionsPerMonth)> GetActiveRecreationalConfigAsync(string memberStatus)
    {
        var priceKey = $"RecreationalPackage.{memberStatus}.Price";
        var durationKey = "RecreationalPackage.DurationMonths";
        var sessionsKey = "RecreationalPackage.SessionsPerMonth";

        var priceStr = await _sysConfig.GetSettingAsync(priceKey);
        var durationStr = await _sysConfig.GetSettingAsync(durationKey);
        var sessionsStr = await _sysConfig.GetSettingAsync(sessionsKey);

        if (string.IsNullOrEmpty(priceStr) || string.IsNullOrEmpty(durationStr) || string.IsNullOrEmpty(sessionsStr))
            throw new Exception("Recreational Package config missing");

        return (
            decimal.Parse(priceStr),
            int.Parse(durationStr),
            int.Parse(sessionsStr)
        );
    }

    public async Task SetTrainingConfigAsync(int programId, int durationMonths, int sessionsPerMonth, decimal memberPrice, decimal nonMemberPrice, int actorUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Expire existing
        var existing = await _context.PackageConfigs
            .Where(c => c.ProgramId == programId && c.EffectiveTo == null)
            .ToListAsync();

        foreach (var c in existing)
            c.EffectiveTo = today;

        _context.PackageConfigs.Add(new PackageConfig
        {
            ProgramId = programId,
            DurationMonths = durationMonths,
            SessionsPerMonth = sessionsPerMonth,
            MemberStatus = "MEMBER",
            Price = memberPrice,
            EffectiveFrom = today
        });

        _context.PackageConfigs.Add(new PackageConfig
        {
            ProgramId = programId,
            DurationMonths = durationMonths,
            SessionsPerMonth = sessionsPerMonth,
            MemberStatus = "NON_MEMBER",
            Price = nonMemberPrice,
            EffectiveFrom = today
        });

        await _context.SaveChangesAsync();
    }

    public async Task SetRecreationalConfigAsync(int durationMonths, int sessionsPerMonth, decimal memberPrice, decimal nonMemberPrice, int actorUserId)
    {
        await _sysConfig.SetSettingAsync("RecreationalPackage.MEMBER.Price", memberPrice.ToString());
        await _sysConfig.SetSettingAsync("RecreationalPackage.NON_MEMBER.Price", nonMemberPrice.ToString());
        await _sysConfig.SetSettingAsync("RecreationalPackage.DurationMonths", durationMonths.ToString());
        await _sysConfig.SetSettingAsync("RecreationalPackage.SessionsPerMonth", sessionsPerMonth.ToString());
    }
}
