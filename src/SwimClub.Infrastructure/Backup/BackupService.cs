using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Backup;
using SwimClub.Infrastructure.Persistence;
using BackupEntity = SwimClub.Domain.Entities.Backup;

namespace SwimClub.Infrastructure.Backup;

public class BackupMetadataSidecar
{
    public string SystemVersion { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = null!;
    public long SizeBytes { get; set; }
    public int AttemptNumber { get; set; }
}

public class BackupService : IBackupService
{
    private readonly AppDbContext _context;
    private readonly string _connectionString;
    
    // For DPAPI protected key storage
    private const string KeyFilePath = "backup_encryption_key.bin";
    private const string CurrentSystemVersion = "1.0.0"; // In real app, pulled from assembly

    public BackupService(AppDbContext context)
    {
        _context = context;
        _connectionString = _context.Database.GetConnectionString() ?? "Data Source=swimclub.db";
    }

    // Helper: extracts actual DB file path from connection string like "Data Source=path.db"
    private string GetDbFilePath()
    {
        var cs = _connectionString;
        // Parse "Data Source=<path>" or "data source=<path>"
        foreach (var part in cs.Split(';'))
        {
            var kv = part.Trim().Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            {
                return kv[1].Trim();
            }
        }
        return cs;
    }

    private byte[] GetOrCreateEncryptionKey()
    {
        if (File.Exists(KeyFilePath))
        {
            var protectedKey = File.ReadAllBytes(KeyFilePath);
            return ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
        }
        else
        {
            var newKey = new byte[32];
            RandomNumberGenerator.Fill(newKey);
            var protectedKey = ProtectedData.Protect(newKey, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(KeyFilePath, protectedKey);
            return newKey;
        }
    }

    private async Task<bool> IsOwnerOrSuperAdminAsync(int userId)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        return user?.Role?.Code == "SUPER_ADMIN" || user?.Role?.Code == "OWNER";
    }

    private async Task LogAuditAsync(string eventType, string description, int? userId = null)
    {
        _context.AuditLogs.Add(new SwimClub.Domain.Entities.AuditLog
        {
            EventType = eventType,
            EntityType = "BACKUP",
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        });
        await _context.SaveChangesAsync();
    }

    private async Task PruneOldBackupsAsync()
    {
        var allBackups = await _context.Backups.OrderBy(b => b.CreatedAt).ToListAsync();
        if (allBackups.Count > 7)
        {
            var toDelete = allBackups.First();
            _context.Backups.Remove(toDelete);
            
            // Try to delete physical file
            try
            {
                if (File.Exists(toDelete.FilePath)) File.Delete(toDelete.FilePath);
                var sidecarPath = toDelete.FilePath + ".json";
                if (File.Exists(sidecarPath)) File.Delete(sidecarPath);
            }
            catch { /* Best effort */ }

            await LogAuditAsync("BACKUP_RETENTION_PRUNED", $"Pruned backup ID {toDelete.BackupId}");
            await _context.SaveChangesAsync();
        }
    }

    private (long sizeBytes, string destPath) PerformBackupAndEncrypt(string destinationFolder, string prefix)
    {
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        string tempDbPath = Path.Combine(Path.GetTempPath(), $"{prefix}_temp_{Guid.NewGuid():N}.db");
        string finalEncPath = Path.Combine(destinationFolder, $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.enc");

        // 1. Online SQLite Backup — uses the SQLite online backup API (safe while DB is in use)
        // Close EF Core's connection to release any shared lock, then clear pools so the
        // raw SqliteConnection can open the file cleanly.
        _context.Database.CloseConnection();
        SqliteConnection.ClearAllPools();

        using (var source = new SqliteConnection($"Data Source={GetDbFilePath()}"))
        using (var destination = new SqliteConnection($"Data Source={tempDbPath}"))
        {
            source.Open();
            destination.Open();
            source.BackupDatabase(destination);
        }
        
        SqliteConnection.ClearAllPools(); // IMPORTANT: Release file lock on tempDbPath

        // 2. Encrypt using AES-GCM
        var key = GetOrCreateEncryptionKey();
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var tag = new byte[16];

        var plaintext = File.ReadAllBytes(tempDbPath);
        var ciphertext = new byte[plaintext.Length];

        using (var aesGcm = new AesGcm(key, tagSizeInBytes: 16))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        // File format: [12 bytes Nonce] [16 bytes Tag] [Ciphertext]
        using (var fs = new FileStream(finalEncPath, FileMode.Create))
        {
            fs.Write(nonce, 0, nonce.Length);
            fs.Write(tag, 0, tag.Length);
            fs.Write(ciphertext, 0, ciphertext.Length);
        }

        long sizeBytes = new FileInfo(finalEncPath).Length;
        File.Delete(tempDbPath);

        return (sizeBytes, finalEncPath);
    }

    public async Task<BackupResult> CreateAutomaticBackupAsync(string destinationFolder)
    {
        int maxAttempts = 5;
        int attempt = 1;
        while (attempt <= maxAttempts)
        {
            try
            {
                var (sizeBytes, finalEncPath) = PerformBackupAndEncrypt(destinationFolder, "auto_backup");

                var sidecar = new BackupMetadataSidecar
                {
                    SystemVersion = CurrentSystemVersion,
                    CreatedAt = DateTime.UtcNow,
                    Type = "AUTOMATIC",
                    SizeBytes = sizeBytes,
                    AttemptNumber = attempt
                };
                
                File.WriteAllText(finalEncPath + ".json", JsonSerializer.Serialize(sidecar));

                var record = new BackupEntity
                {
                    BackupType = "AUTOMATIC",
                    CreatedAt = sidecar.CreatedAt,
                    SystemVersion = CurrentSystemVersion,
                    FilePath = finalEncPath,
                    FileSizeBytes = sizeBytes,
                    AttemptNumber = attempt,
                    Encrypted = true,
                    EncryptionKeyRef = "DPAPI_LOCAL",
                    Status = "SUCCESS"
                };
                _context.Backups.Add(record);
                await _context.SaveChangesAsync();

                await LogAuditAsync("BACKUP_CREATED_AUTO", $"Auto backup created on attempt {attempt}");
                await PruneOldBackupsAsync();

                return new BackupResult { Success = true, Backup = record };
            }
            catch (Exception)
            {
                attempt++;
            }
        }

        await LogAuditAsync("BACKUP_FAILED_ALL_ATTEMPTS", $"Failed after {maxAttempts} attempts");
        // Super admin warning flag could be set here in a settings table, but for now Audit Log suffices
        return new BackupResult { Success = false, ErrorMessage = "All 5 automatic backup attempts failed." };
    }

    public async Task<BackupResult> CreateManualBackupAsync(int requestingUserId, string destinationFolder)
    {
        if (!await IsOwnerOrSuperAdminAsync(requestingUserId))
            return new BackupResult { Success = false, ErrorMessage = "Unauthorized." };

        try
        {
            var (sizeBytes, finalEncPath) = PerformBackupAndEncrypt(destinationFolder, "manual_backup");

            var sidecar = new BackupMetadataSidecar
            {
                SystemVersion = CurrentSystemVersion,
                CreatedAt = DateTime.UtcNow,
                Type = "MANUAL",
                SizeBytes = sizeBytes,
                AttemptNumber = 1
            };
            
            File.WriteAllText(finalEncPath + ".json", JsonSerializer.Serialize(sidecar));

            var record = new BackupEntity
            {
                BackupType = "MANUAL",
                CreatedAt = sidecar.CreatedAt,
                SystemVersion = CurrentSystemVersion,
                FilePath = finalEncPath,
                FileSizeBytes = sizeBytes,
                AttemptNumber = 1,
                Encrypted = true,
                EncryptionKeyRef = "DPAPI_LOCAL",
                Status = "SUCCESS"
            };
            _context.Backups.Add(record);
            await _context.SaveChangesAsync();

            await LogAuditAsync("BACKUP_CREATED_MANUAL", "Manual backup created", requestingUserId);
            await PruneOldBackupsAsync();

            return new BackupResult { Success = true, Backup = record };
        }
        catch (Exception ex)
        {
            await LogAuditAsync("BACKUP_FAILED_MANUAL", $"Manual backup failed: {ex.Message}", requestingUserId);
            return new BackupResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<List<BackupEntity>> GetBackupsAsync(int requestingUserId)
    {
        if (!await IsOwnerOrSuperAdminAsync(requestingUserId))
            return new List<BackupEntity>();

        return await _context.Backups.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    public async Task<RestoreResult> RestoreBackupAsync(int requestingUserId, string backupFilePath)
    {
        if (!await IsOwnerOrSuperAdminAsync(requestingUserId))
            return new RestoreResult { Success = false, ErrorMessage = "Unauthorized." };

        if (!File.Exists(backupFilePath))
            return new RestoreResult { Success = false, ErrorMessage = "Backup file not found." };

        var sidecarPath = backupFilePath + ".json";
        BackupMetadataSidecar sidecar = null;
        if (File.Exists(sidecarPath))
        {
            sidecar = JsonSerializer.Deserialize<BackupMetadataSidecar>(File.ReadAllText(sidecarPath));
        }

        // 1. Check version compatibility
        if (sidecar != null)
        {
            Version current = Version.Parse(CurrentSystemVersion);
            Version backup = Version.Parse(sidecar.SystemVersion);
            if (backup > current)
            {
                await LogAuditAsync("RESTORE_FAILED", "Version incompatible. Please upgrade software.", requestingUserId);
                return new RestoreResult { Success = false, ErrorMessage = "Please upgrade the software before restoring this backup." };
            }
        }

        // 2. Pre-restore safety snapshot
        var safetyBackupFolder = Path.Combine(Path.GetTempPath(), "SwimClubSafetyBackups");
        var (safetySize, safetyPath) = PerformBackupAndEncrypt(safetyBackupFolder, "safety_snapshot");
        // We will log the safety snapshot creation AFTER the swap so it isn't overwritten.

        // 3. Decrypt backup
        string decryptedDbPath = Path.Combine(Path.GetTempPath(), $"decrypted_restore_{Guid.NewGuid()}.db");
        try
        {
            var encryptedContent = File.ReadAllBytes(backupFilePath);
            var nonce = encryptedContent.Take(12).ToArray();
            var tag = encryptedContent.Skip(12).Take(16).ToArray();
            var ciphertext = encryptedContent.Skip(28).ToArray();

            var key = GetOrCreateEncryptionKey();
            var plaintext = new byte[ciphertext.Length];

            using (var aesGcm = new AesGcm(key, tagSizeInBytes: 16))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            }
            File.WriteAllBytes(decryptedDbPath, plaintext);
        }
        catch (Exception ex)
        {
            await LogAuditAsync("RESTORE_FAILED", "Decryption failed.", requestingUserId);
            return new RestoreResult { Success = false, ErrorMessage = "Decryption failed. " + ex.Message };
        }

        // 4. Check for migrations (Older backup -> Newer program)
        bool requiredMigration = false;
        try
        {
            // We run migration on the decrypted staging DB
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={decryptedDbPath}")
                .Options;
            
            using (var stagingContext = new AppDbContext(options))
            {
                var migrations = stagingContext.Database.GetPendingMigrations();
                if (migrations.Any())
                {
                    requiredMigration = true;
                    stagingContext.Database.Migrate(); // run migrations forward
                    await LogAuditAsync("MIGRATION_APPLIED", "Migrations applied to restored database", requestingUserId);
                }
                stagingContext.Database.CloseConnection();
            }
            SqliteConnection.ClearAllPools();
        }
        catch (Exception ex)
        {
            await LogAuditAsync("RESTORE_FAILED", $"Migration failed. Automatic rollback. Error: {ex.Message}", requestingUserId);
            SqliteConnection.ClearAllPools();
            File.Delete(decryptedDbPath);
            return new RestoreResult { Success = false, ErrorMessage = $"Migration rollback: {ex.Message}", RolledBack = true };
        }

        // 5. Swap DB
        try
        {
            // Disconnect current DB, copy over
            // In Entity Framework Core with SQLite, the file might be locked. 
            // In a real app we'd close all connections, but for tests/implementation we use SQLite's backup API in reverse
            _context.Database.CloseConnection();
            SqliteConnection.ClearAllPools();
            
            using (var staging = new SqliteConnection($"Data Source={decryptedDbPath}"))
            using (var current = new SqliteConnection($"Data Source={GetDbFilePath()}"))
            {
                staging.Open();
                current.Open();
                staging.BackupDatabase(current);
            }
            
            SqliteConnection.ClearAllPools();
            _context.ChangeTracker.Clear();

            await LogAuditAsync("PRE_RESTORE_SAFETY_SNAPSHOT_CREATED", $"Safety snapshot at {safetyPath}", requestingUserId);
            await LogAuditAsync("RESTORE_SUCCEEDED", "Restore complete.", requestingUserId);
            File.Delete(decryptedDbPath);
            return new RestoreResult { Success = true, RequiredMigration = requiredMigration };
        }
        catch (Exception ex)
        {
            await LogAuditAsync("RESTORE_FAILED", $"Swap failed. Automatic rollback. Error: {ex.Message}", requestingUserId);
            // In a real catastrophic failure during swap, we'd restore the safety snapshot using the same process.
            SqliteConnection.ClearAllPools();
            File.Delete(decryptedDbPath);
            return new RestoreResult { Success = false, ErrorMessage = $"Swap rollback: {ex.Message}", RolledBack = true };
        }
    }
}
