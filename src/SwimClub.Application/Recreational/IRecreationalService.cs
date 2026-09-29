using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Recreational;

public enum RecreationalResult
{
    Success,
    PeriodNotFound,
    PeriodInactive,
    CapacityFull,
    OutsideCheckInWindow,
    DuplicateCheckIn,
    InvalidPayment,
    InvalidPackage,
    PackageExpired,
    PackageNoSessions,
    InvalidQrToken,
    WrongPeriod
}

public interface IRecreationalService
{
    /// <summary>
    /// Creates a new Recreational Period. 
    /// Super Admin only (enforced at controller).
    /// </summary>
    Task<int> CreatePeriodAsync(string name, TimeOnly startTime, TimeOnly endTime, int capacity, int actorUserId);

    /// <summary>
    /// Edits an existing Recreational Period.
    /// </summary>
    Task<RecreationalResult> EditPeriodAsync(int periodId, string name, TimeOnly startTime, TimeOnly endTime, int capacity, string status, int actorUserId);

    /// <summary>
    /// Checks in a single entry (ticket) into a Recreational Period.
    /// Verifies capacity, time window, duplicate check-in, and full payment.
    /// Creates a RecreationalTicket and related RECREATIONAL_TICKET_PAYMENT Transaction.
    /// </summary>
    Task<(RecreationalResult Result, int? TicketId)> RecordSingleEntryAsync(
        int periodId,
        string name,
        string memberStatus,
        decimal amountPaid,
        string? paymentDescription,
        int actorUserId);

    /// <summary>
    /// Checks in a Package holder via QR token into a Recreational Period.
    /// Verifies package validity, correct period, time window, sessions remaining, and capacity.
    /// Decrements sessions remaining and creates a PackageCheckIn.
    /// </summary>
    Task<RecreationalResult> RecordPackageCheckInAsync(
        int periodId,
        string qrToken,
        int actorUserId);
}
