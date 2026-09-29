using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SwimClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cancellation_fee_configs",
                columns: table => new
                {
                    config_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fee_percentage = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cancellation_fee_configs", x => x.config_id);
                    table.CheckConstraint("ck_fee_pct", "fee_percentage > 0 AND fee_percentage <= 100");
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    employee_type = table.Column<string>(type: "TEXT", nullable: false),
                    national_id = table.Column<string>(type: "TEXT", nullable: false),
                    phone = table.Column<string>(type: "TEXT", nullable: true),
                    nickname = table.Column<string>(type: "TEXT", nullable: true),
                    monthly_salary = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    deactivated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.employee_id);
                    table.CheckConstraint("ck_admin_salary", "employee_type <> 'ADMINISTRATOR' OR monthly_salary IS NOT NULL");
                    table.CheckConstraint("ck_employee_status", "status IN ('ACTIVE','INACTIVE')");
                    table.CheckConstraint("ck_employee_type", "employee_type IN ('ADMINISTRATOR','COACH','LIFEGUARD')");
                });

            migrationBuilder.CreateTable(
                name: "lanes",
                columns: table => new
                {
                    lane_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    label = table.Column<string>(type: "TEXT", nullable: false),
                    capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lanes", x => x.lane_id);
                    table.CheckConstraint("ck_lane_capacity", "capacity > 0");
                    table.CheckConstraint("ck_lane_status", "status IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "programs",
                columns: table => new
                {
                    program_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    program_type = table.Column<string>(type: "TEXT", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programs", x => x.program_id);
                    table.CheckConstraint("ck_program_type", "program_type IN ('REGULAR','STAR','TEAM')");
                });

            migrationBuilder.CreateTable(
                name: "qualifications",
                columns: table => new
                {
                    qualification_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name_en = table.Column<string>(type: "TEXT", nullable: false),
                    name_ar = table.Column<string>(type: "TEXT", nullable: false),
                    rank_order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qualifications", x => x.qualification_id);
                });

            migrationBuilder.CreateTable(
                name: "recreational_periods",
                columns: table => new
                {
                    recreational_period_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recreational_periods", x => x.recreational_period_id);
                    table.CheckConstraint("ck_rec_period_capacity", "capacity > 0");
                    table.CheckConstraint("ck_rec_period_status", "status IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    name_en = table.Column<string>(type: "TEXT", nullable: false),
                    name_ar = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "swimmers",
                columns: table => new
                {
                    swimmer_id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    gender = table.Column<string>(type: "TEXT", nullable: false),
                    parent_name = table.Column<string>(type: "TEXT", nullable: true),
                    phone = table.Column<string>(type: "TEXT", nullable: true),
                    member_status = table.Column<string>(type: "TEXT", nullable: false),
                    qr_token = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "INACTIVE"),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_swimmers", x => x.swimmer_id);
                    table.CheckConstraint("ck_swimmer_gender", "gender IN ('MALE','FEMALE')");
                    table.CheckConstraint("ck_swimmer_member_status", "member_status IN ('MEMBER','NON_MEMBER')");
                    table.CheckConstraint("ck_swimmer_status", "status IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    setting_key = table.Column<string>(type: "TEXT", nullable: false),
                    setting_value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.setting_key);
                });

            migrationBuilder.CreateTable(
                name: "package_configs",
                columns: table => new
                {
                    package_config_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    program_id = table.Column<int>(type: "INTEGER", nullable: false),
                    duration_months = table.Column<int>(type: "INTEGER", nullable: false),
                    sessions_per_month = table.Column<int>(type: "INTEGER", nullable: false),
                    member_status = table.Column<string>(type: "TEXT", nullable: false),
                    price = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_package_configs", x => x.package_config_id);
                    table.CheckConstraint("ck_pkg_cfg_member_status", "member_status IN ('MEMBER','NON_MEMBER')");
                    table.ForeignKey(
                        name: "FK_package_configs_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "program_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_periods",
                columns: table => new
                {
                    period_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    program_id = table.Column<int>(type: "INTEGER", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_periods", x => x.period_id);
                    table.CheckConstraint("ck_period_capacity", "capacity > 0");
                    table.CheckConstraint("ck_period_status", "status IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_training_periods_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "program_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_price_configs",
                columns: table => new
                {
                    price_config_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    program_id = table.Column<int>(type: "INTEGER", nullable: false),
                    member_status = table.Column<string>(type: "TEXT", nullable: false),
                    price = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_price_configs", x => x.price_config_id);
                    table.CheckConstraint("ck_price_member_status", "member_status IN ('MEMBER','NON_MEMBER')");
                    table.ForeignKey(
                        name: "FK_training_price_configs_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "program_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_qualifications",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    qualification_id = table.Column<int>(type: "INTEGER", nullable: false),
                    obtained_at = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_qualifications", x => new { x.employee_id, x.qualification_id });
                    table.ForeignKey(
                        name: "FK_employee_qualifications_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_qualifications_qualifications_qualification_id",
                        column: x => x.qualification_id,
                        principalTable: "qualifications",
                        principalColumn: "qualification_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qualification_rate_configs",
                columns: table => new
                {
                    rate_config_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    qualification_id = table.Column<int>(type: "INTEGER", nullable: false),
                    session_rate = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qualification_rate_configs", x => x.rate_config_id);
                    table.ForeignKey(
                        name: "FK_qualification_rate_configs_qualifications_qualification_id",
                        column: x => x.qualification_id,
                        principalTable: "qualifications",
                        principalColumn: "qualification_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recreational_period_schedules",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    recreational_period_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_week = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recreational_period_schedules", x => x.schedule_id);
                    table.CheckConstraint("ck_rec_schedule_day", "day_of_week BETWEEN 0 AND 6");
                    table.ForeignKey(
                        name: "FK_recreational_period_schedules_recreational_periods_recreational_period_id",
                        column: x => x.recreational_period_id,
                        principalTable: "recreational_periods",
                        principalColumn: "recreational_period_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", nullable: false),
                    role_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_by_user_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    deactivated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_users_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_users_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "credits",
                columns: table => new
                {
                    credit_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    swimmer_id = table.Column<string>(type: "TEXT", nullable: false),
                    generated_from_type = table.Column<string>(type: "TEXT", nullable: false),
                    generated_from_id = table.Column<int>(type: "INTEGER", nullable: false),
                    amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    remaining_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credits", x => x.credit_id);
                    table.CheckConstraint("ck_credit_amount", "amount > 0");
                    table.CheckConstraint("ck_credit_generated_from", "generated_from_type IN ('TRAINING_SUBSCRIPTION','PACKAGE')");
                    table.CheckConstraint("ck_credit_remaining", "remaining_amount >= 0");
                    table.ForeignKey(
                        name: "FK_credits_swimmers_swimmer_id",
                        column: x => x.swimmer_id,
                        principalTable: "swimmers",
                        principalColumn: "swimmer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "period_staff_assignments",
                columns: table => new
                {
                    assignment_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    period_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    role = table.Column<string>(type: "TEXT", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_period_staff_assignments", x => x.assignment_id);
                    table.CheckConstraint("ck_staff_role", "role IN ('COACH','LIFEGUARD')");
                    table.ForeignKey(
                        name: "FK_period_staff_assignments_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_period_staff_assignments_training_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "training_periods",
                        principalColumn: "period_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_period_schedules",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    period_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_week = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_period_schedules", x => x.schedule_id);
                    table.CheckConstraint("ck_schedule_day", "day_of_week BETWEEN 0 AND 6");
                    table.ForeignKey(
                        name: "FK_training_period_schedules_training_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "training_periods",
                        principalColumn: "period_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "administrator_daily_attendances",
                columns: table => new
                {
                    attendance_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    attendance_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administrator_daily_attendances", x => x.attendance_id);
                    table.CheckConstraint("ck_admin_att_status", "status IN ('PRESENT','ABSENT')");
                    table.ForeignKey(
                        name: "FK_administrator_daily_attendances_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_administrator_daily_attendances_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    audit_log_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<int>(type: "INTEGER", nullable: true),
                    event_type = table.Column<string>(type: "TEXT", nullable: false),
                    entity_type = table.Column<string>(type: "TEXT", nullable: false),
                    entity_id = table.Column<string>(type: "TEXT", nullable: true),
                    old_value = table.Column<string>(type: "TEXT", nullable: true),
                    new_value = table.Column<string>(type: "TEXT", nullable: true),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.audit_log_id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "backups",
                columns: table => new
                {
                    backup_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    file_path = table.Column<string>(type: "TEXT", nullable: false),
                    file_size_bytes = table.Column<long>(type: "INTEGER", nullable: false),
                    backup_type = table.Column<string>(type: "TEXT", nullable: false),
                    encrypted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    encryption_key_ref = table.Column<string>(type: "TEXT", nullable: true),
                    attempt_number = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backups", x => x.backup_id);
                    table.CheckConstraint("ck_backup_attempt", "attempt_number BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_backup_status", "status IN ('SUCCESS','FAILED')");
                    table.CheckConstraint("ck_backup_type", "backup_type IN ('AUTOMATIC','MANUAL')");
                    table.ForeignKey(
                        name: "FK_backups_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "private_bookings",
                columns: table => new
                {
                    private_booking_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    business_type = table.Column<string>(type: "TEXT", nullable: false),
                    coach_id = table.Column<int>(type: "INTEGER", nullable: true),
                    lane_id = table.Column<int>(type: "INTEGER", nullable: true),
                    start_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    end_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    club_fee_per_swimmer_snapshot = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    club_percentage_snapshot = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    coach_percentage_snapshot = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    total_price = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    paid_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false, defaultValue: 0m),
                    balance_due = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false, computedColumnSql: "total_price - paid_amount", stored: true),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "ACTIVE"),
                    created_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_private_bookings", x => x.private_booking_id);
                    table.CheckConstraint("ck_pb_business_type", "business_type IN ('LANE_RENTAL','COACH_BROUGHT','CLUB_BROUGHT')");
                    table.CheckConstraint("ck_pb_status", "status IN ('ACTIVE','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_private_bookings_employees_coach_id",
                        column: x => x.coach_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_private_bookings_lanes_lane_id",
                        column: x => x.lane_id,
                        principalTable: "lanes",
                        principalColumn: "lane_id");
                    table.ForeignKey(
                        name: "FK_private_bookings_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_defaults_configs",
                columns: table => new
                {
                    config_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    default_start_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    default_end_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    default_capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_by = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_defaults_configs", x => x.config_id);
                    table.ForeignKey(
                        name: "FK_schedule_defaults_configs_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    transaction_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    transaction_type = table.Column<string>(type: "TEXT", nullable: false),
                    amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    related_entity_type = table.Column<string>(type: "TEXT", nullable: true),
                    related_entity_id = table.Column<int>(type: "INTEGER", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.transaction_id);
                    table.CheckConstraint("ck_transaction_type", "transaction_type IN ('TRAINING_PAYMENT','PACKAGE_PAYMENT','PRIVATE_PAYMENT','RECREATIONAL_TICKET','EXPENSE','PAYROLL_PAYMENT','PAYROLL_ADJUSTMENT','REFUND','REFUND_ADJUSTMENT','CREDIT_USAGE')");
                    table.ForeignKey(
                        name: "FK_transactions_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "packages",
                columns: table => new
                {
                    package_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    swimmer_id = table.Column<string>(type: "TEXT", nullable: false),
                    package_type = table.Column<string>(type: "TEXT", nullable: false),
                    program_id = table.Column<int>(type: "INTEGER", nullable: true),
                    training_period_id = table.Column<int>(type: "INTEGER", nullable: true),
                    recreational_period_id = table.Column<int>(type: "INTEGER", nullable: true),
                    start_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    end_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    duration_snapshot_days = table.Column<int>(type: "INTEGER", nullable: false),
                    sessions_per_month_snapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    available_sessions_total = table.Column<int>(type: "INTEGER", nullable: false),
                    available_sessions_remaining = table.Column<int>(type: "INTEGER", nullable: false),
                    reference_training_price_snapshot = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    total_price = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    paid_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false, defaultValue: 0m),
                    balance_due = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false, computedColumnSql: "total_price - paid_amount", stored: true),
                    credit_granted_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    credit_description = table.Column<string>(type: "TEXT", nullable: true),
                    outstanding_declared_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    outstanding_description = table.Column<string>(type: "TEXT", nullable: true),
                    qr_token = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "ACTIVE"),
                    renewed_from_package_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GrantedCreditCreditId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packages", x => x.package_id);
                    table.CheckConstraint("ck_package_type", "package_type IN ('TRAINING','RECREATIONAL')");
                    table.CheckConstraint("ck_pkg_credit_outstanding_exclusive", "credit_granted_amount IS NULL OR outstanding_declared_amount IS NULL");
                    table.CheckConstraint("ck_pkg_period_consistency", "(package_type='TRAINING' AND training_period_id IS NOT NULL AND recreational_period_id IS NULL) OR (package_type='RECREATIONAL' AND recreational_period_id IS NOT NULL AND training_period_id IS NULL)");
                    table.CheckConstraint("ck_pkg_sessions_remaining", "available_sessions_remaining >= 0");
                    table.CheckConstraint("ck_pkg_status", "status IN ('ACTIVE','EXPIRED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_packages_credits_GrantedCreditCreditId",
                        column: x => x.GrantedCreditCreditId,
                        principalTable: "credits",
                        principalColumn: "credit_id");
                    table.ForeignKey(
                        name: "FK_packages_packages_renewed_from_package_id",
                        column: x => x.renewed_from_package_id,
                        principalTable: "packages",
                        principalColumn: "package_id");
                    table.ForeignKey(
                        name: "FK_packages_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "program_id");
                    table.ForeignKey(
                        name: "FK_packages_recreational_periods_recreational_period_id",
                        column: x => x.recreational_period_id,
                        principalTable: "recreational_periods",
                        principalColumn: "recreational_period_id");
                    table.ForeignKey(
                        name: "FK_packages_swimmers_swimmer_id",
                        column: x => x.swimmer_id,
                        principalTable: "swimmers",
                        principalColumn: "swimmer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_packages_training_periods_training_period_id",
                        column: x => x.training_period_id,
                        principalTable: "training_periods",
                        principalColumn: "period_id");
                    table.ForeignKey(
                        name: "FK_packages_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    swimmer_id = table.Column<string>(type: "TEXT", nullable: false),
                    program_id = table.Column<int>(type: "INTEGER", nullable: false),
                    period_id = table.Column<int>(type: "INTEGER", nullable: false),
                    start_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    end_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    unit_price_snapshot = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    configured_session_count_snapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    total_price = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    paid_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false, defaultValue: 0m),
                    balance_due = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false, computedColumnSql: "total_price - paid_amount", stored: true),
                    credit_granted_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    credit_description = table.Column<string>(type: "TEXT", nullable: true),
                    outstanding_declared_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    outstanding_description = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "ACTIVE"),
                    renewed_from_subscription_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GrantedCreditCreditId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_subscriptions", x => x.subscription_id);
                    table.CheckConstraint("ck_sub_credit_outstanding_exclusive", "credit_granted_amount IS NULL OR outstanding_declared_amount IS NULL");
                    table.CheckConstraint("ck_sub_status", "status IN ('ACTIVE','PAUSED','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_training_subscriptions_credits_GrantedCreditCreditId",
                        column: x => x.GrantedCreditCreditId,
                        principalTable: "credits",
                        principalColumn: "credit_id");
                    table.ForeignKey(
                        name: "FK_training_subscriptions_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "program_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_training_subscriptions_swimmers_swimmer_id",
                        column: x => x.swimmer_id,
                        principalTable: "swimmers",
                        principalColumn: "swimmer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_training_subscriptions_training_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "training_periods",
                        principalColumn: "period_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_training_subscriptions_training_subscriptions_renewed_from_subscription_id",
                        column: x => x.renewed_from_subscription_id,
                        principalTable: "training_subscriptions",
                        principalColumn: "subscription_id");
                    table.ForeignKey(
                        name: "FK_training_subscriptions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "private_booking_participants",
                columns: table => new
                {
                    participant_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    private_booking_id = table.Column<int>(type: "INTEGER", nullable: false),
                    participant_type = table.Column<string>(type: "TEXT", nullable: false),
                    swimmer_id = table.Column<string>(type: "TEXT", nullable: true),
                    guest_name = table.Column<string>(type: "TEXT", nullable: true),
                    guest_phone = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_private_booking_participants", x => x.participant_id);
                    table.CheckConstraint("ck_participant_identity", "(participant_type='SWIMMER' AND swimmer_id IS NOT NULL AND guest_name IS NULL) OR (participant_type='GUEST' AND guest_name IS NOT NULL AND swimmer_id IS NULL)");
                    table.CheckConstraint("ck_participant_type", "participant_type IN ('SWIMMER','GUEST')");
                    table.ForeignKey(
                        name: "FK_private_booking_participants_private_bookings_private_booking_id",
                        column: x => x.private_booking_id,
                        principalTable: "private_bookings",
                        principalColumn: "private_booking_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_private_booking_participants_swimmers_swimmer_id",
                        column: x => x.swimmer_id,
                        principalTable: "swimmers",
                        principalColumn: "swimmer_id");
                });

            migrationBuilder.CreateTable(
                name: "credit_usages",
                columns: table => new
                {
                    usage_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    credit_id = table.Column<int>(type: "INTEGER", nullable: false),
                    applied_to_entity_type = table.Column<string>(type: "TEXT", nullable: false),
                    applied_to_entity_id = table.Column<int>(type: "INTEGER", nullable: false),
                    amount_used = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    used_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_usages", x => x.usage_id);
                    table.CheckConstraint("ck_credit_usage_amount", "amount_used > 0");
                    table.ForeignKey(
                        name: "FK_credit_usages_credits_credit_id",
                        column: x => x.credit_id,
                        principalTable: "credits",
                        principalColumn: "credit_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_usages_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "transaction_id");
                    table.ForeignKey(
                        name: "FK_credit_usages_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    expense_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    expense_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    category = table.Column<string>(type: "TEXT", nullable: true),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.expense_id);
                    table.CheckConstraint("ck_expense_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_expenses_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "transaction_id");
                    table.ForeignKey(
                        name: "FK_expenses_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payrolls",
                columns: table => new
                {
                    payroll_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    period_year = table.Column<int>(type: "INTEGER", nullable: false),
                    period_month = table.Column<int>(type: "INTEGER", nullable: false),
                    calculated_amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    qualification_rate_snapshot = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "NOT_PAID"),
                    payment_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    paid_by_user_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PayrollTransactionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payrolls", x => x.payroll_id);
                    table.CheckConstraint("ck_payroll_month", "period_month BETWEEN 1 AND 12");
                    table.CheckConstraint("ck_payroll_status", "status IN ('NOT_PAID','PAID')");
                    table.ForeignKey(
                        name: "FK_payrolls_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payrolls_transactions_PayrollTransactionId",
                        column: x => x.PayrollTransactionId,
                        principalTable: "transactions",
                        principalColumn: "transaction_id");
                    table.ForeignKey(
                        name: "FK_payrolls_users_paid_by_user_id",
                        column: x => x.paid_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "recreational_tickets",
                columns: table => new
                {
                    ticket_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    recreational_period_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    member_status = table.Column<string>(type: "TEXT", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    amount_paid = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    payment_description = table.Column<string>(type: "TEXT", nullable: true),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recreational_tickets", x => x.ticket_id);
                    table.CheckConstraint("ck_ticket_amount", "amount_paid > 0");
                    table.CheckConstraint("ck_ticket_member_status", "member_status IN ('MEMBER','NON_MEMBER')");
                    table.ForeignKey(
                        name: "FK_recreational_tickets_recreational_periods_recreational_period_id",
                        column: x => x.recreational_period_id,
                        principalTable: "recreational_periods",
                        principalColumn: "recreational_period_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recreational_tickets_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "transaction_id");
                    table.ForeignKey(
                        name: "FK_recreational_tickets_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    refund_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    related_entity_type = table.Column<string>(type: "TEXT", nullable: false),
                    related_entity_id = table.Column<int>(type: "INTEGER", nullable: false),
                    amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: true),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.refund_id);
                    table.CheckConstraint("ck_refund_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_refunds_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "transaction_id");
                    table.ForeignKey(
                        name: "FK_refunds_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "package_checkins",
                columns: table => new
                {
                    checkin_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    package_id = table.Column<int>(type: "INTEGER", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    attendance_method = table.Column<string>(type: "TEXT", nullable: false),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecreationalPeriodId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_package_checkins", x => x.checkin_id);
                    table.CheckConstraint("ck_pkg_checkin_method", "attendance_method IN ('QR','MANUAL')");
                    table.ForeignKey(
                        name: "FK_package_checkins_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "package_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_package_checkins_recreational_periods_RecreationalPeriodId",
                        column: x => x.RecreationalPeriodId,
                        principalTable: "recreational_periods",
                        principalColumn: "recreational_period_id");
                    table.ForeignKey(
                        name: "FK_package_checkins_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "package_period_changes",
                columns: table => new
                {
                    change_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    package_id = table.Column<int>(type: "INTEGER", nullable: false),
                    old_period_id = table.Column<int>(type: "INTEGER", nullable: false),
                    new_period_id = table.Column<int>(type: "INTEGER", nullable: false),
                    change_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    sessions_carried_over = table.Column<int>(type: "INTEGER", nullable: false),
                    changed_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_package_period_changes", x => x.change_id);
                    table.ForeignKey(
                        name: "FK_package_period_changes_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "package_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_package_period_changes_training_periods_new_period_id",
                        column: x => x.new_period_id,
                        principalTable: "training_periods",
                        principalColumn: "period_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_package_period_changes_training_periods_old_period_id",
                        column: x => x.old_period_id,
                        principalTable: "training_periods",
                        principalColumn: "period_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_package_period_changes_users_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "package_qr_histories",
                columns: table => new
                {
                    qr_history_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    package_id = table.Column<int>(type: "INTEGER", nullable: false),
                    qr_token = table.Column<string>(type: "TEXT", nullable: false),
                    issued_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_package_qr_histories", x => x.qr_history_id);
                    table.ForeignKey(
                        name: "FK_package_qr_histories_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "package_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    related_entity_type = table.Column<string>(type: "TEXT", nullable: false),
                    related_entity_id = table.Column<int>(type: "INTEGER", nullable: false),
                    amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    payment_method = table.Column<string>(type: "TEXT", nullable: false),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_modified_by = table.Column<int>(type: "INTEGER", nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TransactionId = table.Column<int>(type: "INTEGER", nullable: true),
                    PackageId = table.Column<int>(type: "INTEGER", nullable: true),
                    PrivateBookingId = table.Column<int>(type: "INTEGER", nullable: true),
                    TrainingSubscriptionSubscriptionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.payment_id);
                    table.CheckConstraint("ck_payment_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_payments_packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "packages",
                        principalColumn: "package_id");
                    table.ForeignKey(
                        name: "FK_payments_private_bookings_PrivateBookingId",
                        column: x => x.PrivateBookingId,
                        principalTable: "private_bookings",
                        principalColumn: "private_booking_id");
                    table.ForeignKey(
                        name: "FK_payments_training_subscriptions_TrainingSubscriptionSubscriptionId",
                        column: x => x.TrainingSubscriptionSubscriptionId,
                        principalTable: "training_subscriptions",
                        principalColumn: "subscription_id");
                    table.ForeignKey(
                        name: "FK_payments_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "transaction_id");
                    table.ForeignKey(
                        name: "FK_payments_users_last_modified_by",
                        column: x => x.last_modified_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_payments_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    session_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    period_id = table.Column<int>(type: "INTEGER", nullable: false),
                    subscription_id = table.Column<int>(type: "INTEGER", nullable: false),
                    scheduled_start_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    scheduled_end_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "SCHEDULED"),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.session_id);
                    table.CheckConstraint("ck_session_status", "status IN ('SCHEDULED','PAUSED','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_sessions_training_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "training_periods",
                        principalColumn: "period_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sessions_training_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "training_subscriptions",
                        principalColumn: "subscription_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_subscription_pauses",
                columns: table => new
                {
                    pause_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    subscription_id = table.Column<int>(type: "INTEGER", nullable: false),
                    pause_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    resume_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_subscription_pauses", x => x.pause_id);
                    table.ForeignKey(
                        name: "FK_training_subscription_pauses_training_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "training_subscriptions",
                        principalColumn: "subscription_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "coach_dues",
                columns: table => new
                {
                    coach_due_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    source_type = table.Column<string>(type: "TEXT", nullable: false),
                    source_id = table.Column<int>(type: "INTEGER", nullable: false),
                    amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    period_year = table.Column<int>(type: "INTEGER", nullable: false),
                    period_month = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    consumed_in_payroll_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coach_dues", x => x.coach_due_id);
                    table.CheckConstraint("ck_coach_due_month", "period_month BETWEEN 1 AND 12");
                    table.CheckConstraint("ck_coach_due_source_type", "source_type IN ('CLUB_BROUGHT_SHARE','CANCELLATION_FEE')");
                    table.ForeignKey(
                        name: "FK_coach_dues_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_coach_dues_payrolls_consumed_in_payroll_id",
                        column: x => x.consumed_in_payroll_id,
                        principalTable: "payrolls",
                        principalColumn: "payroll_id");
                    table.ForeignKey(
                        name: "FK_coach_dues_private_bookings_source_id",
                        column: x => x.source_id,
                        principalTable: "private_bookings",
                        principalColumn: "private_booking_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attendances",
                columns: table => new
                {
                    attendance_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    session_id = table.Column<int>(type: "INTEGER", nullable: false),
                    swimmer_id = table.Column<string>(type: "TEXT", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    attendance_method = table.Column<string>(type: "TEXT", nullable: false),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendances", x => x.attendance_id);
                    table.CheckConstraint("ck_attendance_method", "attendance_method IN ('QR','MANUAL')");
                    table.ForeignKey(
                        name: "FK_attendances_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendances_swimmers_swimmer_id",
                        column: x => x.swimmer_id,
                        principalTable: "swimmers",
                        principalColumn: "swimmer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendances_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_attendances",
                columns: table => new
                {
                    attendance_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    session_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    is_late_edit = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_modified_by = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_attendances", x => x.attendance_id);
                    table.CheckConstraint("ck_emp_att_status", "status IN ('PRESENT','ABSENT')");
                    table.ForeignKey(
                        name: "FK_employee_attendances_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_attendances_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_attendances_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_replacements",
                columns: table => new
                {
                    replacement_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    session_id = table.Column<int>(type: "INTEGER", nullable: false),
                    original_employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    replacing_employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    rate_applied_snapshot = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_replacements", x => x.replacement_id);
                    table.ForeignKey(
                        name: "FK_employee_replacements_employees_original_employee_id",
                        column: x => x.original_employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_replacements_employees_replacing_employee_id",
                        column: x => x.replacing_employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_replacements_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_replacements_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "role_id", "code", "name_ar", "name_en" },
                values: new object[,]
                {
                    { 1, "SUPER_ADMIN", "مسؤول النظام", "Super Admin" },
                    { 2, "OWNER", "المالك", "Owner" },
                    { 3, "ADMINISTRATOR", "مدير", "Administrator" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_administrator_daily_attendances_employee_id_attendance_date",
                table: "administrator_daily_attendances",
                columns: new[] { "employee_id", "attendance_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_administrator_daily_attendances_recorded_by",
                table: "administrator_daily_attendances",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_recorded_by",
                table: "attendances",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_session_id",
                table: "attendances",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendances_swimmer_id",
                table: "attendances",
                column: "swimmer_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_backups_created_by",
                table: "backups",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_coach_dues_consumed_in_payroll_id",
                table: "coach_dues",
                column: "consumed_in_payroll_id");

            migrationBuilder.CreateIndex(
                name: "IX_coach_dues_employee_id",
                table: "coach_dues",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_coach_dues_source_id",
                table: "coach_dues",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_usages_credit_id",
                table: "credit_usages",
                column: "credit_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_usages_recorded_by",
                table: "credit_usages",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_credit_usages_TransactionId",
                table: "credit_usages",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credits_swimmer_id",
                table: "credits",
                column: "swimmer_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_attendances_employee_id",
                table: "employee_attendances",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_attendances_recorded_by",
                table: "employee_attendances",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_employee_attendances_session_id_employee_id",
                table: "employee_attendances",
                columns: new[] { "session_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_qualifications_qualification_id",
                table: "employee_qualifications",
                column: "qualification_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_replacements_original_employee_id",
                table: "employee_replacements",
                column: "original_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_replacements_recorded_by",
                table: "employee_replacements",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_employee_replacements_replacing_employee_id",
                table: "employee_replacements",
                column: "replacing_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_replacements_session_id",
                table: "employee_replacements",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_national_id",
                table: "employees",
                column: "national_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expenses_recorded_by",
                table: "expenses",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_TransactionId",
                table: "expenses",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lanes_label",
                table: "lanes",
                column: "label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_package_checkins_package_id",
                table: "package_checkins",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_package_checkins_recorded_by",
                table: "package_checkins",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_package_checkins_RecreationalPeriodId",
                table: "package_checkins",
                column: "RecreationalPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_package_configs_program_id",
                table: "package_configs",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "IX_package_period_changes_changed_by",
                table: "package_period_changes",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_package_period_changes_new_period_id",
                table: "package_period_changes",
                column: "new_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_package_period_changes_old_period_id",
                table: "package_period_changes",
                column: "old_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_package_period_changes_package_id",
                table: "package_period_changes",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_package_qr_histories_package_id",
                table: "package_qr_histories",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_created_by",
                table: "packages",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_packages_GrantedCreditCreditId",
                table: "packages",
                column: "GrantedCreditCreditId");

            migrationBuilder.CreateIndex(
                name: "IX_packages_program_id",
                table: "packages",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_qr_token",
                table: "packages",
                column: "qr_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_packages_recreational_period_id",
                table: "packages",
                column: "recreational_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_renewed_from_package_id",
                table: "packages",
                column: "renewed_from_package_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_swimmer_id",
                table: "packages",
                column: "swimmer_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_training_period_id",
                table: "packages",
                column: "training_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_last_modified_by",
                table: "payments",
                column: "last_modified_by");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PackageId",
                table: "payments",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PrivateBookingId",
                table: "payments",
                column: "PrivateBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_recorded_by",
                table: "payments",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TrainingSubscriptionSubscriptionId",
                table: "payments",
                column: "TrainingSubscriptionSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TransactionId",
                table: "payments",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_employee_id_period_year_period_month",
                table: "payrolls",
                columns: new[] { "employee_id", "period_year", "period_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_paid_by_user_id",
                table: "payrolls",
                column: "paid_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_PayrollTransactionId",
                table: "payrolls",
                column: "PayrollTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_period_staff_assignments_employee_id",
                table: "period_staff_assignments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_period_staff_assignments_period_id",
                table: "period_staff_assignments",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "IX_private_booking_participants_private_booking_id",
                table: "private_booking_participants",
                column: "private_booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_private_booking_participants_swimmer_id",
                table: "private_booking_participants",
                column: "swimmer_id");

            migrationBuilder.CreateIndex(
                name: "IX_private_bookings_coach_id",
                table: "private_bookings",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_private_bookings_created_by",
                table: "private_bookings",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_private_bookings_lane_id",
                table: "private_bookings",
                column: "lane_id");

            migrationBuilder.CreateIndex(
                name: "IX_qualification_rate_configs_qualification_id",
                table: "qualification_rate_configs",
                column: "qualification_id");

            migrationBuilder.CreateIndex(
                name: "IX_qualifications_rank_order",
                table: "qualifications",
                column: "rank_order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recreational_period_schedules_recreational_period_id_day_of_week",
                table: "recreational_period_schedules",
                columns: new[] { "recreational_period_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recreational_tickets_recorded_by",
                table: "recreational_tickets",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_recreational_tickets_recreational_period_id",
                table: "recreational_tickets",
                column: "recreational_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_recreational_tickets_TransactionId",
                table: "recreational_tickets",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refunds_recorded_by",
                table: "refunds",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_TransactionId",
                table: "refunds",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_defaults_configs_updated_by",
                table: "schedule_defaults_configs",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_period_id",
                table: "sessions",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_subscription_id",
                table: "sessions",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_swimmers_qr_token",
                table: "swimmers",
                column: "qr_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_period_schedules_period_id_day_of_week",
                table: "training_period_schedules",
                columns: new[] { "period_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_periods_program_id",
                table: "training_periods",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_price_configs_program_id",
                table: "training_price_configs",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_subscription_pauses_subscription_id",
                table: "training_subscription_pauses",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_subscriptions_created_by",
                table: "training_subscriptions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_training_subscriptions_GrantedCreditCreditId",
                table: "training_subscriptions",
                column: "GrantedCreditCreditId");

            migrationBuilder.CreateIndex(
                name: "IX_training_subscriptions_period_id",
                table: "training_subscriptions",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_subscriptions_program_id",
                table: "training_subscriptions",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_subscriptions_renewed_from_subscription_id",
                table: "training_subscriptions",
                column: "renewed_from_subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_subscriptions_swimmer_id",
                table: "training_subscriptions",
                column: "swimmer_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_recorded_by",
                table: "transactions",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_users_created_by_user_id",
                table: "users",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_employee_id",
                table: "users",
                column: "employee_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "administrator_daily_attendances");

            migrationBuilder.DropTable(
                name: "attendances");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "backups");

            migrationBuilder.DropTable(
                name: "cancellation_fee_configs");

            migrationBuilder.DropTable(
                name: "coach_dues");

            migrationBuilder.DropTable(
                name: "credit_usages");

            migrationBuilder.DropTable(
                name: "employee_attendances");

            migrationBuilder.DropTable(
                name: "employee_qualifications");

            migrationBuilder.DropTable(
                name: "employee_replacements");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "package_checkins");

            migrationBuilder.DropTable(
                name: "package_configs");

            migrationBuilder.DropTable(
                name: "package_period_changes");

            migrationBuilder.DropTable(
                name: "package_qr_histories");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "period_staff_assignments");

            migrationBuilder.DropTable(
                name: "private_booking_participants");

            migrationBuilder.DropTable(
                name: "qualification_rate_configs");

            migrationBuilder.DropTable(
                name: "recreational_period_schedules");

            migrationBuilder.DropTable(
                name: "recreational_tickets");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "schedule_defaults_configs");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "training_period_schedules");

            migrationBuilder.DropTable(
                name: "training_price_configs");

            migrationBuilder.DropTable(
                name: "training_subscription_pauses");

            migrationBuilder.DropTable(
                name: "payrolls");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "packages");

            migrationBuilder.DropTable(
                name: "private_bookings");

            migrationBuilder.DropTable(
                name: "qualifications");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "training_subscriptions");

            migrationBuilder.DropTable(
                name: "recreational_periods");

            migrationBuilder.DropTable(
                name: "lanes");

            migrationBuilder.DropTable(
                name: "credits");

            migrationBuilder.DropTable(
                name: "training_periods");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "swimmers");

            migrationBuilder.DropTable(
                name: "programs");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
