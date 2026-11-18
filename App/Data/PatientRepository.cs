using Microsoft.Data.SqlClient;
using Saffron.Models;

namespace Saffron.Data;

public interface IPatientRepository
{
    Task<IReadOnlyList<PatientListRow>> ListActiveAsync(TenantInfo tenant, CancellationToken ct = default);
    Task<PatientRecord?> GetAsync(TenantInfo tenant, int patientId, CancellationToken ct = default);
    Task<bool> IsActiveAsync(TenantInfo tenant, int patientId, CancellationToken ct = default);
}

public sealed class PatientRepository : IPatientRepository
{
    private readonly ITenantConnections _connections;

    public PatientRepository(ITenantConnections connections)
    {
        _connections = connections;
    }

    public async Task<IReadOnlyList<PatientListRow>> ListActiveAsync(TenantInfo tenant, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_Patient_List");
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var rows = new List<PatientListRow>();
        while (await r.ReadAsync(ct))
        {
            rows.Add(new PatientListRow(
                (int)r["PatientId"],
                (string)r["Mrn"],
                (string)r["FirstName"],
                (string)r["LastName"],
                (string)r["Bed"],
                (string)r["UnitCode"],
                (string)r["UnitName"]));
        }
        return rows;
    }

    public async Task<PatientRecord?> GetAsync(TenantInfo tenant, int patientId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = Sql.Proc(conn, "dbo.usp_Patient_Get");
        cmd.Parameters.AddWithValue("@PatientId", patientId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;

        var lastReading = Sql.NullIfDbNull(r["LastReadingUtc"]) as DateTime?;
        var minutes = Sql.NullIfDbNull(r["MinutesSinceLastReading"]) as int?;
        return new PatientRecord(
            (int)r["PatientId"],
            (string)r["Mrn"],
            (string)r["FirstName"],
            (string)r["LastName"],
            (DateTime)r["DateOfBirth"],
            (string)r["Bed"],
            (string)r["UnitCode"],
            (string)r["UnitName"],
            (string)r["HospitalName"],
            lastReading,
            minutes);
    }

    public async Task<bool> IsActiveAsync(TenantInfo tenant, int patientId, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.Patient WHERE PatientId = @id AND IsActive = 1";
        cmd.Parameters.AddWithValue("@id", patientId);
        return (int)(await cmd.ExecuteScalarAsync(ct))! > 0;
    }
}
