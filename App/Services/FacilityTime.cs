using System.Globalization;
using Saffron.Models;

namespace Saffron.Services;

public static class FacilityTime
{
    public const int CorrectionIntervalMinutes = 60;
    public const int FutureToleranceMinutes = 5;

    public static DateTime Utc(DateTime dbValue) => DateTime.SpecifyKind(dbValue, DateTimeKind.Utc);

    public static DateTime? FacilityTimeToUtc(string hhmm, TenantInfo tenant, DateTime nowUtc)
    {
        if (!DateTime.TryParseExact(hhmm.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return null;

        var tz = TimeZoneInfo.FindSystemTimeZoneById(tenant.FacilityTimeZone);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var local = new DateTime(localNow.Year, localNow.Month, localNow.Day, parsed.Hour, parsed.Minute, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    public static string? Iso(DateTime? utc) =>
        utc.HasValue ? DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture) : null;

    public static DateTime? ParseIso(string? iso) =>
        DateTime.TryParseExact(iso, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var utc)
            ? utc
            : null;

    public static DateTime ToFacilityLocal(DateTime utc, TenantInfo tenant) =>
        TimeZoneInfo.ConvertTimeFromUtc(Utc(utc), TimeZoneInfo.FindSystemTimeZoneById(tenant.FacilityTimeZone));
}
