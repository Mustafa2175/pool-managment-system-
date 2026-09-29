namespace SwimClub.Domain.Entities;

/// <summary>
/// Login identity. Linked to an Employee only for ADMINISTRATOR role (Decision 12).
/// OWNER and SUPER_ADMIN never have an Employee record.
/// </summary>
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public int RoleId { get; set; }
    public int? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public Employee? Employee { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<User> CreatedUsers { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
