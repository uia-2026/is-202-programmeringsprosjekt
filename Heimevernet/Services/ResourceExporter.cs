using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Services;

/// <summary>
/// Converts Resources to downloadable CSV and JSON files (#94).
/// </summary>
public static class ResourceExporter
{
    private const string CsvDateFormat = "dd.MM.yyyy HH:mm";

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
    /// Builds an indented UTF-8 JSON file. Dates use ISO 8601 and Status is written
    /// by name, so the file is machine-readable.
    /// </summary>
    public static byte[] ToJson(IEnumerable<ResourceViewModel> resources)
    {
        var rows = resources.Select(resource => new
        {
            resource.Title,
            Category = resource.CategoryName,
            resource.Description,
            resource.Region,
            resource.AvailableFrom,
            resource.AvailableTo,
            Status = resource.Status.ToString(),
            resource.ContactPoint
        });

        return JsonSerializer.SerializeToUtf8Bytes(rows, JsonOptions);
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
