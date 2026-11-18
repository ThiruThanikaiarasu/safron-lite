using Microsoft.Extensions.FileProviders;
using Saffron.Data;
using Saffron.Services;
using Saffron.Session;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var catalog = builder.Configuration["SAFFRON_CATALOG"] ?? builder.Configuration.GetConnectionString("Catalog");
if (string.IsNullOrWhiteSpace(catalog))
    throw new InvalidOperationException("SAFFRON_CATALOG (or ConnectionStrings:Catalog) must be set.");

var redisEndpoint = builder.Configuration["SAFFRON_REDIS"] ?? builder.Configuration["Redis"];
if (string.IsNullOrWhiteSpace(redisEndpoint))
    throw new InvalidOperationException("SAFFRON_REDIS (or Redis) must be set.");

var idleSeconds = int.TryParse(builder.Configuration["SESSION_IDLE_SECONDS"], out var configuredIdle) ? configuredIdle : 1800;
var settings = new SaffronSettings(catalog, redisEndpoint, idleSeconds);

var redis = ConnectionMultiplexer.Connect(redisEndpoint);

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<ITenantConnections, TenantConnections>();
builder.Services.AddSingleton<ICatalogRepository, CatalogRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPatientRepository, PatientRepository>();
builder.Services.AddSingleton<IReadingRepository, ReadingRepository>();
builder.Services.AddSingleton<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IFacilityRepository, FacilityRepository>();
builder.Services.AddSingleton<ISharedSessionStore, SharedSessionStore>();

builder.Services.AddSingleton<TenantResolver>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<PatientService>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton<GlucoseService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/care/assets",
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "assets")),
});

app.MapControllers();

app.Run();

public partial class Program { }
