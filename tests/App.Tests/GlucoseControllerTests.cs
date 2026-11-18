using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Saffron.Controllers.Api;
using Saffron.Models;
using Saffron.Services;
using Xunit;

namespace Saffron.Tests;

public class ApiGlucoseControllerTests
{
    private static readonly SessionInfo NurseSession = new()
    {
        Tenant = "alder",
        UserId = 9,
        Username = "nurse",
        DisplayName = "Nurse Kim",
        Role = "Nurse",
    };

    private readonly FakePatients _patients = new() { Patient = Fixtures.Patient };
    private readonly FakeOrders _orders = new() { Active = Fixtures.ActiveOrder };
    private readonly FakeReadings _readings = new();
    private readonly FakeSessions _sessions = new();

    private GlucoseController CreateController(bool withSession)
    {
        _sessions.Session = withSession ? NurseSession : null;

        var http = new DefaultHttpContext();
        http.Request.Headers["X-Saffron-Tenant"] = "alder";
        http.Request.Headers.Cookie = "SAFFRON_SID=" + new string('a', 32);

        var glucose = new GlucoseService(_patients, _orders, _readings, TimeProvider.System, NullLogger<GlucoseService>.Instance);
        var resolver = new TenantResolver(new FakeCatalog(Tenants.Alder, Tenants.Birch));

        return new GlucoseController(resolver, _sessions, glucose)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
        };
    }

    private static GlucoseRequest Request(int value) => new() { PatientId = 7, Value = value };

    private static JsonElement Body(IActionResult result)
    {
        var json = JsonSerializer.Serialize(((ObjectResult)result).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact]
    public async Task Evaluate_without_a_session_returns_401_with_the_legacy_message()
    {
        var controller = CreateController(withSession: false);

        var result = await controller.Evaluate(Request(200), CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, obj.StatusCode);
        Assert.False(Body(result).GetProperty("ok").GetBoolean());
        Assert.Equal("Session expired. Please log in again.", Body(result).GetProperty("error").GetString());
    }

    [Fact]
    public async Task Evaluate_returns_the_rate_for_an_in_range_value()
    {
        var controller = CreateController(withSession: true);

        var result = await controller.Evaluate(Request(200), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var body = Body(result);
        Assert.True(body.GetProperty("ok").GetBoolean());
        var data = body.GetProperty("data");
        Assert.Equal(4.00m, data.GetProperty("computedRate").GetDecimal());
        Assert.False(data.GetProperty("lowGlucose").GetBoolean());
    }

    [Fact]
    public async Task Evaluate_of_a_low_value_gives_zero_rate_and_the_low_flag()
    {
        var controller = CreateController(withSession: true);

        var result = await controller.Evaluate(Request(60), CancellationToken.None);

        var data = Body(result).GetProperty("data");
        Assert.True(data.GetProperty("lowGlucose").GetBoolean());
        Assert.Equal(0m, data.GetProperty("computedRate").GetDecimal());
    }

    [Fact]
    public async Task Evaluate_rejects_a_value_outside_the_range_without_a_status_change()
    {
        var controller = CreateController(withSession: true);

        var result = await controller.Evaluate(Request(700), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var body = Body(result);
        Assert.False(body.GetProperty("ok").GetBoolean());
        Assert.Equal("Value must be between 20 and 600.", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Save_records_the_reading_for_the_session_user()
    {
        var controller = CreateController(withSession: true);

        var result = await controller.Save(Request(200), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(9, _readings.InsertedBy);
        Assert.Equal(200, _readings.InsertedValue);
        Assert.Equal(101, Body(result).GetProperty("data").GetProperty("readingId").GetInt32());
    }
}
