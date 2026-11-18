using Saffron.Data;
using Saffron.Models;

namespace Saffron.Services;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;

    public OrderService(IOrderRepository orders)
    {
        _orders = orders;
    }

    public Task<int> DiscontinueAsync(TenantInfo tenant, int orderId, int userId, CancellationToken ct = default) =>
        _orders.DiscontinueAsync(tenant, orderId, userId, ct);

    public Task<int> ResumeAsync(TenantInfo tenant, int orderId, int userId, CancellationToken ct = default) =>
        _orders.ResumeAsync(tenant, orderId, userId, ct);
}
