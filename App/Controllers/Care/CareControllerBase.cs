using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Saffron.Models;
using Saffron.Services;
using Saffron.Session;

namespace Saffron.Controllers.Care;

public abstract class CareControllerBase : Controller
{
    private readonly TenantResolver _tenants;
    private readonly ISharedSessionStore _sessions;

    protected CareControllerBase(TenantResolver tenants, ISharedSessionStore sessions)
    {
        _tenants = tenants;
        _sessions = sessions;
    }

    protected TenantInfo? Tenant { get; private set; }

    protected SessionInfo? CurrentSession { get; private set; }

    protected bool IsAdmin => CurrentSession?.Role == "Admin";

    protected virtual bool RequiresLogin => true;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ct = HttpContext.RequestAborted;
        Tenant = await _tenants.ResolveHostAsync(Request, ct);
        if (Tenant == null)
        {
            context.Result = ErrorPage(404, "Facility not found", "Unknown facility. Check the address you are using.");
            return;
        }

        CurrentSession = await _sessions.LoadAsync(Tenant, Request.Cookies[SharedSessionStore.CookieName], refreshPayload: true, ct);
        if (CurrentSession == null && RequiresLogin)
        {
            context.Result = Redirect("/care/login");
            return;
        }

        ViewData["Tenant"] = Tenant;
        ViewData["User"] = CurrentSession;
        await next();
    }

    protected ViewResult ErrorPage(int status, string heading, string message)
    {
        var result = View("~/Views/Shared/ErrorPage.cshtml", new ErrorPageModel(heading, message));
        result.StatusCode = status;
        return result;
    }

    protected ViewResult NotFoundPage() => ErrorPage(404, "404 Page Not Found", "The page you requested was not found.");
}
