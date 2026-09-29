using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Employees;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Employees;
using SwimClub.Infrastructure.Persistence;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class EmployeeTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private IEmployeeService _employeeService = null!;
    private IUserManagementService _userService = null!;
    private IQualificationService _qualificationService = null!;
    private IAttendanceService _attendanceService = null!;

    private AppDbContext CreateContext()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"swimclub-emp-test-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public async Task InitializeAsync()
    {
        _context = CreateContext();
        var auditLog = new Logging.AuditLogService(_context, new SwimClub.Infrastructure.Security.CurrentUserService());
        
        _employeeService = new EmployeeService(_context, auditLog);
        _userService = new UserManagementService(_context, auditLog);
        _qualificationService = new QualificationService(_context, auditLog);
        _attendanceService = new AttendanceService(_context, auditLog);

        await SeedBaseDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedBaseDataAsync()
    {
        if (!await _context.Roles.AnyAsync())
        {
            _context.Roles.AddRange(
                new Role { Code = "SUPER_ADMIN", NameEn = "Super Admin", NameAr = "Super Admin" },
                new Role { Code = "OWNER", NameEn = "Owner", NameAr = "Owner" },
                new Role { Code = "ADMINISTRATOR", NameEn = "Administrator", NameAr = "Administrator" }
            );
            await _context.SaveChangesAsync();
        }

        var superAdminRole = await _context.Roles.FirstAsync(r => r.Code == "SUPER_ADMIN");
        _context.Users.Add(new User
        {
            Username = "sysadmin",
            PasswordHash = "hash",
            RoleId = superAdminRole.RoleId,
            IsActive = true
        });
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAdministratorEmployee_CreatesLinkedUser_WithCorrectUsername()
    {
        var (res, emp) = await _employeeService.CreateEmployeeAsync("John Doe", "ADMINISTRATOR", "123456789", null, 5000, null, 1);
        Assert.Equal(EmployeeResult.Success, res);
        Assert.NotNull(emp);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == emp.EmployeeId);
        Assert.NotNull(user);
        Assert.Equal("John Doe", user.Username);
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("123456789", user.PasswordHash));
    }

    [Fact]
    public async Task CreateCoach_NoUserCreated()
    {
        var (res, emp) = await _employeeService.CreateEmployeeAsync("Coach Bob", "COACH", "222222", null, null, null, 1);
        Assert.Equal(EmployeeResult.Success, res);
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == emp!.EmployeeId);
        Assert.Null(user); // No login for coach
    }

    [Fact]
    public async Task CreateLifeguard_NoUserCreated()
    {
        var (res, emp) = await _employeeService.CreateEmployeeAsync("Life Guard", "LIFEGUARD", "333333", null, null, null, 1);
        Assert.Equal(EmployeeResult.Success, res);
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == emp!.EmployeeId);
        Assert.Null(user);
    }

    [Fact]
    public async Task CreateEmployee_UsernameCollision_RequiresNickname()
    {
        await _employeeService.CreateEmployeeAsync("Alice", "ADMINISTRATOR", "111", null, 5000, null, 1);
        
        // Attempt to create another "Alice" without nickname
        var (res, emp) = await _employeeService.CreateEmployeeAsync("Alice", "ADMINISTRATOR", "222", null, 5000, null, 1);
        
        Assert.Equal(EmployeeResult.NicknameRequired, res);
        Assert.Null(emp);
    }

    [Fact]
    public async Task CreateEmployee_WithNickname_UsesNamePlusNickname()
    {
        await _employeeService.CreateEmployeeAsync("Alice", "ADMINISTRATOR", "111", null, 5000, null, 1);
        
        var (res, emp) = await _employeeService.CreateEmployeeAsync("Alice", "ADMINISTRATOR", "222", null, 5000, "Smith", 1);
        
        Assert.Equal(EmployeeResult.Success, res);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == emp!.EmployeeId);
        Assert.Equal("Alice Smith", user!.Username);
    }

    [Fact]
    public async Task DeactivateEmployee_SetInactiveAndDeactivatesUser()
    {
        var (_, emp) = await _employeeService.CreateEmployeeAsync("To Deactivate", "ADMINISTRATOR", "999", null, 5000, null, 1);
        
        var res = await _employeeService.DeactivateEmployeeAsync(emp!.EmployeeId, 1);
        Assert.Equal(EmployeeResult.Success, res);

        var refreshedEmp = await _context.Employees.Include(e => e.User).FirstAsync(e => e.EmployeeId == emp.EmployeeId);
        Assert.Equal("INACTIVE", refreshedEmp.Status);
        Assert.NotNull(refreshedEmp.DeactivatedAt);
        Assert.False(refreshedEmp.User!.IsActive);
        Assert.NotNull(refreshedEmp.User.DeactivatedAt);
    }

    [Fact]
    public async Task ReactivateEmployee_RestoresStatusPreservesCredentials()
    {
        var (_, emp) = await _employeeService.CreateEmployeeAsync("To Reactivate", "ADMINISTRATOR", "888", null, 5000, null, 1);
        await _employeeService.DeactivateEmployeeAsync(emp!.EmployeeId, 1);
        
        var userBefore = await _context.Users.AsNoTracking().FirstAsync(u => u.EmployeeId == emp.EmployeeId);
        
        var res = await _employeeService.ReactivateEmployeeAsync(emp.EmployeeId, 1);
        Assert.Equal(EmployeeResult.Success, res);

        var refreshedEmp = await _context.Employees.Include(e => e.User).FirstAsync(e => e.EmployeeId == emp.EmployeeId);
        Assert.Equal("ACTIVE", refreshedEmp.Status);
        Assert.Null(refreshedEmp.DeactivatedAt);
        Assert.True(refreshedEmp.User!.IsActive);
        Assert.Null(refreshedEmp.User.DeactivatedAt);
        
        // Password hash unchanged
        Assert.Equal(userBefore.PasswordHash, refreshedEmp.User.PasswordHash);
        
        // Audit log created
        bool auditExists = await _context.AuditLogs.AnyAsync(a => a.EventType == "REACTIVATE_USER" && a.EntityType == "User" && a.EntityId == refreshedEmp.User.UserId.ToString());
        Assert.True(auditExists);
    }

    [Fact]
    public async Task AdministratorSalaryRequired_CoachSalaryForbidden()
    {
        var (resAdmin, _) = await _employeeService.CreateEmployeeAsync("Admin No Salary", "ADMINISTRATOR", "1", null, null, null, 1);
        Assert.Equal(EmployeeResult.SalaryRequired, resAdmin);
        
        var (resCoach, _) = await _employeeService.CreateEmployeeAsync("Coach Salary", "COACH", "2", null, 1000, null, 1);
        Assert.Equal(EmployeeResult.SalaryForbidden, resCoach);
    }

    [Fact]
    public async Task QualificationRankOrder_MustBeUnique()
    {
        await _qualificationService.CreateQualificationAsync("Q1", "Q1", 10);
        var (res, _) = await _qualificationService.CreateQualificationAsync("Q2", "Q2", 10);
        
        Assert.Equal(QualificationResult.DuplicateRankOrder, res);
    }

    [Fact]
    public async Task AssignQualificationToCoach_Succeeds_AssignToAdmin_Fails()
    {
        var (_, q) = await _qualificationService.CreateQualificationAsync("Cert", "Cert", 5);
        var (_, coach) = await _employeeService.CreateEmployeeAsync("C", "COACH", "C1", null, null, null, 1);
        var (_, admin) = await _employeeService.CreateEmployeeAsync("A", "ADMINISTRATOR", "A1", null, 1000, null, 1);

        var coachRes = await _qualificationService.AssignQualificationToEmployeeAsync(coach!.EmployeeId, q!.QualificationId, DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(QualificationResult.Success, coachRes);

        var adminRes = await _qualificationService.AssignQualificationToEmployeeAsync(admin!.EmployeeId, q!.QualificationId, DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(QualificationResult.NotACoachOrLifeguard, adminRes);
    }

    [Fact]
    public async Task ResolveCurrentRate_UsesHighestRankedQualification_RespectsEffectiveDate()
    {
        var (_, coach) = await _employeeService.CreateEmployeeAsync("Star Coach", "COACH", "S1", null, null, null, 1);
        var (_, q1) = await _qualificationService.CreateQualificationAsync("Junior", "Junior", 1);
        var (_, q2) = await _qualificationService.CreateQualificationAsync("Senior", "Senior", 2);

        // Assign both
        await _qualificationService.AssignQualificationToEmployeeAsync(coach!.EmployeeId, q1!.QualificationId, DateOnly.MinValue);
        await _qualificationService.AssignQualificationToEmployeeAsync(coach.EmployeeId, q2!.QualificationId, DateOnly.MinValue);

        // Rate for Q2 (Senior) is active today
        await _qualificationService.SetRateConfigAsync(q2.QualificationId, 150m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        
        // Rate for Q1 (Junior) is active today, but shouldn't be used since Q2 has higher rank
        await _qualificationService.SetRateConfigAsync(q1.QualificationId, 100m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        var rate = await _qualificationService.ResolveCurrentRateAsync(coach.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(150m, rate);

        // If we look at a date before Q2 rate was effective, we get null because Q2 is still the highest rank, 
        // and it has no rate active on that old date. 
        var oldRate = await _qualificationService.ResolveCurrentRateAsync(coach.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));
        Assert.Null(oldRate);
    }

    [Fact]
    public async Task ResolveCurrentRate_NoQualification_ReturnsNull()
    {
        var (_, coach) = await _employeeService.CreateEmployeeAsync("No Quals", "COACH", "NQ1", null, null, null, 1);
        var rate = await _qualificationService.ResolveCurrentRateAsync(coach!.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Null(rate);
    }

    [Fact]
    public async Task RecordAdminAttendance_Present()
    {
        var (_, admin) = await _employeeService.CreateEmployeeAsync("Att Admin", "ADMINISTRATOR", "AA1", null, 1000, null, 1);
        
        var (res, att) = await _attendanceService.RecordAdministratorAttendanceAsync(admin!.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow), "PRESENT", 1);
        
        Assert.Equal(AttendanceResult.Success, res);
        Assert.Equal("PRESENT", att!.Status);
        
        // Update to ABSENT
        var updateRes = await _attendanceService.UpdateAdministratorAttendanceAsync(att.AttendanceId, "ABSENT", 1);
        Assert.Equal(AttendanceResult.Success, updateRes);
        
        var absentDays = await _attendanceService.GetAdministratorAbsentDaysAsync(admin.EmployeeId, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
        Assert.Equal(1, absentDays);
    }

    [Fact]
    public async Task RecordCoachSessionAttendance_LateEdit_Audited()
    {
        var (_, coach) = await _employeeService.CreateEmployeeAsync("Late Coach", "COACH", "LC1", null, null, null, 1);
        
        var program = new Domain.Entities.Program { Name = "Prog", ProgramType = "REGULAR", IsActive = true };
        _context.Programs.Add(program);
        await _context.SaveChangesAsync();

        var period = new TrainingPeriod { ProgramId = program.ProgramId, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Capacity = 10, Status = "ACTIVE" };
        _context.TrainingPeriods.Add(period);
        await _context.SaveChangesAsync();
        
        var swimmer = new Swimmer { SwimmerId = "SW1", Name = "Swimmer 1", DateOfBirth = new DateOnly(2010, 1, 1), Gender = "MALE", MemberStatus = "MEMBER", Status = "ACTIVE", QrToken = "token" };
        _context.Swimmers.Add(swimmer);
        await _context.SaveChangesAsync();

        var sub = new TrainingSubscription { SwimmerId = swimmer.SwimmerId, ProgramId = program.ProgramId, PeriodId = period.PeriodId, StartDate = new DateOnly(2023, 1, 1), EndDate = new DateOnly(2023, 12, 31), UnitPriceSnapshot = 100, ConfiguredSessionCountSnapshot = 10, TotalPrice = 100, PaidAmount = 100, Status = "ACTIVE", CreatedBy = 1 };
        _context.TrainingSubscriptions.Add(sub);
        await _context.SaveChangesAsync();

        // Seed a Session way in the past
        var session = new Session 
        { 
            PeriodId = period.PeriodId,
            SubscriptionId = sub.SubscriptionId,
            ScheduledStartTime = DateTime.UtcNow.AddDays(-1).Date.AddHours(10),
            ScheduledEndTime = DateTime.UtcNow.AddDays(-1).Date.AddHours(11),
            Status = "COMPLETED"
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        
        var (res, att) = await _attendanceService.RecordEmployeeSessionAttendanceAsync(session.SessionId, coach!.EmployeeId, "PRESENT", 1);
        
        Assert.Equal(AttendanceResult.Success, res);
        Assert.True(att!.IsLateEdit); // Over 1 hour past yesterday 11:00 AM
        
        bool auditExists = await _context.AuditLogs.AnyAsync(a => a.EventType == "EMPLOYEE_ATTENDANCE_LATE_EDIT" && a.EntityId == att.AttendanceId.ToString());
        Assert.True(auditExists);
    }

    [Fact]
    public async Task PasswordReset_OwnerPath_ValidNationalId_Succeeds()
    {
        // 1 = sysadmin user (SUPER_ADMIN)
        var (_, emp) = await _employeeService.CreateEmployeeAsync("Target", "ADMINISTRATOR", "NID123", null, 1000, null, 1);
        var targetUserBefore = await _context.Users.AsNoTracking().FirstAsync(u => u.EmployeeId == emp!.EmployeeId);

        var res = await _userService.ResetPasswordByNationalIdAsync("NID123", 1); // 1 = actor
        
        Assert.Equal(PasswordResetResult.Success, res);
        
        var targetUserAfter = await _context.Users.AsNoTracking().FirstAsync(u => u.EmployeeId == emp!.EmployeeId);
        Assert.NotEqual(targetUserBefore.PasswordHash, targetUserAfter.PasswordHash); // Hash salt changes even if same plaintext
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("NID123", targetUserAfter.PasswordHash));
        
        bool auditExists = await _context.AuditLogs.AnyAsync(a => a.EventType == "PASSWORD_RESET" && !a.Description!.Contains("not found"));
        Assert.True(auditExists);
    }

    [Fact]
    public async Task PasswordReset_OwnerPath_WrongNationalId_Fails_Audited()
    {
        var (_, emp) = await _employeeService.CreateEmployeeAsync("Target2", "ADMINISTRATOR", "REAL_NID", null, 1000, null, 1);

        var res = await _userService.ResetPasswordByNationalIdAsync("FAKE_NID", 1); 
        
        Assert.Equal(PasswordResetResult.TargetNotFound, res);
        
        bool auditExists = await _context.AuditLogs.AnyAsync(a => a.EventType == "PASSWORD_RESET" && a.Description!.Contains("not found"));
        Assert.True(auditExists);
    }

    [Fact]
    public async Task AdminSelfReset_CorrectCredentials_Succeeds()
    {
        var (_, emp) = await _employeeService.CreateEmployeeAsync("Self Reset", "ADMINISTRATOR", "ID777", null, 1000, null, 1);
        
        var res = await _userService.AdminSelfResetPasswordAsync("Self Reset", "ID777");
        Assert.Equal(PasswordResetResult.Success, res);
    }
}
