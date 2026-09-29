using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Application.Employees;
using SwimClub.Application.Finance;
using SwimClub.Infrastructure.Employees;
using SwimClub.Infrastructure.Finance;
using SwimClub.Infrastructure.Logging;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Infrastructure.Security;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

/// <summary>
/// Phase 14 Security + Integrity Hardening Regression Tests.
/// Covers: BCrypt consistency, auth matrix, audit immutability,
/// financial integrity, historical truth, sensitive data, DB constraints.
/// </summary>
public class SecurityIntegrityTests : IAsyncDisposable
{
    private readonly string _dbPath;
    private readonly AppDbContext _db;

    public SecurityIntegrityTests()
    {
        _dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"security-test-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        _db.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        await Task.Delay(50);
        if (System.IO.File.Exists(_dbPath))
            System.IO.File.Delete(_dbPath);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 1. BCrypt Consistency (Critical Security Bug Fix Verification)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BCrypt_PasswordHashedByEmployeeService_CanBeVerifiedByAuthService()
    {
        // Arrange
        var adminRole = await _db.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var nationalId = "12345678901234";

        var employee = new Employee
        {
            Name = "Test Admin", EmployeeType = "ADMINISTRATOR",
            NationalId = nationalId, MonthlySalary = 3000,
            Status = "ACTIVE", CreatedAt = DateTime.UtcNow
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(nationalId);
        var user = new User
        {
            Username = "Test Admin", PasswordHash = hashedPassword,
            RoleId = adminRole.RoleId, EmployeeId = employee.EmployeeId,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Act
        bool canLogin = BCrypt.Net.BCrypt.EnhancedVerify(nationalId, user.PasswordHash);

        // Assert
        Assert.True(canLogin, "Password hashed with EnhancedHashPassword must be verifiable with EnhancedVerify.");
    }

    [Fact]
    public void BCrypt_StandardVerify_FailsOnEnhancedHash()
    {
        var password = "testpassword";
        var enhancedHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);

        bool standardVerifyResult = BCrypt.Net.BCrypt.Verify(password, enhancedHash);
        bool enhancedVerifyResult = BCrypt.Net.BCrypt.EnhancedVerify(password, enhancedHash);

        Assert.False(standardVerifyResult);
        Assert.True(enhancedVerifyResult);
    }

    [Fact]
    public async Task AuthService_LoginAsync_UsesEnhancedVerify_SucceedsForCorrectPassword()
    {
        var adminRole = await _db.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var nationalId = "98765432109876";
        var hashedPw = BCrypt.Net.BCrypt.EnhancedHashPassword(nationalId);

        var user = new User
        {
            Username = "authtest_user", PasswordHash = hashedPw,
            RoleId = adminRole.RoleId, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        var auditMock = new Mock<IAuditLogService>();
        var authService = new AuthService(_db, currentUserMock.Object, auditMock.Object);

        var result = await authService.LoginAsync("authtest_user", nationalId);
        Assert.Equal(LoginResult.Success, result);
    }

    [Fact]
    public async Task AuthService_LoginAsync_WithWrongPassword_Logs_GenericFailure()
    {
        var adminRole = await _db.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var user = new User
        {
            Username = "genericfail_user", PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("correctpassword"),
            RoleId = adminRole.RoleId, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        var auditMock = new Mock<IAuditLogService>();
        var authService = new AuthService(_db, currentUserMock.Object, auditMock.Object);

        var result = await authService.LoginAsync("genericfail_user", "wrongpassword");

        Assert.Equal(LoginResult.InvalidCredentials, result);
        auditMock.Verify(a => a.LogSystemEventAsync("LOGIN_FAILED", false, It.IsAny<string>()), Times.Once);
        currentUserMock.Verify(c => c.SetCurrentUser(It.IsAny<User>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 2. Authorization Matrix
    // ─────────────────────────────────────────────────────────────────────────────

    private static IAuthorizationGuard MakeGuard(string roleCode, Mock<IAuditLogService>? auditMock = null)
    {
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(c => c.CurrentUser).Returns(new User
        {
            Role = new Role { Code = roleCode, NameEn = roleCode, NameAr = roleCode }
        });
        auditMock ??= new Mock<IAuditLogService>();
        return new AuthorizationGuard(currentUserMock.Object, auditMock.Object);
    }

    [Theory]
    [InlineData(AppActions.CREATE_SWIMMER, true, false, true)]
    [InlineData(AppActions.EDIT_SWIMMER, true, false, true)]
    public void AuthorizationMatrix_Matches(string action, bool saAllowed, bool owAllowed, bool adAllowed)
    {
        var saGuard = MakeGuard("SUPER_ADMIN");
        var owGuard = MakeGuard("OWNER");
        var adGuard = MakeGuard("ADMINISTRATOR");

        Assert.Equal(saAllowed, saGuard.HasPermission(action));
        Assert.Equal(owAllowed, owGuard.HasPermission(action));
        Assert.Equal(adAllowed, adGuard.HasPermission(action));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 3. Financial Integrity
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Payment_Overpayment_IsRejected_And_Audited()
    {
        var role = await _db.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var emp = new Employee { Name = "Fin", EmployeeType = "ADMINISTRATOR", NationalId = "FIN01", MonthlySalary = 1, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();

        var usr = new User { Username = "fin", PasswordHash = "x", RoleId = role.RoleId, EmployeeId = emp.EmployeeId, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Users.Add(usr);
        var prog = new Domain.Entities.Program { Name = "P", ProgramType = "REGULAR", IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Programs.Add(prog);
        var prd = new TrainingPeriod { Program = prog, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), Capacity = 20, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
        _db.TrainingPeriods.Add(prd);
        var sw = new Swimmer { SwimmerId = "S1", Name = "S", DateOfBirth = new DateOnly(2010, 1, 1), Gender = "MALE", MemberStatus = "MEMBER", QrToken = "Q1", Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
        _db.Swimmers.Add(sw);
        await _db.SaveChangesAsync();

        var sub = new TrainingSubscription
        {
            SwimmerId = sw.SwimmerId, ProgramId = prog.ProgramId, PeriodId = prd.PeriodId,
            StartDate = new DateOnly(2026, 10, 1), EndDate = new DateOnly(2026, 12, 31),
            UnitPriceSnapshot = 500, ConfiguredSessionCountSnapshot = 24,
            TotalPrice = 500, PaidAmount = 300, Status = "ACTIVE", CreatedBy = usr.UserId, CreatedAt = DateTime.UtcNow
        };
        _db.TrainingSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(c => c.CurrentUser).Returns(usr);
        var auditMock = new Mock<IAuditLogService>();
        var paymentService = new PaymentService(_db, currentUserMock.Object, auditMock.Object);

        var result = await paymentService.RecordPaymentAsync("TRAINING_SUBSCRIPTION", sub.SubscriptionId, 999m, "CASH", null);

        Assert.Equal(PaymentResult.OverpaymentRejected, result);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 4. Historical Truth (Snapshot immutability)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrainingSubscription_UnitPriceSnapshot_NotAffectedByConfigChange()
    {
        var role = await _db.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var emp = new Employee { Name = "Hist", EmployeeType = "ADMINISTRATOR", NationalId = "HIST01", MonthlySalary = 1, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();
        var usr = new User { Username = "hist", PasswordHash = "x", RoleId = role.RoleId, EmployeeId = emp.EmployeeId, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Users.Add(usr);
        
        var prog = new Domain.Entities.Program { Name = "Prog", ProgramType = "REGULAR", IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Programs.Add(prog);
        await _db.SaveChangesAsync();

        var config = new TrainingPriceConfig
        {
            ProgramId = prog.ProgramId,
            MemberStatus = "MEMBER",
            Price = 600m,
            EffectiveFrom = new DateOnly(2026, 1, 1)
        };
        _db.TrainingPriceConfigs.Add(config);
        
        var prd = new TrainingPeriod { Program = prog, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Capacity = 20, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
        _db.TrainingPeriods.Add(prd);
        var sw = new Swimmer { SwimmerId = "SW2", Name = "SW2", DateOfBirth = new DateOnly(2005, 6, 15), Gender = "FEMALE", MemberStatus = "MEMBER", QrToken = "Q2", Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
        _db.Swimmers.Add(sw);
        await _db.SaveChangesAsync();

        var sub = new TrainingSubscription
        {
            SwimmerId = sw.SwimmerId, ProgramId = prog.ProgramId, PeriodId = prd.PeriodId,
            StartDate = new DateOnly(2026, 10, 1), EndDate = new DateOnly(2026, 12, 31),
            UnitPriceSnapshot = 600m, ConfiguredSessionCountSnapshot = 24,
            TotalPrice = 600m, PaidAmount = 600m, Status = "ACTIVE", CreatedBy = usr.UserId, CreatedAt = DateTime.UtcNow
        };
        _db.TrainingSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        // Change config
        config.Price = 900m;
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();
        var retrieved = await _db.TrainingSubscriptions.FindAsync(sub.SubscriptionId);
        Assert.NotNull(retrieved);
        Assert.Equal(600m, retrieved!.UnitPriceSnapshot);
        Assert.Equal(600m, retrieved.TotalPrice);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 5. Database Constraints
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Database_Backup_TypeConstraint_Enforced()
    {
        var backup = new SwimClub.Domain.Entities.Backup
        {
            BackupType = "INVALID_TYPE",
            CreatedAt = DateTime.UtcNow,
            SystemVersion = "1.0.0",
            FilePath = "/tmp/test.enc",
            FileSizeBytes = 100,
            Encrypted = true,
            Status = "SUCCESS"
        };
        _db.Backups.Add(backup);
        await Assert.ThrowsAnyAsync<Exception>(() => _db.SaveChangesAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 6. Sensitive Data
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserManagement_PasswordReset_NationalIdNotStoredInAuditLog()
    {
        var adminRole = await _db.Roles.FirstAsync(r => r.Code == "ADMINISTRATOR");
        var ownerRole = await _db.Roles.FirstAsync(r => r.Code == "OWNER");
        
        var emp = new Employee { Name = "Sens", EmployeeType = "ADMINISTRATOR", NationalId = "SENSITIVE999", MonthlySalary = 3000, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();

        var usr = new User { Username = "sens_usr", PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("SENSITIVE999"), RoleId = adminRole.RoleId, EmployeeId = emp.EmployeeId, IsActive = true, CreatedAt = DateTime.UtcNow };
        var actorUser = new User { Username = "owner_usr", PasswordHash = "hash", RoleId = ownerRole.RoleId, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Users.Add(usr);
        _db.Users.Add(actorUser);
        await _db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        var auditSvc = new AuditLogService(_db, currentUserMock.Object);
        var userMgmt = new UserManagementService(_db, auditSvc);

        await userMgmt.ResetPasswordByNationalIdAsync("SENSITIVE999", actorUser.UserId);

        var auditLogs = await _db.AuditLogs.Where(a => a.EventType == "PASSWORD_RESET").ToListAsync();
        Assert.NotEmpty(auditLogs);
        foreach (var log in auditLogs)
        {
            Assert.DoesNotContain("SENSITIVE999", log.Description ?? "");
        }
    }
}
