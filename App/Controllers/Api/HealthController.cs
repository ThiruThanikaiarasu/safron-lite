using Microsoft.AspNetCore.Mvc;
using Saffron.Data;
using Saffron.Services;

namespace Saffron.Controllers.Api;

[ApiController]
[Route("api")]
public sealed class HealthController : ControllerBase
{
    private readonly TenantResolver _tenants;
    private readonly IFacilityRepository _facilities;

    public HealthController(TenantResolver tenants, IFacilityRepository facilities)
    {
        _tenants = tenants;
        _facilities = facilities;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var tenant = await _tenants.ResolveApiAsync(Request, ct);
        if (tenant == null)
            return NotFound(new { status = "unknown_tenant" });

        var facility = await _facilities.GetStatusAsync(tenant, ct);
        return Ok(new
        {
            status = "ok",
            tenant = tenant.Code,
            name = tenant.DisplayName,
            hospital = facility.Hospital,
            facilityTimeZone = tenant.FacilityTimeZone,
            patients = facility.Patients
        });
    }
}
