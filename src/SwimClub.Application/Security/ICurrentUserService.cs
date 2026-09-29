using System;
using SwimClub.Domain.Entities;

namespace SwimClub.Application.Security;

public interface ICurrentUserService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    void SetCurrentUser(User user);
    void Clear();
}
