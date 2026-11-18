using Microsoft.AspNetCore.Mvc;
using Saffron.Services;
using Saffron.Session;

namespace Saffron.Controllers.Api;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : SaffronApiControllerBase
{
    private readonly OrderService _orders;

    public OrdersController(TenantResolver tenants, ISharedSessionStore sessions, OrderService orders)
        : base(tenants, sessions)
    {
        _orders = orders;
    }

    [HttpPost("{id:int}/discontinue")]
    public async Task<IActionResult> Discontinue(int id, CancellationToken ct)
    {
        var fail = await GuardAsync();
        if (fail != null) return fail;

        var affected = await _orders.DiscontinueAsync(Tenant!, id, CurrentSession!.UserId, ct);
        if (affected == 0)
            return Conflict(new { message = "Order not found or not active." });

        return Ok(new { orderId = id, status = "Discontinued" });
    }

    [HttpPost("{id:int}/resume")]
    public async Task<IActionResult> Resume(int id, CancellationToken ct)
    {
        var fail = await GuardAsync();
        if (fail != null) return fail;

        if (!string.Equals(CurrentSession!.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new { message = "Only an administrator can resume an order." });

        var affected = await _orders.ResumeAsync(Tenant!, id, CurrentSession.UserId, ct);
        if (affected == 0)
            return Conflict(new { message = "Order cannot be resumed." });

        return Ok(new { orderId = id, status = "Active" });
    }
}
