namespace Heimevernet.Infrastructure.Geo;

public readonly record struct GeoBox(double MinLat, double MaxLat, double MinLng, double MaxLng);

public static class GeoMath
{
    private const double EarthRadiusKm = 6371;
    private const double KmPerDegreeLat = 111.32;

    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>Rough square around a point, cheap to filter on in SQL. Corners lie up to ~1.4x the radius away.</summary>
    public static GeoBox BoundingBox(double lat, double lng, double radiusKm)
    {
        var dLat = radiusKm / KmPerDegreeLat;
        var dLng = radiusKm / (KmPerDegreeLat * Math.Cos(ToRad(lat)));
        return new GeoBox(lat - dLat, lat + dLat, lng - dLng, lng + dLng);
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180;
}