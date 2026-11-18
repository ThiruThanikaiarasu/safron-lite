using System.Text.RegularExpressions;
using Saffron.Data;
using Saffron.Models;

namespace Saffron.Services;

public sealed class TenantResolver
{
    public const string ApiHeaderName = "X-Saffron-Tenant";

    private static readonly Regex HostLabel = new("^[a-z0-9-]{2,20}$", RegexOptions.CultureInvariant);

    private readonly ICatalogRepository _catalog;

    public TenantResolver(ICatalogRepository catalog)
    {
        _catalog = catalog;
    }

    public Task<TenantInfo?> ResolveApiAsync(HttpRequest request, CancellationToken ct = default)
    {
        var code = request.Headers[ApiHeaderName].ToString().Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(code)
            ? Task.FromResult<TenantInfo?>(null)
            : _catalog.FindActiveTenantAsync(code, ct);
    }

    public Task<TenantInfo?> ResolveHostAsync(HttpRequest request, CancellationToken ct = default)
    {
        var code = HostCode(request.Host.Host);
        return code == null
            ? Task.FromResult<TenantInfo?>(null)
            : _catalog.FindActiveTenantAsync(code, ct);
    }

    public static string? HostCode(string? host)
    {
        if (string.IsNullOrEmpty(host))
            return null;

        var labels = host.ToLowerInvariant().Split('.');
        if (labels.Length < 2)
            return null;

        return HostLabel.IsMatch(labels[0]) ? labels[0] : null;
    }
}
