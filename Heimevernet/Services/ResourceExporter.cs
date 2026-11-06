using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Heimevernet.ViewModels.Resource;
using Parquet.Serialization;

namespace Heimevernet.Services;

/// <summary>
/// Converts Resources to downloadable CSV, GeoJSON, and GeoParquet files.
/// </summary>
public static class ResourceExporter
{
    private const string CsvDateFormat = "dd.MM.yyyy HH:mm";

    private sealed record GeoParquetRow(
        string Title,
        string Category,
        string? Description,
        string Region,
        DateTime AvailableFrom,
        DateTime? AvailableTo,
        string Status,
        string? ContactPoint,
        byte[]? Geometry,
        double? Longitude,
        double? Latitude);

    private static readonly string[] CsvHeaders =
    [
        "Title", "Category", "Description", "Region", "AvailableFrom", "AvailableTo", "Status", "ContactPoint"
    ];

    // Keeps æ, ø and å readable in the file. Relaxed escaping is safe here because the
    // JSON is only ever sent as a file download, never embedded in an HTML page.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Builds a UTF-8 CSV file. It starts with a byte order mark so that Excel
    /// reads æ, ø and å correctly.
    /// </summary>
    public static byte[] ToCsv(IEnumerable<ResourceViewModel> resources)
    {
        var csv = new StringBuilder();
        AppendCsvRow(csv, CsvHeaders);

        foreach (var resource in resources)
        {
            AppendCsvRow(csv,
            [
                resource.Title,
                resource.CategoryName,
                resource.Description,
                resource.Region,
                resource.AvailableFrom.ToString(CsvDateFormat, CultureInfo.InvariantCulture),
                resource.AvailableTo?.ToString(CsvDateFormat, CultureInfo.InvariantCulture),
                resource.Status.ToString(),
                resource.ContactPoint
            ]);
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())];
    }

    /// <summary>
    /// Builds an indented UTF-8 GeoJSON FeatureCollection. Dates use ISO 8601 and
    /// Status is written by name, so the file is machine-readable.
    /// </summary>
    public static byte[] ToGeoJson(IEnumerable<ResourceViewModel> resources)
    {
        var featureCollection = new
        {
            type = "FeatureCollection",
            features = resources.Select(resource => new
            {
                type = "Feature",
                id = resource.Id,
                geometry = HasLocation(resource)
                    ? new
                    {
                        type = "Point",
                        coordinates = new[] { resource.Longitude, resource.Latitude }
                    }
                    : null,
                properties = new
                {
                    resource.Title,
                    Category = resource.CategoryName,
                    resource.Description,
                    resource.Region,
                    resource.AvailableFrom,
                    resource.AvailableTo,
                    Status = resource.Status.ToString(),
                    resource.ContactPoint
                }
            })
        };

        return JsonSerializer.SerializeToUtf8Bytes(featureCollection, JsonOptions);
    }

    /// <summary>
    /// Builds a parquet file with the same resource fields plus a WKB point geometry column.
    /// </summary>
    public static byte[] ToGeoParquet(IEnumerable<ResourceViewModel> resources)
    {
        var rows = resources
            .Select(resource => new GeoParquetRow(
                resource.Title,
                resource.CategoryName,
                resource.Description,
                resource.Region,
                resource.AvailableFrom,
                resource.AvailableTo,
                resource.Status.ToString(),
                resource.ContactPoint,
                CreatePointWkb(resource),
                HasLocation(resource) ? resource.Longitude : null,
                HasLocation(resource) ? resource.Latitude : null))
            .ToList();

        using var stream = new MemoryStream();
        ParquetSerializer.SerializeAsync(rows, stream).GetAwaiter().GetResult();
        return stream.ToArray();
    }

    private static bool HasLocation(ResourceViewModel resource) =>
        double.IsFinite(resource.Latitude) &&
        double.IsFinite(resource.Longitude) &&
        resource.Latitude is >= -90 and <= 90 &&
        resource.Longitude is >= -180 and <= 180;

    private static byte[]? CreatePointWkb(ResourceViewModel resource)
    {
        if (!HasLocation(resource))
            return null;

        var wkb = new byte[21];
        wkb[0] = 1; // little-endian WKB
        BinaryPrimitives.WriteInt32LittleEndian(wkb.AsSpan(1), 1); // POINT
        BinaryPrimitives.WriteDoubleLittleEndian(wkb.AsSpan(5), resource.Longitude);
        BinaryPrimitives.WriteDoubleLittleEndian(wkb.AsSpan(13), resource.Latitude);
        return wkb;
    }

    private static void AppendCsvRow(StringBuilder csv, IEnumerable<string?> fields)
    {
        csv.Append(string.Join(',', fields.Select(EscapeCsvField)));
        csv.Append("\r\n");
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Providers write these values, and a spreadsheet could run them as a formula if they
        // start with = + - or @, or with a tab or carriage return.
        var safeValue = value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? $"'{value}" : value;

        return safeValue.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{safeValue.Replace("\"", "\"\"")}\""
            : safeValue;
    }
}
