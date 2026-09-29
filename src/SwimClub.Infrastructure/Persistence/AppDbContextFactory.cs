using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SwimClub.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations tooling (dotnet ef migrations add, etc.).
/// Uses a local development database file.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=swimclub-dev.db")
            .Options;

        return new AppDbContext(options);
    }
}
