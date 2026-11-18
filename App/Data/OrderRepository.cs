using Saffron.Models;
using Saffron.Services;

namespace Saffron.Data;

public interface IOrderRepository
{
    Task<OrderRecord?> GetActiveAsync(TenantInfo tenant, int patientId, CancellationToken ct = default);
    Task<OrderRecord?> GetLatestAsync(TenantInfo tenant, int patientId, CancellationToken ct = default);
    Task<int> DiscontinueAsync(TenantInfo tenant, int orderId, int userId, CancellationToken ct = default);
    Task<int> ResumeAsync(TenantInfo tenant, int orderId, int userId, CancellationToken ct = default);
}

public sealed class OrderRepository : IOrderRepository
{
    private readonly ITenantConnections _connections;

    public OrderRepository(ITenantConnections connections)
    {
        _connections = connections;
    }

    public async Task<OrderRecord?> GetActiveAsync(TenantInfo tenant, int patientId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_Order_GetActive");
        cmd.Parameters.AddWithValue("@PatientId", patientId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;
        return new OrderRecord(
            r.GetInt32(r.GetOrdinal("OrderId")),
            r.GetInt32(r.GetOrdinal("PatientId")),
            r.GetString(r.GetOrdinal("Status")),
            r.GetDecimal(r.GetOrdinal("BaseRate")),
            r.GetDecimal(r.GetOrdinal("Multiplier")),
            r.GetInt32(r.GetOrdinal("TargetValue")),
            r.GetDecimal(r.GetOrdinal("Divisor")),
            FacilityTime.Utc(r.GetDateTime(r.GetOrdinal("OrderedAtUtc"))),
            null,
            null);
    }

    public async Task<OrderRecord?> GetLatestAsync(TenantInfo tenant, int patientId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_Order_GetLatest");
        cmd.Parameters.AddWithValue("@PatientId", patientId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;
        var discontinued = Sql.NullIfDbNull(r["DiscontinuedAtUtc"]) as DateTime?;
        return new OrderRecord(
            (int)r["OrderId"],
            (int)r["PatientId"],
            (string)r["Status"],
            (decimal)r["BaseRate"],
            (decimal)r["Multiplier"],
            (int)r["TargetValue"],
            (decimal)r["Divisor"],
            FacilityTime.Utc((DateTime)r["OrderedAtUtc"]),
            discontinued.HasValue ? FacilityTime.Utc(discontinued.Value) : null,
            r["OrderedByName"] as string);
    }

    public async Task<int> DiscontinueAsync(TenantInfo tenant, int orderId, int userId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_Order_Discontinue");
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<int> ResumeAsync(TenantInfo tenant, int orderId, int userId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_Order_Resume");
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }
}
