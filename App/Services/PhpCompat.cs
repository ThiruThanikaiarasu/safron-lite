using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Saffron.Models;

namespace Saffron.Services;

public static class PhpCompat
{
    private static readonly char[] TrimChars = { ' ', '\t', '\n', '\r', '\0', '\x0B' };
    private static readonly Regex HhmmPattern = new("^([01][0-9]|2[0-3]):[0-5][0-9]$", RegexOptions.CultureInvariant);
    private static readonly Regex LeadingIntPattern = new(@"^\s*[+-]?\d+", RegexOptions.CultureInvariant);

    public static string Trim(string? s) => (s ?? "").Trim(TrimChars);

    public static bool IsHhmm(string s) => HhmmPattern.IsMatch(s);

    public static bool IsTruthy(string? s) => !string.IsNullOrEmpty(s) && s != "0";

    public static int IntCast(string? s)
    {
        var m = LeadingIntPattern.Match(s ?? "");
        return m.Success && int.TryParse(m.Value.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }

    public static BigInteger ParseInteger(string s) =>
        BigInteger.Parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    public static double Round(double value, int places) => Math.Round(value, places, MidpointRounding.AwayFromZero);

    public static string PatientName(string? last, string? first)
    {
        last ??= "";
        first ??= "";
        if (last == "" && first == "")
            return "(unknown)";
        return last.ToUpperInvariant() + ", " + first;
    }

    public static int? RecentAverage(IEnumerable<int> values, int count = 6)
    {
        var slice = values.Take(count).ToList();
        if (slice.Count == 0)
            return null;
        return (int)Round(slice.Sum(v => (long)v) / (double)slice.Count, 0);
    }

    public static string FormatOneDecimal(double value) => Round(value, 1).ToString("F1", CultureInfo.InvariantCulture);

    public const string Mdash = "&mdash;";

    public static string FmtRate(decimal? rate) => rate.HasValue ? FormatOneDecimal((double)rate.Value) : Mdash;

    public static string FloatText(decimal value) => ((double)value).ToString(CultureInfo.InvariantCulture);

    public static string LocalTimeFromIso(string? iso, TenantInfo tenant)
    {
        if (string.IsNullOrEmpty(iso))
            return Mdash;
        var utc = FacilityTime.ParseIso(iso);
        return utc.HasValue
            ? FacilityTime.ToFacilityLocal(utc.Value, tenant).ToString("dd MMM HH:mm", CultureInfo.InvariantCulture)
            : iso;
    }
}
