using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackupEntity = SwimClub.Domain.Entities.Backup;

namespace SwimClub.Application.Backup;

public class BackupResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public BackupEntity? Backup { get; set; }
}

public class RestoreResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiredMigration { get; set; }
    public bool RolledBack { get; set; }
}

public interface IBackupService
{
    /// <summary>
    /// Triggers an automatic backup (usually invoked by a scheduled job).
    /// Enforces the 5-attempt retry loop and Super Admin notification logic on failure.
    /// </summary>
    Task<BackupResult> CreateAutomaticBackupAsync(string destinationFolder);

    /// <summary>
    /// Creates a manual backup on-demand.
    /// Only one attempt is made; fails immediately if unsuccessful.
    /// </summary>
    Task<BackupResult> CreateManualBackupAsync(int requestingUserId, string destinationFolder);

    /// <summary>
    /// Retrieves a list of all retained backups.
    /// </summary>
    Task<List<BackupEntity>> GetBackupsAsync(int requestingUserId);

    /// <summary>
    /// Restores the system from a specific backup file.
    /// Executes the safe restore sequence: Pre-restore snapshot -> decrypt -> version check -> migration -> swap.
    /// </summary>
    Task<RestoreResult> RestoreBackupAsync(int requestingUserId, string backupFilePath);
}
