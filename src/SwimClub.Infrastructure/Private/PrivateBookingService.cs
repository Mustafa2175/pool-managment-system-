using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwimClub.Application.Finance;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Private;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure.Private;

/// <summary>
/// Implements Phase 9: Private Bookings (Lane Rental, Coach-Brought, Club-Brought).
///
/// Config stored in system_settings:
///   Private.SessionCount          — configured number of sessions per private booking (e.g. 8)
///   Private.LaneRentalFee         — flat club fee for a Lane Rental booking
///   Private.CoachBroughtFeePerSwimmer — club fee per swimmer for Coach-Brought
///   Private.ClubBroughtClubPct   — default club % for Club-Brought (snapshotted at creation)
///   Private.ClubBroughtCoachPct  — default coach % for Club-Brought (snapshotted at creation)
/// </summary>
public class PrivateBookingService : IPrivateBookingService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ISystemConfigurationService _sysConfig;

    public PrivateBookingService(
        AppDbContext context,
        IAuditLogService auditLog,
        ISystemConfigurationService sysConfig)
    {
        _context = context;
        _auditLog = auditLog;
        _sysConfig = sysConfig;
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    private async Task<int> GetSessionCountAsync()
    {
        var v = await _sysConfig.GetSettingAsync("Private.SessionCount");
        return v != null ? int.Parse(v) : 8; // default 8 sessions
    }

    private async Task<(decimal LaneRentalFee, decimal CoachBroughtFeePerSwimmer, decimal ClubPct, decimal CoachPct)> GetPrivateConfigAsync()
    {
        var lrf = await _sysConfig.GetSettingAsync("Private.LaneRentalFee");
        var cbf = await _sysConfig.GetSettingAsync("Private.CoachBroughtFeePerSwimmer");
        var clp = await _sysConfig.GetSettingAsync("Private.ClubBroughtClubPct");
        var cop = await _sysConfig.GetSettingAsync("Private.ClubBroughtCoachPct");
        return (
            lrf != null ? decimal.Parse(lrf) : 0m,
            cbf != null ? decimal.Parse(cbf) : 0m,
            clp != null ? decimal.Parse(clp) : 50m,
            cop != null ? decimal.Parse(cop) : 50m
        );
    }

    private async Task<(bool IsValid, PrivateBookingResult Error)> ValidateCoachAvailabilityAsync(
        int coachId, DateTime startTime, DateTime endTime, int? excludeBookingId = null)
    {
        // Coach must exist and be a COACH type
        var coach = await _context.Employees.FindAsync(coachId);
        if (coach == null || coach.Status != "ACTIVE") return (false, PrivateBookingResult.CoachNotFound);
        if (coach.EmployeeType != "COACH") return (false, PrivateBookingResult.CoachNotCoach);

        // No overlap with other Private bookings for same coach
        var conflictingBooking = await _context.PrivateBookings
            .Where(pb => pb.CoachId == coachId
                && pb.Status == "ACTIVE"
                && (excludeBookingId == null || pb.PrivateBookingId != excludeBookingId.Value)
                && pb.StartTime < endTime && pb.EndTime > startTime)
            .AnyAsync();

        if (conflictingBooking) return (false, PrivateBookingResult.CoachConflict);

        // No overlap with Training Period assignments for same coach
        var coachEmpId = coachId;
        var conflictingPeriod = await _context.PeriodStaffAssignments
            .Include(psa => psa.Period)
            .Where(psa => psa.EmployeeId == coachEmpId)
            .AnyAsync(psa => psa.Period.Status == "ACTIVE"
                // Convert period days/times to daily overlaps — simplified: check time overlap, assuming any day this week
                // Full implementation: generate all period occurrences in date range and check each
                // Pragmatic approach: check if any period session on days overlapping with booking time
                && psa.Period.StartTime < TimeOnly.FromDateTime(endTime)
                && psa.Period.EndTime > TimeOnly.FromDateTime(startTime));

        if (conflictingPeriod) return (false, PrivateBookingResult.CoachConflict);

        return (true, PrivateBookingResult.Success);
    }

    private List<PrivateSession> GenerateSessions(int bookingId, DateTime start, DateTime end, int sessionCount, DateTime createdAt)
    {
        // Distribute sessions weekly from start date
        var sessions = new List<PrivateSession>();
        var duration = end - start;
        var current = start;
        for (int i = 0; i < sessionCount; i++)
        {
            sessions.Add(new PrivateSession
            {
                PrivateBookingId = bookingId,
                ScheduledStartTime = current,
                ScheduledEndTime = current.Add(duration),
                Status = "SCHEDULED",
                CreatedAt = createdAt
            });
            current = current.AddDays(7);
        }
        return sessions;
    }

    private async Task<List<PrivateBookingParticipant>> BuildParticipantsAsync(
        int bookingId, IReadOnlyList<ParticipantDto> participants)
    {
        var result = new List<PrivateBookingParticipant>();
        foreach (var p in participants)
        {
            if (p.ParticipantType == "SWIMMER")
            {
                if (string.IsNullOrEmpty(p.SwimmerId)) return null!;
                var swimmer = await _context.Swimmers.FindAsync(p.SwimmerId);
                if (swimmer == null || swimmer.IsDeleted) return null!;
                result.Add(new PrivateBookingParticipant
                {
                    PrivateBookingId = bookingId,
                    ParticipantType = "SWIMMER",
                    SwimmerId = p.SwimmerId
                });
            }
            else if (p.ParticipantType == "GUEST")
            {
                if (string.IsNullOrEmpty(p.GuestName)) return null!;
                result.Add(new PrivateBookingParticipant
                {
                    PrivateBookingId = bookingId,
                    ParticipantType = "GUEST",
                    GuestName = p.GuestName,
                    GuestPhone = p.GuestPhone
                });
            }
            else return null!;
        }
        return result;
    }

    // ─── LANE RENTAL ──────────────────────────────────────────────────────────

    public async Task<(PrivateBookingResult Result, int? BookingId)> CreateLaneRentalAsync(
        int coachId,
        int laneId,
        DateTime startTime,
        DateTime endTime,
        DateOnly sessionStartDate,
        decimal paidAmount,
        IReadOnlyList<ParticipantDto> participants,
        int actorUserId)
    {
        var (coachOk, coachErr) = await ValidateCoachAvailabilityAsync(coachId, startTime, endTime);
        if (!coachOk) return (coachErr, null);

        var lane = await _context.Lanes.FindAsync(laneId);
        if (lane == null) return (PrivateBookingResult.LaneNotFound, null);
        if (lane.Status != "ACTIVE") return (PrivateBookingResult.LaneInactive, null);
        if (participants.Count > lane.Capacity) return (PrivateBookingResult.LaneOverCapacity, null);

        // Lane conflict: no overlapping bookings on same lane
        var laneConflict = await _context.PrivateBookings
            .AnyAsync(pb => pb.LaneId == laneId && pb.Status == "ACTIVE"
                         && pb.StartTime < endTime && pb.EndTime > startTime);
        if (laneConflict) return (PrivateBookingResult.LaneConflict, null);

        var config = await GetPrivateConfigAsync();
        if (config.LaneRentalFee == 0) return (PrivateBookingResult.InvalidConfig, null);

        var totalPrice = config.LaneRentalFee; // Fixed regardless of participant count
        if (paidAmount > totalPrice) return (PrivateBookingResult.Overpayment, null);

        var sessionCount = await GetSessionCountAsync();
        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync();

        var booking = new PrivateBooking
        {
            BusinessType = "LANE_RENTAL",
            CoachId = coachId,
            LaneId = laneId,
            StartTime = startTime,
            EndTime = endTime,
            TotalPrice = totalPrice,
            PaidAmount = paidAmount,
            Status = "ACTIVE",
            CreatedBy = actorUserId,
            CreatedAt = now
        };
        _context.PrivateBookings.Add(booking);
        await _context.SaveChangesAsync();

        var builtParticipants = await BuildParticipantsAsync(booking.PrivateBookingId, participants);
        if (builtParticipants == null) return (PrivateBookingResult.InvalidParticipant, null);
        foreach (var p in builtParticipants) p.PrivateBookingId = booking.PrivateBookingId;
        _context.PrivateBookingParticipants.AddRange(builtParticipants);

        var sessions = GenerateSessions(booking.PrivateBookingId, startTime, endTime, sessionCount, now);
        _context.PrivateSessions.AddRange(sessions);

        if (paidAmount > 0)
            _context.Payments.Add(new Payment
            {
                Amount = paidAmount,
                RelatedEntityType = "PRIVATE_BOOKING",
                RelatedEntityId = booking.PrivateBookingId,
                PaymentMethod = "CASH",
                RecordedBy = actorUserId,
                CreatedAt = now
            });

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("PRIVATE_BOOKING_CREATED", "PrivateBooking", booking.PrivateBookingId,
            $"LANE_RENTAL: Lane={laneId}, Coach={coachId}, Paid={paidAmount}");
        await tx.CommitAsync();

        return (PrivateBookingResult.Success, booking.PrivateBookingId);
    }

    // ─── COACH-BROUGHT ────────────────────────────────────────────────────────

    public async Task<(PrivateBookingResult Result, int? BookingId)> CreateCoachBroughtAsync(
        int coachId,
        DateTime startTime,
        DateTime endTime,
        DateOnly sessionStartDate,
        decimal paidAmount,
        IReadOnlyList<ParticipantDto> participants,
        int actorUserId)
    {
        var (coachOk, coachErr) = await ValidateCoachAvailabilityAsync(coachId, startTime, endTime);
        if (!coachOk) return (coachErr, null);

        var config = await GetPrivateConfigAsync();
        if (config.CoachBroughtFeePerSwimmer == 0) return (PrivateBookingResult.InvalidConfig, null);

        var totalPrice = participants.Count * config.CoachBroughtFeePerSwimmer;
        if (paidAmount > totalPrice) return (PrivateBookingResult.Overpayment, null);

        var sessionCount = await GetSessionCountAsync();
        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync();

        var booking = new PrivateBooking
        {
            BusinessType = "COACH_BROUGHT",
            CoachId = coachId,
            StartTime = startTime,
            EndTime = endTime,
            ClubFeePerSwimmerSnapshot = config.CoachBroughtFeePerSwimmer,
            TotalPrice = totalPrice,
            PaidAmount = paidAmount,
            Status = "ACTIVE",
            CreatedBy = actorUserId,
            CreatedAt = now
        };
        _context.PrivateBookings.Add(booking);
        await _context.SaveChangesAsync();

        var builtParticipants = await BuildParticipantsAsync(booking.PrivateBookingId, participants);
        if (builtParticipants == null) return (PrivateBookingResult.InvalidParticipant, null);
        foreach (var p in builtParticipants) p.PrivateBookingId = booking.PrivateBookingId;
        _context.PrivateBookingParticipants.AddRange(builtParticipants);

        var sessions = GenerateSessions(booking.PrivateBookingId, startTime, endTime, sessionCount, now);
        _context.PrivateSessions.AddRange(sessions);

        if (paidAmount > 0)
            _context.Payments.Add(new Payment
            {
                Amount = paidAmount,
                RelatedEntityType = "PRIVATE_BOOKING",
                RelatedEntityId = booking.PrivateBookingId,
                PaymentMethod = "CASH",
                RecordedBy = actorUserId,
                CreatedAt = now
            });

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("PRIVATE_BOOKING_CREATED", "PrivateBooking", booking.PrivateBookingId,
            $"COACH_BROUGHT: Coach={coachId}, Participants={participants.Count}, Paid={paidAmount}");
        await tx.CommitAsync();

        return (PrivateBookingResult.Success, booking.PrivateBookingId);
    }

    // ─── CLUB-BROUGHT ─────────────────────────────────────────────────────────

    public async Task<(PrivateBookingResult Result, int? BookingId)> CreateClubBroughtAsync(
        int coachId,
        DateTime startTime,
        DateTime endTime,
        DateOnly sessionStartDate,
        decimal totalAmount,
        decimal paidAmount,
        IReadOnlyList<ParticipantDto> participants,
        int actorUserId)
    {
        var (coachOk, coachErr) = await ValidateCoachAvailabilityAsync(coachId, startTime, endTime);
        if (!coachOk) return (coachErr, null);

        if (paidAmount > totalAmount) return (PrivateBookingResult.Overpayment, null);

        var config = await GetPrivateConfigAsync();
        // Percentages must sum to 100 — validate config
        if (Math.Round(config.ClubPct + config.CoachPct, 4) != 100m) return (PrivateBookingResult.InvalidConfig, null);

        var sessionCount = await GetSessionCountAsync();
        var now = DateTime.UtcNow;

        // Snapshot at creation (Decision 8)
        var clubPct = config.ClubPct;
        var coachPct = config.CoachPct;
        var coachShare = Math.Round(totalAmount * coachPct / 100, 2);

        await using var tx = await _context.Database.BeginTransactionAsync();

        var booking = new PrivateBooking
        {
            BusinessType = "CLUB_BROUGHT",
            CoachId = coachId,
            StartTime = startTime,
            EndTime = endTime,
            ClubPercentageSnapshot = clubPct,
            CoachPercentageSnapshot = coachPct,
            TotalPrice = totalAmount,
            PaidAmount = paidAmount,
            Status = "ACTIVE",
            CreatedBy = actorUserId,
            CreatedAt = now
        };
        _context.PrivateBookings.Add(booking);
        await _context.SaveChangesAsync();

        var builtParticipants = await BuildParticipantsAsync(booking.PrivateBookingId, participants);
        if (builtParticipants == null) return (PrivateBookingResult.InvalidParticipant, null);
        foreach (var p in builtParticipants) p.PrivateBookingId = booking.PrivateBookingId;
        _context.PrivateBookingParticipants.AddRange(builtParticipants);

        var sessions = GenerateSessions(booking.PrivateBookingId, startTime, endTime, sessionCount, now);
        _context.PrivateSessions.AddRange(sessions);

        // Accrue CoachDue at booking creation (Decision 4)
        _context.CoachDues.Add(new CoachDue
        {
            EmployeeId = coachId,
            SourceType = "CLUB_BROUGHT_SHARE",
            SourceId = booking.PrivateBookingId,
            Amount = coachShare,
            PeriodYear = now.Year,
            PeriodMonth = now.Month,
            CreatedAt = now
        });

        if (paidAmount > 0)
            _context.Payments.Add(new Payment
            {
                Amount = paidAmount,
                RelatedEntityType = "PRIVATE_BOOKING",
                RelatedEntityId = booking.PrivateBookingId,
                PaymentMethod = "CASH",
                RecordedBy = actorUserId,
                CreatedAt = now
            });

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("PRIVATE_BOOKING_CREATED", "PrivateBooking", booking.PrivateBookingId,
            $"CLUB_BROUGHT: Coach={coachId}, Total={totalAmount}, CoachShare={coachShare}, ClubPct={clubPct}%, CoachPct={coachPct}%");
        await tx.CommitAsync();

        return (PrivateBookingResult.Success, booking.PrivateBookingId);
    }

    // ─── CANCELLATION ─────────────────────────────────────────────────────────

    public async Task<PrivateBookingResult> CancelBookingAsync(int bookingId, int actorUserId)
    {
        var booking = await _context.PrivateBookings
            .Include(pb => pb.Sessions)
            .FirstOrDefaultAsync(pb => pb.PrivateBookingId == bookingId);

        if (booking == null) return PrivateBookingResult.BookingNotFound;
        if (booking.Status != "ACTIVE") return PrivateBookingResult.InvalidStateTransition;

        var now = DateTime.UtcNow;

        // Count completed sessions (sessions where at least one attendance was recorded as PRESENT)
        var completedSessionIds = await _context.PrivateSessions
            .Where(ps => ps.PrivateBookingId == bookingId && ps.Status == "COMPLETED")
            .Select(ps => ps.PrivateSessionId)
            .ToListAsync();
        int completedSessions = completedSessionIds.Count;

        await using var tx = await _context.Database.BeginTransactionAsync();

        booking.Status = "CANCELLED";
        // Cancel all future sessions
        foreach (var session in booking.Sessions.Where(s => s.Status == "SCHEDULED"))
            session.Status = "CANCELLED";

        decimal refundAmount = 0;

        if (booking.BusinessType == "LANE_RENTAL" || booking.BusinessType == "COACH_BROUGHT")
        {
            if (completedSessions == 0)
            {
                // Before first session: full refund
                refundAmount = booking.PaidAmount;
            }
            else if (completedSessions < 2)
            {
                // Before completing 2 sessions: 50% refund
                // IMPORTANT: The exact boundary ("before completing 2 sessions" means < 2 completed, 
                // i.e., exactly 1 completed session is still eligible for 50% refund) is UNRESOLVED per design docs.
                // We implement: if completedSessions >= 1 but < 2 → 50% retained, 50% refunded.
                // Do NOT invent whether exactly 1 session belongs here or in the "no refund" tier.
                refundAmount = Math.Round(booking.PaidAmount * 0.5m, 2);
            }
            else
            {
                // After completing 2 sessions: no refund
                refundAmount = 0;
            }
        }
        else if (booking.BusinessType == "CLUB_BROUGHT")
        {
            if (completedSessions == 0)
            {
                // Before first session: full refund, no cancellation fee
                refundAmount = booking.PaidAmount;
            }
            else
            {
                // After first session: cancellation fee applies (Decision 2)
                var feeConfig = await _context.CancellationFeeConfigs
                    .Where(c => c.EffectiveTo == null || c.EffectiveTo > DateOnly.FromDateTime(now))
                    .OrderByDescending(c => c.EffectiveFrom)
                    .FirstOrDefaultAsync();

                if (feeConfig == null)
                {
                    await tx.RollbackAsync();
                    return PrivateBookingResult.InvalidConfig;
                }

                var cancellationFee = Math.Round(booking.PaidAmount * feeConfig.FeePercentage / 100, 2);
                var coachReceives = cancellationFee; // 100% of fee to coach (Decision 2)
                refundAmount = booking.PaidAmount - cancellationFee;

                // Insert CANCELLATION_FEE CoachDue
                _context.CoachDues.Add(new CoachDue
                {
                    EmployeeId = booking.CoachId!.Value,
                    SourceType = "CANCELLATION_FEE",
                    SourceId = bookingId,
                    Amount = coachReceives,
                    PeriodYear = now.Year,
                    PeriodMonth = now.Month,
                    CreatedAt = now
                });

                // Offset any already-accrued CLUB_BROUGHT_SHARE for this booking (prevent double-payment)
                var originalShare = await _context.CoachDues
                    .Where(cd => cd.SourceId == bookingId && cd.SourceType == "CLUB_BROUGHT_SHARE" && cd.Amount > 0)
                    .FirstOrDefaultAsync();
                if (originalShare != null)
                {
                    _context.CoachDues.Add(new CoachDue
                    {
                        EmployeeId = booking.CoachId!.Value,
                        SourceType = "CLUB_BROUGHT_SHARE",
                        SourceId = bookingId,
                        Amount = -originalShare.Amount, // Negative offset
                        PeriodYear = now.Year,
                        PeriodMonth = now.Month,
                        CreatedAt = now
                    });
                }

                await _auditLog.LogSuccessAsync("COACH_DUE_ACCRUED", "PrivateBooking", bookingId,
                    $"CANCELLATION_FEE: Coach={booking.CoachId}, Amount={coachReceives}");
            }
        }

        if (refundAmount > 0)
        {
            var refund = new Refund
            {
                RelatedEntityType = "PRIVATE_BOOKING",
                RelatedEntityId = bookingId,
                Amount = refundAmount,
                Reason = $"Private Booking Cancellation ({booking.BusinessType})",
                RecordedBy = actorUserId,
                CreatedAt = now
            };
            _context.Refunds.Add(refund);
            await _context.SaveChangesAsync();

            // Confirm refund (creates the REFUND Transaction)
            var refundTx = new Transaction
            {
                TransactionType = "REFUND",
                Amount = -Math.Abs(refundAmount),
                RelatedEntityType = "REFUND",
                RelatedEntityId = refund.RefundId,
                RecordedBy = actorUserId,
                CreatedAt = now
            };
            _context.Transactions.Add(refundTx);
            refund.LinkedTransaction = refundTx;
        }

        await _context.SaveChangesAsync();
        await _auditLog.LogSuccessAsync("PRIVATE_BOOKING_CANCELLED", "PrivateBooking", bookingId,
            $"Type={booking.BusinessType}, CompletedSessions={completedSessions}, Refund={refundAmount}");
        await tx.CommitAsync();

        return PrivateBookingResult.Success;
    }

    // ─── ATTENDANCE ───────────────────────────────────────────────────────────

    public async Task<PrivateBookingResult> RecordAttendanceAsync(
        int sessionId, string participantId, bool attended, int actorUserId)
    {
        var session = await _context.PrivateSessions
            .Include(ps => ps.PrivateBooking)
            .FirstOrDefaultAsync(ps => ps.PrivateSessionId == sessionId);

        if (session == null) return PrivateBookingResult.SessionNotFound;
        if (session.PrivateBooking.Status != "ACTIVE") return PrivateBookingResult.InvalidStateTransition;

        // participantId is the ParticipantId (int) — passed as string for interface uniformity
        if (!int.TryParse(participantId, out int participantIntId))
            return PrivateBookingResult.InvalidParticipant;

        var participant = await _context.PrivateBookingParticipants
            .FirstOrDefaultAsync(p => p.ParticipantId == participantIntId
                                    && p.PrivateBookingId == session.PrivateBookingId);
        if (participant == null) return PrivateBookingResult.InvalidParticipant;

        // Prevent duplicate attendance for same session + participant
        var existing = await _context.PrivateSessionAttendances
            .AnyAsync(a => a.PrivateSessionId == sessionId && a.ParticipantId == participantIntId);
        if (existing) return PrivateBookingResult.AlreadyAttended;

        var now = DateTime.UtcNow;

        _context.PrivateSessionAttendances.Add(new PrivateSessionAttendance
        {
            PrivateSessionId = sessionId,
            ParticipantId = participantIntId,
            Status = attended ? "PRESENT" : "ABSENT",
            RecordedBy = actorUserId,
            RecordedAt = now
        });

        // Mark session as COMPLETED once any attendance is recorded
        if (session.Status == "SCHEDULED")
            session.Status = "COMPLETED";

        await _context.SaveChangesAsync();

        return PrivateBookingResult.Success;
    }
}
