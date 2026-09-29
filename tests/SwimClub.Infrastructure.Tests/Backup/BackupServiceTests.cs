using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Backup;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Backup;
using SwimClub.Infrastructure.Persistence;
using Xunit;
using BackupEntity = SwimClub.Domain.Entities.Backup;

namespace SwimClub.Infrastructure.Tests.Backup;

public class BackupServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private BackupService _service = null!;
    private string _tempFolder = null!;
    private int _superAdminUserId;
    private int _adminUserId;
    private string _dbPath = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"swimclub-backup-test-{Guid.NewGuid()}.db");
        _tempFolder = Path.Combine(Path.GetTempPath(), $"swimclub-backups-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempFolder);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Ensure EF thinks migrations are applied so Restore backup's Migrate() doesn't fail trying to recreate tables
        await _context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY, ProductVersion TEXT NOT NULL); " +
            "INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260923164853_InitialCreate', '8.0.0');");

        _service = new BackupService(_context);

        // Roles
        var saRole = new Role { Code = "SUPER_ADMIN", NameEn = "SA", NameAr = "SA" };
        var adminRole = new Role { Code = "ADMINISTRATOR", NameEn = "Admin", NameAr = "Admin" };
        _context.Roles.AddRange(saRole, adminRole);
        await _context.SaveChangesAsync();

        var saUser = new User { Username = "sa", PasswordHash = "hash", RoleId = saRole.RoleId, IsActive = true };
        var adminUser = new User { Username = "admin", PasswordHash = "hash", RoleId = adminRole.RoleId, IsActive = true };
        _context.Users.AddRange(saUser, adminUser);
        await _context.SaveChangesAsync();

        _superAdminUserId = saUser.UserId;
        _adminUserId = adminUser.UserId;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        SqliteConnection.ClearAllPools();
        
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* locked – GC will clean temp */ }
        try { if (File.Exists("backup_encryption_key.bin")) File.Delete("backup_encryption_key.bin"); } catch { }
    }

    // ─────────────────────────────────────────────────────────────
    // 1. Backup Creation
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateManualBackup_SuperAdmin_SucceedsAndCreatesFile()
    {
        var result = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);

        Assert.True(result.Success, result.ErrorMessage ?? "No error message returned");
        Assert.NotNull(result.Backup);
        Assert.True(File.Exists(result.Backup.FilePath));
    }

    [Fact]
    public async Task CreateManualBackup_NonSuperAdmin_Unauthorized()
    {
        var result = await _service.CreateManualBackupAsync(_adminUserId, _tempFolder);
        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.ErrorMessage!);
    }

    [Fact]
    public async Task CreateAutomaticBackup_Succeeds()
    {
        var result = await _service.CreateAutomaticBackupAsync(_tempFolder);
        Assert.True(result.Success);
        Assert.NotNull(result.Backup);
        Assert.Equal("AUTOMATIC", result.Backup.BackupType);
    }

    // ─────────────────────────────────────────────────────────────
    // 2. Encryption
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task BackupFile_IsEncrypted_CannotBeReadAsPlainSQLite()
    {
        var result = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(result.Success);

        var bytes = File.ReadAllBytes(result.Backup!.FilePath);
        // SQLite files start with "SQLite format 3\0"
        var sqliteHeader = Encoding.ASCII.GetString(bytes.Take(16).ToArray());
        Assert.DoesNotContain("SQLite format 3", sqliteHeader);
    }

    [Fact]
    public async Task BackupRecord_IsEncryptedFlagTrue()
    {
        var result = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(result.Backup!.Encrypted);
        Assert.Equal("DPAPI_LOCAL", result.Backup.EncryptionKeyRef);
    }

    // ─────────────────────────────────────────────────────────────
    // 3. Metadata (sidecar)
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task BackupSidecar_ContainsMetadata()
    {
        var result = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(result.Success);

        var sidecarPath = result.Backup!.FilePath + ".json";
        Assert.True(File.Exists(sidecarPath));

        var sidecar = JsonSerializer.Deserialize<BackupMetadataSidecar>(File.ReadAllText(sidecarPath));
        Assert.NotNull(sidecar);
        Assert.Equal("1.0.0", sidecar!.SystemVersion);
        Assert.True(sidecar.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task BackupRecord_ContainsVersionAndTimestamp()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.Equal("1.0.0", result.Backup!.SystemVersion);
        Assert.InRange(result.Backup.CreatedAt, before, after);
    }

    // ─────────────────────────────────────────────────────────────
    // 4. Retention – Max 7, delete oldest on 8th
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Retention_After8Backups_OldestIsDeleted()
    {
        // Create 7 backups
        BackupResult? firstResult = null;
        for (int i = 0; i < 7; i++)
        {
            var r = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
            if (i == 0) firstResult = r;
            await Task.Delay(10); // ensure distinct timestamps
        }

        var oldestPath = firstResult!.Backup!.FilePath;
        Assert.True(File.Exists(oldestPath));

        // Create 8th backup – should prune oldest
        await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);

        var allBackups = await _context.Backups.ToListAsync();
        Assert.Equal(7, allBackups.Count);
        Assert.False(File.Exists(oldestPath), "Oldest physical file should be deleted");
    }

    [Fact]
    public async Task Retention_PruneAuditLog_Written()
    {
        for (int i = 0; i < 8; i++)
        {
            await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
            await Task.Delay(10);
        }

        var log = await _context.AuditLogs
            .FirstOrDefaultAsync(l => l.EventType == "BACKUP_RETENTION_PRUNED");
        Assert.NotNull(log);
    }

    // ─────────────────────────────────────────────────────────────
    // 5. Automatic Retry & Failure Logging
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task AutoBackup_FailsOnInvalidPath_LogsFailureAfter5Attempts()
    {
        var invalidPath = "Z:\\NonexistentDrive\\Backup"; // drive that doesn't exist
        var result = await _service.CreateAutomaticBackupAsync(invalidPath);

        Assert.False(result.Success);
        Assert.Contains("5 automatic backup attempts failed", result.ErrorMessage!);

        var log = await _context.AuditLogs
            .FirstOrDefaultAsync(l => l.EventType == "BACKUP_FAILED_ALL_ATTEMPTS");
        Assert.NotNull(log);
    }

    // ─────────────────────────────────────────────────────────────
    // 6. Restore
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Restore_ValidBackup_Succeeds()
    {
        // Seed a swimmer pre-backup
        _context.Swimmers.Add(new Swimmer { SwimmerId = "RESTORE_TEST", Name = "Test Swimmer", DateOfBirth = new DateOnly(2000,1,1), Gender = "MALE", MemberStatus = "MEMBER", QrToken = "TOKZ", Status = "ACTIVE" });
        await _context.SaveChangesAsync();

        var backupResult = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(backupResult.Success);

        var restoreResult = await _service.RestoreBackupAsync(_superAdminUserId, backupResult.Backup!.FilePath);
        Assert.True(restoreResult.Success, restoreResult.ErrorMessage);

        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(l => l.EventType == "RESTORE_SUCCEEDED");
        Assert.NotNull(auditLog);
    }

    [Fact]
    public async Task Restore_CreatesPreRestoreSafetySnapshot()
    {
        var backupResult = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(backupResult.Success);

        await _service.RestoreBackupAsync(_superAdminUserId, backupResult.Backup!.FilePath);

        var safetyLog = await _context.AuditLogs
            .FirstOrDefaultAsync(l => l.EventType == "PRE_RESTORE_SAFETY_SNAPSHOT_CREATED");
        Assert.NotNull(safetyLog);
    }

    [Fact]
    public async Task Restore_NonSuperAdmin_Unauthorized()
    {
        var backupResult = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(backupResult.Success);

        var restoreResult = await _service.RestoreBackupAsync(_adminUserId, backupResult.Backup!.FilePath);
        Assert.False(restoreResult.Success);
        Assert.Contains("Unauthorized", restoreResult.ErrorMessage!);
    }

    [Fact]
    public async Task Restore_MissingFile_Fails()
    {
        var result = await _service.RestoreBackupAsync(_superAdminUserId, "/nonexistent/file.enc");
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage!);
    }

    // ─────────────────────────────────────────────────────────────
    // 7. Exact-State Verification After Restore
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Restore_ExactState_SwimmersPresentAfterRestore()
    {
        var swimmerId = $"EXACT_{Guid.NewGuid().ToString()[..8]}";
        _context.Swimmers.Add(new Swimmer { SwimmerId = swimmerId, Name = "Exact Test", DateOfBirth = new DateOnly(1995,5,10), Gender = "FEMALE", MemberStatus = "NON_MEMBER", QrToken = swimmerId, Status = "ACTIVE" });
        await _context.SaveChangesAsync();

        var backupResult = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(backupResult.Success);

        // Delete the swimmer post-backup
        var swimmer = await _context.Swimmers.FindAsync(swimmerId);
        _context.Swimmers.Remove(swimmer!);
        await _context.SaveChangesAsync();

        // Restore
        var restoreResult = await _service.RestoreBackupAsync(_superAdminUserId, backupResult.Backup!.FilePath);
        Assert.True(restoreResult.Success, restoreResult.ErrorMessage);

        // Verify swimmer is back
        var restored = await _context.Swimmers.FindAsync(swimmerId);
        Assert.NotNull(restored);
        Assert.Equal("Exact Test", restored!.Name);
    }

    // ─────────────────────────────────────────────────────────────
    // 8. Migration – newer backup rejected
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Restore_NewerBackupVersion_IsRejected()
    {
        // Create a backup and manually mutate its sidecar version to be newer
        var backupResult = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(backupResult.Success);

        var sidecarPath = backupResult.Backup!.FilePath + ".json";
        var sidecar = JsonSerializer.Deserialize<BackupMetadataSidecar>(File.ReadAllText(sidecarPath))!;
        sidecar.SystemVersion = "9.0.0"; // Future version
        File.WriteAllText(sidecarPath, JsonSerializer.Serialize(sidecar));

        var restoreResult = await _service.RestoreBackupAsync(_superAdminUserId, backupResult.Backup.FilePath);

        Assert.False(restoreResult.Success);
        Assert.Contains("upgrade", restoreResult.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Restore_NewerBackupRejection_LogsAudit()
    {
        var backupResult = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(backupResult.Success);

        var sidecarPath = backupResult.Backup!.FilePath + ".json";
        var sidecar = JsonSerializer.Deserialize<BackupMetadataSidecar>(File.ReadAllText(sidecarPath))!;
        sidecar.SystemVersion = "99.0.0";
        File.WriteAllText(sidecarPath, JsonSerializer.Serialize(sidecar));

        await _service.RestoreBackupAsync(_superAdminUserId, backupResult.Backup.FilePath);

        var log = await _context.AuditLogs.FirstOrDefaultAsync(l => l.EventType == "RESTORE_FAILED");
        Assert.NotNull(log);
        Assert.Contains("incompatible", log!.Description!, StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────
    // 9. Newer program restoring older backup (migration path)
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Restore_OlderBackupVersion_IsPermittedAndSucceeds()
    {
        // Backup with an older version sidecar — current system (1.0.0) is newer, so it should succeed
        var backupResult = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(backupResult.Success);

        var sidecarPath = backupResult.Backup!.FilePath + ".json";
        var sidecar = JsonSerializer.Deserialize<BackupMetadataSidecar>(File.ReadAllText(sidecarPath))!;
        sidecar.SystemVersion = "0.9.0"; // Older version
        File.WriteAllText(sidecarPath, JsonSerializer.Serialize(sidecar));

        var restoreResult = await _service.RestoreBackupAsync(_superAdminUserId, backupResult.Backup.FilePath);

        // Should succeed (older backup restoring into newer program is allowed)
        Assert.True(restoreResult.Success, restoreResult.ErrorMessage);
    }

    // ─────────────────────────────────────────────────────────────
    // 10. Failed restore → rollback (tampered file)
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Restore_TamperedEncryptedFile_FailsGracefully()
    {
        var backupResult = await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        Assert.True(backupResult.Success);

        // Corrupt the encrypted file by modifying ciphertext bytes
        var bytes = File.ReadAllBytes(backupResult.Backup!.FilePath);
        bytes[50] ^= 0xFF; // flip bits
        File.WriteAllBytes(backupResult.Backup.FilePath, bytes);

        var restoreResult = await _service.RestoreBackupAsync(_superAdminUserId, backupResult.Backup.FilePath);

        Assert.False(restoreResult.Success);
        Assert.Contains("Decryption failed", restoreResult.ErrorMessage!);
    }

    // ─────────────────────────────────────────────────────────────
    // 11. GetBackups – authorization
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetBackups_SuperAdmin_ReturnsAll()
    {
        await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);
        await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);

        var backups = await _service.GetBackupsAsync(_superAdminUserId);
        Assert.True(backups.Count >= 2);
    }

    [Fact]
    public async Task GetBackups_NonSuperAdmin_ReturnsEmpty()
    {
        await _service.CreateManualBackupAsync(_superAdminUserId, _tempFolder);

        var backups = await _service.GetBackupsAsync(_adminUserId);
        Assert.Empty(backups);
    }

    // ─────────────────────────────────────────────────────────────
    // 12. Auto backup attempt tracking
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task AutoBackup_RecordsAttemptNumber()
    {
        var result = await _service.CreateAutomaticBackupAsync(_tempFolder);
        Assert.True(result.Success);
        Assert.Equal(1, result.Backup!.AttemptNumber);
    }
}
