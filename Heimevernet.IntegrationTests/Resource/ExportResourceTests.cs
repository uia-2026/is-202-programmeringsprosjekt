using System.Net;
using System.Text.Json;
using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.ResourceTests;

/// <summary>
/// Integration tests verifying authorization, region filtering, and file
/// responses for the Resource CSV and JSON exports (#94).
/// </summary>
public sealed class ExportResourceTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public ExportResourceTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that anonymous users are redirected to the login page.
    /// </summary>
    [Theory]
    [InlineData("/Resource/ExportCsv")]
    [InlineData("/Resource/ExportJson")]
    public async Task Export_AnonymousUser_RedirectsToLogin(string url)
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.OriginalString ?? string.Empty);
    }

    /// <summary>
    /// Verifies that Resource Providers cannot export, matching the print list.
    /// </summary>
    [Theory]
    [InlineData("/Resource/ExportCsv")]
    [InlineData("/Resource/ExportJson")]
    public async Task Export_ResourceProvider_IsDenied(string url)
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(role: UserRole.ResourceProvider);
        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/AccessDenied",
            response.Headers.Location?.OriginalString ?? string.Empty);
    }

    /// <summary>
    /// Verifies that the CSV download has the right headers and contains only
    /// available Resources from the requested region.
    /// </summary>
    [Fact]
    public async Task ExportCsv_WithRegion_ReturnsOnlyAvailableResourcesInRegion()
    {
        var seed = await SeedResourcesAsync();
        using var client = await CreatePublicActorClientAsync();

        using var response = await client.GetAsync($"/Resource/ExportCsv?region={seed.Region}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains(seed.Region, response.Content.Headers.ContentDisposition?.FileNameStar);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains(seed.MatchingTitle, csv);
        Assert.DoesNotContain(seed.BusyTitle, csv);
        Assert.DoesNotContain(seed.OtherRegionTitle, csv);
    }

    /// <summary>
    /// Verifies that the JSON download has the right headers and contains only
    /// available Resources from the requested region.
    /// </summary>
    [Fact]
    public async Task ExportJson_WithRegion_ReturnsOnlyAvailableResourcesInRegion()
    {
        var seed = await SeedResourcesAsync();
        using var client = await CreatePublicActorClientAsync();

        using var response = await client.GetAsync($"/Resource/ExportJson?region={seed.Region}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var row = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(seed.MatchingTitle, row.GetProperty("Title").GetString());
        Assert.Equal("Available", row.GetProperty("Status").GetString());
    }

    /// <summary>
    /// Verifies that exporting without a region includes every region.
    /// </summary>
    [Fact]
    public async Task ExportCsv_WithoutRegion_ReturnsAllRegions()
    {
        var seed = await SeedResourcesAsync();
        using var client = await CreatePublicActorClientAsync();

        using var response = await client.GetAsync("/Resource/ExportCsv");
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Contains(seed.MatchingTitle, csv);
        Assert.Contains(seed.OtherRegionTitle, csv);
        Assert.DoesNotContain(seed.BusyTitle, csv);
    }

    /// <summary>
    /// Verifies that the print page's export links keep the selected region.
    /// </summary>
    [Fact]
    public async Task PrintByRegion_ExportLinks_PreserveSelectedRegion()
    {
        var seed = await SeedResourcesAsync();
        using var client = await CreatePublicActorClientAsync();

        using var response = await client.GetAsync($"/Resource/PrintByRegion?region={seed.Region}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"href=\"/Resource/ExportCsv?region={seed.Region}\"", html);
        Assert.Contains($"href=\"/Resource/ExportJson?region={seed.Region}\"", html);
    }

    private async Task<HttpClient> CreatePublicActorClientAsync()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(role: UserRole.PublicActor);

        return await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);
    }

    private async Task<ExportSeed> SeedResourcesAsync()
    {
        var data = new TestDataFactory(_factory.Services);
        var owner = await data.CreateUserAsync(role: UserRole.ResourceProvider);
        var categoryId = await data.GetCategoryIdAsync();
        var suffix = Guid.NewGuid().ToString("N");

        var seed = new ExportSeed(
            Region: $"Region{suffix}",
            MatchingTitle: $"Matching {suffix}",
            BusyTitle: $"Busy {suffix}",
            OtherRegionTitle: $"Other {suffix}");

        Resource CreateResource(string title, string region, ResourceStatus status) => new()
        {
            UserId = owner.Id,
            CategoryId = categoryId,
            Title = title,
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = region,
            ContactPoint = "Test contact point",
            QuantityOffered = 1,
            Status = status,
            AvailableFrom = DateTime.UtcNow.AddHours(-1)
        };

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Resources.AddRange(
            CreateResource(seed.MatchingTitle, seed.Region, ResourceStatus.Available),
            CreateResource(seed.BusyTitle, seed.Region, ResourceStatus.Busy),
            CreateResource(seed.OtherRegionTitle, $"Other{suffix}", ResourceStatus.Available));

        await db.SaveChangesAsync();

        return seed;
    }

    private sealed record ExportSeed(
        string Region,
        string MatchingTitle,
        string BusyTitle,
        string OtherRegionTitle);
}
