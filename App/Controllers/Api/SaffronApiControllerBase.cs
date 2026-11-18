using Microsoft.AspNetCore.Mvc;
using Saffron.Models;
using Saffron.Services;
using Saffron.Session;

namespace Saffron.Controllers.Api;

public abstract class SaffronApiControllerBase : ControllerBase
{
    private readonly TenantResolver _tenants;
    private readonly ISharedSessionStore _sessions;

    protected SaffronApiControllerBase(TenantResolver tenants, ISharedSessionStore sessions)
    {
        _tenants = tenants;
        _sessions = sessions;
    }

    protected TenantInfo? Tenant { get; private set; }

    protected SessionInfo? CurrentSession { get; private set; }

    protected async Task<bool> AuthenticateAsync()
    {
        var ct = HttpContext.RequestAborted;
        Tenant = await _tenants.ResolveApiAsync(Request, ct);
        if (Tenant == null)
            return false;

        CurrentSession = await _sessions.LoadAsync(Tenant, Request.Cookies[SharedSessionStore.CookieName], refreshPayload: false, ct);
        return CurrentSession != null;
    }

    protected async Task<IActionResult?> GuardAsync()
    {
        if (await AuthenticateAsync())
            return null;

        return Tenant == null
            ? BadRequest(new { error = "unknown_tenant" })
            : Unauthorized(new { error = "unauthorized" });
    }
}
