using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Saffron.Models;
using Saffron.Services;
using Xunit;

namespace Saffron.Tests;

public class GlucoseFormValidationTests
{
    private static IFormCollection Form(params (string Key, string Value)[] items) =>
        new FormCollection(items
            .GroupBy(i => i.Key)
            .ToDictionary(g => g.Key, g => new StringValues(g.Select(x => x.Value).ToArray())));

    [Fact]
    public void A_valid_preview_has_no_errors()
    {
        var errors = GlucoseFormValidation.Validate(Form(("value", "120"), ("confirm_value", "120")), saving: false);
        Assert.Empty(errors);
    }

    [Fact]
    public void Empty_value_fields_report_required()
    {
        var errors = GlucoseFormValidation.Validate(Form(("value", ""), ("confirm_value", "  ")), saving: false);
        Assert.Equal(
            new[] { "The Glucose value field is required.", "The Confirm value field is required." },
            errors);
    }

    [Theory]
    [InlineData("12.5", "The Glucose value field must contain an integer.")]
    [InlineData("19", "The Glucose value field must contain a number greater than or equal to 20.")]
    [InlineData("601", "The Glucose value field must contain a number less than or equal to 600.")]
    public void Value_must_be_an_integer_between_20_and_600(string value, string expected)
    {
        var errors = GlucoseFormValidation.Validate(Form(("value", value), ("confirm_value", value)), saving: false);
        Assert.Equal(new[] { expected }, errors);
    }

    [Fact]
    public void Confirmation_must_match_the_value()
    {
        var errors = GlucoseFormValidation.Validate(Form(("value", "120"), ("confirm_value", "121")), saving: false);
        Assert.Equal(new[] { "The Confirm value field does not match the Glucose value field." }, errors);
    }

    [Fact]
    public void Entered_time_must_be_24_hour_hh_mm()
    {
        var errors = GlucoseFormValidation.Validate(
            Form(("value", "120"), ("confirm_value", "120"), ("entered_time", "9:00")), saving: false);
        Assert.Equal(new[] { "The Time taken must be HH:MM (24 hour)." }, errors);
    }

    [Fact]
    public void Comment_is_limited_to_200_characters()
    {
        var errors = GlucoseFormValidation.Validate(
            Form(("value", "120"), ("confirm_value", "120"), ("comment", new string('x', 201))), saving: false);
        Assert.Equal(new[] { "The Comment field cannot exceed 200 characters in length." }, errors);
    }

    [Fact]
    public void Saving_requires_acknowledgements_and_low_acknowledgement_when_low()
    {
        var errors = GlucoseFormValidation.Validate(
            Form(("value", "60"), ("confirm_value", "60"), ("api_low", "1")), saving: true);
        Assert.Equal(
            new[]
            {
                "The Patient identity check field is required.",
                "The Value double-check field is required.",
                "The Low glucose acknowledgement field is required.",
            },
            errors);
    }
}

public class AuthServiceTests
{
    private static string LegacyHash(string password) =>
        "$2y$" + BCrypt.Net.BCrypt.HashPassword(password)[4..];

    private static UserRecord User(bool active = true, string password = "secret") =>
        new(5, "nurse", LegacyHash(password), "Nurse Kim", "Nurse", active);

    [Fact]
    public async Task Correct_password_for_an_active_user_authenticates_and_touches_login()
    {
        var users = new FakeUsers { User = User() };
        var auth = new AuthService(users);

        var result = await auth.AuthenticateAsync(Tenants.Alder, "nurse", "secret");

        Assert.NotNull(result);
        Assert.Equal(5, users.TouchedUserId);
    }

    [Fact]
    public async Task Wrong_password_is_rejected_without_touching_login()
    {
        var users = new FakeUsers { User = User() };
        var auth = new AuthService(users);

        Assert.Null(await auth.AuthenticateAsync(Tenants.Alder, "nurse", "wrong"));
        Assert.Null(users.TouchedUserId);
    }

    [Fact]
    public async Task Inactive_user_is_rejected()
    {
        var auth = new AuthService(new FakeUsers { User = User(active: false) });
        Assert.Null(await auth.AuthenticateAsync(Tenants.Alder, "nurse", "secret"));
    }

    [Fact]
    public async Task Unknown_or_empty_username_is_rejected()
    {
        var auth = new AuthService(new FakeUsers { User = User() });
        Assert.Null(await auth.AuthenticateAsync(Tenants.Alder, "nobody", "secret"));
        Assert.Null(await auth.AuthenticateAsync(Tenants.Alder, "", "secret"));
    }
}
