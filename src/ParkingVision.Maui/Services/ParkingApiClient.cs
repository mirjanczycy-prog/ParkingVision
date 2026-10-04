using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ParkingVision.Core;

namespace ParkingVision.Maui.Services;

public sealed class ParkingApiClient(AppSettings settings)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    private static string Inv(double v) => v.ToString(CultureInfo.InvariantCulture);

    public async Task<List<ZoneAvailabilityDto>> GetZonesAsync(double lat, double lon, int radiusM, SpotType? type, CancellationToken ct = default)
    {
        var url = $"{settings.ApiBaseUrl}/api/zones?lat={Inv(lat)}&lon={Inv(lon)}&radiusM={radiusM}" + (type is null ? "" : $"&type={type}");
        return await _http.GetFromJsonAsync<List<ZoneAvailabilityDto>>(url, Json, ct) ?? new();
    }

    public async Task<ZoneDetailDto?> GetZoneAsync(int id, double lat, double lon, SpotType? type, CancellationToken ct = default)
    {
        var url = $"{settings.ApiBaseUrl}/api/zones/{id}?lat={Inv(lat)}&lon={Inv(lon)}" + (type is null ? "" : $"&type={type}");
        return await _http.GetFromJsonAsync<ZoneDetailDto>(url, Json, ct);
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try { using var r = await _http.GetAsync($"{settings.ApiBaseUrl}/api/health", ct); return r.IsSuccessStatusCode; }
        catch { return false; }
    }
}
