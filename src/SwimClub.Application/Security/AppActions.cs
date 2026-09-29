namespace SwimClub.Application.Security;

/// <summary>
/// Constants representing all authorizable actions in the system.
/// Maps directly to the authorization matrix in SRS §3.
/// </summary>
public static class AppActions
{
    // Authentication & Core
    public const string LOGIN = "LOGIN";

    // Swimmers
    public const string CREATE_SWIMMER = "CREATE_SWIMMER";
    public const string EDIT_SWIMMER = "EDIT_SWIMMER";
    public const string DELETE_SWIMMER = "DELETE_SWIMMER";
    public const string VIEW_SWIMMER = "VIEW_SWIMMER";

    // Training
    public const string CREATE_TRAINING_PERIOD = "CREATE_TRAINING_PERIOD";
    public const string EDIT_TRAINING_PERIOD = "EDIT_TRAINING_PERIOD";
    public const string ASSIGN_COACH = "ASSIGN_COACH";
    public const string ASSIGN_LIFEGUARD = "ASSIGN_LIFEGUARD";

    // Employees
    public const string VIEW_EMPLOYEE = "VIEW_EMPLOYEE";
    public const string EDIT_EMPLOYEE = "EDIT_EMPLOYEE";
    
    // Financial
    public const string RECORD_PAYMENT = "RECORD_PAYMENT";
    public const string RECORD_EXPENSE = "RECORD_EXPENSE";
    public const string MARK_PAYROLL_PAID = "MARK_PAYROLL_PAID";
    public const string CORRECT_PAYMENT = "CORRECT_PAYMENT";
    public const string ADJUST_PAYROLL = "ADJUST_PAYROLL";
    public const string ADJUST_REFUND = "ADJUST_REFUND";
    
    // Admin & Auth
    public const string RESET_PASSWORD = "RESET_PASSWORD";
    public const string REACTIVATE_USER = "REACTIVATE_USER";
    public const string MANAGE_USERS = "MANAGE_USERS";
    public const string EDIT_GENERAL_CONFIGURATION = "EDIT_GENERAL_CONFIGURATION";
    public const string EDIT_PRICING_RATES = "EDIT_PRICING_RATES";
    public const string MANAGE_QUALIFICATIONS_AND_RATES = "MANAGE_QUALIFICATIONS_AND_RATES";
    public const string EDIT_CANCELLATION_FEE = "EDIT_CANCELLATION_FEE";
    public const string BACKUP_RESTORE = "BACKUP_RESTORE";
    public const string VIEW_AUDIT_LOG = "VIEW_AUDIT_LOG";
    public const string MANAGE_LANES = "MANAGE_LANES";
}
