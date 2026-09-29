using SwimClub.Application.Security;
using SwimClub.Domain.Entities;

namespace SwimClub.Infrastructure.Security;

public class CurrentUserService : ICurrentUserService
{
    private User? _currentUser;

    public User? CurrentUser => _currentUser;

    public bool IsAuthenticated => _currentUser != null;

    public void SetCurrentUser(User user)
    {
        _currentUser = user;
    }

    public void Clear()
    {
        _currentUser = null;
    }
}
