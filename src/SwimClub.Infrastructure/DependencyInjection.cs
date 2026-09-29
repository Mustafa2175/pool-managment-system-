using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwimClub.Infrastructure.Persistence;

namespace SwimClub.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the AppDbContext with SQLite, WAL mode, FK enforcement, and busy timeout.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(
                $"Data Source={databasePath}",
                sqliteOptions =>
                {
                    // Command timeout for busy/retry scenarios
                    sqliteOptions.CommandTimeout(30);
                });
        });

        services.AddScoped<DatabaseInitializer>();

        // Phase 2: Core Infrastructure Security & Config Services
        services.AddSingleton<SwimClub.Application.Security.ICurrentUserService, SwimClub.Infrastructure.Security.CurrentUserService>();
        services.AddScoped<SwimClub.Application.Interfaces.IAuditLogService, SwimClub.Infrastructure.Logging.AuditLogService>();
        services.AddScoped<SwimClub.Application.Security.IAuthService, SwimClub.Infrastructure.Security.AuthService>();
        services.AddScoped<SwimClub.Application.Security.IAuthorizationGuard, SwimClub.Infrastructure.Security.AuthorizationGuard>();
        services.AddScoped<SwimClub.Application.Interfaces.ISystemConfigurationService, SwimClub.Infrastructure.Configuration.SystemConfigurationService>();

        // Phase 3: Financial Foundation
        services.AddScoped<SwimClub.Application.Interfaces.IUnitOfWork, SwimClub.Infrastructure.Persistence.UnitOfWork>();
        services.AddScoped<SwimClub.Application.Finance.ITransactionService, SwimClub.Infrastructure.Finance.TransactionService>();
        services.AddScoped<SwimClub.Application.Finance.IPaymentService, SwimClub.Infrastructure.Finance.PaymentService>();
        services.AddScoped<SwimClub.Application.Finance.IRefundService, SwimClub.Infrastructure.Finance.RefundService>();
        services.AddScoped<SwimClub.Application.Finance.IExpenseService, SwimClub.Infrastructure.Finance.ExpenseService>();
        services.AddScoped<SwimClub.Application.Finance.ICreditService, SwimClub.Infrastructure.Finance.CreditService>();

        // Phase 4: Employees + Qualifications + Attendance
        services.AddScoped<SwimClub.Application.Employees.IEmployeeService, SwimClub.Infrastructure.Employees.EmployeeService>();
        services.AddScoped<SwimClub.Application.Employees.IUserManagementService, SwimClub.Infrastructure.Employees.UserManagementService>();
        services.AddScoped<SwimClub.Application.Employees.IQualificationService, SwimClub.Infrastructure.Employees.QualificationService>();
        services.AddScoped<SwimClub.Application.Employees.IAttendanceService, SwimClub.Infrastructure.Employees.AttendanceService>();

        // Phase 5: Swimmers
        services.AddScoped<SwimClub.Application.Swimmers.ISwimmerService, SwimClub.Infrastructure.Swimmers.SwimmerService>();

        // Phase 6: Training Core
        services.AddScoped<SwimClub.Application.Training.ITrainingConfigService, SwimClub.Infrastructure.Training.TrainingConfigService>();
        services.AddScoped<SwimClub.Application.Training.ITrainingPeriodService, SwimClub.Infrastructure.Training.TrainingPeriodService>();
        services.AddScoped<SwimClub.Application.Training.ITrainingSubscriptionService, SwimClub.Infrastructure.Training.TrainingSubscriptionService>();

        // Phase 7: Training Operations
        services.AddScoped<SwimClub.Application.Training.ITrainingAttendanceService, SwimClub.Infrastructure.Training.TrainingAttendanceService>();
        services.AddScoped<SwimClub.Application.Training.ITrainingLifecycleService, SwimClub.Infrastructure.Training.TrainingLifecycleService>();

        // Packages (Phase 8)
        services.AddScoped<SwimClub.Application.Packages.IPackageConfigService, SwimClub.Infrastructure.Packages.PackageConfigService>();
        services.AddScoped<SwimClub.Application.Packages.IPackageLifecycleService, SwimClub.Infrastructure.Packages.PackageLifecycleService>();
        services.AddScoped<SwimClub.Application.Packages.IPackageAttendanceService, SwimClub.Infrastructure.Packages.PackageAttendanceService>();

        // Private Bookings (Phase 9)
        services.AddScoped<SwimClub.Application.Private.IPrivateBookingService, SwimClub.Infrastructure.Private.PrivateBookingService>();

        // Recreational (Phase 10)
        services.AddScoped<SwimClub.Application.Recreational.IRecreationalService, SwimClub.Infrastructure.Recreational.RecreationalService>();

        // Payroll (Phase 11)
        services.AddScoped<SwimClub.Application.Payroll.IPayrollService, SwimClub.Infrastructure.Payroll.PayrollService>();

        // Reports (Phase 12)
        services.AddScoped<SwimClub.Application.Reports.IReportsService, SwimClub.Infrastructure.Reports.ReportsService>();

        // Backup (Phase 13)
        services.AddScoped<SwimClub.Application.Backup.IBackupService, SwimClub.Infrastructure.Backup.BackupService>();

        return services;
    }
}
