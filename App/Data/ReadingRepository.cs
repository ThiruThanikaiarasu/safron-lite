using Microsoft.Data.SqlClient;
using Saffron.Models;
using Saffron.Services;

namespace Saffron.Data;

public interface IReadingRepository
{
    Task<IReadOnlyList<ReadingRow>> HistoryAsync(TenantInfo tenant, int patientId, int top, CancellationToken ct = default);
    Task<IReadOnlyList<int>> RecentValuesAsync(TenantInfo tenant, int patientId, int count, CancellationToken ct = default);
    Task<DateTime?> LastCorrectionUtcAsync(TenantInfo tenant, int patientId, CancellationToken ct = default);
    Task<int> InsertAsync(TenantInfo tenant, int patientId, int value, DateTime takenAtUtc, decimal rate, int enteredBy, string? comment, CancellationToken ct = default);
    Task<(decimal? Rate, bool IsLow)> GetStoredAsync(TenantInfo tenant, int readingId, CancellationToken ct = default);
}

public sealed class ReadingRepository : IReadingRepository
{
    private readonly ITenantConnections _connections;

    public ReadingRepository(ITenantConnections connections)
    {
        _connections = connections;
    }

    public async Task<IReadOnlyList<ReadingRow>> HistoryAsync(TenantInfo tenant, int patientId, int top, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_Reading_History");
        cmd.Parameters.AddWithValue("@PatientId", patientId);
        cmd.Parameters.AddWithValue("@Top", top);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var rows = new List<ReadingRow>();
        while (await r.ReadAsync(ct))
        {
            rows.Add(new ReadingRow(
                (int)r["ReadingId"],
                (int)r["Value"],
                (DateTime)r["TakenAtUtc"],
                Sql.NullIfDbNull(r["Rate"]) as decimal?,
                (bool)r["IsLow"],
                Sql.NullIfDbNull(r["EnteredByName"]) as string,
                Sql.NullIfDbNull(r["Comment"]) as string));
        }
        return rows;
    }

    public async Task<IReadOnlyList<int>> RecentValuesAsync(TenantInfo tenant, int patientId, int count, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TOP (@n) Value FROM dbo.GlucoseReading WHERE PatientId = @id ORDER BY TakenAtUtc DESC, ReadingId DESC";
        cmd.Parameters.AddWithValue("@n", count);
        cmd.Parameters.AddWithValue("@id", patientId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var values = new List<int>();
        while (await r.ReadAsync(ct))
            values.Add(r.GetInt32(0));
        return values;
    }

    public async Task<DateTime?> LastCorrectionUtcAsync(TenantInfo tenant, int patientId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAX(TakenAtUtc) FROM dbo.GlucoseReading WHERE PatientId = @id AND Rate > 0";
        cmd.Parameters.AddWithValue("@id", patientId);
        var o = await cmd.ExecuteScalarAsync(ct);
        if (o == null || o == DBNull.Value)
            return null;
        return FacilityTime.Utc((DateTime)o);
    }

    public async Task<int> InsertAsync(TenantInfo tenant, int patientId, int value, DateTime takenAtUtc, decimal rate, int enteredBy, string? comment, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_Reading_Insert");
        cmd.Parameters.AddWithValue("@PatientId", patientId);
        cmd.Parameters.AddWithValue("@Value", value);
        cmd.Parameters.AddWithValue("@TakenAtUtc", takenAtUtc);
        cmd.Parameters.AddWithValue("@Rate", rate);
        cmd.Parameters.AddWithValue("@EnteredBy", enteredBy);
        cmd.Parameters.AddWithValue("@Comment", (object?)comment ?? DBNull.Value);
        var outId = cmd.Parameters.Add("@ReadingId", System.Data.SqlDbType.Int);
        outId.Direction = System.Data.ParameterDirection.Output;
        await cmd.ExecuteNonQueryAsync(ct);
        return (int)outId.Value!;
    }

    public async Task<(decimal? Rate, bool IsLow)> GetStoredAsync(TenantInfo tenant, int readingId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Rate, IsLow FROM dbo.GlucoseReading WHERE ReadingId = @id";
        cmd.Parameters.AddWithValue("@id", readingId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        return (Sql.NullIfDbNull(r["Rate"]) as decimal?, (bool)r["IsLow"]);
    }
}
