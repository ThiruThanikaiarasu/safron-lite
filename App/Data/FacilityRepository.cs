using Saffron.Models;

namespace Saffron.Data;

public sealed record FacilityStatus(int Patients, string? Hospital);

public interface IFacilityRepository
{
    Task<FacilityStatus> GetStatusAsync(TenantInfo tenant, CancellationToken ct = default);
}

public sealed class FacilityRepository : IFacilityRepository
{
    private readonly ITenantConnections _connections;

    public FacilityRepository(ITenantConnections connections)
    {
        _connections = connections;
    }

    public async Task<FacilityStatus> GetStatusAsync(TenantInfo tenant, CancellationToken ct = default)
    {
        await using var conn = await _connections.OpenAsync(tenant, ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT (SELECT COUNT(*) FROM dbo.Patient WHERE IsActive = 1), (SELECT TOP 1 Name FROM dbo.Hospital ORDER BY HospitalId)";
        await using var r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        var patients = r.GetInt32(0);
        var hospital = r.IsDBNull(1) ? null : r.GetString(1);
        return new FacilityStatus(patients, hospital);
    }
}
