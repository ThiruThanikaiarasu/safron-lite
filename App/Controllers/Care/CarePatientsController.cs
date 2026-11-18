using Microsoft.AspNetCore.Mvc;
using Saffron.Models;
using Saffron.Services;
using Saffron.Session;

namespace Saffron.Controllers.Care;

public sealed class CarePatientsController : CareControllerBase
{
    private const int HistoryRows = 20;
    private const int AverageCount = 6;

    private readonly PatientService _patients;

    public CarePatientsController(TenantResolver tenants, ISharedSessionStore sessions, PatientService patients)
        : base(tenants, sessions)
    {
        _patients = patients;
    }

    [Route("care")]
    [Route("care/patients")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await _patients.ListAsync(Tenant!, ct);
        ViewData["Title"] = "Patients";
        return View("~/Views/Care/PatientList.cshtml", rows);
    }

    [Route("care/patients/{id:int}")]
    public async Task<IActionResult> Detail(int id, string? saved, CancellationToken ct)
    {
        var patient = await _patients.FindAsync(Tenant!, id, ct);
        if (patient == null)
            return NotFoundPage();

        var summary = await _patients.GetSummaryAsync(Tenant!, id, ct);
        var history = await _patients.ReadingsAsync(Tenant!, id, HistoryRows, ct);
        var average = PhpCompat.RecentAverage(history.Select(h => h.Value), AverageCount);

        ViewData["Title"] = PhpCompat.PatientName(patient.LastName, patient.FirstName);
        ViewData["Scripts"] = new[] { "patient-detail.js" };

        var savedId = PhpCompat.IsTruthy(saved) ? PhpCompat.IntCast(saved) : (int?)null;
        return View("~/Views/Care/PatientDetail.cshtml",
            new PatientDetailViewModel(patient, summary, history, average, IsAdmin, savedId));
    }
}
