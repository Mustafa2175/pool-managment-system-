using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Configuration;

public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly AppDbContext _dbContext;
    
    private const string IsFirstRunSetupCompleteKey = "IsFirstRunSetupComplete";

    public SystemConfigurationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var setting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        return setting?.SettingValue;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        var setting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        if (setting == null)
        {
            setting = new SystemSetting { SettingKey = key, SettingValue = value };
            _dbContext.SystemSettings.Add(setting);
        }
        else
        {
            setting.SettingValue = value;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsFirstRunSetupCompleteAsync()
    {
        var val = await GetSettingAsync(IsFirstRunSetupCompleteKey);
        return val == "TRUE";
    }

    public async Task MarkFirstRunSetupCompleteAsync()
    {
        await SetSettingAsync(IsFirstRunSetupCompleteKey, "TRUE");
    }
}
