using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Persistence;
using SwimClub.Infrastructure.Security;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class AuthServiceTests
{
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;
            
        var db = new AppDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        using var db = GetDbContext();
        var user = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = true,
            Role = new Role { Code = "ADMINISTRATOR", NameEn = "Admin", NameAr = "Admin" }
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        var auditMock = new Mock<IAuditLogService>();
        var authService = new AuthService(db, currentUserMock.Object, auditMock.Object);

        var result = await authService.LoginAsync("admin", "password123");

        Assert.Equal(LoginResult.Success, result);
        currentUserMock.Verify(c => c.SetCurrentUser(It.Is<User>(u => u.Username == "admin")), Times.Once);
        auditMock.Verify(a => a.LogSuccessAsync("LOGIN_SUCCESS", "User", user.UserId, null), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsInvalidCredentials()
    {
        using var db = GetDbContext();
        var user = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = true,
            Role = new Role { Code = "ADMINISTRATOR", NameEn = "Admin", NameAr = "Admin" }
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        var auditMock = new Mock<IAuditLogService>();
        var authService = new AuthService(db, currentUserMock.Object, auditMock.Object);

        var result = await authService.LoginAsync("admin", "wrongpass");

        Assert.Equal(LoginResult.InvalidCredentials, result);
        currentUserMock.Verify(c => c.SetCurrentUser(It.IsAny<User>()), Times.Never);
        auditMock.Verify(a => a.LogSystemEventAsync("LOGIN_FAILED", false, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedUser_ReturnsDeactivated()
    {
        using var db = GetDbContext();
        var user = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = false,
            Role = new Role { Code = "ADMINISTRATOR", NameEn = "Admin", NameAr = "Admin" }
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        var auditMock = new Mock<IAuditLogService>();
        var authService = new AuthService(db, currentUserMock.Object, auditMock.Object);

        var result = await authService.LoginAsync("admin", "password123");

        Assert.Equal(LoginResult.Deactivated, result);
    }
}
