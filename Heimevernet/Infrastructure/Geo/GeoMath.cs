namespace Heimevernet.Infrastructure.Geo;

/// <summary>
/// A rectangle of latitude and longitude, used for map areas and cheap distance pre-filters in SQL.
/// </summary>
/// <param name="MinLat">Southern edge (min latitude).</param>
/// <param name="MaxLat">Northern edge (max latitude).</param>
/// <param name="MinLng">Western edge (min longitude).</param>
/// <param name="MaxLng">Eastern edge (max longitude).</param>
public readonly record struct GeoBox(double MinLat, double MaxLat, double MinLng, double MaxLng);

/// <summary>
/// Simple geographic helpers for distances and bounding boxes.
/// </summary>
public static class GeoMath
{
    private const double EarthRadiusKm = 6371;
    private const double KmPerDegreeLat = 111.32;

    /// <summary>
    /// Great-circle distance between two points, using the haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of the first point.</param>
    /// <param name="lon1">Longitude of the first point.</param>
    /// <param name="lat2">Latitude of the second point.</param>
    /// <param name="lon2">Longitude of the second point.</param>
    /// <returns>The distance in kilometres.</returns>
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>Rough square around a point, cheap to filter on in SQL. Corners lie up to ~1.4x the radius away.</summary>
    /// <param name="lat">Latitude of the centre.</param>
    /// <param name="lng">Longitude of the centre.</param>
    /// <param name="radiusKm">Radius in kilometres.</param>
    /// <returns>A <see cref="GeoBox"/> that contains the whole circle.</returns>
    public static GeoBox BoundingBox(double lat, double lng, double radiusKm)
    {
        var dLat = radiusKm / KmPerDegreeLat;
        var dLng = radiusKm / (KmPerDegreeLat * Math.Cos(ToRad(lat)));
        return new GeoBox(lat - dLat, lat + dLat, lng - dLng, lng + dLng);
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180;
}