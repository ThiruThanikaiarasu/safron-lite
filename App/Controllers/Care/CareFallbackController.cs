using Microsoft.AspNetCore.Mvc;
using Saffron.Models;

namespace Saffron.Controllers.Care;

public sealed class CareFallbackController : Controller
{
    [Route("care/{*path}")]
    public IActionResult NotFoundPage()
    {
        var result = View("~/Views/Shared/ErrorPage.cshtml", new ErrorPageModel("404 Page Not Found", "The page you requested was not found."));
        result.StatusCode = 404;
        return result;
    }
}
