using System.Text.Json.Serialization;
using ParkingVision.Api;
using ParkingVision.Core;
using ParkingVision.Data;

var builder = WebApplication.CreateBuilder(args);

var settings = builder.Configuration.GetSection("Parking").Get<ParkingSettings>() ?? new ParkingSettings();
builder.Services.AddParkingData(builder.Configuration["Database:Path"], settings);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod())); // prototype only
builder.Services.AddHostedService<MaintenanceWorker>();
builder.Services.AddOpenApi();

var app = builder.Build();
await DbSetup.EnsureDatabaseAsync(app.Services);
if (app.Configuration.GetValue<bool>("Demo:SeedOnStartup"))
    await app.Services.GetRequiredService<DemoSeeder>().SeedIfEmptyAsync();

app.UseCors();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

var api = app.MapGroup("/api");
api.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

// ---------------- public read API (used by the MAUI app) ----------------
api.MapGet("/zones", (AvailabilityService s, double? lat, double? lon, double? radiusM, SpotType? type, CancellationToken ct) =>
    s.GetZonesAsync(lat, lon, radiusM, type, ct));

api.MapGet("/zones/{id:int}", async (int id, AvailabilityService s, double? lat, double? lon, SpotType? type, CancellationToken ct) =>
    await s.GetZoneAsync(id, lat, lon, type, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

// ---------------- admin / diagnostics ----------------
var admin = api.MapGroup("/admin");
admin.MapGet("/cameras", (TopologyService t, CancellationToken ct) => t.GetCamerasAsync(ct));
admin.MapGet("/coverage", (TopologyService t, CancellationToken ct) => t.GetCoverageAsync(ct));

// ---------------- ingest (edge devices / parkomat integrations) - protected by X-Api-Key ----------------
var ingest = api.MapGroup("/ingest").AddEndpointFilter(ApiKeyFilter.Create(app.Configuration["Ingest:ApiKey"]));

ingest.MapPost("/observations", async (ObservationBatchDto batch, IObservationIngest svc, CancellationToken ct) =>
{
    try { return Results.Ok(new { accepted = await svc.IngestAsync(batch, ct) }); }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
});

ingest.MapPost("/parkomat-tickets", async (List<ParkomatTicketDto> tickets, IParkomatIngest svc, CancellationToken ct) =>
    Results.Ok(new { added = await svc.IngestAsync(tickets, ct) }));

app.Run();
