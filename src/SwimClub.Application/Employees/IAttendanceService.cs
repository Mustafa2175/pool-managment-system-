using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Employees;

public enum AttendanceResult
{
    Success,
    NotFound,
    EmployeeNotFound,
    SessionNotFound
}

/// <summary>
/// Manages both Administrator daily attendance and Coach/Lifeguard session attendance.
/// Enforces Decision 17 (1-hour post-Period deadline for late edits).
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Records daily attendance for an Administrator.
    /// Status must be PRESENT or ABSENT.
    /// </summary>
    Task<(AttendanceResult Result, AdministratorDailyAttendance? Attendance)> RecordAdministratorAttendanceAsync(
        int employeeId,
        DateOnly date,
        string status,
        int recordedBy);

    Task<AttendanceResult> UpdateAdministratorAttendanceAsync(
        int attendanceId,
        string newStatus,
        int recordedBy);

    /// <summary>
    /// Gets the count of ABSENT days for an Administrator in a given month.
    /// Used by Payroll calculation.
    /// </summary>
    Task<int> GetAdministratorAbsentDaysAsync(int employeeId, int year, int month);

    /// <summary>
    /// Records session attendance for a Coach or Lifeguard.
    /// Evaluates if the current time is past the Session.EndTime + 1 hour deadline.
    /// If so, flags IsLateEdit=true and audits EMPLOYEE_ATTENDANCE_LATE_EDIT (Decision 17).
    /// </summary>
    Task<(AttendanceResult Result, EmployeeAttendance? Attendance)> RecordEmployeeSessionAttendanceAsync(
        int sessionId,
        int employeeId,
        string status,
        int recordedBy);

    /// <summary>
    /// Updates session attendance for a Coach or Lifeguard.
    /// Evaluates the 1-hour deadline exactly as Record does.
    /// </summary>
    Task<AttendanceResult> UpdateEmployeeSessionAttendanceAsync(
        int attendanceId,
        string newStatus,
        int recordedBy);
        
    Task<IReadOnlyList<EmployeeAttendance>> GetEmployeeSessionAttendancesAsync(int sessionId);
}
