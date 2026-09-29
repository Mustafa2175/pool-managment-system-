using System;
using System.Threading.Tasks;

namespace SwimClub.Application.Training;

public enum LifecycleResult
{
    Success,
    SubscriptionNotFound,
    InvalidStateTransition,
    RefundFailed,
    Unauthorized
}

public interface ITrainingLifecycleService
{
    /// <summary>
    /// Pauses an ACTIVE subscription.
    /// </summary>
    Task<LifecycleResult> PauseSubscriptionAsync(int subscriptionId, int actorUserId);

    /// <summary>
    /// Resumes a PAUSED subscription, calculating the shift and pushing future sessions.
    /// </summary>
    Task<LifecycleResult> ResumeSubscriptionAsync(int subscriptionId, int actorUserId);

    /// <summary>
    /// Renews an active/completed subscription into a new one.
    /// Uses the remaining sessions logic if active, or immediate logic if completed.
    /// </summary>
    Task<(LifecycleResult Result, int? NewSubscriptionId)> RenewSubscriptionAsync(
        int oldSubscriptionId, DateOnly? requestedStartDate, decimal paidAmount, int actorUserId);

    /// <summary>
    /// Cancels a subscription.
    /// If before first session -> full refund. If after -> zero refund, remaining sessions cancelled.
    /// </summary>
    Task<LifecycleResult> CancelSubscriptionAsync(int subscriptionId, int actorUserId);
}
