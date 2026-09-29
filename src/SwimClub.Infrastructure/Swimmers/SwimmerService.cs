using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Swimmers;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Swimmers;

public class SwimmerService : ISwimmerService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public SwimmerService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    // ─────────────────────────────────────────────
    // REGISTRATION
    // ─────────────────────────────────────────────

    public async Task<(SwimmerResult Result, Swimmer? Swimmer)> RegisterSwimmerAsync(
        string name,
        DateOnly dateOfBirth,
        string gender,
        string memberStatus,
        string? parentName,
        string? phone,
        int createdByUserId)
    {
        string swimmerId = await GenerateSwimmerIdAsync();
        string qrToken   = GenerateQrToken();

        var swimmer = new Swimmer
        {
            SwimmerId    = swimmerId,
            Name         = name,
            DateOfBirth  = dateOfBirth,
            Gender       = gender,
            MemberStatus = memberStatus,
            ParentName   = parentName,
            Phone        = phone,
            QrToken      = qrToken,
            Status       = "INACTIVE",
            IsDeleted    = false,
            CreatedAt    = DateTime.UtcNow
        };

        _context.Swimmers.Add(swimmer);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("CREATE_SWIMMER", "Swimmer", null,
            $"Registered swimmer {swimmerId} – {name}");

        return (SwimmerResult.Success, swimmer);
    }

    // ─────────────────────────────────────────────
    // EDITING
    // ─────────────────────────────────────────────

    public async Task<SwimmerResult> EditSwimmerAsync(
        string swimmerId,
        string name,
        DateOnly dateOfBirth,
        string gender,
        string memberStatus,
        string? parentName,
        string? phone)
    {
        var swimmer = await FindActiveAsync(swimmerId);
        if (swimmer is null) return SwimmerResult.NotFound;

        swimmer.Name         = name;
        swimmer.DateOfBirth  = dateOfBirth;
        swimmer.Gender       = gender;
        swimmer.MemberStatus = memberStatus;
        swimmer.ParentName   = parentName;
        swimmer.Phone        = phone;

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("EDIT_SWIMMER", "Swimmer", null, swimmerId);

        return SwimmerResult.Success;
    }

    // ─────────────────────────────────────────────
    // SOFT DELETE
    // ─────────────────────────────────────────────

    public async Task<SwimmerResult> DeleteSwimmerAsync(string swimmerId, int actorUserId)
    {
        var swimmer = await _context.Swimmers
            .Include(s => s.TrainingSubscriptions)
            .Include(s => s.Packages)
            .FirstOrDefaultAsync(s => s.SwimmerId == swimmerId && !s.IsDeleted);

        if (swimmer is null) return SwimmerResult.NotFound;

        // Decision 20: only Training Subscriptions / Packages in active state block deletion.
        // Private Bookings are explicitly excluded.
        bool hasActiveBlock = swimmer.TrainingSubscriptions
            .Any(ts => ts.Status == "ACTIVE" || ts.Status == "PAUSED")
            || swimmer.Packages
            .Any(p => p.Status == "ACTIVE" || p.Status == "PAUSED");

        if (hasActiveBlock)
        {
            await _auditLog.LogFailureAsync("DELETE_SWIMMER_BLOCKED", 
                "Active subscription/package exists", "Swimmer", null);
            return SwimmerResult.ActiveSubscriptionExists;
        }

        swimmer.IsDeleted = true;
        swimmer.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("DELETE_SWIMMER", "Swimmer", null, swimmerId);

        return SwimmerResult.Success;
    }

    // ─────────────────────────────────────────────
    // STATUS REFRESH  (Decision 20)
    // ─────────────────────────────────────────────

    public async Task RefreshSwimmerStatusAsync(string swimmerId)
    {
        var swimmer = await _context.Swimmers
            .Include(s => s.TrainingSubscriptions)
            .Include(s => s.Packages)
            .FirstOrDefaultAsync(s => s.SwimmerId == swimmerId && !s.IsDeleted);

        if (swimmer is null) return;

        bool isActive = swimmer.TrainingSubscriptions
            .Any(ts => ts.Status == "ACTIVE" || ts.Status == "PAUSED")
            || swimmer.Packages
            .Any(p => p.Status == "ACTIVE" || p.Status == "PAUSED");

        string newStatus = isActive ? "ACTIVE" : "INACTIVE";

        if (swimmer.Status != newStatus)
        {
            swimmer.Status = newStatus;
            await _context.SaveChangesAsync();
        }
    }

    // ─────────────────────────────────────────────
    // LIST
    // ─────────────────────────────────────────────

    public async Task<IReadOnlyList<SwimmerListItem>> ListSwimmersAsync(
        string? statusFilter = null,
        string? memberStatusFilter = null,
        int skip = 0,
        int take = 50)
    {
        var query = _context.Swimmers
            .Where(s => !s.IsDeleted)
            .Include(s => s.TrainingSubscriptions)
            .Include(s => s.Packages)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
            query = query.Where(s => s.Status == statusFilter);

        if (!string.IsNullOrEmpty(memberStatusFilter))
            query = query.Where(s => s.MemberStatus == memberStatusFilter);

        var swimmers = await query
            .OrderBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return swimmers.Select(s => ToListItem(s)).ToList();
    }

    // ─────────────────────────────────────────────
    // GLOBAL SEARCH
    // ─────────────────────────────────────────────

    public async Task<IReadOnlyList<SwimmerListItem>> SearchSwimmersAsync(string query)
    {
        string q = query.Trim().ToLower();

        var swimmers = await _context.Swimmers
            .Where(s => !s.IsDeleted &&
                (s.Name.ToLower().Contains(q) ||
                 s.SwimmerId.ToLower().Contains(q) ||
                 (s.Phone != null && s.Phone.Contains(q)) ||
                 (s.ParentName != null && s.ParentName.ToLower().Contains(q))))
            .Include(s => s.TrainingSubscriptions)
            .Include(s => s.Packages)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return swimmers.Select(s => ToListItem(s)).ToList();
    }

    // ─────────────────────────────────────────────
    // PROFILE
    // ─────────────────────────────────────────────

    public async Task<SwimmerProfile?> GetSwimmerProfileAsync(string swimmerId)
    {
        var swimmer = await _context.Swimmers
            .Include(s => s.TrainingSubscriptions)
                .ThenInclude(ts => ts.Period)
            .Include(s => s.Packages)
            .FirstOrDefaultAsync(s => s.SwimmerId == swimmerId && !s.IsDeleted);

        if (swimmer is null) return null;

        return new SwimmerProfile(
            swimmer,
            swimmer.TrainingSubscriptions.OrderByDescending(ts => ts.CreatedAt).ToList(),
            swimmer.Packages.OrderByDescending(p => p.CreatedAt).ToList());
    }

    public async Task<Swimmer?> GetSwimmerByQrTokenAsync(string qrToken)
    {
        return await _context.Swimmers
            .FirstOrDefaultAsync(s => s.QrToken == qrToken && !s.IsDeleted);
    }

    public async Task<Swimmer?> GetSwimmerByIdAsync(string swimmerId)
    {
        return await FindActiveAsync(swimmerId);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private async Task<Swimmer?> FindActiveAsync(string swimmerId)
        => await _context.Swimmers.FirstOrDefaultAsync(s => s.SwimmerId == swimmerId && !s.IsDeleted);

    /// <summary>
    /// Generates the next sequential SwimmerId in SW-XXXXXX format.
    /// Finds the current maximum and increments by 1.
    /// </summary>
    private async Task<string> GenerateSwimmerIdAsync()
    {
        // Pull all IDs in memory — for production scale a sequence table would be better,
        // but here we conform to the SQLite-first offline model.
        var allIds = await _context.Swimmers
            .Select(s => s.SwimmerId)
            .ToListAsync();

        int next = 1;
        if (allIds.Count > 0)
        {
            int maxNum = allIds
                .Select(id => int.TryParse(id.Replace("SW-", ""), out int n) ? n : 0)
                .Max();
            next = maxNum + 1;
        }

        return $"SW-{next:D6}";
    }

    /// <summary>
    /// Generates a cryptographically random, URL-safe, unique QR token prefixed SWIM-.
    /// </summary>
    private static string GenerateQrToken()
        => $"SWIM-{Guid.NewGuid():N}".ToUpper();

    /// <summary>
    /// Projects a Swimmer + its loaded subscriptions/packages into a list-row DTO.
    /// Business rule (list spec): show Current subscription name if ACTIVE,
    /// Last subscription if INACTIVE, nothing if none.
    /// </summary>
    private static SwimmerListItem ToListItem(Swimmer s)
    {
        string? subscriptionSummary = null;

        // Active subscription first
        var activeSub = s.TrainingSubscriptions
            .Where(ts => ts.Status == "ACTIVE" || ts.Status == "PAUSED")
            .OrderByDescending(ts => ts.StartDate)
            .FirstOrDefault();

        var activePkg = s.Packages
            .Where(p => p.Status == "ACTIVE" || p.Status == "PAUSED")
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefault();

        if (activeSub is not null)
            subscriptionSummary = $"Training Subscription (since {activeSub.StartDate:dd MMM yyyy})";
        else if (activePkg is not null)
            subscriptionSummary = $"Package (since {activePkg.StartDate:dd MMM yyyy})";
        else
        {
            // Last subscription
            var lastSub = s.TrainingSubscriptions
                .OrderByDescending(ts => ts.StartDate)
                .FirstOrDefault();
            var lastPkg = s.Packages
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefault();

            if (lastSub is not null)
                subscriptionSummary = $"Last: Training (ended {lastSub.EndDate:dd MMM yyyy})";
            else if (lastPkg is not null)
                subscriptionSummary = $"Last: Package (ended {lastPkg.EndDate:dd MMM yyyy})";
        }

        return new SwimmerListItem(
            s.SwimmerId,
            s.Name,
            s.Gender,
            s.ParentName,
            s.Phone,
            s.MemberStatus,
            s.Status,
            subscriptionSummary);
    }
}
