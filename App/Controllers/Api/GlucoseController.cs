using Microsoft.AspNetCore.Mvc;
using Saffron.Models;
using Saffron.Services;
using Saffron.Session;

namespace Saffron.Controllers.Api;

[Route("api/glucose")]
public sealed class GlucoseController : SaffronApiControllerBase
{
    private const string SessionExpired = "Session expired. Please log in again.";

    private readonly GlucoseService _glucose;

    public GlucoseController(TenantResolver tenants, ISharedSessionStore sessions, GlucoseService glucose)
        : base(tenants, sessions)
    {
        _glucose = glucose;
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] GlucoseRequest? req, CancellationToken ct)
    {
        if (!await AuthenticateAsync())
            return StatusCode(401, new { ok = false, error = SessionExpired });

        var result = await _glucose.EvaluateAsync(Tenant!, req ?? new GlucoseRequest(), ct);
        return Render(result);
    }

    [HttpPost("readings")]
    public async Task<IActionResult> Save([FromBody] GlucoseRequest? req, CancellationToken ct)
    {
        if (!await AuthenticateAsync())
            return StatusCode(401, new { ok = false, error = SessionExpired });

        var result = await _glucose.SaveAsync(Tenant!, req ?? new GlucoseRequest(), CurrentSession!.UserId, ct);
        return Render(result);
    }

    private IActionResult Render<T>(EvaluateResult<T> result) where T : class =>
        Ok(result.Ok
            ? new { ok = true, data = result.Data }
            : (object)new { ok = false, error = result.Error });
}
