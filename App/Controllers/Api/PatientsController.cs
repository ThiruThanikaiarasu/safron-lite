using Microsoft.AspNetCore.Mvc;
using Saffron.Data;
using Saffron.Services;
using Saffron.Session;

namespace Saffron.Controllers.Api;

[ApiController]
[Route("api/patients")]
public sealed class PatientsController : SaffronApiControllerBase
{
    private readonly PatientService _patients;

    public PatientsController(TenantResolver tenants, ISharedSessionStore sessions, PatientService patients)
        : base(tenants, sessions)
    {
        _patients = patients;
    }

    [HttpGet("")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var fail = await GuardAsync();
        if (fail != null) return fail;

        return Ok(await _patients.ListAsync(Tenant!, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Summary(int id, CancellationToken ct)
    {
        var fail = await GuardAsync();
        if (fail != null) return fail;

        var summary = await _patients.GetSummaryAsync(Tenant!, id, ct);
        return summary == null ? NotFound() : Ok(summary);
    }

    [HttpGet("{id:int}/readings")]
    public async Task<IActionResult> Readings(int id, [FromQuery] int top = 20, CancellationToken ct = default)
    {
        var fail = await GuardAsync();
        if (fail != null) return fail;

        return Ok(await _patients.ReadingsAsync(Tenant!, id, top, ct));
    }
}
