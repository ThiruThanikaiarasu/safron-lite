using Saffron.Services;
using Saffron.Session;
using Xunit;

namespace Saffron.Tests;

public class RateCalculatorTests
{
    private static readonly RateOrder Order = new(2m, 1m, 120, 40m);

    [Theory]
    [InlineData(120, 2.00)]
    [InlineData(200, 4.00)]
    [InlineData(40, 0.00)]
    public void Rate_follows_the_sliding_scale_and_is_never_negative(int value, decimal expected) =>
        Assert.Equal(expected, RateCalculator.ComputeRate(value, Order));

    [Fact]
    public void Rate_is_rounded_to_two_decimals() =>
        Assert.Equal(2.33m, RateCalculator.ComputeRate(121, new RateOrder(2m, 1m, 120, 3m)));
}

public class GlucoseRulesTests
{
    [Theory]
    [InlineData(19, false)]
    [InlineData(20, true)]
    [InlineData(600, true)]
    [InlineData(601, false)]
    public void Values_are_accepted_between_20_and_600(int value, bool expected) =>
        Assert.Equal(expected, GlucoseRules.InRange(value));

    [Theory]
    [InlineData(71, true)]
    [InlineData(72, false)]
    public void Values_below_72_are_low(int value, bool expected) =>
        Assert.Equal(expected, GlucoseRules.IsLow(value));

    [Theory]
    [InlineData(299, false)]
    [InlineData(300, true)]
    public void Values_of_300_or_more_need_a_repeat(int value, bool expected) =>
        Assert.Equal(expected, GlucoseRules.NeedsRepeat(value));
}

public class FacilityTimeTests
{
    private static readonly DateTime SummerNoon = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Facility_clock_time_converts_to_utc_in_the_facility_zone()
    {
        var utc = FacilityTime.FacilityTimeToUtc("10:30", Tenants.Alder, SummerNoon);
        Assert.Equal(new DateTime(2026, 7, 1, 9, 30, 0), utc!.Value);
    }

    [Fact]
    public void Malformed_facility_time_is_rejected()
    {
        Assert.Null(FacilityTime.FacilityTimeToUtc("25:00", Tenants.Alder, SummerNoon));
        Assert.Null(FacilityTime.FacilityTimeToUtc("9:00", Tenants.Alder, SummerNoon));
    }

    [Fact]
    public void Iso_text_round_trips_utc_values()
    {
        var iso = FacilityTime.Iso(new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc));
        Assert.Equal("2026-07-01T09:30:00Z", iso);
        Assert.Equal(new DateTime(2026, 7, 1, 9, 30, 0), FacilityTime.ParseIso(iso)!.Value);
    }

    [Fact]
    public void Local_time_uses_the_tenant_zone()
    {
        var local = FacilityTime.ToFacilityLocal(new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc), Tenants.Birch);
        Assert.Equal(5, local.Hour);
        Assert.Equal(30, local.Minute);
    }
}

public class PhpCompatTests
{
    [Fact]
    public void Recent_average_rounds_half_away_from_zero()
    {
        Assert.Equal<int?>(3, PhpCompat.RecentAverage(new[] { 2, 3 }));
        Assert.Equal<int?>(2, PhpCompat.RecentAverage(new[] { 1, 2 }));
    }

    [Fact]
    public void Recent_average_uses_only_the_first_six_values_and_handles_none()
    {
        Assert.Equal<int?>(10, PhpCompat.RecentAverage(new[] { 10, 10, 10, 10, 10, 10, 0 }));
        Assert.Null(PhpCompat.RecentAverage(Array.Empty<int>()));
    }

    [Fact]
    public void Rate_display_uses_one_decimal_and_a_dash_entity_when_missing()
    {
        Assert.Equal("&mdash;", PhpCompat.FmtRate(null));
        Assert.Equal("1.3", PhpCompat.FmtRate(1.25m));
        Assert.Equal("2.0", PhpCompat.FmtRate(2m));
    }

    [Fact]
    public void Patient_name_uppercases_the_last_name()
    {
        Assert.Equal("DOE, Jane", PhpCompat.PatientName("Doe", "Jane"));
        Assert.Equal("(unknown)", PhpCompat.PatientName("", ""));
    }

    [Fact]
    public void Int_cast_reads_a_leading_integer_like_php()
    {
        Assert.Equal(12, PhpCompat.IntCast("12abc"));
        Assert.Equal(0, PhpCompat.IntCast("abc"));
        Assert.Equal(-5, PhpCompat.IntCast("-5"));
    }

    [Fact]
    public void Truthiness_matches_php_for_strings()
    {
        Assert.False(PhpCompat.IsTruthy(null));
        Assert.False(PhpCompat.IsTruthy(""));
        Assert.False(PhpCompat.IsTruthy("0"));
        Assert.True(PhpCompat.IsTruthy("abc"));
    }

    [Fact]
    public void Float_text_drops_trailing_zeros_like_php_echo()
    {
        Assert.Equal("2", PhpCompat.FloatText(2.00m));
        Assert.Equal("2.5", PhpCompat.FloatText(2.50m));
    }

    [Fact]
    public void Local_time_text_formats_in_facility_zone()
    {
        Assert.Equal("&mdash;", PhpCompat.LocalTimeFromIso(null, Tenants.Alder));
        Assert.Equal("01 Jul 10:30", PhpCompat.LocalTimeFromIso("2026-07-01T09:30:00Z", Tenants.Alder));
    }
}

public class TenantAndSessionIdTests
{
    [Theory]
    [InlineData("alder.example.org", "alder")]
    [InlineData("Alder.Example.org", "alder")]
    [InlineData("localhost", null)]
    [InlineData("bad_code.example.org", null)]
    [InlineData("abcdefghijklmnopqrstu.example.org", null)]
    public void Facility_code_comes_from_the_first_host_label(string host, string? expected) =>
        Assert.Equal(expected, TenantResolver.HostCode(host));

    [Fact]
    public void Session_ids_are_32_to_64_lowercase_hex_characters()
    {
        Assert.True(SharedSessionStore.IsValidId(new string('a', 32)));
        Assert.True(SharedSessionStore.IsValidId(new string('f', 64)));
        Assert.False(SharedSessionStore.IsValidId(new string('a', 31)));
        Assert.False(SharedSessionStore.IsValidId(new string('A', 32)));
        Assert.False(SharedSessionStore.IsValidId(null));
    }

    [Fact]
    public void Session_key_is_scoped_to_the_tenant()
    {
        Assert.Equal("sess:alder:abc", SharedSessionStore.Key("alder", "abc"));
    }
}
