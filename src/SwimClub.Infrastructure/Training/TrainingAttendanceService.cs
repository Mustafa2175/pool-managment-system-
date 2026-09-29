using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Employees;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Training;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Training;

public class TrainingAttendanceService : ITrainingAttendanceService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IQualificationService _qualificationService;

    public TrainingAttendanceService(AppDbContext context, IAuditLogService auditLog, IQualificationService qualificationService)
    {
        _context = context;
        _auditLog = auditLog;
        _qualificationService = qualificationService;
    }

    public async Task<TrainingAttendanceResult> RecordSwimmerAttendanceAsync(string swimmerId, int sessionId, int actorUserId)
    {
        return await ProcessSwimmerAttendanceAsync(swimmerId, sessionId, actorUserId, "MANUAL");
    }

    public async Task<TrainingAttendanceResult> RecordSwimmerAttendanceByQrAsync(string qrToken, int sessionId, int actorUserId)
    {
        var swimmer = await _context.Swimmers.FirstOrDefaultAsync(s => s.QrToken == qrToken);
        if (swimmer == null) return TrainingAttendanceResult.SwimmerNotFound;

        return await ProcessSwimmerAttendanceAsync(swimmer.SwimmerId, sessionId, actorUserId, "QR");
    }

    private async Task<TrainingAttendanceResult> ProcessSwimmerAttendanceAsync(string swimmerId, int sessionId, int actorUserId, string method)
    {
        var session = await _context.Sessions
            .Include(s => s.Subscription)
            .Include(s => s.Attendance)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null || session.Subscription.SwimmerId != swimmerId)
            return TrainingAttendanceResult.SessionNotFound;

        if (session.Attendance != null)
            return TrainingAttendanceResult.AlreadyAttended;

        // Verify Attendance Window [-30m, +30m]
        var now = DateTime.UtcNow;
        var startWindow = session.ScheduledStartTime.AddMinutes(-30);
        var endWindow = session.ScheduledStartTime.AddMinutes(30);

        if (now < startWindow || now > endWindow)
        {
            await _auditLog.LogSystemEventAsync("ATTENDANCE_REJECTED", false, $"Swimmer {swimmerId} tried to attend session {sessionId} outside window (Now: {now}, Window: {startWindow} - {endWindow})");
            return TrainingAttendanceResult.OutsideAttendanceWindow; // Hard reject, no override (Doc 05, 2.6)
        }

        using var tx = await _context.Database.BeginTransactionAsync();

        var attendance = new Attendance
        {
            SessionId = sessionId,
            SwimmerId = swimmerId,
            CheckedInAt = now,
            AttendanceMethod = method,
            RecordedBy = actorUserId,
            CreatedAt = now
        };

        _context.Attendances.Add(attendance);
        
        session.Status = "COMPLETED";

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("SWIMMER_ATTENDANCE", "Attendance", attendance.AttendanceId, $"Method: {method}");
        
        await tx.CommitAsync();

        return TrainingAttendanceResult.Success;
    }

    public async Task<TrainingAttendanceResult> RecordEmployeeAttendanceAsync(int employeeId, int sessionId, int actorUserId)
    {
        var session = await _context.Sessions
            .Include(s => s.Period)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null) return TrainingAttendanceResult.SessionNotFound;

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return TrainingAttendanceResult.EmployeeNotFound;

        var existing = await _context.EmployeeAttendances
            .FirstOrDefaultAsync(ea => ea.SessionId == sessionId && ea.EmployeeId == employeeId);

        var now = DateTime.UtcNow;
        var deadline = session.ScheduledEndTime.AddHours(1);
        bool isLateEdit = now > deadline;

        if (existing == null)
        {
            _context.EmployeeAttendances.Add(new EmployeeAttendance
            {
                SessionId = sessionId,
                EmployeeId = employeeId,
                Status = "PRESENT",
                IsLateEdit = isLateEdit,
                RecordedBy = actorUserId,
                CreatedAt = now
            });
        }
        else
        {
            existing.Status = "PRESENT";
            existing.IsLateEdit = isLateEdit;
            existing.LastModifiedBy = actorUserId;
            existing.LastModifiedAt = now;
        }

        await _context.SaveChangesAsync();

        if (isLateEdit)
        {
            await _auditLog.LogSystemEventAsync("LATE_EMPLOYEE_ATTENDANCE", true, $"User {actorUserId} edited attendance for employee {employeeId} after 1h deadline.");
        }

        return TrainingAttendanceResult.Success;
    }

    public async Task<TrainingAttendanceResult> AssignReplacementStaffAsync(int originalEmployeeId, int replacementEmployeeId, int sessionId, int actorUserId)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null) return TrainingAttendanceResult.SessionNotFound;

        var original = await _context.Employees.FindAsync(originalEmployeeId);
        var replacement = await _context.Employees.FindAsync(replacementEmployeeId);
        
        if (original == null || replacement == null) return TrainingAttendanceResult.EmployeeNotFound;

        // Resolve rate snapshot for replacement
        var sessionDate = DateOnly.FromDateTime(session.ScheduledStartTime);
        var rate = await _qualificationService.ResolveCurrentRateAsync(replacementEmployeeId, sessionDate);
        var rateSnapshot = rate ?? 0m; // If no qualification/rate config found, default to 0

        var existing = await _context.EmployeeReplacements
            .FirstOrDefaultAsync(er => er.SessionId == sessionId && er.OriginalEmployeeId == originalEmployeeId);

        if (existing == null)
        {
            _context.EmployeeReplacements.Add(new EmployeeReplacement
            {
                SessionId = sessionId,
                OriginalEmployeeId = originalEmployeeId,
                ReplacingEmployeeId = replacementEmployeeId,
                RateAppliedSnapshot = rateSnapshot,
                RecordedBy = actorUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.ReplacingEmployeeId = replacementEmployeeId;
            existing.RateAppliedSnapshot = rateSnapshot;
        }

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("EMPLOYEE_REPLACEMENT", "Session", sessionId, $"Replaced {originalEmployeeId} with {replacementEmployeeId}");

        return TrainingAttendanceResult.Success;
    }
}
