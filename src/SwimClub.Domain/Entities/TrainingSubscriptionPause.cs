namespace SwimClub.Domain.Entities;

/// <summary>Pause record for a Training Subscription.</summary>
public class TrainingSubscriptionPause
{
    public int PauseId { get; set; }
    public int SubscriptionId { get; set; }
    public DateOnly PauseDate { get; set; }
    public DateOnly? ResumeDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public TrainingSubscription Subscription { get; set; } = null!;
}
