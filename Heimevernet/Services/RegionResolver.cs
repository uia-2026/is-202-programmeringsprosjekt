using System.Text.Json;
using Heimevernet.Services.Interfaces;
using System.Threading;

namespace Heimevernet.Services;

/// <summary>
/// Uses Nominatim reverse-geocoding to resolve a region name for coordinates.
/// Returns a result that distinguishes between an actual resolved international location
/// and a failed lookup (network/timeout/etc.).
/// </summary>
public class RegionResolver : Services.Interfaces.IRegionResolver
{
    private readonly IHttpClientFactory _factory;

    public RegionResolver(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<RegionResolutionResult> TryResolveRegionAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        try
        {
            var client = _factory.CreateClient("nominatim");
            var url = $"reverse?format=jsonv2&lat={latitude}&lon={longitude}&accept-language=nb";
            using var resp = await client.GetAsync(url, cancellationToken);
            if (!resp.IsSuccessStatusCode)
                return new RegionResolutionResult { Success = false };

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("address", out var addr))
                return new RegionResolutionResult { Success = false };

            var countryCode = addr.TryGetProperty("country_code", out var ccEl) ? ccEl.GetString()?.ToLowerInvariant() : null;
            var isNorway = countryCode == "no" || countryCode == "sj";


            string? county = null;
            if (addr.TryGetProperty("county", out var countyEl)) county = countyEl.GetString()?.Trim();
            if (string.IsNullOrEmpty(county) && addr.TryGetProperty("state", out var stateEl)) county = stateEl.GetString()?.Trim();

            // Fallbacks: prefer 'region' then 'city' when county/state are not provided by the API
            if (string.IsNullOrEmpty(county) && addr.TryGetProperty("region", out var regionEl)) county = regionEl.GetString()?.Trim();
            if (string.IsNullOrEmpty(county) && addr.TryGetProperty("city", out var cityEl)) county = cityEl.GetString()?.Trim();

            return new RegionResolutionResult
            {
                Success = true,
                IsInNorway = isNorway,
                Region = county
            };
        }
        catch
        {
            // Network error, timeout or JSON parse failure. Signal failure so caller can apply an explicit fallback.
            return new RegionResolutionResult { Success = false };
        }
    }
}
