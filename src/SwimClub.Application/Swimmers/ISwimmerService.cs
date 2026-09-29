using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Swimmers;

public enum SwimmerResult
{
    Success,
    NotFound,
    ActiveSubscriptionExists,   // Blocks deletion per Decision 20
    DuplicateSwimmerId          // Should not occur with auto-generation, guard for safety
}

public record SwimmerListItem(
    string SwimmerId,
    string Name,
    string Gender,
    string? ParentName,
    string? Phone,
    string MemberStatus,
    string Status,
    /// <summary>
    /// Current or last subscription summary. Null if the swimmer has never had a subscription.
    /// </summary>
    string? SubscriptionSummary);

public record SwimmerProfile(
    Swimmer Swimmer,
    IReadOnlyList<TrainingSubscription> Subscriptions,
    IReadOnlyList<Package> Packages);

/// <summary>
/// Manages the Swimmer lifecycle: creation, editing, search, status computation, and soft deletion.
///
/// Key business rules:
/// - Swimmer ID auto-generated in SW-XXXXXX format.
/// - QrToken auto-generated as a unique random token.
/// - Status = ACTIVE iff at least one linked TrainingSubscription OR Package is ACTIVE/PAUSED (Decision 20).
///   Private Bookings NEVER contribute to status.
/// - Soft delete only. Deletion blocked if any active Training Subscription or Package exists (Decision 20).
/// - Phone is optional and not unique.
/// - Global search matches Name, SwimmerId, Phone, ParentName.
/// </summary>
public interface ISwimmerService
{
    /// <summary>
    /// Registers a new Swimmer. Auto-generates SwimmerId (SW-XXXXXX) and QrToken.
    /// </summary>
    Task<(SwimmerResult Result, Swimmer? Swimmer)> RegisterSwimmerAsync(
        string name,
        DateOnly dateOfBirth,
        string gender,
        string memberStatus,
        string? parentName,
        string? phone,
        int createdByUserId);

    /// <summary>
    /// Edits mutable swimmer fields (name, dob, gender, memberStatus, parentName, phone).
    /// Membership change is always allowed and affects only future subscriptions' pricing.
    /// </summary>
    Task<SwimmerResult> EditSwimmerAsync(
        string swimmerId,
        string name,
        DateOnly dateOfBirth,
        string gender,
        string memberStatus,
        string? parentName,
        string? phone);

    /// <summary>
    /// Soft-deletes a Swimmer. Rejected if any active Training Subscription or Package exists.
    /// Active Private Bookings do NOT block deletion (Decision 20).
    /// </summary>
    Task<SwimmerResult> DeleteSwimmerAsync(string swimmerId, int actorUserId);

    /// <summary>
    /// Recomputes and persists the swimmer's derived Status field based on
    /// active/paused Training Subscriptions or Packages. Called after subscription state changes.
    /// </summary>
    Task RefreshSwimmerStatusAsync(string swimmerId);

    /// <summary>
    /// Returns a paginated list of non-deleted swimmers with their current/last subscription summary.
    /// Optionally filter by status or memberStatus.
    /// </summary>
    Task<IReadOnlyList<SwimmerListItem>> ListSwimmersAsync(
        string? statusFilter = null,
        string? memberStatusFilter = null,
        int skip = 0,
        int take = 50);

    /// <summary>
    /// Global search: matches Name, SwimmerId, Phone, or ParentName.
    /// Returns only non-deleted records. At least 1 character required.
    /// </summary>
    Task<IReadOnlyList<SwimmerListItem>> SearchSwimmersAsync(string query);

    /// <summary>
    /// Loads the full Swimmer profile for the profile tabs.
    /// </summary>
    Task<SwimmerProfile?> GetSwimmerProfileAsync(string swimmerId);

    /// <summary>
    /// Looks up a swimmer by their QR token (for desk check-in flows).
    /// Returns null if not found or deleted.
    /// </summary>
    Task<Swimmer?> GetSwimmerByQrTokenAsync(string qrToken);

    Task<Swimmer?> GetSwimmerByIdAsync(string swimmerId);
}
