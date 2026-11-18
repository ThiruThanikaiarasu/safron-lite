using Saffron.Data;
using Saffron.Models;

namespace Saffron.Services;

public sealed record PatientListItem(int PatientId, string Mrn, string Name, string Unit, string Bed);

public sealed record PatientDetail(
    int PatientId,
    string Mrn,
    string FirstName,
    string LastName,
    string DateOfBirth,
    string Unit,
    string UnitName,
    string Bed,
    string Hospital,
    string? LastReadingUtc);

public sealed record OrderSummary(
    int OrderId,
    string Status,
    decimal BaseRate,
    decimal Multiplier,
    int TargetValue,
    decimal Divisor,
    string? OrderedAtUtc,
    string? OrderedBy,
    string? DiscontinuedAtUtc);

public sealed record RecentSummary(int Count, decimal? Mean);

public sealed record ReadingDto(int ReadingId, int Value, string? TakenAtUtc, decimal? Rate, bool IsLow, string? EnteredBy, string? Comment);

public sealed record PatientSummary(PatientDetail Patient, OrderSummary? Order, RecentSummary Recent, int? MinutesSinceLastReading);

public sealed class PatientService
{
    public const int RecentCount = 6;

    private readonly IPatientRepository _patients;
    private readonly IOrderRepository _orders;
    private readonly IReadingRepository _readings;

    public PatientService(IPatientRepository patients, IOrderRepository orders, IReadingRepository readings)
    {
        _patients = patients;
        _orders = orders;
        _readings = readings;
    }

    public async Task<IReadOnlyList<PatientListItem>> ListAsync(TenantInfo tenant, CancellationToken ct = default)
    {
        var rows = await _patients.ListActiveAsync(tenant, ct);
        return rows.Select(r => new PatientListItem(r.PatientId, r.Mrn, PhpCompat.PatientName(r.LastName, r.FirstName), r.UnitCode, r.Bed)).ToList();
    }

    public Task<PatientRecord?> FindAsync(TenantInfo tenant, int patientId, CancellationToken ct = default) =>
        _patients.GetAsync(tenant, patientId, ct);

    public async Task<PatientSummary?> GetSummaryAsync(TenantInfo tenant, int patientId, CancellationToken ct = default)
    {
        var p = await _patients.GetAsync(tenant, patientId, ct);
        if (p == null)
            return null;

        var latest = await _orders.GetLatestAsync(tenant, patientId, ct);
        var values = await _readings.RecentValuesAsync(tenant, patientId, RecentCount, ct);

        decimal? mean = null;
        if (values.Count > 0)
            mean = Math.Round(values.Average(v => (decimal)v));

        var detail = new PatientDetail(
            p.PatientId,
            p.Mrn,
            p.FirstName,
            p.LastName,
            p.DateOfBirth.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            p.UnitCode,
            p.UnitName,
            p.Bed,
            p.HospitalName,
            FacilityTime.Iso(p.LastReadingUtc));

        var order = latest == null ? null : new OrderSummary(
            latest.OrderId,
            latest.Status,
            latest.BaseRate,
            latest.Multiplier,
            latest.TargetValue,
            latest.Divisor,
            FacilityTime.Iso(latest.OrderedAtUtc),
            latest.OrderedByName,
            FacilityTime.Iso(latest.DiscontinuedAtUtc));

        return new PatientSummary(detail, order, new RecentSummary(values.Count, mean), p.MinutesSinceLastReading);
    }

    public async Task<IReadOnlyList<ReadingDto>> ReadingsAsync(TenantInfo tenant, int patientId, int top, CancellationToken ct = default)
    {
        var rows = await _readings.HistoryAsync(tenant, patientId, top, ct);
        return rows.Select(r => new ReadingDto(
            r.ReadingId,
            r.Value,
            FacilityTime.Iso(r.TakenAtUtc),
            r.Rate,
            r.IsLow,
            r.EnteredByName,
            r.Comment)).ToList();
    }
}
