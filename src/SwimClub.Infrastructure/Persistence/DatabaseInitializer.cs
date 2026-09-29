using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SwimClub.Infrastructure.Persistence;

/// <summary>
/// Initializes the SQLite database: applies pending migrations and ensures the DB exists.
/// Configured with WAL mode and foreign key enforcement.
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Applying EF Core migrations...");
            await _context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
