using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Saffron.Data;
using Saffron.Models;
using StackExchange.Redis;

namespace Saffron.Session;

public interface ISharedSessionStore
{
    Task<SessionInfo?> LoadAsync(TenantInfo tenant, string? sid, bool refreshPayload, CancellationToken ct = default);
    Task<string> CreateAsync(TenantInfo tenant, UserRecord user, CancellationToken ct = default);
    Task DestroyAsync(TenantInfo tenant, string? sid, CancellationToken ct = default);
}

public sealed class SharedSessionStore : ISharedSessionStore
{
    public const string CookieName = "SAFFRON_SID";

    private static readonly Regex IdPattern = new("^[a-f0-9]{32,64}$", RegexOptions.CultureInvariant);

    private readonly IConnectionMultiplexer _redis;
    private readonly TimeProvider _clock;
    private readonly int _idleSeconds;

    public SharedSessionStore(IConnectionMultiplexer redis, TimeProvider clock, SaffronSettings settings)
    {
        _redis = redis;
        _clock = clock;
        _idleSeconds = settings.SessionIdleSeconds;
    }

    public static bool IsValidId([NotNullWhen(true)] string? id) => !string.IsNullOrEmpty(id) && IdPattern.IsMatch(id);

    public static string Key(string tenant, string id) => "sess:" + tenant + ":" + id;

    public async Task<SessionInfo?> LoadAsync(TenantInfo tenant, string? sid, bool refreshPayload, CancellationToken ct = default)
    {
        if (!IsValidId(sid))
            return null;

        var db = _redis.GetDatabase();
        var key = Key(tenant.Code, sid);
        var raw = await db.StringGetAsync(key);
        if (raw.IsNullOrEmpty)
            return null;

        SessionInfo? s;
        try
        {
            s = JsonSerializer.Deserialize<SessionInfo>(raw.ToString());
        }
        catch (JsonException)
        {
            return null;
        }

        if (s == null || !string.Equals(s.Tenant, tenant.Code, StringComparison.Ordinal))
            return null;

        s.Id = sid;
        var idle = TimeSpan.FromSeconds(_idleSeconds);
        if (refreshPayload)
        {
            s.LastSeen = _clock.GetUtcNow().ToUnixTimeSeconds();
            await db.StringSetAsync(key, JsonSerializer.Serialize(s), idle);
        }
        else
        {
            await db.KeyExpireAsync(key, idle);
        }
        return s;
    }

    public async Task<string> CreateAsync(TenantInfo tenant, UserRecord user, CancellationToken ct = default)
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(20)).ToLowerInvariant();
        var now = _clock.GetUtcNow().ToUnixTimeSeconds();
        var payload = new SessionInfo
        {
            Tenant = tenant.Code,
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.RoleName,
            Created = now,
            LastSeen = now,
        };
        await _redis.GetDatabase().StringSetAsync(Key(tenant.Code, id), JsonSerializer.Serialize(payload), TimeSpan.FromSeconds(_idleSeconds));
        return id;
    }

    public async Task DestroyAsync(TenantInfo tenant, string? sid, CancellationToken ct = default)
    {
        if (IsValidId(sid))
            await _redis.GetDatabase().KeyDeleteAsync(Key(tenant.Code, sid!));
    }
}
