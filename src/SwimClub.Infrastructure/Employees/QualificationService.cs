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

public class QualificationService : IQualificationService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public QualificationService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<(QualificationResult Result, Qualification? Qualification)> CreateQualificationAsync(
        string nameEn,
        string nameAr,
        int rankOrder)
    {
        if (await _context.Qualifications.AnyAsync(q => q.RankOrder == rankOrder))
            return (QualificationResult.DuplicateRankOrder, null);

        var qualification = new Qualification
        {
            NameEn = nameEn,
            NameAr = nameAr,
            RankOrder = rankOrder
        };

        _context.Qualifications.Add(qualification);
        await _context.SaveChangesAsync();

        await _auditLog.LogSuccessAsync("CREATE_QUALIFICATION", "Qualification", qualification.QualificationId);

        return (QualificationResult.Success, qualification);
    }

    public async Task<QualificationResult> UpdateRankOrderAsync(
        int qualificationId,
        int newRankOrder)
    {
        var qualification = await _context.Qualifications.FindAsync(qualificationId);
        if (qualification == null) return QualificationResult.NotFound;

        if (qualification.RankOrder == newRankOrder) return QualificationResult.Success;

        if (await _context.Qualifications.AnyAsync(q => q.RankOrder == newRankOrder))
            return QualificationResult.DuplicateRankOrder;

        qualification.RankOrder = newRankOrder;
        await _context.SaveChangesAsync();
        
        await _auditLog.LogSuccessAsync("UPDATE_QUALIFICATION_RANK", "Qualification", qualificationId);

        return QualificationResult.Success;
    }

    public async Task<QualificationResult> SetRateConfigAsync(
        int qualificationId,
        decimal sessionRate,
        DateOnly effectiveFrom)
    {
        if (!await _context.Qualifications.AnyAsync(q => q.QualificationId == qualificationId))
            return QualificationResult.NotFound;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Close currently active config if any
            var currentConfig = await _context.QualificationRateConfigs
                .Where(c => c.QualificationId == qualificationId && c.EffectiveTo == null)
                .FirstOrDefaultAsync();

            if (currentConfig != null)
            {
                currentConfig.EffectiveTo = effectiveFrom.AddDays(-1);
            }

            var newConfig = new QualificationRateConfig
            {
                QualificationId = qualificationId,
                SessionRate = sessionRate,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = null
            };

            _context.QualificationRateConfigs.Add(newConfig);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            await _auditLog.LogSuccessAsync("SET_QUALIFICATION_RATE", "Qualification", qualificationId);

            return QualificationResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<QualificationResult> AssignQualificationToEmployeeAsync(
        int employeeId,
        int qualificationId,
        DateOnly obtainedAt)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return QualificationResult.NotFound;
        if (employee.EmployeeType != "COACH" && employee.EmployeeType != "LIFEGUARD")
            return QualificationResult.NotACoachOrLifeguard;

        if (!await _context.Qualifications.AnyAsync(q => q.QualificationId == qualificationId))
            return QualificationResult.NotFound;

        if (await _context.EmployeeQualifications.AnyAsync(eq => eq.EmployeeId == employeeId && eq.QualificationId == qualificationId))
            return QualificationResult.Success; // Already assigned

        var eq = new EmployeeQualification
        {
            EmployeeId = employeeId,
            QualificationId = qualificationId,
            ObtainedAt = obtainedAt
        };

        _context.EmployeeQualifications.Add(eq);
        await _context.SaveChangesAsync();
        
        await _auditLog.LogSuccessAsync("ASSIGN_QUALIFICATION", "Employee", employeeId, $"Qualification {qualificationId}");

        return QualificationResult.Success;
    }

    public async Task<QualificationResult> RemoveQualificationFromEmployeeAsync(
        int employeeId,
        int qualificationId)
    {
        var eq = await _context.EmployeeQualifications
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.QualificationId == qualificationId);
            
        if (eq == null) return QualificationResult.NotFound;
        
        _context.EmployeeQualifications.Remove(eq);
        await _context.SaveChangesAsync();
        
        await _auditLog.LogSuccessAsync("REMOVE_QUALIFICATION", "Employee", employeeId, $"Qualification {qualificationId}");
        
        return QualificationResult.Success;
    }

    public async Task<decimal?> ResolveCurrentRateAsync(int employeeId, DateOnly asOfDate)
    {
        // Decision 6: Resolve rate via MAX(rank_order) -> active rate config
        var highestRankQualification = await _context.EmployeeQualifications
            .Where(eq => eq.EmployeeId == employeeId)
            .Include(eq => eq.Qualification)
            .OrderByDescending(eq => eq.Qualification.RankOrder)
            .Select(eq => eq.Qualification)
            .FirstOrDefaultAsync();

        if (highestRankQualification == null)
            return null; // No qualifications

        var rateConfig = await _context.QualificationRateConfigs
            .Where(rc => rc.QualificationId == highestRankQualification.QualificationId &&
                         rc.EffectiveFrom <= asOfDate &&
                         (rc.EffectiveTo == null || rc.EffectiveTo > asOfDate))
            .FirstOrDefaultAsync();

        return rateConfig?.SessionRate;
    }

    public async Task<IReadOnlyList<Qualification>> GetAllQualificationsAsync()
    {
        return await _context.Qualifications
            .OrderByDescending(q => q.RankOrder)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<EmployeeQualification>> GetEmployeeQualificationsAsync(int employeeId)
    {
        return await _context.EmployeeQualifications
            .Include(eq => eq.Qualification)
            .Where(eq => eq.EmployeeId == employeeId)
            .OrderByDescending(eq => eq.Qualification.RankOrder)
            .ToListAsync();
    }
}
