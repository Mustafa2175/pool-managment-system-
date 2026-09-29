using System;
using System.Threading.Tasks;

namespace SwimClub.Application.Training;

public enum SubscriptionResult
{
    Success,
    PeriodNotActive,
    InvalidStartDate,       // Past, or does not fall on a Period scheduled day
    CapacityReached,        // Period is full
    ScheduleOverlap,        // Swimmer has overlapping active subscription
    DuplicateSubscription,  // Swimmer already has an active subscription for THIS period
    Overpayment,            // PaidAmount > TotalPrice (Decision 21)
    CreditAndOutstandingMutuallyExclusive // Decision 1
}

public interface ITrainingSubscriptionService
{
    /// <summary>
    /// Creates a Training Subscription for a swimmer.
    /// Automatically calculates total price based on configuration.
    /// Generates Session records based on the period's schedule and the global session count.
    /// </summary>
    Task<(SubscriptionResult Result, int? SubscriptionId)> CreateSubscriptionAsync(
        string swimmerId,
        int programId,
        int periodId,
        DateOnly startDate,
        decimal paidAmount,
        decimal? creditGrantedAmount,
        string? creditDescription,
        decimal? outstandingDeclaredAmount,
        string? outstandingDescription,
        int actorUserId);
}
