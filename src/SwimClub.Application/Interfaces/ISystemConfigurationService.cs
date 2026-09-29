using System.Threading.Tasks;

namespace SwimClub.Application.Interfaces;

public interface ISystemConfigurationService
{
    Task<string?> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value);
    Task<bool> IsFirstRunSetupCompleteAsync();
    Task MarkFirstRunSetupCompleteAsync();
}
