using Microsoft.EntityFrameworkCore;
using SwimClub.Domain.Entities;
using PayrollEntity = SwimClub.Domain.Entities.Payroll;
using BackupEntity = SwimClub.Domain.Entities.Backup;

namespace SwimClub.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext for the SwimClub application.
/// Configures all entities per the finalized ERD (01-ERD.md) and Database Schema (02-Database-Schema.md).
/// SQLite with WAL mode, FK enforcement, and generated balance_due columns.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Identity & Access
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Qualification> Qualifications => Set<Qualification>();
    public DbSet<EmployeeQualification> EmployeeQualifications => Set<EmployeeQualification>();
    public DbSet<QualificationRateConfig> QualificationRateConfigs => Set<QualificationRateConfig>();

    // Swimmers
    public DbSet<Swimmer> Swimmers => Set<Swimmer>();

    // Training
    public DbSet<Domain.Entities.Program> Programs => Set<Domain.Entities.Program>();
    public DbSet<TrainingPeriod> TrainingPeriods => Set<TrainingPeriod>();
    public DbSet<TrainingPeriodSchedule> TrainingPeriodSchedules => Set<TrainingPeriodSchedule>();
    public DbSet<PeriodStaffAssignment> PeriodStaffAssignments => Set<PeriodStaffAssignment>();
    public DbSet<TrainingPriceConfig> TrainingPriceConfigs => Set<TrainingPriceConfig>();
    public DbSet<TrainingSubscription> TrainingSubscriptions => Set<TrainingSubscription>();
    public DbSet<TrainingSubscriptionPause> TrainingSubscriptionPauses => Set<TrainingSubscriptionPause>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<EmployeeAttendance> EmployeeAttendances => Set<EmployeeAttendance>();
    public DbSet<EmployeeReplacement> EmployeeReplacements => Set<EmployeeReplacement>();
    public DbSet<AdministratorDailyAttendance> AdministratorDailyAttendances => Set<AdministratorDailyAttendance>();

    // Packages
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageConfig> PackageConfigs => Set<PackageConfig>();
    public DbSet<PackageQrHistory> PackageQrHistories => Set<PackageQrHistory>();
    public DbSet<PackageCheckIn> PackageCheckIns => Set<PackageCheckIn>();
    public DbSet<PackagePeriodChange> PackagePeriodChanges => Set<PackagePeriodChange>();

    // Recreational
    public DbSet<RecreationalPeriod> RecreationalPeriods => Set<RecreationalPeriod>();
    public DbSet<RecreationalPeriodSchedule> RecreationalPeriodSchedules => Set<RecreationalPeriodSchedule>();
    public DbSet<RecreationalTicket> RecreationalTickets => Set<RecreationalTicket>();

    // Private
    public DbSet<Lane> Lanes => Set<Lane>();
    public DbSet<PrivateBooking> PrivateBookings => Set<PrivateBooking>();
    public DbSet<PrivateBookingParticipant> PrivateBookingParticipants => Set<PrivateBookingParticipant>();
    public DbSet<PrivateSession> PrivateSessions => Set<PrivateSession>();
    public DbSet<PrivateSessionAttendance> PrivateSessionAttendances => Set<PrivateSessionAttendance>();
    public DbSet<CoachDue> CoachDues => Set<CoachDue>();

    // Finance
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<CreditUsage> CreditUsages => Set<CreditUsage>();
    public DbSet<PayrollEntity> Payrolls => Set<PayrollEntity>();

    // Config & Admin
    public DbSet<CancellationFeeConfig> CancellationFeeConfigs => Set<CancellationFeeConfig>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<ScheduleDefaultsConfig> ScheduleDefaultsConfigs => Set<ScheduleDefaultsConfig>();
    public DbSet<BackupEntity> Backups => Set<BackupEntity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ApplyAllConfigurations(modelBuilder);
        SeedRoles(modelBuilder);
    }

    private static void ApplyAllConfigurations(ModelBuilder b)
    {
        // --- ROLE ---
        b.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.Code).HasColumnName("code").IsRequired();
            e.Property(x => x.NameEn).HasColumnName("name_en").IsRequired();
            e.Property(x => x.NameAr).HasColumnName("name_ar").IsRequired();
        });

        // --- USER ---
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Username).HasColumnName("username").IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.DeactivatedAt).HasColumnName("deactivated_at");

            e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Employee).WithOne(emp => emp.User).HasForeignKey<User>(x => x.EmployeeId);
            e.HasOne(x => x.CreatedBy).WithMany(u => u.CreatedUsers).HasForeignKey(x => x.CreatedByUserId);
        });

        // --- EMPLOYEE ---
        b.Entity<Employee>(e =>
        {
            e.ToTable("employees");
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.EmployeeType).HasColumnName("employee_type").IsRequired();
            e.HasCheckConstraint("ck_employee_type", "employee_type IN ('ADMINISTRATOR','COACH','LIFEGUARD')");
            e.Property(x => x.NationalId).HasColumnName("national_id").IsRequired();
            e.HasIndex(x => x.NationalId).IsUnique();
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.Nickname).HasColumnName("nickname");
            e.Property(x => x.MonthlySalary).HasColumnName("monthly_salary").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("ACTIVE");
            e.HasCheckConstraint("ck_employee_status", "status IN ('ACTIVE','INACTIVE')");
            e.HasCheckConstraint("ck_admin_salary", "employee_type <> 'ADMINISTRATOR' OR monthly_salary IS NOT NULL");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.DeactivatedAt).HasColumnName("deactivated_at");
        });

        // --- QUALIFICATION ---
        b.Entity<Qualification>(e =>
        {
            e.ToTable("qualifications");
            e.HasKey(x => x.QualificationId);
            e.Property(x => x.QualificationId).HasColumnName("qualification_id");
            e.Property(x => x.NameEn).HasColumnName("name_en").IsRequired();
            e.Property(x => x.NameAr).HasColumnName("name_ar").IsRequired();
            e.Property(x => x.RankOrder).HasColumnName("rank_order");
            e.HasIndex(x => x.RankOrder).IsUnique();
        });

        // --- EMPLOYEE QUALIFICATION ---
        b.Entity<EmployeeQualification>(e =>
        {
            e.ToTable("employee_qualifications");
            e.HasKey(x => new { x.EmployeeId, x.QualificationId });
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.QualificationId).HasColumnName("qualification_id");
            e.Property(x => x.ObtainedAt).HasColumnName("obtained_at");
            e.HasOne(x => x.Employee).WithMany(emp => emp.EmployeeQualifications).HasForeignKey(x => x.EmployeeId);
            e.HasOne(x => x.Qualification).WithMany(q => q.EmployeeQualifications).HasForeignKey(x => x.QualificationId);
        });

        // --- QUALIFICATION RATE CONFIG ---
        b.Entity<QualificationRateConfig>(e =>
        {
            e.ToTable("qualification_rate_configs");
            e.HasKey(x => x.RateConfigId);
            e.Property(x => x.RateConfigId).HasColumnName("rate_config_id");
            e.Property(x => x.QualificationId).HasColumnName("qualification_id");
            e.Property(x => x.SessionRate).HasColumnName("session_rate").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            e.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            e.HasOne(x => x.Qualification).WithMany(q => q.QualificationRateConfigs).HasForeignKey(x => x.QualificationId);
        });

        // --- SWIMMER ---
        b.Entity<Swimmer>(e =>
        {
            e.ToTable("swimmers");
            e.HasKey(x => x.SwimmerId);
            e.Property(x => x.SwimmerId).HasColumnName("swimmer_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
            e.Property(x => x.Gender).HasColumnName("gender").IsRequired();
            e.HasCheckConstraint("ck_swimmer_gender", "gender IN ('MALE','FEMALE')");
            e.Property(x => x.ParentName).HasColumnName("parent_name");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.MemberStatus).HasColumnName("member_status").IsRequired();
            e.HasCheckConstraint("ck_swimmer_member_status", "member_status IN ('MEMBER','NON_MEMBER')");
            e.Property(x => x.QrToken).HasColumnName("qr_token").IsRequired();
            e.HasIndex(x => x.QrToken).IsUnique();
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("INACTIVE");
            e.HasCheckConstraint("ck_swimmer_status", "status IN ('ACTIVE','INACTIVE')");
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // --- PROGRAM ---
        b.Entity<Domain.Entities.Program>(e =>
        {
            e.ToTable("programs");
            e.HasKey(x => x.ProgramId);
            e.Property(x => x.ProgramId).HasColumnName("program_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.ProgramType).HasColumnName("program_type").IsRequired();
            e.HasCheckConstraint("ck_program_type", "program_type IN ('REGULAR','STAR','TEAM')");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // --- TRAINING PERIOD ---
        b.Entity<TrainingPeriod>(e =>
        {
            e.ToTable("training_periods");
            e.HasKey(x => x.PeriodId);
            e.Property(x => x.PeriodId).HasColumnName("period_id");
            e.Property(x => x.ProgramId).HasColumnName("program_id");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.Property(x => x.Capacity).HasColumnName("capacity");
            e.HasCheckConstraint("ck_period_capacity", "capacity > 0");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("ACTIVE");
            e.HasCheckConstraint("ck_period_status", "status IN ('ACTIVE','INACTIVE')");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Program).WithMany(p => p.TrainingPeriods).HasForeignKey(x => x.ProgramId);
        });

        // --- TRAINING PERIOD SCHEDULE ---
        b.Entity<TrainingPeriodSchedule>(e =>
        {
            e.ToTable("training_period_schedules");
            e.HasKey(x => x.ScheduleId);
            e.Property(x => x.ScheduleId).HasColumnName("schedule_id");
            e.Property(x => x.PeriodId).HasColumnName("period_id");
            e.Property(x => x.DayOfWeek).HasColumnName("day_of_week");
            e.HasCheckConstraint("ck_schedule_day", "day_of_week BETWEEN 0 AND 6");
            e.HasIndex(x => new { x.PeriodId, x.DayOfWeek }).IsUnique();
            e.HasOne(x => x.Period).WithMany(p => p.Schedules).HasForeignKey(x => x.PeriodId);
        });

        // --- PERIOD STAFF ASSIGNMENT ---
        b.Entity<PeriodStaffAssignment>(e =>
        {
            e.ToTable("period_staff_assignments");
            e.HasKey(x => x.AssignmentId);
            e.Property(x => x.AssignmentId).HasColumnName("assignment_id");
            e.Property(x => x.PeriodId).HasColumnName("period_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.Role).HasColumnName("role").IsRequired();
            e.HasCheckConstraint("ck_staff_role", "role IN ('COACH','LIFEGUARD')");
            e.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            e.HasOne(x => x.Period).WithMany(p => p.StaffAssignments).HasForeignKey(x => x.PeriodId);
            e.HasOne(x => x.Employee).WithMany(emp => emp.PeriodStaffAssignments).HasForeignKey(x => x.EmployeeId);
        });

        // --- TRAINING PRICE CONFIG ---
        b.Entity<TrainingPriceConfig>(e =>
        {
            e.ToTable("training_price_configs");
            e.HasKey(x => x.PriceConfigId);
            e.Property(x => x.PriceConfigId).HasColumnName("price_config_id");
            e.Property(x => x.ProgramId).HasColumnName("program_id");
            e.Property(x => x.MemberStatus).HasColumnName("member_status").IsRequired();
            e.HasCheckConstraint("ck_price_member_status", "member_status IN ('MEMBER','NON_MEMBER')");
            e.Property(x => x.Price).HasColumnName("price").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            e.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            e.HasOne(x => x.Program).WithMany(p => p.TrainingPriceConfigs).HasForeignKey(x => x.ProgramId);
        });

        // --- TRAINING SUBSCRIPTION ---
        b.Entity<TrainingSubscription>(e =>
        {
            e.ToTable("training_subscriptions");
            e.HasKey(x => x.SubscriptionId);
            e.Property(x => x.SubscriptionId).HasColumnName("subscription_id");
            e.Property(x => x.SwimmerId).HasColumnName("swimmer_id").IsRequired();
            e.Property(x => x.ProgramId).HasColumnName("program_id");
            e.Property(x => x.PeriodId).HasColumnName("period_id");
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            e.Property(x => x.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.ConfiguredSessionCountSnapshot).HasColumnName("configured_session_count_snapshot");
            e.Property(x => x.TotalPrice).HasColumnName("total_price").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.PaidAmount).HasColumnName("paid_amount").HasColumnType("DECIMAL(12,2)").HasDefaultValue(0m);
            // balance_due — generated column in SQLite
            e.Property(x => x.BalanceDue)
                .HasColumnName("balance_due")
                .HasColumnType("DECIMAL(12,2)")
                .HasComputedColumnSql("total_price - paid_amount", stored: true);
            e.Property(x => x.CreditGrantedAmount).HasColumnName("credit_granted_amount").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.CreditDescription).HasColumnName("credit_description");
            e.Property(x => x.OutstandingDeclaredAmount).HasColumnName("outstanding_declared_amount").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.OutstandingDescription).HasColumnName("outstanding_description");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("ACTIVE");
            e.HasCheckConstraint("ck_sub_status", "status IN ('ACTIVE','PAUSED','COMPLETED','CANCELLED')");
            e.HasCheckConstraint("ck_sub_credit_outstanding_exclusive",
                "credit_granted_amount IS NULL OR outstanding_declared_amount IS NULL");
            e.Property(x => x.RenewedFromSubscriptionId).HasColumnName("renewed_from_subscription_id");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.Swimmer).WithMany(s => s.TrainingSubscriptions).HasForeignKey(x => x.SwimmerId);
            e.HasOne(x => x.Program).WithMany().HasForeignKey(x => x.ProgramId);
            e.HasOne(x => x.Period).WithMany(p => p.TrainingSubscriptions).HasForeignKey(x => x.PeriodId);
            e.HasOne(x => x.RenewedFromSubscription).WithMany().HasForeignKey(x => x.RenewedFromSubscriptionId);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy);
        });

        // CREDIT RELATIONSHIP TO TrainingSubscription is configured on the Credit entity itself.

        // --- TRAINING SUBSCRIPTION PAUSE ---
        b.Entity<TrainingSubscriptionPause>(e =>
        {
            e.ToTable("training_subscription_pauses");
            e.HasKey(x => x.PauseId);
            e.Property(x => x.PauseId).HasColumnName("pause_id");
            e.Property(x => x.SubscriptionId).HasColumnName("subscription_id");
            e.Property(x => x.PauseDate).HasColumnName("pause_date");
            e.Property(x => x.ResumeDate).HasColumnName("resume_date");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Subscription).WithMany(s => s.Pauses).HasForeignKey(x => x.SubscriptionId);
        });

        // --- SESSION ---
        b.Entity<Session>(e =>
        {
            e.ToTable("sessions");
            e.HasKey(x => x.SessionId);
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.PeriodId).HasColumnName("period_id");
            e.Property(x => x.SubscriptionId).HasColumnName("subscription_id");
            e.Property(x => x.ScheduledStartTime).HasColumnName("scheduled_start_time");
            e.Property(x => x.ScheduledEndTime).HasColumnName("scheduled_end_time");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("SCHEDULED");
            e.HasCheckConstraint("ck_session_status", "status IN ('SCHEDULED','PAUSED','COMPLETED','CANCELLED')");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Period).WithMany(p => p.Sessions).HasForeignKey(x => x.PeriodId);
            e.HasOne(x => x.Subscription).WithMany(s => s.Sessions).HasForeignKey(x => x.SubscriptionId);
            e.HasOne(x => x.Attendance).WithOne(a => a.Session).HasForeignKey<Attendance>(a => a.SessionId);
        });

        // --- ATTENDANCE ---
        b.Entity<Attendance>(e =>
        {
            e.ToTable("attendances");
            e.HasKey(x => x.AttendanceId);
            e.Property(x => x.AttendanceId).HasColumnName("attendance_id");
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.SwimmerId).HasColumnName("swimmer_id").IsRequired();
            e.Property(x => x.CheckedInAt).HasColumnName("checked_in_at");
            e.Property(x => x.AttendanceMethod).HasColumnName("attendance_method").IsRequired();
            e.HasCheckConstraint("ck_attendance_method", "attendance_method IN ('QR','MANUAL')");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Swimmer).WithMany().HasForeignKey(x => x.SwimmerId);
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
        });

        // --- EMPLOYEE ATTENDANCE ---
        b.Entity<EmployeeAttendance>(e =>
        {
            e.ToTable("employee_attendances");
            e.HasKey(x => x.AttendanceId);
            e.Property(x => x.AttendanceId).HasColumnName("attendance_id");
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.HasCheckConstraint("ck_emp_att_status", "status IN ('PRESENT','ABSENT')");
            e.Property(x => x.IsLateEdit).HasColumnName("is_late_edit").HasDefaultValue(false);
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.LastModifiedAt).HasColumnName("last_modified_at");
            e.Property(x => x.LastModifiedBy).HasColumnName("last_modified_by");
            e.HasIndex(x => new { x.SessionId, x.EmployeeId }).IsUnique();
            e.HasOne(x => x.Session).WithMany(s => s.EmployeeAttendances).HasForeignKey(x => x.SessionId);
            e.HasOne(x => x.Employee).WithMany(emp => emp.EmployeeAttendances).HasForeignKey(x => x.EmployeeId);
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
        });

        // --- EMPLOYEE REPLACEMENT ---
        b.Entity<EmployeeReplacement>(e =>
        {
            e.ToTable("employee_replacements");
            e.HasKey(x => x.ReplacementId);
            e.Property(x => x.ReplacementId).HasColumnName("replacement_id");
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.OriginalEmployeeId).HasColumnName("original_employee_id");
            e.Property(x => x.ReplacingEmployeeId).HasColumnName("replacing_employee_id");
            e.Property(x => x.RateAppliedSnapshot).HasColumnName("rate_applied_snapshot").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Session).WithMany(s => s.EmployeeReplacements).HasForeignKey(x => x.SessionId);
            e.HasOne(x => x.OriginalEmployee).WithMany(emp => emp.ReplacedBy)
                .HasForeignKey(x => x.OriginalEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReplacingEmployee).WithMany(emp => emp.ReplacingAs)
                .HasForeignKey(x => x.ReplacingEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
        });

        // --- ADMINISTRATOR DAILY ATTENDANCE ---
        b.Entity<AdministratorDailyAttendance>(e =>
        {
            e.ToTable("administrator_daily_attendances");
            e.HasKey(x => x.AttendanceId);
            e.Property(x => x.AttendanceId).HasColumnName("attendance_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.AttendanceDate).HasColumnName("attendance_date");
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.HasCheckConstraint("ck_admin_att_status", "status IN ('PRESENT','ABSENT')");
            e.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique();
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Employee).WithMany(emp => emp.AdministratorDailyAttendances).HasForeignKey(x => x.EmployeeId);
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
        });

        // --- PACKAGE ---
        b.Entity<Package>(e =>
        {
            e.ToTable("packages");
            e.HasKey(x => x.PackageId);
            e.Property(x => x.PackageId).HasColumnName("package_id");
            e.Property(x => x.SwimmerId).HasColumnName("swimmer_id").IsRequired();
            e.Property(x => x.PackageType).HasColumnName("package_type").IsRequired();
            e.HasCheckConstraint("ck_package_type", "package_type IN ('TRAINING','RECREATIONAL')");
            e.Property(x => x.ProgramId).HasColumnName("program_id");
            e.Property(x => x.TrainingPeriodId).HasColumnName("training_period_id");
            e.Property(x => x.RecreationalPeriodId).HasColumnName("recreational_period_id");
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            e.Property(x => x.DurationSnapshotDays).HasColumnName("duration_snapshot_days");
            e.Property(x => x.SessionsPerMonthSnapshot).HasColumnName("sessions_per_month_snapshot");
            e.Property(x => x.AvailableSessionsTotal).HasColumnName("available_sessions_total");
            e.Property(x => x.AvailableSessionsRemaining).HasColumnName("available_sessions_remaining");
            e.HasCheckConstraint("ck_pkg_sessions_remaining", "available_sessions_remaining >= 0");
            e.Property(x => x.ReferenceTrainingPriceSnapshot).HasColumnName("reference_training_price_snapshot").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.TotalPrice).HasColumnName("total_price").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.PaidAmount).HasColumnName("paid_amount").HasColumnType("DECIMAL(12,2)").HasDefaultValue(0m);
            e.Property(x => x.BalanceDue)
                .HasColumnName("balance_due")
                .HasColumnType("DECIMAL(12,2)")
                .HasComputedColumnSql("total_price - paid_amount", stored: true);
            e.Property(x => x.CreditGrantedAmount).HasColumnName("credit_granted_amount").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.CreditDescription).HasColumnName("credit_description");
            e.Property(x => x.OutstandingDeclaredAmount).HasColumnName("outstanding_declared_amount").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.OutstandingDescription).HasColumnName("outstanding_description");
            e.Property(x => x.QrToken).HasColumnName("qr_token");
            e.HasIndex(x => x.QrToken).IsUnique();
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("ACTIVE");
            e.HasCheckConstraint("ck_pkg_status", "status IN ('ACTIVE','EXPIRED','CANCELLED')");
            e.HasCheckConstraint("ck_pkg_credit_outstanding_exclusive",
                "credit_granted_amount IS NULL OR outstanding_declared_amount IS NULL");
            e.HasCheckConstraint("ck_pkg_period_consistency",
                "(package_type='TRAINING' AND training_period_id IS NOT NULL AND recreational_period_id IS NULL) OR " +
                "(package_type='RECREATIONAL' AND recreational_period_id IS NOT NULL AND training_period_id IS NULL)");
            e.Property(x => x.RenewedFromPackageId).HasColumnName("renewed_from_package_id");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.Swimmer).WithMany(s => s.Packages).HasForeignKey(x => x.SwimmerId);
            e.HasOne(x => x.Program).WithMany().HasForeignKey(x => x.ProgramId);
            e.HasOne(x => x.TrainingPeriod).WithMany().HasForeignKey(x => x.TrainingPeriodId);
            e.HasOne(x => x.RecreationalPeriod).WithMany(rp => rp.Packages).HasForeignKey(x => x.RecreationalPeriodId);
            e.HasOne(x => x.RenewedFromPackage).WithMany().HasForeignKey(x => x.RenewedFromPackageId);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy);
            // Note: GrantedCredit relationship is configured on the Credit entity side.
        });

        // --- PACKAGE CONFIG ---
        b.Entity<PackageConfig>(e =>
        {
            e.ToTable("package_configs");
            e.HasKey(x => x.PackageConfigId);
            e.Property(x => x.PackageConfigId).HasColumnName("package_config_id");
            e.Property(x => x.ProgramId).HasColumnName("program_id");
            e.Property(x => x.DurationMonths).HasColumnName("duration_months");
            e.Property(x => x.SessionsPerMonth).HasColumnName("sessions_per_month");
            e.Property(x => x.MemberStatus).HasColumnName("member_status").IsRequired();
            e.HasCheckConstraint("ck_pkg_cfg_member_status", "member_status IN ('MEMBER','NON_MEMBER')");
            e.Property(x => x.Price).HasColumnName("price").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            e.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            e.HasOne(x => x.Program).WithMany().HasForeignKey(x => x.ProgramId);
        });

        // --- PACKAGE QR HISTORY ---
        b.Entity<PackageQrHistory>(e =>
        {
            e.ToTable("package_qr_histories");
            e.HasKey(x => x.QrHistoryId);
            e.Property(x => x.QrHistoryId).HasColumnName("qr_history_id");
            e.Property(x => x.PackageId).HasColumnName("package_id");
            e.Property(x => x.QrToken).HasColumnName("qr_token").IsRequired();
            e.Property(x => x.IssuedAt).HasColumnName("issued_at");
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            e.HasOne(x => x.Package).WithMany(p => p.QrHistory).HasForeignKey(x => x.PackageId);
        });

        // --- PACKAGE CHECK-IN ---
        b.Entity<PackageCheckIn>(e =>
        {
            e.ToTable("package_checkins");
            e.HasKey(x => x.CheckInId);
            e.Property(x => x.CheckInId).HasColumnName("checkin_id");
            e.Property(x => x.PackageId).HasColumnName("package_id");
            e.Property(x => x.CheckedInAt).HasColumnName("checked_in_at");
            e.Property(x => x.AttendanceMethod).HasColumnName("attendance_method").IsRequired();
            e.HasCheckConstraint("ck_pkg_checkin_method", "attendance_method IN ('QR','MANUAL')");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Package).WithMany(p => p.CheckIns).HasForeignKey(x => x.PackageId);
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
        });

        // --- PACKAGE PERIOD CHANGE ---
        b.Entity<PackagePeriodChange>(e =>
        {
            e.ToTable("package_period_changes");
            e.HasKey(x => x.ChangeId);
            e.Property(x => x.ChangeId).HasColumnName("change_id");
            e.Property(x => x.PackageId).HasColumnName("package_id");
            e.Property(x => x.OldPeriodId).HasColumnName("old_period_id");
            e.Property(x => x.NewPeriodId).HasColumnName("new_period_id");
            e.Property(x => x.ChangeDate).HasColumnName("change_date");
            e.Property(x => x.SessionsCarriedOver).HasColumnName("sessions_carried_over");
            e.Property(x => x.ChangedBy).HasColumnName("changed_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Package).WithMany(p => p.PeriodChanges).HasForeignKey(x => x.PackageId);
            e.HasOne(x => x.OldPeriod).WithMany().HasForeignKey(x => x.OldPeriodId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NewPeriod).WithMany().HasForeignKey(x => x.NewPeriodId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedBy);
        });

        // --- RECREATIONAL PERIOD ---
        b.Entity<RecreationalPeriod>(e =>
        {
            e.ToTable("recreational_periods");
            e.HasKey(x => x.RecreationalPeriodId);
            e.Property(x => x.RecreationalPeriodId).HasColumnName("recreational_period_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.Property(x => x.Capacity).HasColumnName("capacity");
            e.HasCheckConstraint("ck_rec_period_capacity", "capacity > 0");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("ACTIVE");
            e.HasCheckConstraint("ck_rec_period_status", "status IN ('ACTIVE','INACTIVE')");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // --- RECREATIONAL PERIOD SCHEDULE ---
        b.Entity<RecreationalPeriodSchedule>(e =>
        {
            e.ToTable("recreational_period_schedules");
            e.HasKey(x => x.ScheduleId);
            e.Property(x => x.ScheduleId).HasColumnName("schedule_id");
            e.Property(x => x.RecreationalPeriodId).HasColumnName("recreational_period_id");
            e.Property(x => x.DayOfWeek).HasColumnName("day_of_week");
            e.HasCheckConstraint("ck_rec_schedule_day", "day_of_week BETWEEN 0 AND 6");
            e.HasIndex(x => new { x.RecreationalPeriodId, x.DayOfWeek }).IsUnique();
            e.HasOne(x => x.RecreationalPeriod).WithMany(rp => rp.Schedules).HasForeignKey(x => x.RecreationalPeriodId);
        });

        // --- RECREATIONAL TICKET ---
        b.Entity<RecreationalTicket>(e =>
        {
            e.ToTable("recreational_tickets");
            e.HasKey(x => x.TicketId);
            e.Property(x => x.TicketId).HasColumnName("ticket_id");
            e.Property(x => x.RecreationalPeriodId).HasColumnName("recreational_period_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.MemberStatus).HasColumnName("member_status").IsRequired();
            e.HasCheckConstraint("ck_ticket_member_status", "member_status IN ('MEMBER','NON_MEMBER')");
            e.Property(x => x.CheckedInAt).HasColumnName("checked_in_at");
            e.Property(x => x.AmountPaid).HasColumnName("amount_paid").HasColumnType("DECIMAL(12,2)");
            e.HasCheckConstraint("ck_ticket_amount", "amount_paid > 0");
            e.Property(x => x.PaymentDescription).HasColumnName("payment_description");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.HasOne(x => x.RecreationalPeriod).WithMany(rp => rp.Tickets).HasForeignKey(x => x.RecreationalPeriodId);
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
            e.HasOne(x => x.Transaction).WithOne().HasForeignKey<RecreationalTicket>("TransactionId");
        });

        // --- LANE ---
        b.Entity<Lane>(e =>
        {
            e.ToTable("lanes");
            e.HasKey(x => x.LaneId);
            e.Property(x => x.LaneId).HasColumnName("lane_id");
            e.Property(x => x.Label).HasColumnName("label").IsRequired();
            e.HasIndex(x => x.Label).IsUnique();
            e.Property(x => x.Capacity).HasColumnName("capacity");
            e.HasCheckConstraint("ck_lane_capacity", "capacity > 0");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("ACTIVE");
            e.HasCheckConstraint("ck_lane_status", "status IN ('ACTIVE','INACTIVE')");
        });

        // --- PRIVATE BOOKING ---
        b.Entity<PrivateBooking>(e =>
        {
            e.ToTable("private_bookings");
            e.HasKey(x => x.PrivateBookingId);
            e.Property(x => x.PrivateBookingId).HasColumnName("private_booking_id");
            e.Property(x => x.BusinessType).HasColumnName("business_type").IsRequired();
            e.HasCheckConstraint("ck_pb_business_type", "business_type IN ('LANE_RENTAL','COACH_BROUGHT','CLUB_BROUGHT')");
            e.Property(x => x.CoachId).HasColumnName("coach_id");
            e.Property(x => x.LaneId).HasColumnName("lane_id");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.Property(x => x.ClubFeePerSwimmerSnapshot).HasColumnName("club_fee_per_swimmer_snapshot").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.ClubPercentageSnapshot).HasColumnName("club_percentage_snapshot").HasColumnType("DECIMAL(5,2)");
            e.Property(x => x.CoachPercentageSnapshot).HasColumnName("coach_percentage_snapshot").HasColumnType("DECIMAL(5,2)");
            e.Property(x => x.TotalPrice).HasColumnName("total_price").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.PaidAmount).HasColumnName("paid_amount").HasColumnType("DECIMAL(12,2)").HasDefaultValue(0m);
            e.Property(x => x.BalanceDue)
                .HasColumnName("balance_due")
                .HasColumnType("DECIMAL(12,2)")
                .HasComputedColumnSql("total_price - paid_amount", stored: true);
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("ACTIVE");
            e.HasCheckConstraint("ck_pb_status", "status IN ('ACTIVE','COMPLETED','CANCELLED')");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.Coach).WithMany().HasForeignKey(x => x.CoachId);
            e.HasOne(x => x.Lane).WithMany(l => l.PrivateBookings).HasForeignKey(x => x.LaneId);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy);
        });

        // --- PRIVATE SESSION ---
        b.Entity<PrivateSession>(e =>
        {
            e.ToTable("private_sessions");
            e.HasKey(x => x.PrivateSessionId);
            e.Property(x => x.PrivateSessionId).HasColumnName("private_session_id");
            e.Property(x => x.PrivateBookingId).HasColumnName("private_booking_id");
            e.Property(x => x.ScheduledStartTime).HasColumnName("scheduled_start_time");
            e.Property(x => x.ScheduledEndTime).HasColumnName("scheduled_end_time");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("SCHEDULED");
            e.HasCheckConstraint("ck_ps_status", "status IN ('SCHEDULED','COMPLETED','CANCELLED')");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.PrivateBooking).WithMany(pb => pb.Sessions).HasForeignKey(x => x.PrivateBookingId);
        });

        // --- PRIVATE SESSION ATTENDANCE ---
        b.Entity<PrivateSessionAttendance>(e =>
        {
            e.ToTable("private_session_attendances");
            e.HasKey(x => x.AttendanceId);
            e.Property(x => x.AttendanceId).HasColumnName("attendance_id");
            e.Property(x => x.PrivateSessionId).HasColumnName("private_session_id");
            e.Property(x => x.ParticipantId).HasColumnName("participant_id");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("PRESENT");
            e.HasCheckConstraint("ck_psa_status", "status IN ('PRESENT','ABSENT')");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            e.HasIndex(x => new { x.PrivateSessionId, x.ParticipantId }).IsUnique();
            e.HasOne(x => x.PrivateSession).WithMany(ps => ps.Attendances).HasForeignKey(x => x.PrivateSessionId);
            e.HasOne(x => x.Participant).WithMany().HasForeignKey(x => x.ParticipantId);
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
        });

        // --- PRIVATE BOOKING PARTICIPANT ---
        b.Entity<PrivateBookingParticipant>(e =>
        {
            e.ToTable("private_booking_participants");
            e.HasKey(x => x.ParticipantId);
            e.Property(x => x.ParticipantId).HasColumnName("participant_id");
            e.Property(x => x.PrivateBookingId).HasColumnName("private_booking_id");
            e.Property(x => x.ParticipantType).HasColumnName("participant_type").IsRequired();
            e.HasCheckConstraint("ck_participant_type", "participant_type IN ('SWIMMER','GUEST')");
            e.Property(x => x.SwimmerId).HasColumnName("swimmer_id");
            e.Property(x => x.GuestName).HasColumnName("guest_name");
            e.Property(x => x.GuestPhone).HasColumnName("guest_phone");
            e.HasCheckConstraint("ck_participant_identity",
                "(participant_type='SWIMMER' AND swimmer_id IS NOT NULL AND guest_name IS NULL) OR " +
                "(participant_type='GUEST' AND guest_name IS NOT NULL AND swimmer_id IS NULL)");
            e.HasOne(x => x.PrivateBooking).WithMany(pb => pb.Participants).HasForeignKey(x => x.PrivateBookingId);
            e.HasOne(x => x.Swimmer).WithMany(s => s.PrivateBookingParticipants).HasForeignKey(x => x.SwimmerId);
        });

        // --- COACH DUE ---
        b.Entity<CoachDue>(e =>
        {
            e.ToTable("coach_dues");
            e.HasKey(x => x.CoachDueId);
            e.Property(x => x.CoachDueId).HasColumnName("coach_due_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.SourceType).HasColumnName("source_type").IsRequired();
            e.HasCheckConstraint("ck_coach_due_source_type", "source_type IN ('CLUB_BROUGHT_SHARE','CANCELLATION_FEE')");
            e.Property(x => x.SourceId).HasColumnName("source_id");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.PeriodYear).HasColumnName("period_year");
            e.Property(x => x.PeriodMonth).HasColumnName("period_month");
            e.HasCheckConstraint("ck_coach_due_month", "period_month BETWEEN 1 AND 12");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ConsumedInPayrollId).HasColumnName("consumed_in_payroll_id");
            e.HasOne(x => x.Employee).WithMany(emp => emp.CoachDues).HasForeignKey(x => x.EmployeeId);
            e.HasOne(x => x.PrivateBooking).WithMany(pb => pb.CoachDues).HasForeignKey(x => x.SourceId);
            e.HasOne(x => x.ConsumedInPayroll).WithMany(p => p.ConsumedCoachDues).HasForeignKey(x => x.ConsumedInPayrollId);
        });

        // --- PAYROLL ---
        b.Entity<PayrollEntity>(e =>
        {
            e.ToTable("payrolls");
            e.HasKey(x => x.PayrollId);
            e.Property(x => x.PayrollId).HasColumnName("payroll_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.PeriodYear).HasColumnName("period_year");
            e.Property(x => x.PeriodMonth).HasColumnName("period_month");
            e.HasCheckConstraint("ck_payroll_month", "period_month BETWEEN 1 AND 12");
            e.HasIndex(x => new { x.EmployeeId, x.PeriodYear, x.PeriodMonth }).IsUnique();
            e.Property(x => x.CalculatedAmount).HasColumnName("calculated_amount").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.QualificationRateSnapshot).HasColumnName("qualification_rate_snapshot").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("NOT_PAID");
            e.HasCheckConstraint("ck_payroll_status", "status IN ('NOT_PAID','PAID')");
            e.Property(x => x.PaymentDate).HasColumnName("payment_date");
            e.Property(x => x.PaidByUserId).HasColumnName("paid_by_user_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Employee).WithMany(emp => emp.Payrolls).HasForeignKey(x => x.EmployeeId);
            e.HasOne(x => x.PaidByUser).WithMany().HasForeignKey(x => x.PaidByUserId);
            e.HasOne(x => x.PayrollPaymentTransaction).WithOne().HasForeignKey<PayrollEntity>("PayrollTransactionId");
        });

        // --- PAYMENT ---
        b.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.PaymentId).HasColumnName("payment_id");
            e.Property(x => x.RelatedEntityType).HasColumnName("related_entity_type").IsRequired();
            e.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("DECIMAL(12,2)");
            e.HasCheckConstraint("ck_payment_amount", "amount > 0");
            e.Property(x => x.PaymentMethod).HasColumnName("payment_method").IsRequired();
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.LastModifiedBy).HasColumnName("last_modified_by");
            e.Property(x => x.LastModifiedAt).HasColumnName("last_modified_at");
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
            e.HasOne(x => x.LastModifiedByUser).WithMany().HasForeignKey(x => x.LastModifiedBy);
            e.HasOne(x => x.LinkedTransaction).WithOne().HasForeignKey<Payment>("TransactionId");
        });

        // --- TRANSACTION ---
        b.Entity<Transaction>(e =>
        {
            e.ToTable("transactions");
            e.HasKey(x => x.TransactionId);
            e.Property(x => x.TransactionId).HasColumnName("transaction_id");
            e.Property(x => x.TransactionType).HasColumnName("transaction_type").IsRequired();
            e.HasCheckConstraint("ck_transaction_type",
                "transaction_type IN ('TRAINING_PAYMENT','PACKAGE_PAYMENT','PRIVATE_PAYMENT'," +
                "'RECREATIONAL_TICKET_PAYMENT','EXPENSE','PAYROLL_PAYMENT','PAYROLL_ADJUSTMENT'," +
                "'REFUND','REFUND_ADJUSTMENT','CREDIT_USAGE')");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("DECIMAL(12,2)");
            e.Property(x => x.RelatedEntityType).HasColumnName("related_entity_type");
            e.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
        });

        // --- REFUND ---
        b.Entity<Refund>(e =>
        {
            e.ToTable("refunds");
            e.HasKey(x => x.RefundId);
            e.Property(x => x.RefundId).HasColumnName("refund_id");
            e.Property(x => x.RelatedEntityType).HasColumnName("related_entity_type").IsRequired();
            e.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("DECIMAL(12,2)");
            e.HasCheckConstraint("ck_refund_amount", "amount > 0");
            e.Property(x => x.Reason).HasColumnName("reason");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
            e.HasOne(x => x.LinkedTransaction).WithOne().HasForeignKey<Refund>("TransactionId");
        });

        // --- EXPENSE ---
        b.Entity<Expense>(e =>
        {
            e.ToTable("expenses");
            e.HasKey(x => x.ExpenseId);
            e.Property(x => x.ExpenseId).HasColumnName("expense_id");
            e.Property(x => x.Description).HasColumnName("description").IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("DECIMAL(12,2)");
            e.HasCheckConstraint("ck_expense_amount", "amount > 0");
            e.Property(x => x.ExpenseDate).HasColumnName("expense_date");
            e.Property(x => x.Category).HasColumnName("category");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
            e.HasOne(x => x.LinkedTransaction).WithOne().HasForeignKey<Expense>("TransactionId");
        });

        // --- CREDIT ---
        b.Entity<Credit>(e =>
        {
            e.ToTable("credits");
            e.HasKey(x => x.CreditId);
            e.Property(x => x.CreditId).HasColumnName("credit_id");
            e.Property(x => x.SwimmerId).HasColumnName("swimmer_id").IsRequired();
            e.Property(x => x.GeneratedFromType).HasColumnName("generated_from_type").IsRequired();
            e.HasCheckConstraint("ck_credit_generated_from", "generated_from_type IN ('TRAINING_SUBSCRIPTION','PACKAGE')");
            e.Property(x => x.GeneratedFromId).HasColumnName("generated_from_id");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("DECIMAL(12,2)");
            e.HasCheckConstraint("ck_credit_amount", "amount > 0");
            e.Property(x => x.RemainingAmount).HasColumnName("remaining_amount").HasColumnType("DECIMAL(12,2)");
            e.HasCheckConstraint("ck_credit_remaining", "remaining_amount >= 0");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Swimmer).WithMany(s => s.Credits).HasForeignKey(x => x.SwimmerId);
        });

        // --- CREDIT USAGE ---
        b.Entity<CreditUsage>(e =>
        {
            e.ToTable("credit_usages");
            e.HasKey(x => x.UsageId);
            e.Property(x => x.UsageId).HasColumnName("usage_id");
            e.Property(x => x.CreditId).HasColumnName("credit_id");
            e.Property(x => x.AppliedToEntityType).HasColumnName("applied_to_entity_type").IsRequired();
            e.Property(x => x.AppliedToEntityId).HasColumnName("applied_to_entity_id");
            e.Property(x => x.AmountUsed).HasColumnName("amount_used").HasColumnType("DECIMAL(12,2)");
            e.HasCheckConstraint("ck_credit_usage_amount", "amount_used > 0");
            e.Property(x => x.UsedAt).HasColumnName("used_at");
            e.Property(x => x.RecordedBy).HasColumnName("recorded_by");
            e.HasOne(x => x.Credit).WithMany(c => c.Usages).HasForeignKey(x => x.CreditId);
            e.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedBy);
            e.HasOne(x => x.RevenueTransaction).WithOne().HasForeignKey<CreditUsage>("TransactionId");
        });

        // --- CANCELLATION FEE CONFIG ---
        b.Entity<CancellationFeeConfig>(e =>
        {
            e.ToTable("cancellation_fee_configs");
            e.HasKey(x => x.ConfigId);
            e.Property(x => x.ConfigId).HasColumnName("config_id");
            e.Property(x => x.FeePercentage).HasColumnName("fee_percentage").HasColumnType("DECIMAL(5,2)");
            e.HasCheckConstraint("ck_fee_pct", "fee_percentage > 0 AND fee_percentage <= 100");
            e.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            e.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // --- SYSTEM SETTING ---
        b.Entity<SystemSetting>(e =>
        {
            e.ToTable("system_settings");
            e.HasKey(x => x.SettingKey);
            e.Property(x => x.SettingKey).HasColumnName("setting_key");
            e.Property(x => x.SettingValue).HasColumnName("setting_value").IsRequired();
        });

        // --- SCHEDULE DEFAULTS CONFIG ---
        b.Entity<ScheduleDefaultsConfig>(e =>
        {
            e.ToTable("schedule_defaults_configs");
            e.HasKey(x => x.ConfigId);
            e.Property(x => x.ConfigId).HasColumnName("config_id");
            e.Property(x => x.DefaultStartTime).HasColumnName("default_start_time");
            e.Property(x => x.DefaultEndTime).HasColumnName("default_end_time");
            e.Property(x => x.DefaultCapacity).HasColumnName("default_capacity");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
            e.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedBy);
        });

        // --- BACKUP ---
        b.Entity<BackupEntity>(e =>
        {
            e.ToTable("backups");
            e.HasKey(x => x.BackupId);
            e.Property(x => x.BackupId).HasColumnName("backup_id");
            e.Property(x => x.FilePath).HasColumnName("file_path").IsRequired();
            e.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
            e.Property(x => x.BackupType).HasColumnName("backup_type").IsRequired();
            e.HasCheckConstraint("ck_backup_type", "backup_type IN ('AUTOMATIC','MANUAL')");
            e.Property(x => x.SystemVersion).HasColumnName("system_version").IsRequired().HasDefaultValue("1.0.0");
            e.Property(x => x.Encrypted).HasColumnName("encrypted").HasDefaultValue(true);
            e.Property(x => x.EncryptionKeyRef).HasColumnName("encryption_key_ref");
            e.Property(x => x.AttemptNumber).HasColumnName("attempt_number").HasDefaultValue(1);
            e.HasCheckConstraint("ck_backup_attempt", "attempt_number BETWEEN 1 AND 5");
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.HasCheckConstraint("ck_backup_status", "status IN ('SUCCESS','FAILED')");
            e.Property(x => x.ErrorMessage).HasColumnName("error_message");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy);
        });

        // --- AUDIT LOG ---
        b.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.AuditLogId);
            e.Property(x => x.AuditLogId).HasColumnName("audit_log_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.EventType).HasColumnName("event_type").IsRequired();
            e.Property(x => x.EntityType).HasColumnName("entity_type").IsRequired();
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.OldValue).HasColumnName("old_value");
            e.Property(x => x.NewValue).HasColumnName("new_value");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.User).WithMany(u => u.AuditLogs).HasForeignKey(x => x.UserId);

            // Audit log index for efficient entity-based queries
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.CreatedAt);
        });

    }

    private static void SeedRoles(ModelBuilder b)
    {
        // Seed the fixed, closed role set (Decision 27).
        b.Entity<Role>().HasData(
            new Role { RoleId = 1, Code = "SUPER_ADMIN", NameEn = "Super Admin", NameAr = "مسؤول النظام" },
            new Role { RoleId = 2, Code = "OWNER", NameEn = "Owner", NameAr = "المالك" },
            new Role { RoleId = 3, Code = "ADMINISTRATOR", NameEn = "Administrator", NameAr = "مدير" }
        );
    }
}
