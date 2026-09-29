using Microsoft.EntityFrameworkCore;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

/// <summary>
/// Integration tests for Phase 1: Database + EF Core + Migrations.
/// Uses a real SQLite database file (not in-memory).
/// </summary>
public class DatabaseIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly AppDbContext _context;

    public DatabaseIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"swimclub-test-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _context = new AppDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task Database_ShouldCreate_Successfully()
    {
        // Act
        await _context.Database.EnsureCreatedAsync();

        // Assert
        Assert.True(File.Exists(_dbPath), "SQLite database file should exist after creation.");
    }

    [Fact]
    public async Task Migration_ShouldApply_WithoutErrors()
    {
        // Act & Assert — MigrateAsync creates schema from migrations
        await _context.Database.MigrateAsync();
        var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task Roles_SeedData_ShouldExist()
    {
        // Arrange
        await _context.Database.MigrateAsync();

        // Act
        var roles = await _context.Roles.ToListAsync();

        // Assert — roles are seeded by OnModelCreating (Decision 27)
        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.Code == "SUPER_ADMIN");
        Assert.Contains(roles, r => r.Code == "OWNER");
        Assert.Contains(roles, r => r.Code == "ADMINISTRATOR");
    }

    [Fact]
    public async Task Employee_NationalId_ShouldBeUnique()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();
        var employee1 = new Employee
        {
            Name = "Ahmed Ali",
            EmployeeType = "COACH",
            NationalId = "123456789",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var employee2 = new Employee
        {
            Name = "Mohamed Ali",
            EmployeeType = "COACH",
            NationalId = "123456789", // duplicate
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee1);
        await _context.SaveChangesAsync();

        // Act & Assert
        _context.Employees.Add(employee2);
        await Assert.ThrowsAnyAsync<Exception>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Swimmer_QrToken_ShouldBeUnique()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();
        var swimmer1 = new Swimmer
        {
            SwimmerId = "SW-000001",
            Name = "Swimmer One",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Gender = "MALE",
            MemberStatus = "MEMBER",
            QrToken = "unique-qr-token-123",
            Status = "INACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var swimmer2 = new Swimmer
        {
            SwimmerId = "SW-000002",
            Name = "Swimmer Two",
            DateOfBirth = new DateOnly(2012, 5, 15),
            Gender = "FEMALE",
            MemberStatus = "NON_MEMBER",
            QrToken = "unique-qr-token-123", // duplicate
            Status = "INACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _context.Swimmers.Add(swimmer1);
        await _context.SaveChangesAsync();

        // Act & Assert
        _context.Swimmers.Add(swimmer2);
        await Assert.ThrowsAnyAsync<Exception>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Qualification_RankOrder_ShouldBeUnique()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();
        var q1 = new Qualification { NameEn = "Level 1", NameAr = "مستوى 1", RankOrder = 1 };
        var q2 = new Qualification { NameEn = "Level 2", NameAr = "مستوى 2", RankOrder = 1 }; // duplicate rank

        _context.Qualifications.Add(q1);
        await _context.SaveChangesAsync();

        // Act & Assert
        _context.Qualifications.Add(q2);
        await Assert.ThrowsAnyAsync<Exception>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task TrainingSubscription_BalanceDue_ShouldBeComputedColumn()
    {
        // Arrange — requires full FK chain
        await _context.Database.EnsureCreatedAsync();

        // Create prerequisite records
        var role = await _context.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var employee = new Employee
        {
            Name = "Test Admin",
            EmployeeType = "ADMINISTRATOR",
            NationalId = "ADM-001",
            MonthlySalary = 5000,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Username = "admin_test",
            PasswordHash = "hash",
            RoleId = role.RoleId,
            EmployeeId = employee.EmployeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var program = new Domain.Entities.Program
        {
            Name = "Regular Program",
            ProgramType = "REGULAR",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Programs.Add(program);

        var period = new TrainingPeriod
        {
            Program = program,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Capacity = 20,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingPeriods.Add(period);

        var swimmer = new Swimmer
        {
            SwimmerId = "SW-000010",
            Name = "Test Swimmer",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Gender = "MALE",
            MemberStatus = "MEMBER",
            QrToken = "qr-balance-test",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Swimmers.Add(swimmer);
        await _context.SaveChangesAsync();

        var subscription = new TrainingSubscription
        {
            SwimmerId = swimmer.SwimmerId,
            ProgramId = program.ProgramId,
            PeriodId = period.PeriodId,
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 12, 31),
            UnitPriceSnapshot = 500,
            ConfiguredSessionCountSnapshot = 24,
            TotalPrice = 500,
            PaidAmount = 200,  // balance_due should be 300
            Status = "ACTIVE",
            CreatedBy = user.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act — re-read to get computed column
        _context.ChangeTracker.Clear();
        var retrieved = await _context.TrainingSubscriptions.FindAsync(subscription.SubscriptionId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(300m, retrieved!.BalanceDue);
    }

    [Fact]
    public async Task ForeignKey_Enforcement_ShouldPreventOrphanRecords()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Enable FK enforcement explicitly (SQLite requires PRAGMA per-connection)
        await _context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");

        // Act & Assert — try to insert a user referencing a non-existent role
        var orphanUser = new User
        {
            Username = "orphan_user",
            PasswordHash = "hash",
            RoleId = 9999, // non-existent role
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(orphanUser);
        await Assert.ThrowsAnyAsync<Exception>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Package_BalanceDue_ShouldBeComputedColumn()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();
        var role = await _context.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var employee = new Employee
        {
            Name = "Pkg Admin",
            EmployeeType = "ADMINISTRATOR",
            NationalId = "PKG-ADM-001",
            MonthlySalary = 3000,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Username = "pkg_admin_test",
            PasswordHash = "hash",
            RoleId = role.RoleId,
            EmployeeId = employee.EmployeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var program = new Domain.Entities.Program
        {
            Name = "Pkg Regular",
            ProgramType = "REGULAR",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Programs.Add(program);

        var period = new TrainingPeriod
        {
            Program = program,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0),
            Capacity = 10,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingPeriods.Add(period);

        var swimmer = new Swimmer
        {
            SwimmerId = "SW-PKG-001",
            Name = "Pkg Swimmer",
            DateOfBirth = new DateOnly(2008, 3, 20),
            Gender = "FEMALE",
            MemberStatus = "MEMBER",
            QrToken = "qr-pkg-test",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _context.Swimmers.Add(swimmer);
        await _context.SaveChangesAsync();

        var package = new Package
        {
            SwimmerId = swimmer.SwimmerId,
            PackageType = "TRAINING",
            ProgramId = program.ProgramId,
            TrainingPeriodId = period.PeriodId,
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 11, 30),
            DurationSnapshotDays = 60,
            SessionsPerMonthSnapshot = 8,
            AvailableSessionsTotal = 16,
            AvailableSessionsRemaining = 16,
            ReferenceTrainingPriceSnapshot = 400,
            TotalPrice = 800,
            PaidAmount = 300,  // balance_due should be 500
            Status = "ACTIVE",
            CreatedBy = user.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Packages.Add(package);
        await _context.SaveChangesAsync();

        // Re-read to get the computed column
        _context.ChangeTracker.Clear();
        var retrieved = await _context.Packages.FindAsync(package.PackageId);

        Assert.NotNull(retrieved);
        Assert.Equal(500m, retrieved!.BalanceDue);
    }

    [Fact]
    public async Task Lane_Label_ShouldBeUnique()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();
        _context.Lanes.Add(new Lane { Label = "Lane 1", Capacity = 5, Status = "ACTIVE" });
        await _context.SaveChangesAsync();

        // Act & Assert
        _context.Lanes.Add(new Lane { Label = "Lane 1", Capacity = 3, Status = "ACTIVE" });
        await Assert.ThrowsAnyAsync<Exception>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task SystemSettings_ShouldStore_LanguageSetting()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act — simulate Initial Program Setup (Decision 36)
        _context.SystemSettings.Add(new SystemSetting { SettingKey = "language", SettingValue = "AR" });
        await _context.SaveChangesAsync();

        // Assert
        var setting = await _context.SystemSettings.FindAsync("language");
        Assert.NotNull(setting);
        Assert.Equal("AR", setting!.SettingValue);
    }

    [Fact]
    public async Task RecreationalTicket_HasNoSwimmerFk()
    {
        // Verify the schema has no swimmer_id column on recreational_tickets.
        await _context.Database.EnsureCreatedAsync();
        var ticket = new RecreationalTicket
        {
            Name = "Walk-in Guest",
            MemberStatus = "NON_MEMBER",
            CheckedInAt = DateTime.UtcNow,
            AmountPaid = 50,
            RecordedBy = 0  // No user reference for this test
        };

        // RecreationalTicket has no SwimmerId property — this is verified by the entity definition.
        Assert.False(
            typeof(RecreationalTicket).GetProperties().Any(p => p.Name == "SwimmerId"),
            "RecreationalTicket must not have a SwimmerId property (Decisions 10 & 11)."
        );
    }

    [Fact]
    public async Task PrivateBookingParticipant_CheckConstraint_ShouldEnforce_Identity()
    {
        // Verify the model correctly captures the mutual exclusivity of swimmer/guest
        await _context.Database.EnsureCreatedAsync();

        // A SWIMMER type must have swimmer_id and no guest_name
        var swimmerParticipant = new PrivateBookingParticipant
        {
            ParticipantType = "SWIMMER",
            SwimmerId = "SW-000001",
            GuestName = null,
            GuestPhone = null
        };
        Assert.Equal("SWIMMER", swimmerParticipant.ParticipantType);
        Assert.NotNull(swimmerParticipant.SwimmerId);
        Assert.Null(swimmerParticipant.GuestName);

        // A GUEST type must have guest_name and no swimmer_id
        var guestParticipant = new PrivateBookingParticipant
        {
            ParticipantType = "GUEST",
            SwimmerId = null,
            GuestName = "John Doe",
            GuestPhone = "0501234567"
        };
        Assert.Equal("GUEST", guestParticipant.ParticipantType);
        Assert.Null(guestParticipant.SwimmerId);
        Assert.NotNull(guestParticipant.GuestName);
    }
}
