using Microsoft.Data.SqlClient;
using Saffron.Data;
using Saffron.Models;

namespace Saffron.Services;

public sealed class Evaluation
{
    public bool Low;
    public decimal Rate;
    public DateTime TakenAtUtc;
    public DateTime? LastCorrectionUtc;
    public DateTime? NextCheckDueUtc;
    public List<string> Messages = new();
}

public sealed record EvaluationDto(
    int PatientId,
    int Value,
    string? TakenAtUtc,
    bool LowGlucose,
    decimal ComputedRate,
    IReadOnlyList<string> Messages,
    string? LastCorrectionUtc,
    string? NextCheckDueUtc);

public sealed record SaveDto(
    int ReadingId,
    int PatientId,
    int Value,
    string? TakenAtUtc,
    bool LowGlucose,
    decimal ComputedRate,
    decimal? StoredRate,
    bool IsLow,
    IReadOnlyList<string> Messages);

public sealed record EvaluateResult<T>(bool Ok, string? Error, T? Data) where T : class;

public sealed class GlucoseService
{
    private readonly IPatientRepository _patients;
    private readonly IOrderRepository _orders;
    private readonly IReadingRepository _readings;
    private readonly TimeProvider _clock;
    private readonly ILogger<GlucoseService> _log;

    public GlucoseService(IPatientRepository patients, IOrderRepository orders, IReadingRepository readings, TimeProvider clock, ILogger<GlucoseService> log)
    {
        _patients = patients;
        _orders = orders;
        _readings = readings;
        _clock = clock;
        _log = log;
    }

    public async Task<EvaluateResult<EvaluationDto>> EvaluateAsync(TenantInfo tenant, GlucoseRequest req, CancellationToken ct = default)
    {
        var (ev, error) = await PrepareAsync(tenant, req, ct);
        if (ev == null)
            return new EvaluateResult<EvaluationDto>(false, error, null);

        return new EvaluateResult<EvaluationDto>(true, null, new EvaluationDto(
            req.PatientId,
            req.Value!.Value,
            FacilityTime.Iso(ev.TakenAtUtc),
            ev.Low,
            ev.Rate,
            ev.Messages,
            FacilityTime.Iso(ev.LastCorrectionUtc),
            FacilityTime.Iso(ev.NextCheckDueUtc)));
    }

    public async Task<EvaluateResult<SaveDto>> SaveAsync(TenantInfo tenant, GlucoseRequest req, int enteredBy, CancellationToken ct = default)
    {
        var (ev, error) = await PrepareAsync(tenant, req, ct);
        if (ev == null)
            return new EvaluateResult<SaveDto>(false, error, null);

        int readingId;
        try
        {
            readingId = await _readings.InsertAsync(tenant, req.PatientId, req.Value!.Value, ev.TakenAtUtc, ev.Rate, enteredBy, req.Comment, ct);
        }
        catch (SqlException ex)
        {
            return new EvaluateResult<SaveDto>(false, ex.Message, null);
        }

        var stored = await _readings.GetStoredAsync(tenant, readingId, ct);
        return new EvaluateResult<SaveDto>(true, null, new SaveDto(
            readingId,
            req.PatientId,
            req.Value.Value,
            FacilityTime.Iso(ev.TakenAtUtc),
            ev.Low,
            ev.Rate,
            stored.Rate,
            stored.IsLow,
            ev.Messages));
    }

    private async Task<(Evaluation? Evaluation, string? Error)> PrepareAsync(TenantInfo tenant, GlucoseRequest req, CancellationToken ct)
    {
        if (req.Value == null)
            return (null, "Value is required.");

        int value = req.Value.Value;
        if (!GlucoseRules.InRange(value))
            return (null, "Value must be between " + GlucoseRules.MinValue + " and " + GlucoseRules.MaxValue + ".");

        if (!await _patients.IsActiveAsync(tenant, req.PatientId, ct))
            return (null, "Patient not found.");

        var now = _clock.GetUtcNow().UtcDateTime;
        var ev = new Evaluation { TakenAtUtc = now };

        if (!string.IsNullOrWhiteSpace(req.EnteredTime))
        {
            var t = FacilityTime.FacilityTimeToUtc(req.EnteredTime, tenant, now);
            if (t == null)
                return (null, "Time must be entered as HH:MM.");
            if (t.Value > now.AddMinutes(FacilityTime.FutureToleranceMinutes))
                return (null, "Entered time is in the future.");
            ev.TakenAtUtc = t.Value;
        }

        ev.Low = GlucoseRules.IsLow(value);
        if (ev.Low)
        {
            ev.Rate = 0m;
            ev.Messages.Add("Low glucose. Hold the infusion and follow the low glucose procedure.");
        }
        else
        {
            ev.Rate = await RateForAsync(tenant, req.PatientId, value, ct);
        }

        if (GlucoseRules.NeedsRepeat(value))
            ev.Messages.Add("High value. Repeat the measurement within 15 minutes.");

        ev.LastCorrectionUtc = await _readings.LastCorrectionUtcAsync(tenant, req.PatientId, ct);
        if (ev.LastCorrectionUtc.HasValue)
        {
            ev.NextCheckDueUtc = ev.LastCorrectionUtc.Value.AddMinutes(FacilityTime.CorrectionIntervalMinutes);
            if (now < ev.NextCheckDueUtc.Value)
            {
                int mins = (int)Math.Ceiling((ev.NextCheckDueUtc.Value - now).TotalMinutes);
                ev.Messages.Add("Correction interval not elapsed. Next check due in " + mins + " minutes.");
            }
        }

        return (ev, null);
    }

    private async Task<decimal> RateForAsync(TenantInfo tenant, int patientId, int value, CancellationToken ct)
    {
        try
        {
            var order = await _orders.GetActiveAsync(tenant, patientId, ct);
            if (order == null)
                throw new InvalidOperationException("No active order for patient " + patientId);
            var ro = new RateOrder(order.BaseRate, order.Multiplier, order.TargetValue, order.Divisor);
            return RateCalculator.ComputeRate(value, ro);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogWarning(ex, "Rate calculation failed for patient {PatientId}", patientId);
            return 0m;
        }
    }
}
