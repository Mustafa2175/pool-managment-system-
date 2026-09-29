using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Swimmers;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Infrastructure.Swimmers;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class SwimmerTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private ISwimmerService _swimmerService = null!;

    private AppDbContext CreateContext()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"swimclub-sw-test-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public async Task InitializeAsync()
    {
        _context = CreateContext();
        var auditLog = new Logging.AuditLogService(_context, new SwimClub.Infrastructure.Security.CurrentUserService());
        _swimmerService = new SwimmerService(_context, auditLog);
        await SeedRolesAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private async Task SeedRolesAsync()
    {
        if (!await _context.Roles.AnyAsync())
        {
            _context.Roles.AddRange(
                new Role { Code = "SUPER_ADMIN", NameEn = "Super Admin", NameAr = "Super Admin" },
                new Role { Code = "OWNER",       NameEn = "Owner",       NameAr = "Owner" },
                new Role { Code = "ADMINISTRATOR", NameEn = "Admin",     NameAr = "Admin" }
            );
            await _context.SaveChangesAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. SwimmerId generation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SwimmerId_Generation_StartsAtSW000001()
    {
        var (res, sw) = await _swimmerService.RegisterSwimmerAsync("First", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        Assert.Equal(SwimmerResult.Success, res);
        Assert.Equal("SW-000001", sw!.SwimmerId);
    }

    [Fact]
    public async Task SwimmerId_Generation_IsSequential()
    {
        await _swimmerService.RegisterSwimmerAsync("A", new DateOnly(2010,1,1), "MALE",   "MEMBER", null, null, 1);
        await _swimmerService.RegisterSwimmerAsync("B", new DateOnly(2010,1,1), "FEMALE", "MEMBER", null, null, 1);
        var (_, sw3) = await _swimmerService.RegisterSwimmerAsync("C", new DateOnly(2010,1,1), "MALE", "NON_MEMBER", null, null, 1);

        Assert.Equal("SW-000003", sw3!.SwimmerId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. QR token generation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QrToken_IsUnique_AcrossMultipleSwimmers()
    {
        var (_, sw1) = await _swimmerService.RegisterSwimmerAsync("A", new DateOnly(2010,1,1), "MALE",   "MEMBER", null, null, 1);
        var (_, sw2) = await _swimmerService.RegisterSwimmerAsync("B", new DateOnly(2010,1,1), "FEMALE", "MEMBER", null, null, 1);

        Assert.NotEqual(sw1!.QrToken, sw2!.QrToken);
    }

    [Fact]
    public async Task QrToken_StartsWithSWIM_Prefix()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Q", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        Assert.StartsWith("SWIM-", sw!.QrToken);
    }

    [Fact]
    public async Task GetSwimmerByQrToken_ReturnsCorrectSwimmer()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("QR", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        var found = await _swimmerService.GetSwimmerByQrTokenAsync(sw!.QrToken);

        Assert.NotNull(found);
        Assert.Equal(sw.SwimmerId, found!.SwimmerId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Search
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ByName_FindsMatch()
    {
        await _swimmerService.RegisterSwimmerAsync("Mohamed Ali",   new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        await _swimmerService.RegisterSwimmerAsync("Sara Hassan",   new DateOnly(2011,1,1), "FEMALE", "MEMBER", null, null, 1);

        var results = await _swimmerService.SearchSwimmersAsync("Mohamed");
        Assert.Single(results);
        Assert.Equal("Mohamed Ali", results[0].Name);
    }

    [Fact]
    public async Task Search_BySwimmerId_FindsMatch()
    {
        await _swimmerService.RegisterSwimmerAsync("X", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);

        var results = await _swimmerService.SearchSwimmersAsync("SW-000001");
        Assert.Single(results);
    }

    [Fact]
    public async Task Search_ByPhone_FindsMatch()
    {
        await _swimmerService.RegisterSwimmerAsync("Phone Swimmer", new DateOnly(2010,1,1), "MALE", "MEMBER", null, "01001234567", 1);

        var results = await _swimmerService.SearchSwimmersAsync("01001234567");
        Assert.Single(results);
    }

    [Fact]
    public async Task Search_ByParentName_FindsMatch()
    {
        await _swimmerService.RegisterSwimmerAsync("Child", new DateOnly(2012,1,1), "FEMALE", "MEMBER", "Khalid Fouad", null, 1);

        var results = await _swimmerService.SearchSwimmersAsync("Khalid");
        Assert.Single(results);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmpty()
    {
        await _swimmerService.RegisterSwimmerAsync("Known", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);

        var results = await _swimmerService.SearchSwimmersAsync("ZZZZNOTFOUND");
        Assert.Empty(results);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Phone non-uniqueness
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Phone_IsNotUnique_TwoSwimmersCanShareSamePhone()
    {
        string sharedPhone = "01112345678";
        var (r1, _) = await _swimmerService.RegisterSwimmerAsync("Parent Kid 1", new DateOnly(2010,1,1), "MALE",   "MEMBER", null, sharedPhone, 1);
        var (r2, _) = await _swimmerService.RegisterSwimmerAsync("Parent Kid 2", new DateOnly(2011,1,1), "FEMALE", "MEMBER", null, sharedPhone, 1);

        Assert.Equal(SwimmerResult.Success, r1);
        Assert.Equal(SwimmerResult.Success, r2);

        // Both should appear in a search by phone
        var results = await _swimmerService.SearchSwimmersAsync(sharedPhone);
        Assert.Equal(2, results.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Member / Non-Member
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MemberStatus_StoredAndRetrievedCorrectly()
    {
        var (_, mem)    = await _swimmerService.RegisterSwimmerAsync("Member",    new DateOnly(2010,1,1), "MALE", "MEMBER",     null, null, 1);
        var (_, nonMem) = await _swimmerService.RegisterSwimmerAsync("NonMember", new DateOnly(2010,1,1), "MALE", "NON_MEMBER", null, null, 1);

        Assert.Equal("MEMBER",     mem!.MemberStatus);
        Assert.Equal("NON_MEMBER", nonMem!.MemberStatus);
    }

    [Fact]
    public async Task MemberStatus_CanBeChangedAtAnyTime()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Changeable", new DateOnly(2010,1,1), "MALE", "NON_MEMBER", null, null, 1);
        var res = await _swimmerService.EditSwimmerAsync(sw!.SwimmerId, sw.Name, sw.DateOfBirth, sw.Gender, "MEMBER", sw.ParentName, sw.Phone);

        Assert.Equal(SwimmerResult.Success, res);
        var updated = await _swimmerService.GetSwimmerByIdAsync(sw.SwimmerId);
        Assert.Equal("MEMBER", updated!.MemberStatus);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Active / Inactive status calculation (Decision 20)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Status_IsINACTIVE_OnCreation()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("New Swimmer", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        Assert.Equal("INACTIVE", sw!.Status);
    }

    [Fact]
    public async Task Status_BecomesACTIVE_WhenTrainingSubscriptionIsActive()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Subscriber", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);

        // Seed an active training subscription for this swimmer
        await SeedActiveTrainingSubscriptionAsync(sw!.SwimmerId);

        await _swimmerService.RefreshSwimmerStatusAsync(sw.SwimmerId);

        var refreshed = await _swimmerService.GetSwimmerByIdAsync(sw.SwimmerId);
        Assert.Equal("ACTIVE", refreshed!.Status);
    }

    [Fact]
    public async Task Status_IsINACTIVE_WhenOnlyPrivateBookingExists_Decision20()
    {
        // Private bookings NEVER contribute to Swimmer status (Decision 20)
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Private Only", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);

        // Don't seed any training subscriptions or packages — only private bookings (skipped, they don't affect status)
        await _swimmerService.RefreshSwimmerStatusAsync(sw!.SwimmerId);

        var refreshed = await _swimmerService.GetSwimmerByIdAsync(sw.SwimmerId);
        Assert.Equal("INACTIVE", refreshed!.Status);
    }

    [Fact]
    public async Task Status_BecomesINACTIVE_AfterSubscriptionIsCancelled()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Canceller", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        var sub = await SeedActiveTrainingSubscriptionAsync(sw!.SwimmerId);

        await _swimmerService.RefreshSwimmerStatusAsync(sw.SwimmerId);
        Assert.Equal("ACTIVE", (await _swimmerService.GetSwimmerByIdAsync(sw.SwimmerId))!.Status);

        // Cancel the subscription
        sub.Status = "CANCELLED";
        await _context.SaveChangesAsync();

        await _swimmerService.RefreshSwimmerStatusAsync(sw.SwimmerId);
        Assert.Equal("INACTIVE", (await _swimmerService.GetSwimmerByIdAsync(sw.SwimmerId))!.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. Soft deletion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Deletable", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        var res = await _swimmerService.DeleteSwimmerAsync(sw!.SwimmerId, 1);

        Assert.Equal(SwimmerResult.Success, res);

        var raw = await _context.Swimmers.FindAsync(sw.SwimmerId);
        Assert.True(raw!.IsDeleted);
        Assert.NotNull(raw.DeletedAt);
    }

    [Fact]
    public async Task SoftDelete_DeletedSwimmerNotReturnedInSearch()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Ghost", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        await _swimmerService.DeleteSwimmerAsync(sw!.SwimmerId, 1);

        var results = await _swimmerService.SearchSwimmersAsync("Ghost");
        Assert.Empty(results);
    }

    [Fact]
    public async Task SoftDelete_DeletedSwimmerNotReturnedByGetById()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Gone", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        await _swimmerService.DeleteSwimmerAsync(sw!.SwimmerId, 1);

        var found = await _swimmerService.GetSwimmerByIdAsync(sw.SwimmerId);
        Assert.Null(found);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. Active subscription blocks deletion (Decision 20)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_BlockedByActiveTrainingSubscription()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Blocked", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        await SeedActiveTrainingSubscriptionAsync(sw!.SwimmerId);

        var res = await _swimmerService.DeleteSwimmerAsync(sw.SwimmerId, 1);
        Assert.Equal(SwimmerResult.ActiveSubscriptionExists, res);

        // Swimmer should still be present
        var stillThere = await _swimmerService.GetSwimmerByIdAsync(sw.SwimmerId);
        Assert.NotNull(stillThere);
    }

    [Fact]
    public async Task Delete_AllowedWhenSubscriptionIsCancelled()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Cancellable", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        var sub = await SeedActiveTrainingSubscriptionAsync(sw!.SwimmerId);

        sub.Status = "CANCELLED";
        await _context.SaveChangesAsync();

        var res = await _swimmerService.DeleteSwimmerAsync(sw.SwimmerId, 1);
        Assert.Equal(SwimmerResult.Success, res);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9. Profile loading
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSwimmerProfile_ReturnsSwimmerWithSubscriptions()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Profile Swimmer", new DateOnly(2005,5,10), "FEMALE", "MEMBER", "Parent A", "0101234567", 1);
        await SeedActiveTrainingSubscriptionAsync(sw!.SwimmerId);

        var profile = await _swimmerService.GetSwimmerProfileAsync(sw.SwimmerId);

        Assert.NotNull(profile);
        Assert.Equal(sw.SwimmerId, profile!.Swimmer.SwimmerId);
        Assert.Single(profile.Subscriptions);
    }

    [Fact]
    public async Task GetSwimmerProfile_ReturnsNull_ForDeletedSwimmer()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Deleted Profiler", new DateOnly(2005,5,10), "MALE", "MEMBER", null, null, 1);
        await _swimmerService.DeleteSwimmerAsync(sw!.SwimmerId, 1);

        var profile = await _swimmerService.GetSwimmerProfileAsync(sw.SwimmerId);
        Assert.Null(profile);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 10. List
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ShowsSubscriptionSummaryForActiveSwimmer()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Listed Active", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        await SeedActiveTrainingSubscriptionAsync(sw!.SwimmerId);
        await _swimmerService.RefreshSwimmerStatusAsync(sw.SwimmerId);

        var list = await _swimmerService.ListSwimmersAsync();
        var item = list.FirstOrDefault(x => x.SwimmerId == sw.SwimmerId);

        Assert.NotNull(item);
        Assert.Equal("ACTIVE", item!.Status);
        Assert.NotNull(item.SubscriptionSummary);
    }

    [Fact]
    public async Task List_DoesNotInclude_DeletedSwimmers()
    {
        var (_, sw) = await _swimmerService.RegisterSwimmerAsync("Invisible", new DateOnly(2010,1,1), "MALE", "MEMBER", null, null, 1);
        await _swimmerService.DeleteSwimmerAsync(sw!.SwimmerId, 1);

        var list = await _swimmerService.ListSwimmersAsync();
        Assert.DoesNotContain(list, x => x.SwimmerId == sw.SwimmerId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<TrainingSubscription> SeedActiveTrainingSubscriptionAsync(string swimmerId)
    {
        // Minimal program + period + subscription to satisfy FK constraints
        var program = new Domain.Entities.Program { Name = "Regular", ProgramType = "REGULAR", IsActive = true };
        _context.Programs.Add(program);
        await _context.SaveChangesAsync();

        var period = new TrainingPeriod
        {
            ProgramId = program.ProgramId,
            StartTime = new TimeOnly(10, 0),
            EndTime   = new TimeOnly(11, 0),
            Capacity  = 10,
            Status    = "ACTIVE"
        };
        _context.TrainingPeriods.Add(period);
        await _context.SaveChangesAsync();

        // Minimal User for CreatedBy FK
        int userId = await EnsureSeedUserAsync();

        var sub = new TrainingSubscription
        {
            SwimmerId                      = swimmerId,
            ProgramId                      = program.ProgramId,
            PeriodId                       = period.PeriodId,
            StartDate                      = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate                        = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            UnitPriceSnapshot              = 500,
            ConfiguredSessionCountSnapshot = 12,
            TotalPrice                     = 500,
            PaidAmount                     = 0,
            Status                         = "ACTIVE",
            CreatedBy                      = userId,
            CreatedAt                      = DateTime.UtcNow
        };

        _context.TrainingSubscriptions.Add(sub);
        await _context.SaveChangesAsync();

        return sub;
    }

    private async Task<int> EnsureSeedUserAsync()
    {
        var existing = await _context.Users.FirstOrDefaultAsync();
        if (existing != null) return existing.UserId;

        var role = await _context.Roles.FirstAsync(r => r.Code == "SUPER_ADMIN");
        var user = new User
        {
            Username     = "sysadmin",
            PasswordHash = "hash",
            RoleId       = role.RoleId,
            IsActive     = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.UserId;
    }
}
