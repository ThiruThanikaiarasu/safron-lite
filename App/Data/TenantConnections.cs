using Microsoft.Data.SqlClient;
using Saffron.Models;

namespace Saffron.Data;

public interface ITenantConnections
{
    Task<SqlConnection> OpenAsync(TenantInfo tenant, CancellationToken ct = default);
}

public sealed class TenantConnections : ITenantConnections
{
    private readonly string _catalog;

    public TenantConnections(SaffronSettings settings)
    {
        _catalog = settings.CatalogConnectionString;
    }

    public async Task<SqlConnection> OpenAsync(TenantInfo tenant, CancellationToken ct = default)
    {
        var builder = new SqlConnectionStringBuilder(_catalog) { InitialCatalog = tenant.DbName };
        var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
