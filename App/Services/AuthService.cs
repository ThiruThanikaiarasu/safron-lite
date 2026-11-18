using Saffron.Data;
using Saffron.Models;

namespace Saffron.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;

    public AuthService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<UserRecord?> AuthenticateAsync(TenantInfo tenant, string username, string password, CancellationToken ct = default)
    {
        if (username == "")
            return null;

        var user = await _users.FindByUsernameAsync(tenant, username, ct);
        if (user == null || !user.IsActive)
            return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        await _users.TouchLoginAsync(tenant, user.UserId, ct);
        return user;
    }
}
