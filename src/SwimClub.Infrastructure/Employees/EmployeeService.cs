using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Employees;
using SwimClub.Application.Interfaces;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using BCrypt.Net;

namespace SwimClub.Infrastructure.Employees;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public EmployeeService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<(EmployeeResult Result, Employee? Employee)> CreateEmployeeAsync(
        string name,
        string employeeType,
        string nationalId,
        string? phone,
        decimal? monthlySalary,
        string? nickname,
        int createdByUserId)
    {
        if (employeeType != "ADMINISTRATOR" && employeeType != "COACH" && employeeType != "LIFEGUARD")
        {
            return (EmployeeResult.InvalidType, null);
        }

        if (employeeType == "ADMINISTRATOR" && !monthlySalary.HasValue)
        {
            return (EmployeeResult.SalaryRequired, null);
        }

        if (employeeType != "ADMINISTRATOR" && monthlySalary.HasValue)
        {
            return (EmployeeResult.SalaryForbidden, null);
        }

        // Check for duplicate national ID
        bool nationalIdExists = await _context.Employees.AnyAsync(e => e.NationalId == nationalId);
        if (nationalIdExists)
        {
            return (EmployeeResult.DuplicateNationalId, null);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var employee = new Employee
            {
                Name = name,
                EmployeeType = employeeType,
                NationalId = nationalId,
                Phone = phone,
                MonthlySalary = monthlySalary,
                Nickname = nickname,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // If ADMINISTRATOR, also create the User
            if (employeeType == "ADMINISTRATOR")
            {
                string targetUsername = name;
                if (!string.IsNullOrWhiteSpace(nickname))
                {
                    targetUsername = $"{name} {nickname}";
                }

                bool usernameExists = await _context.Users.AnyAsync(u => u.Username == targetUsername);
                if (usernameExists)
                {
                    await transaction.RollbackAsync();
                    return string.IsNullOrWhiteSpace(nickname) 
                        ? (EmployeeResult.NicknameRequired, null)
                        : (EmployeeResult.NicknameCollision, null);
                }

                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "ADMINISTRATOR");
                if (adminRole == null)
                {
                    throw new InvalidOperationException("ADMINISTRATOR role not found.");
                }

                // Initial password is the National ID
                string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(nationalId);

                var user = new User
                {
                    Username = targetUsername,
                    PasswordHash = hashedPassword,
                    RoleId = adminRole.RoleId,
                    EmployeeId = employee.EmployeeId,
                    IsActive = true,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            await _auditLog.LogSuccessAsync("CREATE_EMPLOYEE", "Employee", employee.EmployeeId, 
                $"Created employee {name} ({employeeType})");

            return (EmployeeResult.Success, employee);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<EmployeeResult> DeactivateEmployeeAsync(int employeeId, int actorUserId)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee == null) return EmployeeResult.NotFound;
        if (employee.Status == "INACTIVE") return EmployeeResult.AlreadyInactive;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            employee.Status = "INACTIVE";
            employee.DeactivatedAt = DateTime.UtcNow;

            if (employee.User != null && employee.User.IsActive)
            {
                employee.User.IsActive = false;
                employee.User.DeactivatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLog.LogSuccessAsync("DEACTIVATE_EMPLOYEE", "Employee", employeeId);

            return EmployeeResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<EmployeeResult> ReactivateEmployeeAsync(int employeeId, int actorUserId)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee == null) return EmployeeResult.NotFound;
        if (employee.Status == "ACTIVE") return EmployeeResult.AlreadyActive;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            employee.Status = "ACTIVE";
            employee.DeactivatedAt = null;

            if (employee.User != null && !employee.User.IsActive)
            {
                employee.User.IsActive = true;
                employee.User.DeactivatedAt = null;
                // Username and PasswordHash remain untouched per Decision 15
                
                await _auditLog.LogSuccessAsync("REACTIVATE_USER", "User", employee.User.UserId);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLog.LogSuccessAsync("REACTIVATE_EMPLOYEE", "Employee", employeeId);

            return EmployeeResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
    {
        return await _context.Employees
            .Include(e => e.User)
            .Include(e => e.EmployeeQualifications)
                .ThenInclude(eq => eq.Qualification)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
    }

    public async Task<IReadOnlyList<Employee>> GetAllEmployeesAsync(string? typeFilter = null, string? statusFilter = null)
    {
        var query = _context.Employees.AsQueryable();

        if (!string.IsNullOrEmpty(typeFilter))
            query = query.Where(e => e.EmployeeType == typeFilter);
            
        if (!string.IsNullOrEmpty(statusFilter))
            query = query.Where(e => e.Status == statusFilter);

        return await query
            .Include(e => e.User)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }
}
