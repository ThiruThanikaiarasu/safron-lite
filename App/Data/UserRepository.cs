using Saffron.Models;

namespace Saffron.Data;

public interface IUserRepository
{
    Task<UserRecord?> FindByUsernameAsync(TenantInfo tenant, string username, CancellationToken ct = default);
    Task TouchLoginAsync(TenantInfo tenant, int userId, CancellationToken ct = default);
}

public sealed class UserRepository : IUserRepository
{
    private readonly ITenantConnections _connections;

    public UserRepository(ITenantConnections connections)
    {
        _connections = connections;
    }

    public async Task<UserRecord?> FindByUsernameAsync(TenantInfo tenant, string username, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_User_GetByUsername");
        cmd.Parameters.AddWithValue("@Username", username);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;
        return new UserRecord(
            (int)r["UserId"],
            (string)r["Username"],
            (string)r["PasswordHash"],
            (string)r["DisplayName"],
            (string)r["RoleName"],
            (bool)r["IsActive"]);
    }

    public async Task TouchLoginAsync(TenantInfo tenant, int userId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_User_TouchLogin");
        cmd.Parameters.AddWithValue("@UserId", userId);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
