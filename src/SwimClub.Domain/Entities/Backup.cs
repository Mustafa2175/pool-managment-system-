namespace SwimClub.Domain.Entities;

/// <summary>
/// Backup record. Encryption is mandatory for every backup (Decision 25).
/// Automatic backups retry up to 5 times (Decision 26).
/// </summary>
public class Backup
{
    public int BackupId { get; set; }
    public string FilePath { get; set; } = null!;
    public long FileSizeBytes { get; set; }

    /// <summary>AUTOMATIC | MANUAL</summary>
    public string BackupType { get; set; } = null!;

    /// <summary>Always true — encryption is mandatory (Decision 25).</summary>
    public bool Encrypted { get; set; } = true;

    /// <summary>Reference to the encryption key (DPAPI-protected).</summary>
    public string? EncryptionKeyRef { get; set; }

    /// <summary>Which of up to 5 attempts succeeded (Decision 26).</summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>SUCCESS | FAILED</summary>
    public string Status { get; set; } = null!;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }

    public User? CreatedByUser { get; set; }
}
