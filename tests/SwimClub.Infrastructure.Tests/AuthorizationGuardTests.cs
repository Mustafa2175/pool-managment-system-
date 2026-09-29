using System;
using Moq;
using SwimClub.Application.Interfaces;
using SwimClub.Application.Security;
using SwimClub.Domain.Entities;
using SwimClub.Infrastructure.Security;
using Xunit;

namespace SwimClub.Infrastructure.Tests;

public class AuthorizationGuardTests
{
    [Fact]
    public void Authorize_UserWithPermission_DoesNotThrow()
    {
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(c => c.CurrentUser).Returns(new User 
        { 
            Role = new Role { Code = "SUPER_ADMIN", NameEn = "SA", NameAr = "SA" } 
        });

        var auditMock = new Mock<IAuditLogService>();
        var guard = new AuthorizationGuard(currentUserMock.Object, auditMock.Object);

        var exception = Record.Exception(() => guard.Authorize(AppActions.CREATE_SWIMMER));
        Assert.Null(exception);
    }

    [Fact]
    public void Authorize_UserWithoutPermission_ThrowsUnauthorized_AndLogs()
    {
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(c => c.CurrentUser).Returns(new User 
        { 
            Role = new Role { Code = "OWNER", NameEn = "OW", NameAr = "OW" } 
        });

        var auditMock = new Mock<IAuditLogService>();
        var guard = new AuthorizationGuard(currentUserMock.Object, auditMock.Object);

        Assert.Throws<UnauthorizedAccessException>(() => guard.Authorize(AppActions.CREATE_SWIMMER));
        auditMock.Verify(a => a.LogFailureAsync("UNAUTHORIZED_ACTION_ATTEMPT", It.IsAny<string>(), null, null), Times.Once);
    }
}
