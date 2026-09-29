using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SwimClub.Application.Private;

public enum PrivateBookingResult
{
    Success,
    CoachNotFound,
    CoachNotCoach,
    LaneNotFound,
    LaneInactive,
    LaneOverCapacity,
    LaneConflict,
    CoachConflict,
    InvalidParticipant,
    InvalidConfig,
    BookingNotFound,
    InvalidStateTransition,
    Overpayment,
    SessionNotFound,
    AlreadyAttended,
    NoSessionsConfigured
}

public record ParticipantDto(
    string ParticipantType, // SWIMMER | GUEST
    string? SwimmerId,
    string? GuestName,
    string? GuestPhone);

public interface IPrivateBookingService
{
    /// <summary>
    /// Creates a Lane Rental booking. Club fee is fixed; participant count affects only capacity check.
    /// </summary>
    Task<(PrivateBookingResult Result, int? BookingId)> CreateLaneRentalAsync(
        int coachId,
        int laneId,
        DateTime startTime,
        DateTime endTime,
        DateOnly sessionStartDate,
        decimal paidAmount,
        IReadOnlyList<ParticipantDto> participants,
        int actorUserId);

    /// <summary>
    /// Creates a Coach-Brought booking. Club revenue = participants × fee per swimmer.
    /// </summary>
    Task<(PrivateBookingResult Result, int? BookingId)> CreateCoachBroughtAsync(
        int coachId,
        DateTime startTime,
        DateTime endTime,
        DateOnly sessionStartDate,
        decimal paidAmount,
        IReadOnlyList<ParticipantDto> participants,
        int actorUserId);

    /// <summary>
    /// Creates a Club-Brought booking. Percentages snapshotted at creation; CoachDue accrued.
    /// </summary>
    Task<(PrivateBookingResult Result, int? BookingId)> CreateClubBroughtAsync(
        int coachId,
        DateTime startTime,
        DateTime endTime,
        DateOnly sessionStartDate,
        decimal totalAmount,
        decimal paidAmount,
        IReadOnlyList<ParticipantDto> participants,
        int actorUserId);

    Task<PrivateBookingResult> CancelBookingAsync(int bookingId, int actorUserId);

    Task<PrivateBookingResult> RecordAttendanceAsync(int sessionId, string participantId, bool attended, int actorUserId);
}
