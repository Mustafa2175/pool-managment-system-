using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SwimClub.Application.Security;
using SwimClub.Infrastructure.Logging;
using SwimClub.Infrastructure.Persistence;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class AuditLogImmutabilityTests : IDisposable
{
    private readonly AppDbContext _db;

    public AuditLogImmutabilityTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;
            
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task AuditLogService_WritesLog_Successfully()
    {
        var currentUserMock = new Mock<ICurrentUserService>();
        var auditService = new AuditLogService(_db, currentUserMock.Object);

        await auditService.LogSystemEventAsync("TEST_EVENT", true, "Testing audit");

        var log = await _db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("TEST_EVENT", log.EventType);
    }

    public void Dispose()
    {
        _db.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }
}
