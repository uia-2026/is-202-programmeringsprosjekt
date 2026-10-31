using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Heimevernet.IntegrationTests.NeedTests;

/// <summary>
/// Integration tests verifying Need creation, authorization,
/// model validation, and database persistence.
/// </summary>
public sealed class CreateNeedTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public CreateNeedTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that submitting valid form data creates a new Need entity in the database
    /// with expected property values and redirects the user to the Need details view.
    /// </summary>
    [Fact]
    public async Task Create_ValidInput_CreatesNeedAndRedirectsToDetails()
    {
        // Arrange: Seed user and category, then authenticate client session.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        var categoryId = await data.GetCategoryIdAsync(_factory);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        var title = $"Test need {Guid.NewGuid():N}";

        // Act: Submit valid Need creation form containing anti-forgery tokens.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Create",
            "/Need/Create",
            CreateValidFormData(categoryId, title));

        // Assert HTTP response behavior (302 Redirect to Details view).
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Need/Details",
            response.Headers.Location?.OriginalString ?? string.Empty);

        // Assert database state persistence in isolated DbContext scope.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var need = await db.Needs.SingleOrDefaultAsync(n => n.Title == title);

        Assert.NotNull(need);
        Assert.Equal(user.Id, need.UserId);
        Assert.Equal(categoryId, need.CategoryId);
        Assert.Equal("Test region", need.Region);
        Assert.Equal(NeedPriority.Medium, need.Priority);
        Assert.Equal(25, need.QuantityNeeded);
        Assert.Equal("Test contact point", need.ContactPoint);
    }

    /// <summary>
    /// Verifies that authenticated users with unauthorized roles (e.g. ResourceProvider)
    /// are redirected to the Access Denied route when attempting to access the creation form.
    /// </summary>
    [Fact]
    public async Task Create_ResourceProvider_ReturnsForbidden()
    {
        // Arrange: Seed user with ResourceProvider role and create authenticated client.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(role: UserRole.ResourceProvider);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        // Act: Attempt to access Need creation GET endpoint.
        using var response = await client.GetAsync("/Need/Create");

        // Assert: MVC Cookie Authentication redirects to AccessDenied on forbidden access.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.AbsolutePath);
    }

    /// <summary>
    /// Verifies that unauthenticated anonymous requests attempting to access the creation form
    /// are redirected to the login page.
    /// </summary>
    [Fact]
    public async Task Create_AnonymousUser_RedirectsToLogin()
    {
        // Arrange: Create unauthenticated client with auto-redirect disabled.
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Act: Attempt unauthenticated request to Need creation page.
        using var response = await client.GetAsync("/Need/Create");

        // Assert: Expect 302 Redirect to the Account Login route.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.OriginalString ?? string.Empty);
    }

    /// <summary>
    /// Verifies that submitting form data missing a Title fails model validation,
    /// returns the view with a validation error message, and does not persist a record to the database.
    /// </summary>
    [Fact]
    public async Task Create_MissingTitle_DoesNotCreateNeed()
    {
        // Arrange: Prepare form payload with an empty Title field.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        var categoryId = await data.GetCategoryIdAsync(_factory);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        var formData = CreateValidFormData(categoryId);
        formData["Title"] = "";

        // Act: Submit form with missing Title.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Create",
            "/Need/Create",
            formData);

        // Assert: HTML view re-rendered (200 OK) with validation error.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title is required.", html);

        // Verify database state remains unmodified for this user.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Needs.AnyAsync(n => n.UserId == user.Id));
    }

    /// <summary>
    /// Verifies that submitting form data without selecting a Category fails model validation
    /// and prevents database record creation.
    /// </summary>
    [Fact]
    public async Task Create_MissingCategory_DoesNotCreateNeed()
    {
        // Arrange: Prepare form payload with null category selection.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        var formData = CreateValidFormData(categoryId: null);

        // Act: Submit form without category.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Create",
            "/Need/Create",
            formData);

        // Assert: Verify HTTP response status and HTML validation error text.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Please select a category.", html);

        // Verify database state remains unmodified.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Needs.AnyAsync(
            n => n.UserId == user.Id));
    }

    /// <summary>
    /// Verifies that submitting a QuantityNeeded value outside allowed ranges (e.g. 0)
    /// triggers validation errors and prevents record creation.
    /// </summary>
    [Fact]
    public async Task Create_InvalidQuantity_DoesNotCreateNeed()
    {
        // Arrange: Set invalid quantity (0).
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        var categoryId = await data.GetCategoryIdAsync(_factory);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        var formData = CreateValidFormData(categoryId);
        formData["QuantityNeeded"] = "0";

        // Act: Submit form with invalid quantity.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Create",
            "/Need/Create",
            formData);

        // Assert: HTTP 200 OK with model validation message in HTML output.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Quantity needed must be between 1 and 10000.", html);

        // Verify database state remains unmodified.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Needs.AnyAsync(
            n => n.UserId == user.Id));
    }

    /// <summary>
    /// Verifies that submitting geographic coordinates outside valid boundaries (e.g. Latitude > 90)
    /// triggers validation errors and halts database persistence.
    /// </summary>
    [Fact]
    public async Task Create_InvalidLatitude_DoesNotCreateNeed()
    {
        // Arrange: Set out-of-bounds latitude value (91).
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        var categoryId = await data.GetCategoryIdAsync(_factory);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        var formData = CreateValidFormData(categoryId);
        formData["Latitude"] = "91";

        // Act: Submit form with invalid latitude.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Create",
            "/Need/Create",
            formData);

        // Assert: HTTP 200 OK with coordinate range validation error.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Latitude must be between -90 and 90.", html);

        // Verify database state remains unmodified.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Needs.AnyAsync(
            n => n.UserId == user.Id));
    }


    /// <summary>
    /// Helper method generating a baseline valid form key-value dictionary for Need creation POST requests.
    /// </summary>
    private static Dictionary<string, string> CreateValidFormData(
        int? categoryId,
        string title = "Integration test need")
    {
        return new Dictionary<string, string>
        {
            ["CategoryId"] = categoryId?.ToString() ?? "",
            ["Title"] = title,
            ["Description"] = "Integration test description",
            ["Latitude"] = "0",
            ["Longitude"] = "0",
            ["Region"] = "Test region",
            ["Priority"] = NeedPriority.Medium.ToString(),
            ["ContactPoint"] = "Test contact point",
            ["QuantityNeeded"] = "25"
        };
    }
}