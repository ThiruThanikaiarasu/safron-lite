using Saffron.Data;
using Saffron.Models;
using Saffron.Session;

namespace Saffron.Tests;

internal static class Tenants
{
    public static readonly TenantInfo Alder = new("alder", "Alder Ward", "Saffron_Alder", "Europe/London");
    public static readonly TenantInfo Birch = new("birch", "Birch Ward", "Saffron_Birch", "America/New_York");
}

internal static class Fixtures
{
    public static readonly PatientRecord Patient = new(
        7, "MRN0007", "Jane", "Doe", new DateTime(1960, 1, 2), "B-12", "MED", "Medical", "Saffron General", null, null);

    public static readonly OrderRecord ActiveOrder = new(
        3, 7, "Active", 2m, 1m, 120, 40m, new DateTime(2026, 7, 1, 8, 0, 0), null, "Dr Ada");
}

internal sealed class FakeCatalog : ICatalogRepository
{
    private readonly Dictionary<string, TenantInfo> _tenants;

    public FakeCatalog(params TenantInfo[] tenants) =>
        _tenants = tenants.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

    public Task<TenantInfo?> FindActiveTenantAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(_tenants.TryGetValue(code, out var t) ? t : null);
}

internal sealed class FakeUsers : IUserRepository
{
    public UserRecord? User { get; set; }

    public int? TouchedUserId { get; private set; }

    public Task<UserRecord?> FindByUsernameAsync(TenantInfo tenant, string username, CancellationToken ct = default) =>
        Task.FromResult(User != null && User.Username == username ? User : null);

    public Task TouchLoginAsync(TenantInfo tenant, int userId, CancellationToken ct = default)
    {
        TouchedUserId = userId;
        return Task.CompletedTask;
    }
}

internal sealed class FakePatients : IPatientRepository
{
    public PatientRecord? Patient { get; set; }

    public Task<IReadOnlyList<PatientListRow>> ListActiveAsync(TenantInfo tenant, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PatientListRow>>(Array.Empty<PatientListRow>());

    public Task<PatientRecord?> GetAsync(TenantInfo tenant, int patientId, CancellationToken ct = default) =>
        Task.FromResult(Patient?.PatientId == patientId ? Patient : null);

    public Task<bool> IsActiveAsync(TenantInfo tenant, int patientId, CancellationToken ct = default) =>
        Task.FromResult(Patient?.PatientId == patientId);
}

internal sealed class FakeOrders : IOrderRepository
{
    public OrderRecord? Active { get; set; }

    public Task<OrderRecord?> GetActiveAsync(TenantInfo tenant, int patientId, CancellationToken ct = default) =>
        Task.FromResult(Active);

    public Task<OrderRecord?> GetLatestAsync(TenantInfo tenant, int patientId, CancellationToken ct = default) =>
        Task.FromResult(Active);

    public Task<int> DiscontinueAsync(TenantInfo tenant, int orderId, int userId, CancellationToken ct = default) =>
        Task.FromResult(1);

    public Task<int> ResumeAsync(TenantInfo tenant, int orderId, int userId, CancellationToken ct = default) =>
        Task.FromResult(1);
}

internal sealed class FakeReadings : IReadingRepository
{
    public DateTime? LastCorrection { get; set; }

    public int? InsertedBy { get; private set; }

    public int InsertedValue { get; private set; }

    public Task<IReadOnlyList<ReadingRow>> HistoryAsync(TenantInfo tenant, int patientId, int top, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ReadingRow>>(Array.Empty<ReadingRow>());

    public Task<IReadOnlyList<int>> RecentValuesAsync(TenantInfo tenant, int patientId, int count, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());

    public Task<DateTime?> LastCorrectionUtcAsync(TenantInfo tenant, int patientId, CancellationToken ct = default) =>
        Task.FromResult(LastCorrection);

    public Task<int> InsertAsync(TenantInfo tenant, int patientId, int value, DateTime takenAtUtc, decimal rate, int enteredBy, string? comment, CancellationToken ct = default)
    {
        InsertedValue = value;
        InsertedBy = enteredBy;
        return Task.FromResult(101);
    }

    public Task<(decimal? Rate, bool IsLow)> GetStoredAsync(TenantInfo tenant, int readingId, CancellationToken ct = default) =>
        Task.FromResult<(decimal?, bool)>((4m, false));
}

internal sealed class FakeSessions : ISharedSessionStore
{
    public SessionInfo? Session { get; set; }

    public Task<SessionInfo?> LoadAsync(TenantInfo tenant, string? sid, bool refreshPayload, CancellationToken ct = default) =>
        Task.FromResult(Session);

    public Task<string> CreateAsync(TenantInfo tenant, UserRecord user, CancellationToken ct = default) =>
        Task.FromResult(new string('a', 32));

    public Task DestroyAsync(TenantInfo tenant, string? sid, CancellationToken ct = default) =>
        Task.CompletedTask;
}
