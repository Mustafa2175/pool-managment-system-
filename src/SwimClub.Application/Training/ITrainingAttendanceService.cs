using System;
using System.Threading.Tasks;

namespace SwimClub.Application.Training;

public enum TrainingAttendanceResult
{
    Success,
    SessionNotFound,
    SwimmerNotFound,
    OutsideAttendanceWindow,
    AlreadyAttended,
    EmployeeNotFound,
    Unauthorized
}

public interface ITrainingAttendanceService
{
    /// <summary>
    /// Records swimmer attendance using their SwimmerId.
    /// Enforces the [-30m, +30m] attendance window.
    /// </summary>
    Task<TrainingAttendanceResult> RecordSwimmerAttendanceAsync(string swimmerId, int sessionId, int actorUserId);

    /// <summary>
    /// Records swimmer attendance using their QR Token.
    /// </summary>
    Task<TrainingAttendanceResult> RecordSwimmerAttendanceByQrAsync(string qrToken, int sessionId, int actorUserId);

    /// <summary>
    /// Records Employee (Coach/Lifeguard) attendance for a session.
    /// Enforces the 1-hour post-period rule, and audits if outside.
    /// </summary>
    Task<TrainingAttendanceResult> RecordEmployeeAttendanceAsync(int employeeId, int sessionId, int actorUserId);

    /// <summary>
    /// Assigns a replacement Coach or Lifeguard for a specific Session.
    /// </summary>
    Task<TrainingAttendanceResult> AssignReplacementStaffAsync(int originalEmployeeId, int replacementEmployeeId, int sessionId, int actorUserId);
}
