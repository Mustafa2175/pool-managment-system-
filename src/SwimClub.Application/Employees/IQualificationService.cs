using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Employees;

public enum QualificationResult
{
    Success,
    NotFound,
    DuplicateRankOrder,
    NotACoachOrLifeguard
}

/// <summary>
/// Manages the Qualification catalogue, Employee assignments, and Rate resolution.
/// Enforces Decision 6 (qualifications resolve to rates dynamically by MAX(rank_order)).
/// </summary>
public interface IQualificationService
{
    /// <summary>
    /// Creates a new Qualification tier in the catalogue.
    /// </summary>
    Task<(QualificationResult Result, Qualification? Qualification)> CreateQualificationAsync(
        string nameEn,
        string nameAr,
        int rankOrder);

    /// <summary>
    /// Updates the rank order of an existing Qualification. Must not duplicate another rank.
    /// </summary>
    Task<QualificationResult> UpdateRankOrderAsync(
        int qualificationId,
        int newRankOrder);

    /// <summary>
    /// Sets a new rate config for a qualification.
    /// Automatically closes the currently active rate config (effectiveTo = effectiveFrom - 1 day).
    /// </summary>
    Task<QualificationResult> SetRateConfigAsync(
        int qualificationId,
        decimal sessionRate,
        DateOnly effectiveFrom);

    /// <summary>
    /// Assigns a Qualification to an Employee.
    /// Rejects if the Employee is not a COACH or LIFEGUARD.
    /// </summary>
    Task<QualificationResult> AssignQualificationToEmployeeAsync(
        int employeeId,
        int qualificationId,
        DateOnly obtainedAt);

    Task<QualificationResult> RemoveQualificationFromEmployeeAsync(
        int employeeId,
        int qualificationId);

    /// <summary>
    /// Resolves the current session rate for an employee on a given date.
    /// Formula (Decision 6): Finds the highest rank_order among their qualifications,
    /// then finds the rate config active on asOfDate for that qualification.
    /// Returns null if they hold no qualifications or no rate config is active.
    /// </summary>
    Task<decimal?> ResolveCurrentRateAsync(int employeeId, DateOnly asOfDate);

    Task<IReadOnlyList<Qualification>> GetAllQualificationsAsync();
    
    Task<IReadOnlyList<EmployeeQualification>> GetEmployeeQualificationsAsync(int employeeId);
}
