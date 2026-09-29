using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Training;

public enum TrainingPeriodResult
{
    Success,
    NotFound,
    InvalidSchedule,    // e.g. EndTime <= StartTime
    InvalidCapacity,    // <= 0
    StaffOverlap,       // A coach/lifeguard is double-booked
    Unauthorized        // e.g. Owner tries to edit
}

public interface ITrainingPeriodService
{
    /// <summary>
    /// Creates a new Training Period (Super Admin only).
    /// </summary>
    Task<(TrainingPeriodResult Result, int? PeriodId)> CreatePeriodAsync(
        int programId, 
        TimeOnly startTime, 
        TimeOnly endTime, 
        int capacity,
        IEnumerable<int> daysOfWeek,
        int actorUserId);

    /// <summary>
    /// Edits an existing Training Period (Super Admin or Administrator).
    /// Updates days, times, capacity, and staff assignments.
    /// Modifies future un-attended sessions and leaves past sessions untouched (Decision 14).
    /// </summary>
    Task<TrainingPeriodResult> EditPeriodAsync(
        int periodId,
        TimeOnly startTime, 
        TimeOnly endTime, 
        int capacity,
        IEnumerable<int> daysOfWeek,
        IEnumerable<int> coachEmployeeIds,
        IEnumerable<int> lifeguardEmployeeIds,
        int actorUserId);

    /// <summary>
    /// Activates or Deactivates a Period.
    /// </summary>
    Task<TrainingPeriodResult> SetPeriodStatusAsync(int periodId, string status, int actorUserId);

    Task<TrainingPeriod?> GetPeriodAsync(int periodId);
    Task<IReadOnlyList<TrainingPeriod>> ListPeriodsAsync(int? programId = null, string? status = null);
}
