namespace SwimClub.Domain.Entities;

/// <summary>
/// Fixed, closed set: SUPER_ADMIN, OWNER, ADMINISTRATOR.
/// No custom roles permitted (Decision 27).
/// </summary>
public class Role
{
    public int RoleId { get; set; }
    public string Code { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string NameAr { get; set; } = null!;

    public ICollection<User> Users { get; set; } = [];
}
