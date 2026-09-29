using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Employees;
using SwimClub.Application.Interfaces;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Employees;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public AttendanceService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<(AttendanceResult Result, AdministratorDailyAttendance? Attendance)> RecordAdministratorAttendanceAsync(
        int employeeId,
        DateOnly date,
        string status,
        int recordedBy)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return (AttendanceResult.EmployeeNotFound, null);

        var existing = await _context.AdministratorDailyAttendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == date);
            
        if (existing != null)
        {
            existing.Status = status;
            await _context.SaveChangesAsync();
            return (AttendanceResult.Success, existing);
        }

        var attendance = new AdministratorDailyAttendance
        {
            EmployeeId = employeeId,
            AttendanceDate = date,
            Status = status,
            RecordedBy = recordedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdministratorDailyAttendances.Add(attendance);
        await _context.SaveChangesAsync();
        
        return (AttendanceResult.Success, attendance);
    }

    public async Task<AttendanceResult> UpdateAdministratorAttendanceAsync(
        int attendanceId,
        string newStatus,
        int recordedBy)
    {
        var attendance = await _context.AdministratorDailyAttendances.FindAsync(attendanceId);
        if (attendance == null) return AttendanceResult.NotFound;
        
        attendance.Status = newStatus;
        await _context.SaveChangesAsync();
        
        return AttendanceResult.Success;
    }

    public async Task<int> GetAdministratorAbsentDaysAsync(int employeeId, int year, int month)
    {
        return await _context.AdministratorDailyAttendances
            .CountAsync(a => a.EmployeeId == employeeId && 
                             a.AttendanceDate.Year == year && 
                             a.AttendanceDate.Month == month && 
                             a.Status == "ABSENT");
    }

    public async Task<(AttendanceResult Result, EmployeeAttendance? Attendance)> RecordEmployeeSessionAttendanceAsync(
        int sessionId,
        int employeeId,
        string status,
        int recordedBy)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null) return (AttendanceResult.SessionNotFound, null);
        
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return (AttendanceResult.EmployeeNotFound, null);

        // Decision 17: Late edit is > 1 hour past session end
        // For testing/simplicity, if the caller is recording attendance far after the fact, we flag it.
        // Assuming session date/time are UTC for this comparison.
        var sessionEndDateTime = session.ScheduledEndTime.ToUniversalTime();
        bool isLateEdit = DateTime.UtcNow > sessionEndDateTime.AddHours(1);

        var existing = await _context.EmployeeAttendances
            .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.EmployeeId == employeeId);

        if (existing != null)
        {
            existing.Status = status;
            existing.IsLateEdit = isLateEdit || existing.IsLateEdit; // once late, always late
            existing.LastModifiedAt = DateTime.UtcNow;
            existing.LastModifiedBy = recordedBy;
            
            await _context.SaveChangesAsync();
            
            if (isLateEdit)
            {
                await _auditLog.LogSuccessAsync("EMPLOYEE_ATTENDANCE_LATE_EDIT", "EmployeeAttendance", existing.AttendanceId, 
                    $"Late edit to {status}");
            }
            
            return (AttendanceResult.Success, existing);
        }

        var attendance = new EmployeeAttendance
        {
            SessionId = sessionId,
            EmployeeId = employeeId,
            Status = status,
            IsLateEdit = isLateEdit,
            RecordedBy = recordedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeAttendances.Add(attendance);
        await _context.SaveChangesAsync();
        
        if (isLateEdit)
        {
            await _auditLog.LogSuccessAsync("EMPLOYEE_ATTENDANCE_LATE_EDIT", "EmployeeAttendance", attendance.AttendanceId, 
                $"Late record to {status}");
        }

        return (AttendanceResult.Success, attendance);
    }

    public async Task<AttendanceResult> UpdateEmployeeSessionAttendanceAsync(
        int attendanceId,
        string newStatus,
        int recordedBy)
    {
        var attendance = await _context.EmployeeAttendances
            .Include(a => a.Session)
            .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);
            
        if (attendance == null) return AttendanceResult.NotFound;
        
        var sessionEndDateTime = attendance.Session.ScheduledEndTime.ToUniversalTime();
        bool isLateEdit = DateTime.UtcNow > sessionEndDateTime.AddHours(1);
        
        attendance.Status = newStatus;
        attendance.LastModifiedAt = DateTime.UtcNow;
        attendance.LastModifiedBy = recordedBy;
        
        if (isLateEdit) attendance.IsLateEdit = true;
        
        await _context.SaveChangesAsync();
        
        if (isLateEdit)
        {
            await _auditLog.LogSuccessAsync("EMPLOYEE_ATTENDANCE_LATE_EDIT", "EmployeeAttendance", attendance.AttendanceId, 
                $"Late edit to {newStatus}");
        }
        
        return AttendanceResult.Success;
    }

    public async Task<IReadOnlyList<EmployeeAttendance>> GetEmployeeSessionAttendancesAsync(int sessionId)
    {
        return await _context.EmployeeAttendances
            .Include(a => a.Employee)
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.Employee.Name)
            .ToListAsync();
    }
}
