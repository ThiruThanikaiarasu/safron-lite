using Microsoft.AspNetCore.Mvc;
using Saffron.Models;
using Saffron.Services;
using Saffron.Session;

namespace Saffron.Controllers.Care;

public sealed class CareLoginController : CareControllerBase
{
    private readonly AuthService _auth;
    private readonly ISharedSessionStore _sessions;

    public CareLoginController(TenantResolver tenants, ISharedSessionStore sessions, AuthService auth)
        : base(tenants, sessions)
    {
        _auth = auth;
        _sessions = sessions;
    }

    protected override bool RequiresLogin => false;

    [Route("care/login")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (CurrentSession != null)
            return Redirect("/care/patients");

        ViewData["Title"] = "Sign in";
        if (!HttpMethods.IsPost(Request.Method))
            return View("~/Views/Care/Login.cshtml", new LoginViewModel(null, ""));

        var form = await Request.ReadFormAsync(ct);
        var username = PhpCompat.Trim(form["username"]);
        var password = form["password"].ToString();

        var user = await _auth.AuthenticateAsync(Tenant!, username, password, ct);
        if (user != null)
        {
            var sid = await _sessions.CreateAsync(Tenant!, user, ct);
            Response.Cookies.Append(SharedSessionStore.CookieName, sid, new CookieOptions { HttpOnly = true, Path = "/" });
            return Redirect("/care/patients");
        }

        return View("~/Views/Care/Login.cshtml", new LoginViewModel("Invalid username or password.", username));
    }

    [Route("care/logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _sessions.DestroyAsync(Tenant!, Request.Cookies[SharedSessionStore.CookieName], ct);
        Response.Cookies.Delete(SharedSessionStore.CookieName, new CookieOptions { Path = "/" });
        return Redirect("/care/login");
    }
}
