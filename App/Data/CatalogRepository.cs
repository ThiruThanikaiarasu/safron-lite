using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Saffron.Models;

namespace Saffron.Data;

public interface ICatalogRepository
{
    Task<TenantInfo?> FindActiveTenantAsync(string code, CancellationToken ct = default);
}

public sealed class CatalogRepository : ICatalogRepository
{
    private static readonly ConcurrentDictionary<string, TenantInfo> Cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _catalog;

    public CatalogRepository(SaffronSettings settings)
    {
        _catalog = settings.CatalogConnectionString;
    }

    public async Task<TenantInfo?> FindActiveTenantAsync(string code, CancellationToken ct = default)
    {
        if (Cache.TryGetValue(code, out var cached))
            return cached;

        await using var conn = new SqlConnection(_catalog);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TenantCode, DisplayName, DbName, FacilityTimeZone FROM dbo.Tenant WHERE TenantCode = @code AND IsActive = 1";
        cmd.Parameters.AddWithValue("@code", code);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;

        var tenant = new TenantInfo(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3));
        Cache[code] = tenant;
        return tenant;
    }
}
