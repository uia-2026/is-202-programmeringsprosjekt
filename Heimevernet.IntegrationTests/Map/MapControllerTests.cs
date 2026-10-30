extern alias WebApp;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Heimevernet.ViewModels.Map;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApp::Heimevernet.Services;

namespace Heimevernet.IntegrationTests.MapTests;

/// <summary>
/// Integration tests verifying the geospatial map endpoints (<c>/Map</c> view and <c>/Map/Data</c> JSON endpoint).
/// Tests role-based visibility, bounding box query filtering, status exclusions, marker truncation limits, and authorization.
/// </summary>
public sealed class MapControllerTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public MapControllerTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Sample valid geographical bounding box query string covering latitude 58–59 and longitude 7–8 in Southern Norway.
    /// </summary>
    private static string ValidBounds =>
        "?south=58&north=59&west=7&east=8";

    /// <summary>
    /// Verifies that an authenticated user can access the main map index page (<c>GET /Map</c>).
    /// </summary>
    [Fact]
    public async Task Index_ReturnsView()
    {
        // Arrange: Seed user and establish authenticated HTTP client session.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            user.Email);

        // Act: Issue GET request to main map view endpoint.
        using var response = await client.GetAsync("/Map");

        // Assert: Expect HTTP 200 OK.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Verifies that users with the <c>PublicActor</c> role see active Needs and Available Resources within bounds,
    /// but cannot see Resources that are marked with <c>ResourceStatus.Busy</c>.
    /// </summary>
    [Fact]
    public async Task Data_PublicActor_ReturnsNeedsAndAvailableResources()
    {
        // Arrange: Seed PublicActor and ResourceProvider users alongside Needs, Available Resources, and Busy Resources.
        var data = new TestDataFactory(_factory.Services);
        var actor = await data.CreateUserAsync();
        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await data.CreateMapNeedAsync(actor.Id, 58.2, 7.2);
        var availableResourceId = await data.CreateMapResourceAsync(
            provider.Id, 58.3, 7.3);

        await data.CreateMapResourceAsync(
            provider.Id, 58.4, 7.4, ResourceStatus.Busy);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            actor.Email);

        // Act: Fetch JSON map data for target bounding box.
        using var response = await client.GetAsync($"/Map/Data{ValidBounds}");

        // Assert: Expect HTTP 200 OK and verify marker array contains Need and Available Resource, but excludes Busy Resource.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MapDataViewModel>(JsonOptions);

        Assert.NotNull(result);

        Assert.Contains(result.Markers, m =>
            m.Id == needId && m.Kind == MapMarkerKind.Need);

        Assert.Contains(result.Markers, m =>
            m.Id == availableResourceId && m.Kind == MapMarkerKind.Resource);

        Assert.DoesNotContain(result.Markers, m =>
            m.Kind == MapMarkerKind.Resource && m.Status == ResourceStatus.Busy.ToString());
    }

    /// <summary>
    /// Verifies privacy/role rules for <c>ResourceProvider</c>: providers see all Needs, their OWN Resources (regardless of status),
    /// but CANNOT see Resources owned by other providers.
    /// </summary>
    [Fact]
    public async Task Data_ResourceProvider_ReturnsNeedsAndOwnResources()
    {
        // Arrange: Seed PublicActor, target ResourceProvider, and a second ResourceProvider with map resources.
        var data = new TestDataFactory(_factory.Services);
        var actor = await data.CreateUserAsync();
        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);
        var anotherProvider = await data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await data.CreateMapNeedAsync(actor.Id, 58.2, 7.2);

        var ownAvailableResourceId = await data.CreateMapResourceAsync(
            provider.Id, 58.3, 7.3);

        var ownBusyResourceId = await data.CreateMapResourceAsync(
            provider.Id, 58.4, 7.4, ResourceStatus.Busy);

        var anotherProviderResourceId = await data.CreateMapResourceAsync(
            anotherProvider.Id, 58.5, 7.5);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            provider.Email);

        // Act: Request map JSON dataset as the primary ResourceProvider.
        using var response = await client.GetAsync($"/Map/Data{ValidBounds}");

        // Assert: Verify presence of Need and provider's own Resources (Available & Busy), and absence of rival provider Resources.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MapDataViewModel>(JsonOptions);

        Assert.NotNull(result);

        Assert.Contains(result.Markers, m => m.Id == needId);

        Assert.Contains(result.Markers, m => m.Id == ownAvailableResourceId);
        Assert.Contains(result.Markers, m => m.Id == ownBusyResourceId);

        Assert.DoesNotContain(result.Markers, m =>
            m.Id == anotherProviderResourceId);
    }

    /// <summary>
    /// Verifies that completed/fulfilled Needs (<c>NeedStatus.Resolved</c>) are filtered out from map views.
    /// </summary>
    [Fact]
    public async Task Data_ExcludesResolvedNeeds()
    {
        // Arrange: Seed an active Need and a Resolved Need.
        var data = new TestDataFactory(_factory.Services);
        var actor = await data.CreateUserAsync();

        var activeNeedId = await data.CreateMapNeedAsync(
            actor.Id, 58.2, 7.2);

        var resolvedNeedId = await data.CreateMapNeedAsync(
            actor.Id, 58.3, 7.3, NeedStatus.Resolved);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            actor.Email);

        // Act: Request map data JSON payload.
        using var response = await client.GetAsync($"/Map/Data{ValidBounds}");

        // Assert: Expect HTTP 200 OK with active Need included and Resolved Need excluded.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MapDataViewModel>(JsonOptions);

        Assert.NotNull(result);

        Assert.Contains(result.Markers, m => m.Id == activeNeedId);
        Assert.DoesNotContain(result.Markers, m => m.Id == resolvedNeedId);
    }

    /// <summary>
    /// Verifies geospatial query boundary logic: markers strictly within coordinates are returned,
    /// while coordinates outside the bounding box are filtered out.
    /// </summary>
    [Fact]
    public async Task Data_FiltersMarkersOutsideBounds()
    {
        // Arrange: Seed entities inside bounds (lat 58.4–58.5, lon 7.4–7.5) and outside bounds (lat 60.0, lon 10.0).
        var data = new TestDataFactory(_factory.Services);
        var actor = await data.CreateUserAsync();
        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);

        var insideNeedId = await data.CreateMapNeedAsync(
            actor.Id, 58.5, 7.5);

        var outsideNeedId = await data.CreateMapNeedAsync(
            actor.Id, 60.0, 10.0);

        var insideResourceId = await data.CreateMapResourceAsync(
            provider.Id, 58.4, 7.4);

        var outsideResourceId = await data.CreateMapResourceAsync(
            provider.Id, 60.0, 10.0);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            actor.Email);

        // Act: Query map data endpoint using valid bounding box parameters.
        using var response = await client.GetAsync($"/Map/Data{ValidBounds}");

        // Assert: Ensure markers outside bounding box coordinates are excluded.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MapDataViewModel>(JsonOptions);

        Assert.NotNull(result);

        Assert.Contains(result.Markers, m => m.Id == insideNeedId);
        Assert.DoesNotContain(result.Markers, m => m.Id == outsideNeedId);

        Assert.Contains(result.Markers, m => m.Id == insideResourceId);
        Assert.DoesNotContain(result.Markers, m => m.Id == outsideResourceId);
    }

    /// <summary>
    /// Verifies model binding and input validation for map bounds query parameters.
    /// Invalid types, out-of-range coordinates, inverted min/max, or missing fields return HTTP 400 Bad Request.
    /// </summary>
    /// <param name="query">The malformed or invalid query string parameters being tested.</param>
    [Theory]
    [InlineData("?south=invalid&north=59&west=7&east=8")]
    [InlineData("?south=58&north=91&west=7&east=8")]
    [InlineData("?south=-91&north=59&west=7&east=8")]
    [InlineData("?south=58&north=59&west=-181&east=8")]
    [InlineData("?south=58&north=59&west=7&east=181")]
    [InlineData("?south=59&north=58&west=7&east=8")]
    [InlineData("?north=59&west=7&east=8")]
    public async Task Data_InvalidBounds_ReturnsBadRequest(string query)
    {
        // Arrange: Authenticate user session.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            user.Email);

        // Act: Send map data query with invalid parameters.
        using var response = await client.GetAsync($"/Map/Data{query}");

        // Assert: Expect HTTP 400 Bad Request.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies authentication gatekeeper: unauthenticated anonymous requests
    /// attempting to fetch map JSON data are redirected to the Login endpoint.
    /// </summary>
    [Fact]
    public async Task Data_AnonymousUser_RedirectsToLogin()
    {
        // Arrange: Create unauthenticated client without auto-redirects.
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Act: Attempt anonymous request to map data endpoint.
        using var response = await client.GetAsync($"/Map/Data{ValidBounds}");

        // Assert: Expect 302 Redirect to Account Login route.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.OriginalString ?? string.Empty);
    }

    /// <summary>
    /// Verifies marker cap protection: when marker count within bounds exceeds the maximum threshold (500),
    /// the endpoint sets <c>Truncated = true</c> and caps returned markers to exactly 500.
    /// Uses bulk entity creation to optimize test execution speed.
    /// </summary>
    [Fact]
    public async Task Data_ExceedsMarkerLimit_ReturnsTruncated()
    {
        // Arrange: Seed user and bulk-insert 501 Need records inside map bounds.
        var data = new TestDataFactory(_factory.Services);
        var actor = await data.CreateUserAsync();

        await BulkCreateMapNeedsAsync(actor.Id, count: 501, latitude: 58.2, longitude: 7.2);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            actor.Email);

        // Act: Fetch map data payload.
        using var response = await client.GetAsync($"/Map/Data{ValidBounds}");

        // Assert: Verify Truncated flag is set to true and exactly 500 markers are returned.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MapDataViewModel>(JsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Truncated);

        Assert.Equal(
            500,
            result.Markers.Count(m => m.Kind == MapMarkerKind.Need));
    }

    /// <summary>
    /// Verifies that when marker counts remain within the threshold (e.g. 2 markers),
    /// the endpoint returns <c>Truncated = false</c>.
    /// </summary>
    [Fact]
    public async Task Data_WithinMarkerLimit_ReturnsNotTruncated()
    {
        // Arrange: Seed 2 Need records inside bounds.
        var data = new TestDataFactory(_factory.Services);
        var actor = await data.CreateUserAsync();

        await data.CreateMapNeedAsync(actor.Id, 58.2, 7.2);
        await data.CreateMapNeedAsync(actor.Id, 58.3, 7.3);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            actor.Email);

        // Act: Fetch map data payload.
        using var response = await client.GetAsync($"/Map/Data{ValidBounds}");

        // Assert: Expect HTTP 200 OK and Truncated = false.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MapDataViewModel>(JsonOptions);

        Assert.NotNull(result);
        Assert.False(result.Truncated);
    }

    /// <summary>
    /// Helper method performing a single batch database transaction insert for large volume test fixtures.
    /// Avoids hundreds of individual database round-trips during integration test runs.
    /// </summary>
    private async Task BulkCreateMapNeedsAsync(int userId, int count, double latitude, double longitude)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = db.Categories.Select(c => c.Id).First();

        var needs = Enumerable.Range(1, count).Select(i => new Need
        {
            UserId = userId,
            CategoryId = categoryId,
            Title = $"Bulk Need {i} {Guid.NewGuid():N}",
            Description = "Bulk integration test description",
            Latitude = latitude,
            Longitude = longitude,
            Region = "Agder",
            Priority = NeedPriority.Medium,
            ContactPoint = "Test contact",
            QuantityNeeded = 1,
            Status = NeedStatus.New
        }).ToList();

        db.Needs.AddRange(needs);
        await db.SaveChangesAsync();
    }
}