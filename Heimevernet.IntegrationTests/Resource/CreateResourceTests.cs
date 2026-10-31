
using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Heimevernet.IntegrationTests.ResourceTests;

/// <summary>
/// Integration tests verifying Resource creation, authorization,
/// model validation, and database persistence.
/// </summary>
public sealed class CreateResourceTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public CreateResourceTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that authenticated users with an authorized role
    /// can access the Resource creation form.
    /// </summary>
    [Theory]
    [InlineData(UserRole.PublicActor)]
    [InlineData(UserRole.ResourceProvider)]
    public async Task Create_AuthorizedUser_ReturnsOk(UserRole role)
    {
        // Arrange: Create user with an authorized role.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(role: role);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, user.Email);

        // Act: Request the Resource creation form.
        using var response = await client.GetAsync("/Resource/Create");

        // Assert: The creation form is accessible.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Verifies that anonymous users are redirected to the login page
    /// when attempting to access the Resource creation form.
    /// </summary>
    [Fact]
    public async Task Create_AnonymousUser_RedirectsToLogin()
    {
        // Arrange: Create unauthenticated client with automatic redirects disabled.
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Act: Attempt to access the creation form.
        using var response = await client.GetAsync("/Resource/Create");

        // Assert: Expect redirect to the Account Login route.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.OriginalString ?? string.Empty);
    }

    /// <summary>
    /// Verifies that submitting valid form data creates a Resource entity
    /// and redirects the user to its Details page.
    /// </summary>
    [Theory]
    [InlineData(UserRole.PublicActor)]
    [InlineData(UserRole.ResourceProvider)]
    public async Task Create_ValidInput_CreatesResourceAndRedirectsToDetails(
        UserRole role)
    {
        // Arrange: Seed user and category, then authenticate client session.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(role: role);

        var categoryId = await data.GetCategoryIdAsync(_factory);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, user.Email);

        var title = $"Test resource {Guid.NewGuid():N}";

        // Act: Submit valid Resource creation form.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Resource/Create",
            "/Resource/Create",
            CreateValidFormData(categoryId, title));

        // Assert: Expect redirect to Resource Details.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Resource/Details",
            response.Headers.Location?.OriginalString ?? string.Empty);

        // Assert database state persistence.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var resource = await db.Resources.SingleOrDefaultAsync(
            r => r.Title == title);

        Assert.NotNull(resource);
        Assert.Equal(user.Id, resource.UserId);
        Assert.Equal(categoryId, resource.CategoryId);
        Assert.Equal("Test region", resource.Region);
        Assert.Equal("Integration test description", resource.Description);
        Assert.Equal("Test contact point", resource.ContactPoint);
        Assert.Equal(50, resource.QuantityOffered);
    }

    /// <summary>
    /// Verifies that submitting form data without a Title
    /// triggers model validation and prevents database persistence.
    /// </summary>
    [Fact]
    public async Task Create_MissingTitle_DoesNotCreateResource()
    {
        // Arrange: Prepare form payload with an empty Title.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        var categoryId = await data.GetCategoryIdAsync(_factory);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, user.Email);

        var formData = CreateValidFormData(categoryId);
        formData["Title"] = "";

        // Act: Submit form with missing Title.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Resource/Create",
            "/Resource/Create",
            formData);

        // Assert: Form is redisplayed with validation error.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title is required.", html);

        // Verify that no Resource was created for this user.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Resources.AnyAsync(
            r => r.UserId == user.Id));
    }

    /// <summary>
    /// Verifies that submitting form data without selecting a Category
    /// triggers model validation and prevents database persistence.
    /// </summary>
    [Fact]
    public async Task Create_MissingCategory_DoesNotCreateResource()
    {
        // Arrange: Prepare form payload without a category selection.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, user.Email);

        var formData = CreateValidFormData(categoryId: null);

        // Act: Submit form without a category.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Resource/Create",
            "/Resource/Create",
            formData);

        // Assert: Form is redisplayed with validation error.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Please select a category.", html);

        // Verify that no Resource was created for this user.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Resources.AnyAsync(
            r => r.UserId == user.Id));
    }

    /// <summary>
    /// Verifies that submitting a QuantityOffered value outside
    /// the allowed range triggers validation and prevents persistence.
    /// </summary>
    [Fact]
    public async Task Create_InvalidQuantity_DoesNotCreateResource()
    {
        // Arrange: Set invalid quantity (0).
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        var categoryId = await data.GetCategoryIdAsync(_factory);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, user.Email);

        var formData = CreateValidFormData(categoryId);
        formData["QuantityOffered"] = "0";

        // Act: Submit form with invalid quantity.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Resource/Create",
            "/Resource/Create",
            formData);

        // Assert: Form is redisplayed with validation error.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(
            "Quantity offered must be between 1 and 10000.",
            html);

        // Verify that no Resource was created for this user.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Resources.AnyAsync(
            r => r.UserId == user.Id));
    }

    /// <summary>
    /// Verifies that AvailableTo cannot be earlier than AvailableFrom
    /// and that invalid date ranges prevent database persistence.
    /// </summary>
    [Fact]
    public async Task Create_AvailableToBeforeAvailableFrom_DoesNotCreateResource()
    {
        // Arrange: Set AvailableTo earlier than AvailableFrom.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        var categoryId = await data.GetCategoryIdAsync(_factory);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, user.Email);

        var formData = CreateValidFormData(categoryId);
        formData["AvailableFrom"] = "2026-10-30T12:00:00";
        formData["AvailableTo"] = "2026-10-29T12:00:00";

        // Act: Submit form with invalid date range.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Resource/Create",
            "/Resource/Create",
            formData);

        // Assert: Form is redisplayed with validation error.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(
            "Available until cannot be before available from.",
            html);

        // Verify that no Resource was created for this user.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Resources.AnyAsync(
            r => r.UserId == user.Id));
    }

    /// <summary>
    /// Helper method generating a baseline valid form payload
    /// for Resource creation POST requests.
    /// </summary>
    private static Dictionary<string, string> CreateValidFormData(
        int? categoryId,
        string title = "Integration test resource")
    {
        return new Dictionary<string, string>
        {
            ["CategoryId"] = categoryId?.ToString() ?? "",
            ["Title"] = title,
            ["Description"] = "Integration test description",
            ["Latitude"] = "0",
            ["Longitude"] = "0",
            ["Region"] = "Test region",
            ["AvailableFrom"] = DateTime.Now
                .AddHours(-1)
                .ToString("yyyy-MM-dd'T'HH:mm:ss"),
            ["ContactPoint"] = "Test contact point",
            ["QuantityOffered"] = "50"
        };
    }
}
