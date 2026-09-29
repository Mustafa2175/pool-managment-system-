using System.Threading.Tasks;
using SwimClub.Application.Interfaces;

namespace SwimClub.UI.State;

public class FirstRunSetupManager
{
    private readonly ISystemConfigurationService _configService;

    public FirstRunSetupManager(ISystemConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task<bool> CheckRequiresSetupAsync()
    {
        bool isComplete = await _configService.IsFirstRunSetupCompleteAsync();
        return !isComplete;
    }
}
