using System.Text;
using System.Text.Json;
using Heimevernet.Models;
using Heimevernet.Services;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="ResourceExporter"/>.
/// </summary>
public class ResourceExporterTests
{
    private const string CsvHeader = "Title,Category,Description,Region,AvailableFrom,AvailableTo,Status,ContactPoint";

    private static ResourceViewModel CreateResource(
        string title = "Snow plow",
        string? description = "Large plow",
        string contactPoint = "owner@example.com",
        DateTime? availableTo = null) => new()
        {
            Title = title,
            CategoryName = "Equipment",
            Description = description,
            Region = "Kristiansand",
            AvailableFrom = new DateTime(2026, 11, 4, 8, 30, 0),
            AvailableTo = availableTo,
            Status = ResourceStatus.Available,
            ContactPoint = contactPoint,
            UserId = 42,
            Latitude = 58.1
        };

    private static string[] CsvLines(params ResourceViewModel[] resources)
    {
        var text = new UTF8Encoding(false).GetString(ResourceExporter.ToCsv(resources)).TrimStart('﻿');
        return text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public void ToCsv_Starts_With_Utf8_Byte_Order_Mark()
    {
        var bytes = ResourceExporter.ToCsv([]);

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
    }

    [Fact]
    public void ToCsv_With_No_Resources_Writes_Only_Header()
    {
        Assert.Equal([CsvHeader], CsvLines());
    }

    [Fact]
    public void ToCsv_Writes_Fields_In_Header_Order_With_Formatted_Dates()
    {
        var lines = CsvLines(CreateResource(availableTo: new DateTime(2026, 12, 24, 18, 0, 0)));

        Assert.Equal(
            "Snow plow,Equipment,Large plow,Kristiansand,04.11.2026 08:30,24.12.2026 18:00,Available,owner@example.com",
            lines[1]);
    }

    [Fact]
    public void ToCsv_Leaves_Missing_Optional_Values_Empty()
    {
        var lines = CsvLines(CreateResource(description: null, availableTo: null));

        Assert.Equal(
            "Snow plow,Equipment,,Kristiansand,04.11.2026 08:30,,Available,owner@example.com",
            lines[1]);
    }

    [Theory]
    [InlineData("Plow, large", "\"Plow, large\"")]
    [InlineData("The \"big\" plow", "\"The \"\"big\"\" plow\"")]
    [InlineData("Line one\nLine two", "\"Line one\nLine two\"")]
    [InlineData("Line one\r\nLine two", "\"Line one\r\nLine two\"")]
    public void ToCsv_Quotes_Fields_With_Special_Characters(string title, string expected)
    {
        var text = Encoding.UTF8.GetString(ResourceExporter.ToCsv([CreateResource(title: title)]));

        Assert.Contains($"\r\n{expected},Equipment,", text);
    }

    [Theory]
    [InlineData("=SUM(A1:A2)", "'=SUM(A1:A2)")]
    [InlineData("+47 123", "'+47 123")]
    [InlineData("-1", "'-1")]
    [InlineData("@cmd", "'@cmd")]
    [InlineData("\t=cmd", "'\t=cmd")]
    [InlineData("\r=cmd", "\"'\r=cmd\"")]
    [InlineData("a=b", "a=b")]
    public void ToCsv_Neutralizes_Formula_Prefixes(string title, string expected)
    {
        var lines = CsvLines(CreateResource(title: title));

        Assert.StartsWith($"{expected},Equipment,", lines[1]);
    }

    [Fact]
    public void ToCsv_Neutralizes_Formula_In_Description_And_ContactPoint()
    {
        var lines = CsvLines(CreateResource(description: "=1+1", contactPoint: "@evil"));

        Assert.Equal(
            "Snow plow,Equipment,'=1+1,Kristiansand,04.11.2026 08:30,,Available,'@evil",
            lines[1]);
    }

    [Fact]
    public void ToCsv_Applies_Formula_Prefix_Before_Quoting()
    {
        var lines = CsvLines(CreateResource(title: "=HYPERLINK(\"x\",\"y\")"));

        Assert.StartsWith("\"'=HYPERLINK(\"\"x\"\",\"\"y\"\")\",Equipment,", lines[1]);
    }

    [Fact]
    public void ToJson_Exports_Only_The_Agreed_Fields()
    {
        using var json = JsonDocument.Parse(ResourceExporter.ToJson([CreateResource()]));

        var names = json.RootElement[0].EnumerateObject().Select(p => p.Name);

        Assert.Equal(
            ["Title", "Category", "Description", "Region", "AvailableFrom", "AvailableTo", "Status", "ContactPoint"],
            names);
    }

    [Fact]
    public void ToJson_Uses_Iso_Dates_And_Status_Name()
    {
        using var json = JsonDocument.Parse(ResourceExporter.ToJson([CreateResource()]));
        var row = json.RootElement[0];

        Assert.Equal("2026-11-04T08:30:00", row.GetProperty("AvailableFrom").GetString());
        Assert.Equal(JsonValueKind.Null, row.GetProperty("AvailableTo").ValueKind);
        Assert.Equal("Available", row.GetProperty("Status").GetString());
    }

    [Fact]
    public void ToJson_Is_Indented_And_Keeps_Norwegian_Letters_Readable()
    {
        var text = Encoding.UTF8.GetString(ResourceExporter.ToJson([CreateResource(title: "Snøfres på Sørlandet")]));

        Assert.Contains("Snøfres på Sørlandet", text);
        Assert.Contains("\n", text);
    }

    [Fact]
    public void ToJson_Does_Not_Apply_Csv_Formula_Prefix()
    {
        using var json = JsonDocument.Parse(ResourceExporter.ToJson([CreateResource(title: "=1+1")]));

        Assert.Equal("=1+1", json.RootElement[0].GetProperty("Title").GetString());
    }

    [Fact]
    public void ToJson_With_No_Resources_Writes_Empty_Array()
    {
        using var json = JsonDocument.Parse(ResourceExporter.ToJson([]));

        Assert.Equal(0, json.RootElement.GetArrayLength());
    }
}
