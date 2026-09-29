namespace SwimClub.Domain.Entities;

/// <summary>
/// System settings — key/value pairs, system-wide (not per-user).
/// Includes 'language' (AR | EN) set during Initial Program Setup (Decision 36).
/// </summary>
public class SystemSetting
{
    public string SettingKey { get; set; } = null!;
    public string SettingValue { get; set; } = null!;
}
