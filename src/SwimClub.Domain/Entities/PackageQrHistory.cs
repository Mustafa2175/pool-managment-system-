namespace SwimClub.Domain.Entities;

/// <summary>QR token history for a Package.</summary>
public class PackageQrHistory
{
    public int QrHistoryId { get; set; }
    public int PackageId { get; set; }
    public string QrToken { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Package Package { get; set; } = null!;
}
