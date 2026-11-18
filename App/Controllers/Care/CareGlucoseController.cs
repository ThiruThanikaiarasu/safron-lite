using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Saffron.Models;
using Saffron.Services;
using Saffron.Session;

namespace Saffron.Controllers.Care;

public sealed class CareGlucoseController : CareControllerBase
{
    private readonly PatientService _patients;
    private readonly GlucoseService _glucose;
    private readonly TimeProvider _clock;

    public CareGlucoseController(TenantResolver tenants, ISharedSessionStore sessions, PatientService patients, GlucoseService glucose, TimeProvider clock)
        : base(tenants, sessions)
    {
        _patients = patients;
        _glucose = glucose;
        _clock = clock;
    }

    [Route("care/patients/{id:int}/glucose")]
    public async Task<IActionResult> Entry(int id, CancellationToken ct)
    {
        var patient = await _patients.FindAsync(Tenant!, id, ct);
        if (patient == null)
            return NotFoundPage();

        return Render(patient, null, null, Array.Empty<string>(), GlucoseForm.Empty);
    }

    [Route("care/glucose/preview/{id:int}")]
    public async Task<IActionResult> Preview(int id, CancellationToken ct)
    {
        var patient = await _patients.FindAsync(Tenant!, id, ct);
        if (patient == null)
            return NotFoundPage();
        if (!HttpMethods.IsPost(Request.Method))
            return Redirect($"/care/patients/{id}/glucose");

        var form = await Request.ReadFormAsync(ct);
        var errors = GlucoseFormValidation.Validate(form, saving: false);
        if (errors.Count > 0)
            return Render(patient, null, null, errors, FormFrom(form));

        var result = await _glucose.EvaluateAsync(Tenant!, BuildRequest(id, form), ct);
        if (!result.Ok)
            return Render(patient, null, result.Error, Array.Empty<string>(), FormFrom(form));

        return Render(patient, BuildRecommendation(result.Data!), null, Array.Empty<string>(), FormFrom(form));
    }

    [Route("care/glucose/save/{id:int}")]
    public async Task<IActionResult> Save(int id, CancellationToken ct)
    {
        var patient = await _patients.FindAsync(Tenant!, id, ct);
        if (patient == null)
            return NotFoundPage();
        if (!HttpMethods.IsPost(Request.Method))
            return Redirect($"/care/patients/{id}/glucose");

        var form = await Request.ReadFormAsync(ct);
        var errors = GlucoseFormValidation.Validate(form, saving: true);
        if (errors.Count > 0)
            return Render(patient, null, null, errors, FormFrom(form));

        var result = await _glucose.SaveAsync(Tenant!, BuildRequest(id, form), CurrentSession!.UserId, ct);
        if (!result.Ok)
            return Render(patient, null, result.Error ?? "Could not save the reading.", Array.Empty<string>(), FormFrom(form));

        return Redirect($"/care/patients/{id}?saved={result.Data!.ReadingId}");
    }

    private ViewResult Render(PatientRecord patient, RecommendationViewModel? rec, string? apiError, IReadOnlyList<string> errors, GlucoseForm form)
    {
        ViewData["Title"] = "Glucose entry";
        ViewData["Scripts"] = new[] { "glucose-entry.js" };
        return View("~/Views/Care/GlucoseEntry.cshtml", new GlucoseEntryViewModel(patient, rec, apiError, errors, form));
    }

    private RecommendationViewModel BuildRecommendation(EvaluationDto dto)
    {
        var rate = Math.Min((double)dto.ComputedRate, 15);
        var nextDue = FacilityTime.ParseIso(dto.NextCheckDueUtc) ?? _clock.GetUtcNow().UtcDateTime;
        var nextLocal = FacilityTime.ToFacilityLocal(nextDue, Tenant!).ToString("HH:mm", CultureInfo.InvariantCulture);
        return new RecommendationViewModel(PhpCompat.FormatOneDecimal(rate), dto.LowGlucose, dto.Messages, nextLocal);
    }

    private static GlucoseRequest BuildRequest(int patientId, IFormCollection form)
    {
        var time = PhpCompat.Trim(GlucoseFormValidation.Raw(form, "entered_time"));
        var comment = PhpCompat.Trim(GlucoseFormValidation.Raw(form, "comment"));
        return new GlucoseRequest
        {
            PatientId = patientId,
            Value = PhpCompat.IntCast(GlucoseFormValidation.Raw(form, "value")),
            EnteredTime = time == "" ? null : time,
            Comment = comment == "" ? null : comment,
        };
    }

    private static GlucoseForm FormFrom(IFormCollection form) => new(
        GlucoseFormValidation.Raw(form, "value"),
        GlucoseFormValidation.Raw(form, "confirm_value"),
        GlucoseFormValidation.Raw(form, "entered_time"),
        GlucoseFormValidation.Raw(form, "comment"),
        GlucoseFormValidation.Raw(form, "ack_low") == "1");
}
